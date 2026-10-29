#if ANDROID
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

internal sealed class OrientedImageryPaginationViewHandler : ContentViewHandler
{
    #region Native host

    protected override ContentViewGroup CreatePlatformView() => new PaginationView(Context, (OrientedImageryPaginationView)VirtualView)
    {
        CrossPlatformLayout = VirtualView,
        Focusable = true,
        ImportantForAccessibility = ImportantForAccessibility.Yes,
        DescendantFocusability = DescendantFocusability.BlockDescendants,
    };

    internal void UpdateAccessibility() => PlatformView.SendAccessibilityEvent(EventTypes.WindowContentChanged);

    #endregion

    private sealed class PaginationView(Context context, OrientedImageryPaginationView owner) : ContentViewGroup(context)
    {
        #region Single focus stop with tappable children

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);
            // Hide the content's accessibility subtree, not its touch targets or this host.
            if (GetChildAt(0) is { } content)
                content.ImportantForAccessibility = ImportantForAccessibility.NoHideDescendants;
        }

        #endregion

        #region Hardware arrow keys

        public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
        {
            if (keyCode is Keycode.DpadLeft or Keycode.DpadRight && e?.HasNoModifiers == true)
            {
                if (e.RepeatCount == 0) owner.Move(keyCode == Keycode.DpadRight);
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }

        public override bool OnKeyUp(Keycode keyCode, KeyEvent? e) =>
            (keyCode is Keycode.DpadLeft or Keycode.DpadRight && e?.HasNoModifiers == true) || base.OnKeyUp(keyCode, e);

        #endregion

        #region TalkBack state and actions

        public override void OnInitializeAccessibilityNodeInfo(AccessibilityNodeInfo? info)
        {
            base.OnInitializeAccessibilityNodeInfo(info);
            if (info == null) return;
            info.ContentDescription = owner.Value;
            info.Scrollable = owner.CanGoPrevious || owner.CanGoNext;
            if (owner.CanGoPrevious) info.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionScrollBackward);
            if (owner.CanGoNext) info.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionScrollForward);
        }

        public override bool PerformAccessibilityAction(Android.Views.Accessibility.Action action, Bundle? arguments) =>
            action switch
            {
                Android.Views.Accessibility.Action.ScrollBackward => owner.Move(false),
                Android.Views.Accessibility.Action.ScrollForward => owner.Move(true),
                _ => base.PerformAccessibilityAction(action, arguments),
            };

        #endregion
    }
}
#endif
