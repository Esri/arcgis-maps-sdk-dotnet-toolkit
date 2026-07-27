using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Forms
{
    public sealed partial class FeatureFormViewSample : Page
    {
        private readonly MapOption[] _maps =
        {
            new MapOption("Feature Form", "https://www.arcgis.com/home/item.html?id=f72207ac170a40d8992b7a3507b44fad"),
            new MapOption("Tree Survey", "https://www.arcgis.com/apps/mapviewer/index.html?webmap=d8d2b5430dc4443db996e84182a17c3c"),
            new MapOption("Utility Network", "https://sampleserver7.arcgisonline.com/portal/home/item.html?id=6e3fc6db3d0b4e6589eb4097eb3e5b9b", AccessTokenCredential.CreateAsync(new Uri("https://sampleserver7.arcgisonline.com/portal/sharing/rest"), "editor01", "S7#i2LWmYH75")),
        };

        private Credential? _credential;
        private bool _isActive = true;

        public FeatureFormViewSample()
        {
            this.InitializeComponent();
            Loaded += FeatureFormViewSample_Loaded;
            Unloaded += FeatureFormViewSample_Unloaded;
            mapSelector.ItemsSource = _maps;
            mapSelector.SelectedIndex = 0;
        }

        private async void mapSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mapSelector.SelectedItem is not MapOption map)
            {
                return;
            }

            formViewer.FeatureForm = null;
            SidePanel.Visibility = Visibility.Collapsed;
            if (map.Credential is not null && _credential is null)
            {
                var credential = await map.Credential;
                if (!_isActive || !ReferenceEquals(mapSelector.SelectedItem, map))
                {
                    return;
                }

                _credential = credential;
                AuthenticationManager.Current.AddCredential(credential);
            }

            mapView.Map = new Map(new Uri(map.Uri));
        }

        private void FeatureFormViewSample_Loaded(object sender, RoutedEventArgs e) => _isActive = true;

        private void FeatureFormViewSample_Unloaded(object sender, RoutedEventArgs e)
        {
            _isActive = false;
            if (_credential is not null)
            {
                AuthenticationManager.Current.RemoveCredential(_credential);
                _credential = null;
            }
        }

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
            foreach (var result in results.Where(r => r.LayerContent is FeatureLayer layer))
            {
                var feature = result.GeoElements?.OfType<ArcGISFeature>()?.FirstOrDefault();
                if (feature != null)
                {
                    return feature;
                }
            }
            var subresults = results.Where(r => r.SublayerResults.Any()).SelectMany(r => r.SublayerResults);

            foreach (var s in results.SelectMany(r => r.SublayerResults).Where(r => r.LayerContent is SubtypeSublayer layer))
            {
                var feature = s.GeoElements?.OfType<ArcGISFeature>()?.FirstOrDefault();
                if (feature != null)
                {
                    return feature;
                }
            }
            foreach (var sub in subresults)
            {
                var elm = sub.GeoElements;
                if (elm.OfType<ArcGISFeature>().FirstOrDefault() is ArcGISFeature f)
                    return f;
            }
            return null;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            formViewer.FeatureForm = null;
            SidePanel.Visibility = Visibility.Collapsed;
        }

        private sealed class MapOption
        {
            internal MapOption(string title, string uri, Task<AccessTokenCredential>? credential = null)
            {
                Title = title;
                Uri = uri;
                Credential = credential;
            }

            public string Title { get; }

            internal string Uri { get; }

            internal Task<AccessTokenCredential>? Credential { get; }
        }
    }
}