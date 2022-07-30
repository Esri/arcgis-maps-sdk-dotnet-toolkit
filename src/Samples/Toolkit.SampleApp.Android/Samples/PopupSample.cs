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

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [Activity(Label = "Popup", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
    [SampleInfoAttribute(Category = "Popup", Description = "Use PopupViewer to display detailed feature information")]
    public class PopupSampleActivity : Activity
    {
        UI.Controls.PopupViewer popupViewer;
        MapView mapView;

        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.PopupViewerSample);
            mapView = FindViewById<MapView>(Resource.Id.mapView);
            mapView.Map = new Map(Basemap.CreateLightGrayCanvasVector());
            mapView.GeoViewTapped += mapView_GeoViewTapped;

            // Webmap configured with Popup
            mapView.Map = new Map(new Uri("https://arcgisruntime.maps.arcgis.com/home/item.html?id=064f2e898b094a17b84e4a4cd5e5f549"));

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
                    ShowPopup(popup);
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

        private void ShowPopup(Popup popup)
        {
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