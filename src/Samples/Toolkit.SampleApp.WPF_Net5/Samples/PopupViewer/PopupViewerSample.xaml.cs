using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.PopupViewer
{
    /// <summary>
    /// Interaction logic for PopupViewSample.xaml
    /// </summary>
    public partial class PopupViewerSample : UserControl
    {
        public PopupViewerSample()
        {
            InitializeComponent();

        }

        // Used in Callout to see feature details in PopupViewer
        private RuntimeImage InfoIcon { get; } = new RuntimeImage(new Uri("Samples/PopupViewer/info.png", UriKind.Relative));

        private void mapView_GeoViewTapped(object sender, GeoViewInputEventArgs e) => _ = HandleTap(e);

        private async Task HandleTap(GeoViewInputEventArgs e)
        {
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
                            popupViewer.Visibility = Visibility.Visible;
                            popupViewer.PopupManager = new PopupManager(s as Popup);
                        });
                    mapView.ShowCalloutForGeoElement(popup.GeoElement, e.Position, callout);
                }
                else
                {
                    popupViewer.PopupManager = null;
                    popupViewer.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name);
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