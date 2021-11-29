using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UtilityNetworks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.UtilityNetworkTraceTool
{
    public sealed partial class UtilityNetworkTraceToolBehaviorSample : Page
    {
        private const string WebmapURL = "https://rt-server109.esri.com/portal/home/item.html?id=54fa9aadf6c645d39f006cf279147204";
        private readonly Tuple<string, string, string> _portal1Login = new Tuple<string, string, string>("https://rt-server109.esri.com/portal/sharing/rest", "publisher1", "test.publisher01");

        private const string WebmapURL2 = "http://rtc-100-8.esri.com/portal/home/webmap/viewer.html?webmap=5cd15bb83a0941489cbb594af9422b0a";
        private readonly Tuple<string, string, string> _portal2Login = new Tuple<string, string, string>("https://rtc-100-8.esri.com/portal/sharing/rest", "apptest", "app.test1234");

        private const string WebmapURL3 = "http://rtc-100-8.esri.com/portal/home/webmap/viewer.html?webmap=78f993b89bad4ba0a8a22ce2e0bcfbd0";

        private const string FeatureServerURL = "https://sampleserver7.arcgisonline.com/server/rest/services/UtilityNetwork/NapervilleElectric/FeatureServer";
        private readonly Tuple<string, string, string> _portal3Login = new Tuple<string, string, string>("https://sampleserver7.arcgisonline.com/portal/sharing/rest", "viewer01", "I68VGU^nMurF");

        private Dictionary<string, Action> _geoViewInputs = new Dictionary<string, Action>();
        public UtilityNetworkTraceToolBehaviorSample()
        {
            InitializeComponent();
            Initialize();

            _geoViewInputs["Has TraceTypes"] = new Action(() => GeoViewControl.Content = new MapView() { Map = new Map(new Uri(WebmapURL)) });
            _geoViewInputs["No TraceTypes"] = new Action(() => GeoViewControl.Content = new MapView() { Map = new Map(new Uri(WebmapURL2)) });
            _geoViewInputs["No TraceTypes 2"] = new Action(() => GeoViewControl.Content = new MapView() { Map = new Map(new Uri(WebmapURL3)) });
            _geoViewInputs["SceneView"] = new Action(() => GeoViewControl.Content = new SceneView() { Scene = new Scene(Basemap.CreateTopographicVector()) });

            GeoViewInputs.ItemsSource = _geoViewInputs;
        }

        private async void Initialize()
        {
            string title = null;
            string message = null;
            try
            {
                var portal1Credential = await AuthenticationManager.Current.GenerateCredentialAsync(new Uri(_portal1Login.Item1), _portal1Login.Item2, _portal1Login.Item3);
                AuthenticationManager.Current.AddCredential(portal1Credential);

                var portal2Credential = await AuthenticationManager.Current.GenerateCredentialAsync(new Uri(_portal2Login.Item1), _portal2Login.Item2, _portal2Login.Item3);
                AuthenticationManager.Current.AddCredential(portal2Credential);

                var portal3Credential = await AuthenticationManager.Current.GenerateCredentialAsync(new Uri(_portal3Login.Item1), _portal3Login.Item2, _portal3Login.Item3);
                AuthenticationManager.Current.AddCredential(portal3Credential);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                title = ex.GetType().Name;
            }
            if (!string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(title))
            {
                await new MessageDialog($"Initializing sample failed: {message}") { Title = title }.ShowAsync();
            }
        }

        private void OnGeoViewInputChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GeoViewInputs.SelectedItem is KeyValuePair<string, Action> item && item.Value != null)
            {
                item.Value.Invoke();
            }
        }

        private void OnAddOrRemoveUtilityNetworks(object sender, RoutedEventArgs e)
        {
            if (GeoViewControl.Content is MapView mapView && mapView.Map is Map map)
            {
                if (map.UtilityNetworks.FirstOrDefault(un => un.Name == "NapervilleElectric Utility Network") is UtilityNetwork utilityNetwork)
                {
                    map.UtilityNetworks.Remove(utilityNetwork);
                }
                else
                {
                    map.UtilityNetworks.Add(new UtilityNetwork(new Uri(FeatureServerURL)));
                }
            }
        }

        private void OnClearUtilityNetworks(object sender, RoutedEventArgs e)
        {
            if (GeoViewControl.Content is MapView mapView && mapView.Map is Map map)
            {
                map.UtilityNetworks.Clear();
            }
        }

        private async void OnAddStartingPoints(object sender, RoutedEventArgs e)
        {
            string title = null;
            string message = null;
            try
            {
                if (GeoViewControl.Content is MapView mapView && mapView.Map is Map map)
                {
                    UtilityNetworkTraceTool.StartingPoints = new ObservableCollection<ArcGISFeature>();
                    if (map.OperationalLayers.FirstOrDefault(l => l.Name == "ElecDist Device") is FeatureLayer deviceLayer)
                    {
                        var query = new QueryParameters();
                        query.ObjectIds.Add(171L);
                        query.ObjectIds.Add(463L);
                        var features = await deviceLayer.FeatureTable.QueryFeaturesAsync(query);
                        foreach (ArcGISFeature feature in features)
                        {
                            UtilityNetworkTraceTool.StartingPoints.Add(feature);
                        }
                    }
                    if (map.OperationalLayers.FirstOrDefault(l => l.Name == "ElecDist Line") is FeatureLayer lineLayer)
                    {
                        var query = new QueryParameters();
                        query.ObjectIds.Add(278L);
                        var features = await lineLayer.FeatureTable.QueryFeaturesAsync(query);
                        foreach (ArcGISFeature feature in features)
                        {
                            UtilityNetworkTraceTool.StartingPoints.Add(feature);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                title = ex.GetType().Name;
            }
            if (!string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(title))
            {
                await new MessageDialog($"Querying features failed: {message}") { Title = title }.ShowAsync();
            }
        }

        private void OnClearStartingPoints(object sender, RoutedEventArgs e)
        {
            UtilityNetworkTraceTool.StartingPoints?.Clear();
        }

        private async void OnUtilityNetworkChanged(object sender, UtilityNetworkChangedEventArgs e)
        {
            var name = e.UtilityNetwork?.Name ?? "<null>";
            await new MessageDialog($"{name} selected") { Title = "UtilityNetworkChanged" }.ShowAsync();
        }

        private async void OnUtilityNetworkTraceCompleted(object sender, UtilityNetworkTraceCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                await new MessageDialog($"{e.Error.Message}") { Title =  $"UtilityNetworkTraceCompleted with {e.Error.GetType().Name} error" }.ShowAsync();
            }
            else
            {
                await new MessageDialog($"{e.Results.Count()} results returned") { Title = "UtilityNetworkTraceCompleted" }.ShowAsync();
            }
        }

        private void OnUpdateSymbols(object sender, RoutedEventArgs e)
        {
            var random = new Random();
            var colorBytes = new byte[3];
            random.NextBytes(colorBytes);
            var color = Color.FromArgb(200, colorBytes[0], colorBytes[1], colorBytes[2]);

            UtilityNetworkTraceTool.ResultFillSymbol = new SimpleFillSymbol()
            {
                Color = color,
                Style = (SimpleFillSymbolStyle)random.Next(0, Enum.GetValues(typeof(SimpleFillSymbolStyle)).Length - 1),
            };

            UtilityNetworkTraceTool.ResultLineSymbol = new SimpleLineSymbol()
            {
                Color = color,
                Style = (SimpleLineSymbolStyle)random.Next(0, Enum.GetValues(typeof(SimpleLineSymbolStyle)).Length - 1),
                Width = 5d
            };

            UtilityNetworkTraceTool.ResultPointSymbol = new SimpleMarkerSymbol()
            {
                Color = color,
                Style = (SimpleMarkerSymbolStyle)random.Next(0, Enum.GetValues(typeof(SimpleMarkerSymbolStyle)).Length - 1),
                Size = 20d
            };

            random.NextBytes(colorBytes);
            color = Color.FromArgb(200, colorBytes[0], colorBytes[1], colorBytes[2]);
            UtilityNetworkTraceTool.StartingPointSymbol = new SimpleMarkerSymbol()
            {
                Color = color,
                Style = (SimpleMarkerSymbolStyle)random.Next(0, Enum.GetValues(typeof(SimpleMarkerSymbolStyle)).Length - 1),
                Size = 20d
            };
        }
    }
}
