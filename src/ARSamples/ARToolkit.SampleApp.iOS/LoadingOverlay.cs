using System;
using CoreGraphics;
using UIKit;

namespace ARToolkit.SampleApp
{
    /// <summary>
    /// View to show over the sample list while a sample with offline data is loading.
    /// </summary>
    public sealed class LoadingOverlay : UIView
    {
        private UILabel loadingLabel;

        public LoadingOverlay(CGRect frame) : base(frame)
        {
            BackgroundColor = UIColor.Black;
            Alpha = 0.8f;
            AutoresizingMask = UIViewAutoresizing.All;

            nfloat centerX = Frame.Width / 2;
            nfloat centerY = Frame.Height / 2;

            UIActivityIndicatorView activitySpinner = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.WhiteLarge);
            activitySpinner.Frame = new CGRect(
                centerX - activitySpinner.Frame.Width / 2,
                centerY - activitySpinner.Frame.Height - 20,
                activitySpinner.Frame.Width,
                activitySpinner.Frame.Height);
            activitySpinner.AutoresizingMask = UIViewAutoresizing.All;
            AddSubview(activitySpinner);
            activitySpinner.StartAnimating();

            loadingLabel = new UILabel(new CGRect(
                centerX - (Frame.Width - 20) / 2,
                centerY + 20,
                Frame.Width - 20,
                22
            ))
            {
                BackgroundColor = UIColor.Clear,
                TextColor = UIColor.White,
                Text = "Downloading Data",
                TextAlignment = UITextAlignment.Center,
                AutoresizingMask = UIViewAutoresizing.All
            };
            AddSubview(loadingLabel);
        }
        public void SetText(string text)
        {
            loadingLabel.Text = text;
        }
        public void Hide()
        {
            Animate(
                0.5,
                () => { Alpha = 0; },
                RemoveFromSuperview
            );
        }
    }
}