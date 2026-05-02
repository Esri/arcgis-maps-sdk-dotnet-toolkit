using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// 360 Panorama Image Control
    /// </summary>
    public partial class Image360 : Control
    {
        private SwapChainPanel? swapchainPanel;
        private const float MouseRotationScale = 0.0035f;
        private const float KeyboardRotationDelta = MathF.PI / 90.0f;
        private const float MinPitch = -MathF.PI / 2.0f;
        private const float MaxPitch = MathF.PI / 4.0f;
        private const float MinFieldOfView = MathF.PI / 6.0f;
        private const float MaxFieldOfView = 2.0f * MathF.PI / 3.0f;
        private uint m_indexCount;
        private float m_yaw;
        private float m_pitch;
        private float m_fieldOfView = MathF.PI / 2.0f;
        private bool m_needsRender = true;
        private bool m_isInitialized;
        private bool m_renderHooked;
        private Task? m_initializationTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="Image360"/> class.
        /// </summary>
        public Image360()
        {
            DefaultStyleKey = typeof(Image360);
            MarkerLocations = new ObservableCollection<Point>();
            Loaded += ImageViewer3D_Loaded;
            Unloaded += ImageViewer3D_Unloaded;
            SizeChanged += ImageViewer3D_SizeChanged;

            PointerWheelChanged += OnWheelChanged;
            ManipulationDelta += OnManipulationDelta;

            KeyDown += ImageViewer3D_KeyDown;
        }

        /// <inheritdoc />
        ~Image360()
        {
            ReleaseDeviceResources();
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild("Viewport") is SwapChainPanel panel)
            {
                swapchainPanel = panel;
            }
            Start();
        }

        /// <summary>
        /// Gets or sets the source URI for the 360 image. 
        /// The URI can be a local file path, an application package resource (ms-appx), or a web URL (http/https).
        /// </summary>
        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Identifies the Source dependency property for the Image360 control.
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(Uri), typeof(Image360), new PropertyMetadata(null, (s, e) => ((Image360)s).OnSourcePropertyChanged()));

        private void OnSourcePropertyChanged()
        {
            _ = CreateTextureAsync(true);
        }


        private async Task<ImageData?> LoadTextureDataAsync()
        {
            if (string.IsNullOrEmpty(Source?.OriginalString))
                return null;
            IRandomAccessStream? stream = null;
            try
            {
                if (Source.IsFile)
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(Source.ToString());
                    stream = await file.OpenReadAsync();
                }
                else if (Source.Scheme == "ms-appx")
                {
                    StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(Source);
                    stream = await file.OpenReadAsync();
                }
                else if (Source.Scheme == "http" || Source.Scheme == "https")
                {
                    using var httpClient = new System.Net.Http.HttpClient();
                    var response = await httpClient.GetAsync(Source);
                    if (!response.IsSuccessStatusCode)
                    {
                        Trace.WriteLine("Failed to load image from URL: " + Source + " - " + response.ReasonPhrase);
                        return null;
                    }
                    var imageData = await response.Content.ReadAsByteArrayAsync();
                    stream = new InMemoryRandomAccessStream();
                    var writer = new DataWriter(stream);
                    {
                        writer.WriteBytes(imageData);
                        await writer.StoreAsync();
                        stream.Seek(0);
                    }
                }

                if (stream is not null)
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                    PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Ignore,
                        new BitmapTransform(),
                        ExifOrientationMode.IgnoreExifOrientation,
                        ColorManagementMode.DoNotColorManage);

                    return new ImageData(pixelData.DetachPixelData(), decoder.PixelWidth, decoder.PixelHeight);

                }
                return null;
            }
            finally
            {
                stream?.Dispose();
            }
        }


        private async void ImageViewer3D_Loaded(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private async void Start()
        {
            if (swapchainPanel is null)
                return;
            await EnsureInitializedAsync();
            HookRendering();
            RequestRender();
        }


        private void ImageViewer3D_Unloaded(object sender, RoutedEventArgs e)
        {
            UnhookRendering();
            ReleaseDeviceResources();
            m_isInitialized = false;
        }

        private async void ImageViewer3D_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (swapchainPanel?.XamlRoot is null || swapchainPanel.ActualWidth <= 0 || swapchainPanel.ActualHeight <= 0)
            {
                return;
            }

            if (!m_isInitialized)
            {
                await EnsureInitializedAsync();
                return;
            }

            CreateSizeDependentResources();
            RequestRender();
        }

        private void ImageViewer3D_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Left:
                    m_yaw += KeyboardRotationDelta;
                    RequestRender();
                    break;
                case VirtualKey.Right:
                    m_yaw -= KeyboardRotationDelta;
                    RequestRender();
                    break;
                case VirtualKey.Up:
                    m_pitch = Math.Clamp(m_pitch + KeyboardRotationDelta, MinPitch, MaxPitch);
                    RequestRender();
                    break;
                case VirtualKey.Down:
                    m_pitch = Math.Clamp(m_pitch - KeyboardRotationDelta, MinPitch, MaxPitch);
                    RequestRender();
                    break;
                case VirtualKey.Add:
                    m_fieldOfView = Math.Clamp(m_fieldOfView * .9f, MinFieldOfView, MaxFieldOfView);
                    RequestRender();
                    break;
                case VirtualKey.Subtract:
                    m_fieldOfView = Math.Clamp(m_fieldOfView * 1.1f, MinFieldOfView, MaxFieldOfView);
                    RequestRender();
                    break;
            }
        }

        private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var t = e.Delta.Translation;
            float deltaX = (float)(t.X);
            float deltaY = (float)(t.Y);

            m_yaw -= deltaX * MouseRotationScale;
            m_pitch = Math.Clamp(m_pitch - deltaY * MouseRotationScale, MinPitch, MaxPitch);
            var scale = e.Delta.Scale;
            m_fieldOfView = Math.Clamp(m_fieldOfView / scale, MinFieldOfView, MaxFieldOfView);
            RequestRender();
        }

        private void OnWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            PointerPointProperties properties = e.GetCurrentPoint(swapchainPanel).Properties;
            float delta = properties.MouseWheelDelta > 0 ? -0.1f : 0.1f;
            m_fieldOfView = Math.Clamp(m_fieldOfView + delta, MinFieldOfView, MaxFieldOfView);
            RequestRender();
        }

        private void HookRendering()
        {
            if (m_renderHooked)
            {
                return;
            }

            CompositionTarget.Rendering += CompositionTarget_Rendering;
            m_renderHooked = true;
        }

        private void UnhookRendering()
        {
            if (!m_renderHooked)
            {
                return;
            }

            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            m_renderHooked = false;
        }

        private void CompositionTarget_Rendering(object? sender, object e)
        {
            if (!m_needsRender)
            {
                return;
            }

            if (!m_isInitialized)
            {
                _ = EnsureInitializedAsync();
                return;
            }

            Render();
        }

        private async Task EnsureInitializedAsync()
        {
            if (m_isInitialized)
            {
                return;
            }

            if (m_initializationTask is not null)
            {
                await m_initializationTask;
                return;
            }

            if (swapchainPanel?.XamlRoot is null || swapchainPanel.ActualWidth <= 0 || swapchainPanel.ActualHeight <= 0)
            {
                return;
            }

            m_initializationTask = InitializeAsyncCore();

            try
            {
                await m_initializationTask;
                m_isInitialized = true;
            }
            finally
            {
                m_initializationTask = null;
            }
        }

        /// <summary>
        /// Given a screen coordinate, returns the corresponding location on the physical image normalized to 0..1.
        /// </summary>
        /// <param name="point">The screen coordinate.</param>
        /// <returns>The normalized location on the physical image.</returns>
        public Point GetLocation(Point point)
        {
            // Based on screen location, scale and rotation, calculate the corresponding location on physical image normalized to 0..1
            var width = this.ActualWidth;
            var height = this.ActualHeight;
            if (width <= 0 || height <= 0)
            {
                return default;
            }

            var aspect = (float)(width / height);
            var world = Matrix4x4.CreateRotationY(m_yaw) * Matrix4x4.CreateRotationX(m_pitch);
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(m_fieldOfView, aspect, 0.1f, 10.0f);
            var worldViewProjection = world * projection;

            if (!Matrix4x4.Invert(worldViewProjection, out var inverseWorldViewProjection))
            {
                return default;
            }

            var normalizedX = (float)((point.X / width) * 2.0 - 1.0);
            var normalizedY = (float)(1.0 - (point.Y / height) * 2.0);

            var clipPoint = new Vector4(normalizedX, normalizedY, 1f, 1f);
            var localPointH = Vector4.Transform(clipPoint, inverseWorldViewProjection);
            if (Math.Abs(localPointH.W) <= float.Epsilon)
            {
                return default;
            }

            var localPoint = new Vector3(localPointH.X / localPointH.W, localPointH.Y / localPointH.W, localPointH.Z / localPointH.W);
            var rayLocal = Vector3.Normalize(localPoint);

            var clampedY = Math.Clamp(rayLocal.Y, -1f, 1f);
            var phi = MathF.Acos(clampedY);
            var theta = MathF.Atan2(rayLocal.Z, rayLocal.X);
            if (theta < 0f)
            {
                theta += 2f * MathF.PI;
            }

            var meshU = theta / (2f * MathF.PI);
            var v = phi / MathF.PI;

            return new Point(meshU, v);
        }

        /// <summary>
        /// Gets or sets normalized marker locations rendered as red dots on the panorama image.
        /// </summary>
        public IList<Point> MarkerLocations
        {
            get { return (IList<Point>)GetValue(MarkerLocationsProperty); }
            set { SetValue(MarkerLocationsProperty, value); }
        }

        /// <summary>
        /// Identifies the MarkerLocations dependency property for the Image360 control.
        /// </summary>
        public static readonly DependencyProperty MarkerLocationsProperty =
            DependencyProperty.Register(
                nameof(MarkerLocations),
                typeof(IList<Point>),
                typeof(Image360),
                new PropertyMetadata(null, (s, e) => ((Image360)s).OnMarkerLocationsPropertyChanged((IList<Point>?)e.OldValue, (IList<Point>?)e.NewValue)));

        private void OnMarkerLocationsPropertyChanged(IList<Point>? oldValue, IList<Point>? newValue)
        {
            if (oldValue is INotifyCollectionChanged oldIncc)
            {
                oldIncc.CollectionChanged -= MarkerLocations_Changed;
            }

            if (newValue is INotifyCollectionChanged newIncc)
            {
                newIncc.CollectionChanged += MarkerLocations_Changed;
            }

            UpdateMarkers();
        }

        private void MarkerLocations_Changed(object? sender, EventArgs e)
        {
            UpdateMarkers();
        }

        private void UpdateMarkers()
        {
            m_markersDirty = true;
            RequestRender();
        }
    }
}
