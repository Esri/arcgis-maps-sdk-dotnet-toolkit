using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.PopupViewer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PopupViewerSample : Page
    {
        public PopupViewerSample()
        {
            this.InitializeComponent();
        }

        // Webmap configured with Popup
        public Map Map { get; } = new Map(new Uri("https://www.arcgis.com/home/item.html?id=9f3a674e998f461580006e626611f9ad"));

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
                    popupViewer.Visibility = Visibility.Visible;
                    popupViewer.Popup = popup;
                }
                else
                {
                    popupViewer.Popup = null;
                    popupViewer.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            if (error != null)
                System.Diagnostics.Debug.WriteLine(error);
        }

        private Mapping.Popups.Popup GetPopup(IdentifyLayerResult result)
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
                        return new Mapping.Popups.Popup(geoElement, popupDefinition);
                    }
                }

                return Mapping.Popups.Popup.FromGeoElement(geoElement);
            }

            return null;
        }

        private Mapping.Popups.Popup GetPopup(IEnumerable<IdentifyLayerResult> results)
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
        private async void popupViewer_PopupAttachmentClicked(object sender, UI.Controls.PopupAttachmentClickedEventArgs e)
        {
            // Override the default attachment click behavior (which will download and save attachment)
            if (!e.Attachment.IsLocal) // Attachment hasn't been downloaded
            {
                try
                {
                    // Make first click just load the attachment (or cancel a loading operation). Otherwise fallback to default behavior
                    if (e.Attachment.LoadStatus == LoadStatus.NotLoaded)
                    {
                        e.Handled = true;
                        await e.Attachment.LoadAsync();
                    }
                    else if (e.Attachment.LoadStatus == LoadStatus.FailedToLoad)
                    {
                        e.Handled = true;
                        await e.Attachment.RetryLoadAsync();
                    }
                    else if (e.Attachment.LoadStatus == LoadStatus.Loading)
                    {
                        e.Handled = true;
                        e.Attachment.CancelLoad();
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to download attachment", ex.Message);
                }
            }
        }

        private void popupViewer_LinkClicked(object sender, UI.Controls.HyperlinkClickedEventArgs e)
        {
            // Include below line if you want to prevent the default action
            // e.Handled = true;

            // Perform custom action when a link is clicked
            System.Diagnostics.Debug.WriteLine(e.Uri);
        }
    }
}