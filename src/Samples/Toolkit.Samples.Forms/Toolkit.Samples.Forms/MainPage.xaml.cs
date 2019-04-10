using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Toolkit.Samples.Forms
{
	public partial class MainPage : ContentPage
	{
        public MainPage()
        {
            InitializeComponent();


            // Used to demonstrate display of EditSummary in PopupViewer
            // Provides credentials to token-secured layer that has editor-tracking enabled
            AuthenticationManager.Current.ChallengeHandler = new ChallengeHandler(async (info) =>
            {
                return await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri, "user1", "user1");
            });
            timeSlider.FullExtent = new Esri.ArcGISRuntime.TimeExtent(DateTimeOffset.Now.AddYears(-1), DateTimeOffset.Now);
            timeSlider.CurrentExtent = new Esri.ArcGISRuntime.TimeExtent(DateTimeOffset.Now.AddMonths(-1), DateTimeOffset.Now);
            mapView.GeoViewTapped += mapView_GeoViewTapped;
        }

        // Used in Callout to see feature details in PopupViewer
        private RuntimeImage InfoIcon => new RuntimeImage(new Uri("https://cdn3.iconfinder.com/data/icons/web-and-internet-icons/512/Information-256.png"));

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
                        popupViewer.IsVisible = true;
                        popupViewer.PopupManager = new PopupManager(s as Popup);
                    });
                    mapView.ShowCalloutForGeoElement(popup.GeoElement, e.Position, callout);
                }
                else
                {
                    popupViewer.PopupManager = null;
                    popupViewer.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                error = ex;

            }
            if (error != null)
                await DisplayAlert(error.GetType().Name, error.Message, "OK");
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
