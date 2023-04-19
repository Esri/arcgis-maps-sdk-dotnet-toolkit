using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private async void mapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            try
            {
                var result = await mapView.IdentifyLayersAsync(e.Position, 3, false);

                // Retrieves or builds Popup from IdentifyLayerResult
                var popup = GetPopup(result);
                if (popup != null)
                {
                    PopupBackground.Visibility = Visibility.Visible;
                    popupViewer.Popup = popup;
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

        private void PopupBackground_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PopupBackground.Visibility = Visibility.Collapsed;
            popupViewer.Popup = null;
        }
    }
}