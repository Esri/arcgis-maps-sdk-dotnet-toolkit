using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Popups;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Forms
{
    public sealed partial class FeatureFormViewSample : Page
    {
        public FeatureFormViewSample()
        {
            this.InitializeComponent();
        }

        // Webmap configured with feature forms
        public Map Map { get; } = new Map(new Uri("https://www.arcgis.com/home/item.html?id=f72207ac170a40d8992b7a3507b44fad"));
        private async void mapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            try
            {
                var result = await mapView.IdentifyLayersAsync(e.Position, 3, false);

                // Retrieves feature from IdentifyLayerResult with a form definition
                var feature = GetFeature(result);
                if (feature != null)
                {
                    formViewer.FeatureForm = new FeatureForm(feature);
                    SidePanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error: " + ex.Message);
            }
        }

        private ArcGISFeature GetFeature(IEnumerable<IdentifyLayerResult> results)
        {
            if (results == null)
                return null;
            foreach (var result in results.Where(r => r.LayerContent is FeatureLayer layer && (layer.FeatureFormDefinition is not null || layer.FeatureTable is ArcGISFeatureTable { FeatureFormDefinition: not null })))
            {
                return result.GeoElements?.OfType<ArcGISFeature>()?.FirstOrDefault();
            }

            return null;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            formViewer.FeatureForm = null;
            SidePanel.Visibility = Visibility.Collapsed;
        }
    }
}