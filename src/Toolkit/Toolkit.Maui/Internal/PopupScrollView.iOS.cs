// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  * Licensed under the Apache License, Version 2.0 (the "License");
//  * you may not use this file except in compliance with the License.
//  * You may obtain a copy of the License at
//  *
//  * http://www.apache.org/licenses/LICENSE-2.0
//  *
//  * Unless required by applicable law or agreed to in writing, software
//  * distributed under the License is distributed on an "AS IS" BASIS,
//  * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  * See the License for the specific language governing permissions and
//  * limitations under the License.
//  ******************************************************************************/

#if __IOS__
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal sealed class PopupScrollView : ScrollView
{
}

internal sealed class PopupScrollViewHandler : ScrollViewHandler
{
    protected override UIScrollView CreatePlatformView() => new NativePopupScrollView();
}

internal sealed class NativePopupScrollView : MauiScrollView
{
    private NSObject? _accessibilityFocusObserver;

    internal NativePopupScrollView()
    {
        ShouldGroupAccessibilityChildren = true;
    }

    internal static NativePopupScrollView? FindParent(UIView view)
    {
        for (var parent = view.Superview; parent is not null; parent = parent.Superview)
        {
            if (parent is NativePopupScrollView scrollView)
                return scrollView;
        }
        return null;
    }

    internal static bool ForwardVerticalScroll(UIView view, UIAccessibilityScrollDirection direction) =>
        direction is UIAccessibilityScrollDirection.Up or UIAccessibilityScrollDirection.Down &&
        FindParent(view)?.AccessibilityScroll(direction) == true;

    internal bool CanScroll(UIAccessibilityScrollDirection direction)
    {
        if (Window is null || Hidden || !ScrollEnabled)
            return false;

        var inset = AdjustedContentInset;
        if (Bounds.Height <= inset.Top + inset.Bottom)
            return false;

        double tolerance = 0.5 / Window.Screen.Scale;
        return direction switch
        {
            UIAccessibilityScrollDirection.Up => ContentOffset.Y > -inset.Top + tolerance,
            UIAccessibilityScrollDirection.Down => ContentOffset.Y < ContentSize.Height - Bounds.Height + inset.Bottom - tolerance,
            _ => false,
        };
    }

    public override bool AccessibilityScroll(UIAccessibilityScrollDirection direction)
    {
        if (direction is not (UIAccessibilityScrollDirection.Up or UIAccessibilityScrollDirection.Down))
            return base.AccessibilityScroll(direction);
        if (!CanScroll(direction))
            return false;

        var inset = AdjustedContentInset;
        double pageHeight = Bounds.Height - inset.Top - inset.Bottom;
        if (pageHeight <= 0)
            return false;

        double minimum = -inset.Top;
        double maximum = Math.Max(minimum, ContentSize.Height - Bounds.Height + inset.Bottom);
        double offset = Math.Clamp(ContentOffset.Y +
            (direction == UIAccessibilityScrollDirection.Down ? pageHeight : -pageHeight), minimum, maximum);
        // UIKit rounds offsets to pixels, including fractional content extents at the bottom.
        if (Math.Abs(offset - ContentOffset.Y) < 0.5 / Window.Screen.Scale)
            return false;

        SetContentOffset(new CGPoint(ContentOffset.X, offset), false);
        UIAccessibility.PostNotification(UIAccessibilityPostNotification.PageScrolled, null);
        return true;
    }

    public override void MovedToWindow()
    {
        base.MovedToWindow();
        DisconnectAccessibilityFocusObserver();
        if (Window is not null)
        {
            _accessibilityFocusObserver = NSNotificationCenter.DefaultCenter.AddObserver(
                UIView.ElementFocusedNotification, OnAccessibilityElementFocused);
        }
    }

    public override void DidUpdateFocus(UIFocusUpdateContext context, UIFocusAnimationCoordinator coordinator)
    {
        base.DidUpdateFocus(context, coordinator);
        if (context.NextFocusedItem is NSObject target)
            RevealFocusedElement(target);
    }

    private void OnAccessibilityElementFocused(NSNotification notification)
    {
        if (notification.UserInfo?[UIView.FocusedElementKey] is NSObject target)
            RevealFocusedElement(target);
    }

    private void RevealFocusedElement(NSObject target)
    {
        if (Window is null || Hidden || !ScrollEnabled)
            return;

        var container = target;
        while (container is UIAccessibilityElement element)
            container = element.AccessibilityContainer;

        if (container is not UIView view || view == this || !view.IsDescendantOfView(this))
            return;

        CGRect bounds;
        if (target is UIView targetView)
        {
            bounds = targetView.ConvertRectToView(targetView.Bounds, this);
        }
        else if (target is UIAccessibilityElement accessibilityElement)
        {
            // Inline text links use accessibility elements rather than their own UIView.
            bounds = ConvertRectFromCoordinateSpace(accessibilityElement.AccessibilityFrame, Window.Screen.CoordinateSpace);
        }
        else
            return;

        if (bounds.IsEmpty)
            return;

        bounds = new CGRect(bounds.X - 4, bounds.Y - 4, bounds.Width + 8, bounds.Height + 8);
        if (Bounds.Contains(bounds))
            return;

        // Avoid animating: the focus ring must not move ahead of the scrolled content.
        ScrollRectToVisible(bounds, false);
    }

    private void DisconnectAccessibilityFocusObserver()
    {
        if (_accessibilityFocusObserver is not null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_accessibilityFocusObserver);
            _accessibilityFocusObserver.Dispose();
            _accessibilityFocusObserver = null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            DisconnectAccessibilityFocusObserver();
        base.Dispose(disposing);
    }
}

internal sealed class PopupScrollActions : IDisposable
{
    private UIAccessibilityCustomAction? _up;
    private UIAccessibilityCustomAction? _down;

    internal UIAccessibilityCustomAction[]? GetActions(UIView view, UIAccessibilityCustomAction[]? existing)
    {
        if (NativePopupScrollView.FindParent(view) is not NativePopupScrollView scrollView)
            return existing;

        var actions = new List<UIAccessibilityCustomAction>(existing ?? Array.Empty<UIAccessibilityCustomAction>());
        if (scrollView.CanScroll(UIAccessibilityScrollDirection.Up))
        {
            _up ??= CreateAction(view, "PopupViewerScrollUp", UIAccessibilityScrollDirection.Up);
            actions.Add(_up);
        }
        if (scrollView.CanScroll(UIAccessibilityScrollDirection.Down))
        {
            _down ??= CreateAction(view, "PopupViewerScrollDown", UIAccessibilityScrollDirection.Down);
            actions.Add(_down);
        }
        return actions.ToArray();
    }

    private static UIAccessibilityCustomAction CreateAction(UIView view, string resourceKey, UIAccessibilityScrollDirection direction)
    {
        var target = new WeakReference<UIView>(view);
        UIAccessibilityCustomActionHandler activate = _ =>
            target.TryGetTarget(out var current) && NativePopupScrollView.ForwardVerticalScroll(current, direction);
        return new UIAccessibilityCustomAction(Properties.Resources.GetString(resourceKey), activate);
    }

    public void Dispose()
    {
        _up?.Dispose();
        _down?.Dispose();
        _up = null;
        _down = null;
    }
}
#endif
