using Android.App;
using Android.Widget;
using Android.OS;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Mapping;
using System;
using Esri.ArcGISRuntime.Mapping.Popups;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Data;
using System.Linq;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Security;
using System.IO;


namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [Activity(Label = "Popup", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
    [SampleInfoAttribute(Category = "Popup", Description = "Use PopupViewer to display detailed feature information")]
    public class PopupSampleActivity : Activity
    {
        private RuntimeImage _infoIcon = null;
        private RuntimeImage InfoIcon => _infoIcon;

        UI.Controls.PopupViewer popupViewer;
        MapView mapView;

        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.PopupViewerSample);


            // Used in Callout to see feature details in PopupViewer
            _infoIcon = await RuntimeImage.FromStreamAsync(Assets.Open("info.png"));

            // Used to demonstrate display of EditSummary in PopupViewer
            // Provides credentials to token-secured layer that has editor-tracking enabled
            AuthenticationManager.Current.ChallengeHandler = new ChallengeHandler(async (info) =>
            {
                return await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri, "user1", "user1");
            });

            mapView = FindViewById<MapView>(Resource.Id.mapView);
            mapView.Map = new Map(Basemap.CreateLightGrayCanvasVector());
            mapView.GeoViewTapped += mapView_GeoViewTapped;

            // Webmap configured with Popup
            mapView.Map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=d4fe39d300c24672b1821fa8450b6ae2"));

            popupViewer = FindViewById<UI.Controls.PopupViewer>(Resource.Id.popupViewer);
        }

        private async void mapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            Exception error = null;
            try
            {
                var result = await mapView.IdentifyLayersAsync(e.Position, 3, false);

                // Retrieves or builds Popup from IdentifyLayerResult
                var popup = GetPopup(result);

                // Displays callout and updates visibility of PopupViewer
                if (popup != null)
                {
                    var callout = new CalloutDefinition(popup.GeoElement);
                    callout.Tag = popup;
                    callout.ButtonImage = InfoIcon;
                    callout.OnButtonClick = OnPopupInfoButtonClicked;                   
                    mapView.ShowCalloutForGeoElement(popup.GeoElement, e.Position, callout);
                }
                else
                {
                    popupViewer.PopupManager = null;
                    popupViewer.Visibility = Android.Views.ViewStates.Gone;
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            if (error != null)
            {
                var alert = new AlertDialog.Builder(this);
                alert.SetTitle(error.GetType().Name);
                alert.SetMessage(error.Message);
                alert.SetPositiveButton("OK", (a, b) => { });
                RunOnUiThread(() => alert.Show());
            }
        }

        private void OnPopupInfoButtonClicked(object tag)
        {
            Popup popup = (Popup)tag;
            // Create a popupviewer and show it in a PopupWindow
            var popupViewer = new UI.Controls.PopupViewer(ApplicationContext);
            PopupWindow window = new PopupWindow(ApplicationContext)
            {
                ContentView = popupViewer,
            };
            window.SetBackgroundDrawable(new Android.Graphics.Drawables.ColorDrawable(Android.Graphics.Color.White));
            window.OutsideTouchable = true;
            window.Touchable = true;
            popupViewer.SetPadding(20, 20, 20, 20);
            popupViewer.PopupManager = new PopupManager(popup);
            window.ShowAtLocation(mapView, Android.Views.GravityFlags.Bottom, 0, 0);
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
    }
}