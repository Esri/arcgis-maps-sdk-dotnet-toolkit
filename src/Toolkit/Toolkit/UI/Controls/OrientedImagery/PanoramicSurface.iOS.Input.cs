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
using CoreAnimation;
using CoreGraphics;
using Foundation;
using UIKit;
using static Esri.ArcGISRuntime.Toolkit.UI.Controls.PanoramaCameraState;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives;

internal sealed partial class PanoramicSurface
{
    // A change in touch count or a pinch skips a pan update, so the pan re-anchors there.
    private CGPoint _lastPanTranslation;
    private nint _lastPanTouches;
    private bool _pinching;
    private nfloat _lastWheelTranslation;

    // Keyed by key code, so a key-up clears what its key-down set.
    private readonly Dictionary<UIKeyboardHidUsage, NavigationKeys> _heldKeys = [];
    private CADisplayLink? _keyboardLink;
    private double _keyboardTimestamp;

    private void ConfigureInput()
    {
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
    }

    private void OnTap(UITapGestureRecognizer recognizer)
    {
        if (recognizer.State != UIGestureRecognizerState.Ended)
            return;

        BecomeFirstResponder();
        CGPoint position = recognizer.LocationInView(this);
        SurfaceTapped?.Invoke(position.X, position.Y);
    }

    private void OnPan(UIPanGestureRecognizer recognizer)
    {
        CGPoint translation = recognizer.TranslationInView(this);
        nint touches = recognizer.NumberOfTouches;
        if (recognizer.State == UIGestureRecognizerState.Began)
        {
            // A trackpad scroll has no touches and doesn't take focus.
            if (touches > 0)
                BecomeFirstResponder();

            _lastPanTranslation = CGPoint.Empty;
            _lastPanTouches = touches;
        }

        if ((recognizer.State is UIGestureRecognizerState.Began or UIGestureRecognizerState.Changed) && touches == _lastPanTouches && !_pinching)
        {
            Camera = Camera.Drag((float)(translation.X - _lastPanTranslation.X), (float)(translation.Y - _lastPanTranslation.Y), Bounds.Height);
            RequestRender();
        }

        _lastPanTranslation = translation;
        _lastPanTouches = touches;
    }

    private void OnPinch(UIPinchGestureRecognizer recognizer)
    {
        if (recognizer.State is UIGestureRecognizerState.Began or UIGestureRecognizerState.Changed)
        {
            // Scale accumulates over the gesture, so it resets after each update.
            _pinching = true;
            Camera = Camera.Zoom((float)recognizer.Scale);
            recognizer.Scale = 1;
            RequestRender();
        }
        else
        {
            _pinching = false;
        }
    }

    // One zoom step per event, signed as in the SDK MapView. A notch often arrives as only Began and Ended, so Began
    // counts as an update.
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
            Camera = Camera.ZoomWheel(delta > 0 ? 1f : -1f);
            RequestRender();
        }
    }

    public override bool CanBecomeFirstResponder => true;

    public override bool CanBecomeFocused => true;

    // Handled presses aren't passed on, since UIKit would cancel them.
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

    // The first frame only records its time, since a step spans two frames.
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
            Camera = Camera.Navigate(keys, seconds, ActualHeight);
            RequestRender();
        }

        _keyboardTimestamp = link.Timestamp;
    }
}
#endif
