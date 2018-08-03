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

        private RuntimeImage PopupImage { get; } = new RuntimeImage(new Uri("pack://application:,,,/Samples/PopupViewer/expand.png"));

        private async void mapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            try
            {
                var result = await mapView.IdentifyLayersAsync(e.Position, 3, false);
                var popup = GetPopup(result);
                if(popup != null)
                {
                    if (popup.PopupDefinition != null && !popup.PopupDefinition.ShowEditSummary)
                        popup.PopupDefinition.ShowEditSummary = true;

                    var callout = new CalloutDefinition(popup.GeoElement);
                    callout.Tag = popup;
                    callout.ButtonImage = PopupImage;
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