using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.UI;
using Microsoft.Maui.Controls.Shapes;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "PopupViewer", Description = "Use PopupViewer to display detailed feature information")]
    public partial class PopupViewerSample : ContentPage
    {
        public PopupViewerSample()
        {
            InitializeComponent();
            mapView.Map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("https://www.arcgis.com/home/item.html?id=9f3a674e998f461580006e626611f9ad"));
            mapView.GeoViewTapped += mapView_GeoViewTapped;
        }


        private async void mapView_GeoViewTapped(object? sender, GeoViewInputEventArgs e)
        {
            Exception? error = null;
            try
            {
                var result = await mapView.IdentifyLayersAsync(e.Position, 3, false);

                // Retrieves or builds Popup from IdentifyLayerResult
                var popup = GetPopup(result);

                if (popup != null)
                {
                    popupViewer.Popup = popup;
                    popupPanel.IsVisible = true;
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

        private void CloseButton_Click(object sender, EventArgs e)
        {
            popupPanel.IsVisible = false;
        }

        private void popupViewer_PopupAttachmentClicked(object sender, Esri.ArcGISRuntime.Toolkit.Maui.PopupAttachmentClickedEventArgs e)
        {
            e.Handled = true; // Prevent default launch action
            // Share file:
            // _ = Share.Default.RequestAsync(new ShareFileRequest(new ReadOnlyFile(e.Attachment.Filename!, e.Attachment.ContentType)));

            // Open default file handler
            _ = Microsoft.Maui.ApplicationModel.Launcher.Default.OpenAsync(
                 new Microsoft.Maui.ApplicationModel.OpenFileRequest(e.Attachment.Name, new ReadOnlyFile(e.Attachment.Filename!, e.Attachment.ContentType)));
        }
    }
}