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

namespace Esri.ArcGISRuntime.Toolkit.SampleApp
{
    [Activity(Label = "Toolkit.Samples.Droid", MainLauncher = true)]
    public class MainActivity : Activity
    {
        UI.Controls.Compass compass;
        UI.Controls.ScaleLine scaleLine;
        UI.Controls.PopupViewer popupViewer;
        MapView mapView;

        private RuntimeImage _infoIcon = null;
        private RuntimeImage InfoIcon => _infoIcon;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Used in Callout to see feature details in PopupViewer
            _infoIcon = await RuntimeImage.FromStreamAsync(Assets.Open("info.png"));

            // Used to demonstrate display of EditSummary in PopupViewer
            // Provides credentials to token-secured layer that has editor-tracking enabled
            AuthenticationManager.Current.ChallengeHandler = new ChallengeHandler(async (info) =>
            {
                return await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri, "user1", "user1");
            });

            mapView = FindViewById<MapView>(Resource.Id.mapView);
            mapView.GeoViewTapped += mapView_GeoViewTapped;

            // Webmap configured with Popup
            mapView.Map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=d4fe39d300c24672b1821fa8450b6ae2"));

            popupViewer = FindViewById<UI.Controls.PopupViewer>(Resource.Id.popupViewer);

            scaleLine = FindViewById<UI.Controls.ScaleLine>(Resource.Id.scaleLine);
            scaleLine.MapView = mapView;
            compass = FindViewById<UI.Controls.Compass>(Resource.Id.compass);
            compass.GeoView = mapView;
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
                    callout.OnButtonClick = new Action<object>((s) =>
                    {
                        popupViewer.Visibility = Android.Views.ViewStates.Visible;
                        popupViewer.PopupManager = new PopupManager(popup);
                    });
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

