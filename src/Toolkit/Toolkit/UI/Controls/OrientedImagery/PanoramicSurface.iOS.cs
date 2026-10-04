// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

#if __IOS__
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using CoreAnimation;
using CoreGraphics;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Foundation;
using Metal;
using MetalKit;
using UIKit;
using static Esri.ArcGISRuntime.Toolkit.UI.Controls.PanoramaCameraState;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives;

// The panoramic display's surface on iOS and Mac Catalyst: an MTKView that draws on demand. The display decodes
// panoramas into textures off the UI thread.
internal sealed class PanoramicSurface : MTKView
{
    // Kept out of the raw string, because in excluded code a line starting with # still reads as a directive.
    private const string ShaderSource = "#include <metal_stdlib>\n" + """
        using namespace metal;

        struct SphereVertex { packed_float3 position; packed_float2 texCoord; };
        struct MarkerVertex { float2 position; float2 texCoord; };
        struct VertexOut { float4 position [[position]]; float2 texCoord; };

        vertex VertexOut sphere_vertex(uint id [[vertex_id]], const device SphereVertex* vertices [[buffer(0)]], constant float4x4& worldViewProjection [[buffer(1)]])
        {
            VertexOut out;
            out.position = worldViewProjection * float4(float3(vertices[id].position), 1.0);
            out.texCoord = float2(vertices[id].texCoord);
            return out;
        }

        // Markers are projected to clip space on the CPU, so this is a pass-through.
        vertex VertexOut marker_vertex(uint id [[vertex_id]], constant MarkerVertex* vertices [[buffer(0)]])
        {
            VertexOut out;
            out.position = float4(vertices[id].position, 0.0, 1.0);
            out.texCoord = vertices[id].texCoord;
            return out;
        }

        fragment float4 texture_fragment(VertexOut in [[stage_in]], texture2d<float> image [[texture(0)]], sampler imageSampler [[sampler(0)]])
        {
            return image.sample(imageSampler, in.texCoord);
        }
        """;

    // Compiled once per process, off the UI thread. The first compile after an install can take a second.
    private static readonly Lazy<Task<Pipeline>> s_pipeline = new(() => Task.Run(() => new Pipeline()));

    private Pipeline? _pipeline;
    private Exception? _pipelineError;
    private IMTLTexture? _texture;
    private List<(IMTLTexture Texture, PanoramaMarker Marker)> _markers = [];
    private CGSize _lastSize;

    private float _yaw;
    private float _pitch;
    private float _fieldOfView = MathF.PI / 2f;

    // Pans apply deltas from the last update. A change in touch count or a pinch re-anchors them.
    private CGPoint _lastPanTranslation;
    private nint _lastPanTouches;
    private bool _pinching;
    private nfloat _lastWheelTranslation;

    // Held navigation keys by key code, so a key-up clears what its key-down set. A display link steps the camera while
    // any is held.
    private readonly Dictionary<UIKeyboardHidUsage, NavigationKeys> _heldKeys = [];
    private CADisplayLink? _keyboardLink;
    private double _keyboardTimestamp;

    private NSObject? _foregroundObserver;
    private readonly Action<IMTLCommandBuffer> _onFrameCompleted;

    public PanoramicSurface()
        : base(CGRect.Empty, MTLDevice.SystemDefault)
    {
        ColorPixelFormat = MTLPixelFormat.BGRA8Unorm;
        FramebufferOnly = true;
        Paused = true;
        EnableSetNeedsDisplay = true;
        ClearColor = new MTLClearColor(0.02, 0.02, 0.02, 1);
        IsAccessibilityElement = true;
        SyncContentScale();

        if (Device is null)
            _pipelineError = new NotSupportedException("Metal is not available on this device.");

        _onFrameCompleted = Weakly<IMTLCommandBuffer>(static (surface, buffer) => surface.OnFrameCompleted(buffer));

        AddGestureRecognizer(new UITapGestureRecognizer(Weakly<UITapGestureRecognizer>(static (surface, recognizer) => surface.OnTap(recognizer))));
        AddGestureRecognizer(new UIPanGestureRecognizer(Weakly<UIPanGestureRecognizer>(static (surface, recognizer) => surface.OnPan(recognizer)))
        {
            MinimumNumberOfTouches = 1,
            MaximumNumberOfTouches = 2,
            AllowedScrollTypesMask = UIScrollTypeMask.Continuous, // a trackpad's two-finger scroll looks around too
        });
        AddGestureRecognizer(new UIPinchGestureRecognizer(Weakly<UIPinchGestureRecognizer>(static (surface, recognizer) => surface.OnPinch(recognizer)))
        {
            // Recognized with the pan, so a second finger turns a drag into a pinch.
            ShouldRecognizeSimultaneously = static (pinch, other) => other.View == pinch.View && other is UIPanGestureRecognizer,
        });
        AddGestureRecognizer(new UIPanGestureRecognizer(Weakly<UIPanGestureRecognizer>(static (surface, recognizer) => surface.OnWheel(recognizer)))
        {
            AllowedScrollTypesMask = UIScrollTypeMask.Discrete, // a mouse wheel
            AllowedTouchTypes = [],
        });

        // Its own focus group keeps the arrow keys from moving focus to a neighbor.
        FocusGroupIdentifier = $"{nameof(PanoramicSurface)}-{GetHashCode()}";

        WaitForPipeline();
    }

    // The same events as the Windows and Android surfaces, so the display code is shared.
    public event Action<double, double>? SurfaceTapped;

    public event Action<Exception>? RenderFailed;

    // Metal objects survive backgrounding and removal from the window, so these are never raised.
    public event Action? DeviceLost
    {
        add { }
        remove { }
    }

    public event Action? DeviceRecreated
    {
        add { }
        remove { }
    }

    public event Action? ViewChanged;

    // Raised when the drawable's pixels per point change, so the display can rasterize its markers again.
    public event Action? ScaleChanged;

    public float Yaw
    {
        get => _yaw;
        set => SetCamera(ref _yaw, value);
    }

    public float Pitch
    {
        get => _pitch;
        set => SetCamera(ref _pitch, value);
    }

    public float FieldOfView
    {
        get => _fieldOfView;
        set => SetCamera(ref _fieldOfView, value);
    }

    // In points, like tap positions.
    public double ActualWidth => Bounds.Width;

    public double ActualHeight => Bounds.Height;

    // The decode awaits this, so an image is presented only once it can be drawn.
    internal static Task<Pipeline> GetPipelineAsync() => s_pipeline.Value;

    private void SetCamera(ref float field, float value)
    {
        if (field == value)
            return;

        field = value;
        ViewChanged?.Invoke();
    }

    // Takes ownership of the texture.
    public void SetTexture(IMTLTexture texture)
    {
        _texture?.Dispose();
        _texture = texture;
        RequestRender();
    }

    public void ClearTexture()
    {
        _texture?.Dispose();
        _texture = null;
        RequestRender();
    }

    // Runs in a dispatcher callback, so it must not throw.
    public unsafe void SetMarkers(IReadOnlyList<PanoramaMarker> markers)
    {
        ReleaseMarkers();
        if (Device is IMTLDevice device)
        {
            var uploaded = new List<(IMTLTexture Texture, PanoramaMarker Marker)>(markers.Count);
            foreach (PanoramaMarker marker in markers)
            {
                // A marker that can't be uploaded is skipped. It never fails the panorama.
                if (!marker.IsValid)
                    continue;

                try
                {
                    fixed (byte* pixels = marker.Bgra)
                    {
                        IMTLTexture texture = CreateTexture(device, (IntPtr)pixels, marker.Width, marker.Height, marker.Width * 4);
                        uploaded.Add((texture, marker));
                    }
                }
                catch (Exception)
                {
                    // Skipped, like an invalid swatch.
                }
            }

            _markers = uploaded;
        }

        RequestRender();
    }

    public void SetClearColor(float r, float g, float b, float a) => ClearColor = new MTLClearColor(r, g, b, a);

    // UIKit coalesces requests into one draw per display pass.
    public void RequestRender() => SetNeedsDisplay();

    // Releases the textures, the display link, and the observer when the handler disconnects. UIKit can still call into
    // the view during teardown, so the view itself stays.
    internal void ReleaseResources()
    {
        _texture?.Dispose();
        _texture = null;
        ReleaseMarkers();
        StopKeyboardNavigation();
        StopObservingForeground();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ReleaseResources();

        base.Dispose(disposing);
    }

    // Creates a BGRA8 texture with a CPU copy, so it runs on any thread without a GPU command. Mac GPUs without unified
    // memory have no shared textures, so the storage mode is the default.
    internal static IMTLTexture CreateTexture(IMTLDevice device, IntPtr bgra, int width, int height, int bytesPerRow)
    {
        MTLTextureDescriptor descriptor = MTLTextureDescriptor.CreateTexture2DDescriptor(MTLPixelFormat.BGRA8Unorm, (nuint)width, (nuint)height, false);
        descriptor.Usage = MTLTextureUsage.ShaderRead;
        IMTLTexture texture = device.CreateTexture(descriptor) ?? throw new InvalidOperationException($"Unable to create a {width}x{height} texture.");
        texture.ReplaceRegion(MTLRegion.Create2D(0, 0, (nuint)width, (nuint)height), 0, bgra, (nuint)bytesPerRow);
        return texture;
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        SyncContentScale();

        // The footprint's horizontal field of view follows the aspect ratio.
        if (Bounds.Size != _lastSize)
        {
            _lastSize = Bounds.Size;
            ViewChanged?.Invoke();
            RequestRender();
        }
    }

    // The display rasterizes markers at the main display's density, so the drawable uses the same pixels per point.
    private void SyncContentScale()
    {
        var scale = (nfloat)Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo.Density;
        if (scale > 0 && ContentScaleFactor != scale)
        {
            ContentScaleFactor = scale;
            ScaleChanged?.Invoke();
        }
    }

    private async void WaitForPipeline()
    {
        if (_pipelineError is not null)
            return;

        try
        {
            _pipeline = await GetPipelineAsync();
        }
        catch (Exception ex)
        {
            _pipelineError = ex;
        }

        RequestRender();
    }

    public override void Draw(CGRect rect)
    {
        // A backgrounded app may not use the GPU, but UIKit can still ask for a draw for its snapshot. The foreground
        // draw replaces it.
        if (UIApplication.SharedApplication.ApplicationState == UIApplicationState.Background)
            return;

        if (_pipelineError is not null)
        {
            ReportRenderFailure(_pipelineError);
            return;
        }

        // Without a pipeline or a size, the next request draws.
        if (_pipeline is not Pipeline pipeline || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        // Released right after the commit, so the drawable goes back to the layer's pool without waiting for the GC.
        using MTLRenderPassDescriptor? pass = CurrentRenderPassDescriptor;
        using ICAMetalDrawable? drawable = CurrentDrawable;
        if (pass is null || drawable is null)
            return;

        try
        {
            using IMTLCommandBuffer buffer = pipeline.Queue.CommandBuffer() ?? throw new InvalidOperationException("Unable to create a Metal command buffer.");
            using (IMTLRenderCommandEncoder encoder = buffer.CreateRenderCommandEncoder(pass))
            {
                try
                {
                    // Without a panorama, the pass only clears.
                    if (_texture is IMTLTexture texture)
                        DrawScene(encoder, pipeline, texture, DrawableSize);
                }
                finally
                {
                    encoder.EndEncoding();
                }
            }

            buffer.PresentDrawable(drawable);
            buffer.AddCompletedHandler(_onFrameCompleted);
            buffer.Commit();
        }
        catch (Exception ex)
        {
            ReportRenderFailure(ex);
        }
    }

    private unsafe void DrawScene(IMTLRenderCommandEncoder encoder, Pipeline pipeline, IMTLTexture texture, CGSize size)
    {
        var camera = new PanoramaCameraState(_yaw, _pitch, _fieldOfView);
        Matrix4x4 worldViewProjection = camera.GetWorldViewProjection((float)(size.Width / size.Height));

        // Row-major System.Numerics bytes read as a column-major float4x4 give the transpose the math needs, so the
        // matrix goes up as is.
        encoder.SetRenderPipelineState(pipeline.Sphere);
        encoder.SetCullMode(MTLCullMode.None); // inside-facing sphere
        encoder.SetVertexBuffer(pipeline.SphereVertices, 0, 0);
        encoder.SetVertexBytes((IntPtr)(&worldViewProjection), (nuint)sizeof(Matrix4x4), 1);
        encoder.SetFragmentTexture(texture, 0);
        encoder.SetFragmentSamplerState(pipeline.SphereSampler, 0);
        encoder.DrawIndexedPrimitives(MTLPrimitiveType.Triangle, (nuint)pipeline.SphereIndexCount, MTLIndexType.UInt16, pipeline.SphereIndices, 0);

        DrawMarkers(encoder, pipeline, camera, size);
    }

    // Screen-aligned quads sized to each swatch and shifted by its offset. Markers behind the camera are skipped.
    private unsafe void DrawMarkers(IMTLRenderCommandEncoder encoder, Pipeline pipeline, PanoramaCameraState camera, CGSize size)
    {
        if (_markers.Count == 0)
            return;

        encoder.SetRenderPipelineState(pipeline.Marker);
        encoder.SetFragmentSamplerState(pipeline.MarkerSampler, 0);
        float* quad = stackalloc float[16];
        foreach ((IMTLTexture texture, PanoramaMarker marker) in _markers)
        {
            if (!camera.TryGetMarkerQuad(marker, size.Width, size.Height, out var edges))
                continue;

            // A triangle strip: top-left, bottom-left, top-right, and bottom-right, each as x, y, u, v.
            SetQuadVertex(quad, 0, edges.Left, edges.Top, 0f, 0f);
            SetQuadVertex(quad, 1, edges.Left, edges.Bottom, 0f, 1f);
            SetQuadVertex(quad, 2, edges.Right, edges.Top, 1f, 0f);
            SetQuadVertex(quad, 3, edges.Right, edges.Bottom, 1f, 1f);
            encoder.SetVertexBytes((IntPtr)quad, 16 * sizeof(float), 0);
            encoder.SetFragmentTexture(texture, 0);
            encoder.DrawPrimitives(MTLPrimitiveType.TriangleStrip, 0, 4);
        }

        static unsafe void SetQuadVertex(float* quad, int index, float x, float y, float u, float v)
        {
            quad[index * 4] = x;
            quad[(index * 4) + 1] = y;
            quad[(index * 4) + 2] = u;
            quad[(index * 4) + 3] = v;
        }
    }

    // Runs on a Metal thread. A frame discarded because the app went to the background isn't a failure.
    private void OnFrameCompleted(IMTLCommandBuffer buffer)
    {
        if (buffer.Status == MTLCommandBufferStatus.Error && buffer.Error is NSError error && error.Code != (nint)(long)MTLCommandBufferError.NotPermitted)
            ReportRenderFailure(new InvalidOperationException($"Metal command buffer failed: {error.LocalizedDescription}"));
    }

    public override void MovedToWindow()
    {
        base.MovedToWindow();
        if (Window is null)
        {
            // Off the window, key releases may never arrive and no foreground draw is needed.
            StopKeyboardNavigation();
            StopObservingForeground();
            return;
        }

        if (_foregroundObserver is null)
        {
            Action<NSNotificationEventArgs> onForeground = Weakly<NSNotificationEventArgs>(static (surface, _) => surface.RequestRender());
            _foregroundObserver = UIApplication.Notifications.ObserveWillEnterForeground((_, args) => onForeground(args));
        }

        RequestRender();
    }

    private void StopObservingForeground()
    {
        _foregroundObserver?.Dispose();
        _foregroundObserver = null;
    }

    // The display reports failures as Error. Raised on the UI thread after the draw returns.
    private void ReportRenderFailure(Exception ex) =>
        BeginInvokeOnMainThread(() => RenderFailed?.Invoke(ex));

    private void ReleaseMarkers()
    {
        foreach ((IMTLTexture texture, _) in _markers)
            texture.Dispose();

        _markers = [];
    }

    #region Input (UI thread)

    private void OnTap(UITapGestureRecognizer recognizer)
    {
        if (recognizer.State != UIGestureRecognizerState.Ended)
            return;

        BecomeFirstResponder();
        CGPoint position = recognizer.LocationInView(this);
        SurfaceTapped?.Invoke(position.X, position.Y);
    }

    // The grabbed point follows the finger, as on the other heads. Ignored during a pinch.
    private void OnPan(UIPanGestureRecognizer recognizer)
    {
        CGPoint translation = recognizer.TranslationInView(this);
        nint touches = recognizer.NumberOfTouches;
        if (recognizer.State == UIGestureRecognizerState.Began)
        {
            // A drag takes keyboard focus like a tap. A trackpad scroll has no touches and doesn't.
            if (touches > 0)
                BecomeFirstResponder();

            _lastPanTranslation = CGPoint.Empty;
            _lastPanTouches = touches;
        }

        if ((recognizer.State is UIGestureRecognizerState.Began or UIGestureRecognizerState.Changed) && touches == _lastPanTouches && !_pinching)
        {
            float scale = DragRotationScale(FieldOfView, Bounds.Height);
            Yaw -= (float)(translation.X - _lastPanTranslation.X) * scale;
            Pitch = Math.Clamp(Pitch - ((float)(translation.Y - _lastPanTranslation.Y) * scale), MinPitch, MaxPitch);
            RequestRender();
        }

        _lastPanTranslation = translation;
        _lastPanTouches = touches;
    }

    private void OnPinch(UIPinchGestureRecognizer recognizer)
    {
        if (recognizer.State is UIGestureRecognizerState.Began or UIGestureRecognizerState.Changed)
        {
            // Spreading the fingers zooms in. Scale accumulates over the gesture, so it resets after each update.
            _pinching = true;
            FieldOfView = Math.Clamp(FieldOfView / (float)recognizer.Scale, MinFieldOfView, MaxFieldOfView);
            recognizer.Scale = 1;
            RequestRender();
        }
        else
        {
            _pinching = false;
        }
    }

    // Zooms 0.1 rad per wheel event, as on Windows, in the SDK MapView's direction. A notch often arrives as just Began
    // and Ended, so Began counts.
    private void OnWheel(UIPanGestureRecognizer recognizer)
    {
        if (recognizer.State == UIGestureRecognizerState.Began)
            _lastWheelTranslation = 0;

        if (recognizer.State is not (UIGestureRecognizerState.Began or UIGestureRecognizerState.Changed))
            return;

        nfloat translation = recognizer.TranslationInView(this).Y;
        nfloat delta = translation - _lastWheelTranslation;
        _lastWheelTranslation = translation;
        if (delta != 0)
        {
            FieldOfView = Math.Clamp(FieldOfView + (delta > 0 ? -0.1f : 0.1f), MinFieldOfView, MaxFieldOfView);
            RequestRender();
        }
    }

    public override bool CanBecomeFirstResponder => true;

    public override bool CanBecomeFocused => true;

    // Keyboard navigation as in the SDK MapView: held arrows turn the view, and "+" and "-" zoom. Handled presses
    // aren't passed on, since UIKit would cancel them.
    public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (Unhandled(presses, began: true) is NSSet<UIPress> others)
            base.PressesBegan(others, evt);
    }

    public override void PressesChanged(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (Unhandled(presses, began: false, ended: false) is NSSet<UIPress> others)
            base.PressesChanged(others, evt);
    }

    public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (Unhandled(presses, began: false) is NSSet<UIPress> others)
            base.PressesEnded(others, evt);
    }

    public override void PressesCancelled(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (Unhandled(presses, began: false) is NSSet<UIPress> others)
            base.PressesCancelled(others, evt);
    }

    // Key releases after focus leaves never arrive here.
    public override bool ResignFirstResponder()
    {
        StopKeyboardNavigation();
        return base.ResignFirstResponder();
    }

    // Holds or releases navigation keys and returns the other presses for the responder chain, or null if none.
    private NSSet<UIPress>? Unhandled(NSSet<UIPress> presses, bool began, bool ended = true)
    {
        var others = new List<UIPress>();
        foreach (UIPress press in presses)
        {
            bool handled = false;
            if (press.Key is UIKey key)
            {
                if (began && GetNavigationKey(key) is NavigationKeys navigationKey and not NavigationKeys.None)
                {
                    _heldKeys[key.KeyCode] = navigationKey;
                    StartKeyboardNavigation();
                    handled = true;
                }
                else if (!began)
                {
                    handled = ended ? _heldKeys.Remove(key.KeyCode) : _heldKeys.ContainsKey(key.KeyCode);
                }
            }

            if (!handled)
                others.Add(press);
        }

        return others.Count > 0 ? new NSSet<UIPress>(others.ToArray()) : null;
    }

    // As in the SDK MapView, arrows count only without Shift, Control, Option, or Command, and "+" and "-" zoom with
    // any modifier. Apple keyboards flag arrows as numeric-pad keys, so that flag and caps lock are ignored.
    private static NavigationKeys GetNavigationKey(UIKey key)
    {
        const UIKeyModifierFlags Modifiers = UIKeyModifierFlags.Shift | UIKeyModifierFlags.Control | UIKeyModifierFlags.Alternate | UIKeyModifierFlags.Command;
        bool plain = (key.ModifierFlags & Modifiers) == 0;
        return key.KeyCode switch
        {
            UIKeyboardHidUsage.KeyboardLeftArrow when plain => NavigationKeys.Left,
            UIKeyboardHidUsage.KeyboardRightArrow when plain => NavigationKeys.Right,
            UIKeyboardHidUsage.KeyboardUpArrow when plain => NavigationKeys.Up,
            UIKeyboardHidUsage.KeyboardDownArrow when plain => NavigationKeys.Down,
            _ => key.Characters switch
            {
                "+" => NavigationKeys.ZoomIn,
                "-" => NavigationKeys.ZoomOut,
                _ => NavigationKeys.None,
            },
        };
    }

    private void StartKeyboardNavigation()
    {
        if (_keyboardLink is not null)
            return;

        _keyboardTimestamp = 0;
        _keyboardLink = CADisplayLink.Create(Weakly(static surface => surface.StepKeyboardNavigation()));
        _keyboardLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
    }

    private void StopKeyboardNavigation()
    {
        _heldKeys.Clear();
        _keyboardLink?.Invalidate();
        _keyboardLink?.Dispose();
        _keyboardLink = null;
    }

    // One frame of held-key navigation. The first frame only records its time, since a step spans two frames.
    private void StepKeyboardNavigation()
    {
        NavigationKeys keys = NavigationKeys.None;
        foreach (NavigationKeys key in _heldKeys.Values)
            keys |= key;

        if (keys == NavigationKeys.None || _keyboardLink is not CADisplayLink link)
        {
            StopKeyboardNavigation();
            return;
        }

        if (_keyboardTimestamp != 0)
        {
            double seconds = Math.Min(link.Timestamp - _keyboardTimestamp, MaxKeyboardStepSeconds);
            PanoramaCameraState camera = new PanoramaCameraState(Yaw, Pitch, FieldOfView).Navigate(keys, seconds, ActualHeight);
            Yaw = camera.Yaw;
            Pitch = camera.Pitch;
            FieldOfView = camera.FieldOfView;
            RequestRender();
        }

        _keyboardTimestamp = link.Timestamp;
    }

    #endregion

    // Recognizers, observers, and command buffers keep their callbacks alive. A callback holding the view would keep a
    // removed view and its textures alive, so callbacks reach it weakly.
    private Action<T> Weakly<T>(Action<PanoramicSurface, T> action)
    {
        var surface = new WeakReference<PanoramicSurface>(this);
        return argument =>
        {
            if (surface.TryGetTarget(out PanoramicSurface? target))
                action(target, argument);
        };
    }

    private Action Weakly(Action<PanoramicSurface> action)
    {
        Action<object?> weak = Weakly<object?>((surface, _) => action(surface));
        return () => weak(null);
    }

    // The device, its command queue, and the objects every surface draws with.
    internal sealed class Pipeline
    {
        public Pipeline()
        {
            Device = MTLDevice.SystemDefault ?? throw new NotSupportedException("Metal is not available on this device.");
            Queue = Device.CreateCommandQueue() ?? throw new InvalidOperationException("Unable to create a Metal command queue.");
            IMTLLibrary library = Device.CreateLibrary(ShaderSource, new MTLCompileOptions(), out NSError? error)
                ?? throw new InvalidOperationException($"Metal shader compilation failed: {error?.LocalizedDescription}");
            Sphere = CreatePipelineState(library, "sphere_vertex", blend: false);
            Marker = CreatePipelineState(library, "marker_vertex", blend: true);
            SphereSampler = CreateSampler(MTLSamplerAddressMode.Repeat); // wrap u across the seam
            MarkerSampler = CreateSampler(MTLSamplerAddressMode.ClampToEdge);

            (float[] positions, float[] texCoords, short[] indices) = PanoramaCameraState.CreateSphereMesh();
            float[] vertices = new float[positions.Length / 3 * 5];
            for (int i = 0, j = 0; i < positions.Length / 3; i++)
            {
                vertices[j++] = positions[i * 3];
                vertices[j++] = positions[(i * 3) + 1];
                vertices[j++] = positions[(i * 3) + 2];
                vertices[j++] = texCoords[i * 2];
                vertices[j++] = texCoords[(i * 2) + 1];
            }

            SphereVertices = Device.CreateBuffer(vertices, MTLResourceOptions.StorageModeShared) ?? throw new InvalidOperationException("Unable to create the sphere vertices.");
            SphereIndices = Device.CreateBuffer(indices, MTLResourceOptions.StorageModeShared) ?? throw new InvalidOperationException("Unable to create the sphere indices.");
            SphereIndexCount = indices.Length;
        }

        public IMTLDevice Device { get; }

        public IMTLCommandQueue Queue { get; }

        public IMTLRenderPipelineState Sphere { get; }

        public IMTLRenderPipelineState Marker { get; }

        public IMTLSamplerState SphereSampler { get; }

        public IMTLSamplerState MarkerSampler { get; }

        public IMTLBuffer SphereVertices { get; }

        public IMTLBuffer SphereIndices { get; }

        public int SphereIndexCount { get; }

        // The sphere pass is opaque. The marker pass blends the premultiplied swatches over it.
        private IMTLRenderPipelineState CreatePipelineState(IMTLLibrary library, string vertexFunction, bool blend)
        {
            var descriptor = new MTLRenderPipelineDescriptor
            {
                VertexFunction = library.CreateFunction(vertexFunction),
                FragmentFunction = library.CreateFunction("texture_fragment"),
            };
            MTLRenderPipelineColorAttachmentDescriptor attachment = descriptor.ColorAttachments[0];
            attachment.PixelFormat = MTLPixelFormat.BGRA8Unorm;
            if (blend)
            {
                attachment.BlendingEnabled = true;
                attachment.SourceRgbBlendFactor = MTLBlendFactor.One;
                attachment.DestinationRgbBlendFactor = MTLBlendFactor.OneMinusSourceAlpha;
                attachment.SourceAlphaBlendFactor = MTLBlendFactor.One;
                attachment.DestinationAlphaBlendFactor = MTLBlendFactor.OneMinusSourceAlpha;
            }

            return Device.CreateRenderPipelineState(descriptor, out NSError? error)
                ?? throw new InvalidOperationException($"Metal pipeline creation failed: {error?.LocalizedDescription}");
        }

        private IMTLSamplerState CreateSampler(MTLSamplerAddressMode addressU) =>
            Device.CreateSamplerState(new MTLSamplerDescriptor
            {
                MinFilter = MTLSamplerMinMagFilter.Linear,
                MagFilter = MTLSamplerMinMagFilter.Linear,
                SAddressMode = addressU,
                TAddressMode = MTLSamplerAddressMode.ClampToEdge,
            }) ?? throw new InvalidOperationException("Unable to create a Metal sampler.");
    }
}
#endif
