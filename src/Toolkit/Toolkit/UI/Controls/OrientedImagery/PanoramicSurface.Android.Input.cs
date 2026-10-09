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

#if __ANDROID__
using System;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using static Esri.ArcGISRuntime.Toolkit.UI.Controls.PanoramaCameraState;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives;

// Input, on the UI thread: touch and mouse through the platform gesture detectors, the mouse wheel, hardware-keyboard
// navigation stepped once per frame while keys are held, and whether the focus ring shows.
internal sealed partial class PanoramicSurface : Choreographer.IFrameCallback, ViewTreeObserver.IOnTouchModeChangeListener
{
    private readonly GestureDetector _gestureDetector;
    private readonly ScaleGestureDetector _scaleDetector;

    // Held navigation keys by key code, so a key-up clears exactly what its key-down set. The frame callback steps
    // the camera while any is held; the frame time is 0 until its first frame.
    private readonly Dictionary<Keycode, NavigationKeys> _heldKeys = new();
    private bool _keyboardFramePosted;
    private long _keyboardFrameTime;

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e is null)
            return false;

        // Keyboard input goes to the panorama the user last touched.
        if (e.ActionMasked == MotionEventActions.Down && !IsFocused)
            RequestFocus();

        _scaleDetector.OnTouchEvent(e);
        _gestureDetector.OnTouchEvent(e);
        return true;
    }

    public override PointerIcon? OnResolvePointerIcon(MotionEvent? e, int pointerIndex)
    {
        if (e is null || !e.IsFromSource(InputSourceType.Mouse) || Context is not Context context)
            return base.OnResolvePointerIcon(e, pointerIndex);

        return PointerIcon.GetSystemIcon(context, e.IsButtonPressed(MotionEventButtonState.Primary) ? PointerIconType.Grabbing : PointerIconType.Grab);
    }

    public override bool OnGenericMotionEvent(MotionEvent? e)
    {
        // A mouse wheel, as on the emulator, ChromeOS, or DeX, arrives as a generic scroll event, not a touch. It zooms,
        // as on Windows. Consuming it also stops the fallback that turns an unhandled wheel into a drag.
        if (e?.Action == MotionEventActions.Scroll)
        {
            float notches = e.GetAxisValue(Axis.Vscroll); // positive for wheel up, which zooms in
            if (notches != 0f)
            {
                Camera = Camera.ZoomWheel(notches);
                RequestRender();
                return true;
            }
        }

        return base.OnGenericMotionEvent(e);
    }

    // Hardware-keyboard navigation as in the SDK MapView: held arrows turn the view toward their side, and the keys
    // that type "+" and "-" zoom. The camera steps once per frame while keys are held, so held keys combine.
    public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
    {
        // Android's app quality guidelines have Esc release keyboard focus.
        if (keyCode == Keycode.Escape)
        {
            ClearFocus();
            return true;
        }

        NavigationKeys key = GetNavigationKey(keyCode, e);
        if (key == NavigationKeys.None)
            return base.OnKeyDown(keyCode, e);

        _heldKeys[keyCode] = key;
        if (!_keyboardFramePosted)
        {
            _keyboardFrameTime = 0;
            PostKeyboardFrame();
        }

        return true;
    }

    public override bool OnKeyUp(Keycode keyCode, KeyEvent? e) => _heldKeys.Remove(keyCode) || base.OnKeyUp(keyCode, e);

    protected override void OnFocusChanged(bool gainFocus, FocusSearchDirection direction, Android.Graphics.Rect? previouslyFocusedRect)
    {
        base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);

        // Key-ups after focus leaves never arrive here.
        if (!gainFocus)
            StopKeyboardNavigation();

        UpdateFocusRing();
    }

    // The view keeps focus in touch mode, but the ring is only for keyboard focus.
    void ViewTreeObserver.IOnTouchModeChangeListener.OnTouchModeChanged(bool isInTouchMode) => UpdateFocusRing();

    // A TextureView gets no default focus highlight, so the surface draws its own while it has keyboard focus.
    private void UpdateFocusRing()
    {
        bool show = IsFocused && !IsInTouchMode;
        if (show == _showFocusRing)
            return;

        _showFocusRing = show;
        RequestRender();
    }

    private static (float R, float G, float B) ResolveAccentColor(Context context)
    {
        int argb = unchecked((int)0xFF1A73E8); // Material blue, for a theme without an accent
        using var value = new Android.Util.TypedValue();
        if (context.Theme?.ResolveAttribute(Android.Resource.Attribute.ColorAccent, value, true) == true)
        {
            if (value.Type >= Android.Util.DataType.FirstColorInt && value.Type <= Android.Util.DataType.LastColorInt)
                argb = value.Data;
            else if (value.ResourceId != 0 && context.Resources?.GetColorStateList(value.ResourceId, context.Theme) is { } colors)
                argb = colors.DefaultColor;
        }

        return (((argb >> 16) & 255) / 255f, ((argb >> 8) & 255) / 255f, (argb & 255) / 255f);
    }

    // As in the SDK MapView, arrows count only without modifiers, and the keys that type "+" and "-" zoom with any.
    private static NavigationKeys GetNavigationKey(Keycode keyCode, KeyEvent? e)
    {
        bool plain = e?.HasNoModifiers ?? true;
        return keyCode switch
        {
            Keycode.DpadLeft or Keycode.SystemNavigationLeft when plain => NavigationKeys.Left,
            Keycode.DpadRight or Keycode.SystemNavigationRight when plain => NavigationKeys.Right,
            Keycode.DpadUp or Keycode.SystemNavigationUp when plain => NavigationKeys.Up,
            Keycode.DpadDown or Keycode.SystemNavigationDown when plain => NavigationKeys.Down,
            _ => (char)(e?.UnicodeChar ?? 0) switch
            {
                '+' => NavigationKeys.ZoomIn,
                '-' => NavigationKeys.ZoomOut,
                _ => NavigationKeys.None,
            },
        };
    }

    private void PostKeyboardFrame()
    {
        Choreographer? choreographer = Choreographer.Instance;
        _keyboardFramePosted = choreographer is not null;
        choreographer?.PostFrameCallback(this);
    }

    // Ends held-key navigation, so a detached or disposed view never posts another frame.
    private void StopKeyboardNavigation()
    {
        _heldKeys.Clear();
        if (_keyboardFramePosted)
        {
            Choreographer.Instance?.RemoveFrameCallback(this);
            _keyboardFramePosted = false;
        }
    }

    // One frame of held-key navigation, reposted until every navigation key is released. The first frame only
    // records its time, since a step spans the time between frames.
    public void DoFrame(long frameTimeNanos)
    {
        NavigationKeys keys = NavigationKeys.None;
        foreach (NavigationKeys key in _heldKeys.Values)
            keys |= key;

        if (keys == NavigationKeys.None)
        {
            _keyboardFramePosted = false;
            return;
        }

        if (_keyboardFrameTime != 0)
        {
            double seconds = Math.Min((frameTimeNanos - _keyboardFrameTime) / 1e9, MaxKeyboardStepSeconds);
            Camera = Camera.Navigate(keys, seconds, ActualHeight);
            RequestRender();
        }

        _keyboardFrameTime = frameTimeNanos;
        PostKeyboardFrame();
    }

    // The platform detectors tell a tap from a drag and handle a second pointer joining or leaving.
    private sealed class PanGestureListener : GestureDetector.SimpleOnGestureListener
    {
        private readonly PanoramicSurface _owner;

        public PanGestureListener(PanoramicSurface owner) => _owner = owner;

        public override bool OnDown(MotionEvent e) => true;

        public override bool OnSingleTapUp(MotionEvent e)
        {
            _owner.SurfaceTapped?.Invoke(e.GetX() / _owner.PixelsPerDip, e.GetY() / _owner.PixelsPerDip);
            return true;
        }

        public override bool OnScroll(MotionEvent? e1, MotionEvent e2, float distanceX, float distanceY)
        {
            if (_owner._scaleDetector.IsInProgress)
                return false;

            // distanceX and distanceY are previous minus current, so negating them gives the finger's movement, and
            // the grabbed point follows the finger as on the other heads.
            _owner.Camera = _owner.Camera.Drag(-distanceX, -distanceY, _owner._viewportHeight);
            _owner.RequestRender();
            return true;
        }
    }

    private sealed class PinchListener : ScaleGestureDetector.SimpleOnScaleGestureListener
    {
        private readonly PanoramicSurface _owner;

        public PinchListener(PanoramicSurface owner) => _owner = owner;

        public override bool OnScale(ScaleGestureDetector detector)
        {
            // Spreading the fingers gives a factor above 1 and zooms in, as on the other heads.
            _owner.Camera = _owner.Camera.Zoom(detector.ScaleFactor);
            _owner.RequestRender();
            return true;
        }
    }
}
#endif
