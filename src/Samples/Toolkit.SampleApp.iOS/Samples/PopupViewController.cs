using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [SampleInfoAttribute(Category = "Popup", Description = "Use PopupViewer to display detailed feature information")]
    public partial class PopupViewController : UIViewController
    {
        private MapView mapView;

        public PopupViewController()
        {
        }

        private RuntimeImage InfoIcon;
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Used to demonstrate display of EditSummary in PopupViewer
            // Provides credentials to token-secured layer that has editor-tracking enabled
            AuthenticationManager.Current.ChallengeHandler = new ChallengeHandler(async (info) =>
            {
                return await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri, "user1", "user1");
            });

            mapView = new MapView()
            {
                Map = new Map(Basemap.CreateLightGrayCanvasVector()),
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            this.View.AddSubview(mapView);

            mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            mapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            mapView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor).Active = true;
            
            // Used in Callout to see feature details in PopupViewer
            mapView.GeoViewTapped += mapView_GeoViewTapped;

            // Webmap configured with Popup
            mapView.Map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=d4fe39d300c24672b1821fa8450b6ae2"));
        }

        public override void ViewDidDisappear(bool animated)
        {
            AuthenticationManager.Current.ChallengeHandler = null;
            base.ViewDidDisappear(animated);
        }

        private void mapView_GeoViewTapped(object sender, GeoViewInputEventArgs e) => _ = HandleTap(e);

        private async Task HandleTap(GeoViewInputEventArgs e)
        {
            Exception error = null;
            try
            {
                if (InfoIcon == null)
                {
                    InfoIcon = await UIImage.FromBundle("info.png")?.ToRuntimeImageAsync();
                }
                var result = await mapView.IdentifyLayersAsync(e.Position, 3, false);
                // Retrieves or builds Popup from IdentifyLayerResult
                var popup = GetPopup(result);

                // Displays callout and on (i) button click shows PopupViewer in it's own view controller
                if (popup != null)
                {
                    var callout = new CalloutDefinition(popup.GeoElement);
                    callout.Tag = popup;
                    callout.ButtonImage = InfoIcon;
                    callout.OnButtonClick = new Action<object>((s) =>
                    {
                        var pvc = new PopupInfoViewController(popup);
                        this.PresentModalViewController(pvc, true);
                    });
                    mapView.ShowCalloutForGeoElement(popup.GeoElement, e.Position, callout);
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            if (error != null)
            {
                var alert = UIAlertController.Create(error.GetType().Name, error.Message, UIAlertControllerStyle.Alert);
                alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
                this.PresentViewController(alert, true, null);
            }
        }

        private Popup GetPopup(IdentifyLayerResult result)
        {
            if (result == null)
            {
                return null;
            }

            var popup = result.Popups.FirstOrDefault();
            if (popup != null)
            {
                return popup;
            }

            var geoElement = result.GeoElements.FirstOrDefault();
            if (geoElement != null)
            {
                if (result.LayerContent is IPopupSource)
                {
                    var popupDefinition = ((IPopupSource)result.LayerContent).PopupDefinition;
                    if (popupDefinition != null)
                    {
                        return new Popup(geoElement, popupDefinition);
                    }
                }

                return Popup.FromGeoElement(geoElement);
            }

            return null;
        }

        private Popup GetPopup(IEnumerable<IdentifyLayerResult> results)
        {
            if (results == null)
            {
                return null;
            }
            foreach (var result in results)
            {
                var popup = GetPopup(result);
                if (popup != null)
                {
                    return popup;
                }

                foreach (var subResult in result.SublayerResults)
                {
                    popup = GetPopup(subResult);
                    if (popup != null)
                    {
                        return popup;
                    }
                }
            }

            return null;
        }

        // Separate view controller for just showing the popup contents in its own view.
        private partial class PopupInfoViewController : UIViewController
        {
            private Popup popup;
            private PopupViewer popupViewer;

            public PopupInfoViewController(Popup popup)
            {
                ModalPresentationStyle = UIModalPresentationStyle.FormSheet;
                this.popup = popup;
            }

            public override void ViewDidLoad()
            {
                base.ViewDidLoad();
                UIButton button = new UIButton(UIButtonType.System)
                {
                    TranslatesAutoresizingMaskIntoConstraints = false
                };
                button.SetTitle("Done", UIControlState.Normal);
                button.TouchUpInside += (s, e) =>
                {
                    this.DismissModalViewController(true);
                };
                this.View.AddSubview(button);

                popupViewer = new PopupViewer()
                {
                    Frame = new CoreGraphics.CGRect(0, 0, 414, 736),
                    ContentStretch = new CoreGraphics.CGRect(0, 0, 1, 1),
                    TranslatesAutoresizingMaskIntoConstraints = false
                };
                if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                {
                    popupViewer.BackgroundColor = UIColor.SystemBackgroundColor;
                }
                else
                {
                    popupViewer.BackgroundColor = UIColor.White;
                }
                popupViewer.PopupManager = new PopupManager(popup);
                this.View.AddSubview(popupViewer);
                button.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
                button.LeftAnchor.ConstraintEqualTo(View.LayoutMarginsGuide.LeftAnchor).Active = true;
                popupViewer.TopAnchor.ConstraintEqualTo(button.BottomAnchor).Active = true;
                popupViewer.LeftAnchor.ConstraintEqualTo(View.LeftAnchor).Active = true;
                popupViewer.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor).Active = true;
                popupViewer.RightAnchor.ConstraintEqualTo(View.RightAnchor).Active = true;
            }
        }
    }
}