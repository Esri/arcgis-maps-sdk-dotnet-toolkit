#if __IOS__ && !MACCATALYST
using Foundation;
using Microsoft.Maui.Handlers;
using ObjCRuntime;
using UIKit;
using NativeContentView = Microsoft.Maui.Platform.ContentView;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

internal sealed class OrientedImageryPaginationViewHandler : ContentViewHandler
{
    #region Native host

    protected override NativeContentView CreatePlatformView() => new PaginationView((OrientedImageryPaginationView)VirtualView)
    {
        CrossPlatformLayout = VirtualView,
        IsAccessibilityElement = true,
        AccessibilityRespondsToUserInteraction = true,
        AccessibilityTraits = UIAccessibilityTrait.Adjustable,
        AccessibilityLabel = Properties.Resources.GetString("OrientedImageryViewImageNavigation"),
        AccessibilityHint = Properties.Resources.GetString("OrientedImageryViewImageNavigationHint"),
        AccessibilityValue = ((OrientedImageryPaginationView)VirtualView).Value,
    };

    internal void UpdateAccessibility() => PlatformView.AccessibilityValue = ((OrientedImageryPaginationView)VirtualView).Value;

    #endregion

    private sealed class PaginationView(OrientedImageryPaginationView owner) : NativeContentView
    {
        #region Single focus stop with tappable children

        public override bool CanBecomeFocused => true;

        public override bool CanBecomeFirstResponder => true;

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            foreach (var child in Subviews)
                child.AccessibilityElementsHidden = true;
        }

        #endregion

        #region VoiceOver adjustment

        public override void AccessibilityIncrement() => owner.Move(true);

        public override void AccessibilityDecrement() => owner.Move(false);

        #endregion

        #region Hardware arrow keys

        public override UIKeyCommand[] KeyCommands =>
        [
            Arrow(UIKeyCommand.LeftArrow, "previousImage:"),
            Arrow(UIKeyCommand.RightArrow, "nextImage:"),
        ];

        private static UIKeyCommand Arrow(NSString input, string action)
        {
            var command = UIKeyCommand.Create(input, 0, new Selector(action));
            // Handle these arrows here, rather than letting UIKit move focus into the child buttons.
            command.WantsPriorityOverSystemBehavior = true;
            return command;
        }

        [Export("previousImage:")]
        private void PreviousImage(NSObject sender) => owner.Move(false);

        [Export("nextImage:")]
        private void NextImage(NSObject sender) => owner.Move(true);

        #endregion
    }
}
#endif
