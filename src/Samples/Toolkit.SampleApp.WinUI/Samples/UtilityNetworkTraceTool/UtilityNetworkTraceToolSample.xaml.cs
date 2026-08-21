using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UtilityNetworks;
using System;
using System.Linq;
using Windows.UI.Popups;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.UtilityNetworkTraceTool
{
    public sealed partial class UtilityNetworkTraceToolSample : Page
    {
        private const string FeatureServiceURL = "https://sampleserver7.arcgisonline.com/server/rest/services/UtilityNetwork/NapervilleElectricV5/FeatureServer";
        private const string WebmapURL = "https://www.arcgis.com/home/item.html?id=471eb0bf37074b1fbb972b1da70fb310";

        public UtilityNetworkTraceToolSample()
        {
            InitializeComponent();
            Initialize();

        }

        private async void Initialize()
        {
            try
            {
                // Using public credentials from https://developers.arcgis.com/javascript/latest/sample-code/widgets-untrace/
                var portal1Credential = await AccessTokenCredential.CreateAsync(new Uri("https://sampleserver7.arcgisonline.com/portal/sharing/rest"), "viewer01", "I68VGU^nMurF");
                AuthenticationManager.Current.AddCredential(portal1Credential);

                MyMapView.Map = new Map(new Uri(WebmapURL));
            }
            catch (Exception ex)
            {
                await new MessageDialog($"Initializing sample failed: {ex.Message}").ShowAsync();
            }
        }

        private async void LoadNamedTracesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var traceNames = NamedTraceNamesTextBox.Text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                await UtilityNetworkTraceTool.LoadAsync(traceNames);
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    XamlRoot = XamlRoot,
                    Title = ex.GetType().Name,
                    Content = $"Loading named traces failed: {ex.Message}",
                    CloseButtonText = "OK",
                }.ShowAsync();
            }
        }

        private async void MapSourceRadioButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PortalMapRadioButton.IsChecked == true)
                {
                    MyMapView.Map = new Map(new Uri(WebmapURL));
                    return;
                }

                var map = new Map(new Basemap(new OpenStreetMapLayer()));
                var serviceGeodatabase = new ServiceGeodatabase(new Uri(FeatureServiceURL));
                var utilityNetwork = new UtilityNetwork(serviceGeodatabase);
                map.UtilityNetworks.Add(utilityNetwork);

                MyMapView.Map = map;

                await map.LoadAsync();
                await utilityNetwork.LoadAsync();
                await serviceGeodatabase.LoadAsync();

                ArgumentNullException.ThrowIfNull(serviceGeodatabase.ServiceInfo);
                foreach (var idInfo in serviceGeodatabase.ServiceInfo.LayerInfos)
                {
                    map.OperationalLayers.Insert(0, new FeatureLayer(serviceGeodatabase.GetTable(idInfo.Id)));
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog($"Switching maps failed: {ex.Message}", ex.GetType().Name).ShowAsync();
            }
        }

        private async void AddTraceLocationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var deviceWhereClause = DeviceWhereClauseTextBox.Text;
                if (string.IsNullOrWhiteSpace(deviceWhereClause))
                {
                    throw new InvalidOperationException("A device where clause is required.");
                }

                var map = MyMapView.Map ?? throw new InvalidOperationException("The map has not been initialized.");
                await map.LoadAsync();

                var utilityNetwork = map.UtilityNetworks.FirstOrDefault()
                    ?? throw new InvalidOperationException("The map does not contain a utility network.");
                await utilityNetwork.LoadAsync();

                var deviceSource = utilityNetwork.Definition.NetworkSources.FirstOrDefault(source => source.SourceUsageType == UtilityNetworkSourceUsageType.Device)
                    ?? throw new InvalidOperationException("The utility network does not contain a device network source.");
                var featureTable = deviceSource.FeatureTable as ServiceFeatureTable
                    ?? throw new InvalidOperationException("The device network source is not backed by a service feature table.");
                var queryParameters = new QueryParameters
                {
                    WhereClause = deviceWhereClause,
                    MaxFeatures = 1,
                };
                var queryResult = await featureTable.QueryFeaturesAsync(queryParameters, QueryFeatureFields.LoadAll);
                var device = queryResult.OfType<ArcGISFeature>().FirstOrDefault()
                    ?? throw new InvalidOperationException($"No device matched '{deviceWhereClause}'.");

                if (StartingPointRadioButton.IsChecked == true)
                {
                    UtilityNetworkTraceTool.AddStartingPoint(device, device.Geometry as MapPoint);
                }
                else
                {
                    UtilityNetworkTraceTool.AddBarrier(device, device.Geometry as MapPoint);
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog($"Adding a trace location failed: {ex.Message}", ex.GetType().Name).ShowAsync();
            }
        }

        private void UpdateSymbolButton_Click(object sender, RoutedEventArgs e)
        {
            var styles = Enum.GetValues<SimpleMarkerSymbolStyle>();
            var symbol = new SimpleMarkerSymbol(
                styles[Random.Shared.Next(styles.Length)],
                System.Drawing.Color.FromArgb(255, Random.Shared.Next(256), Random.Shared.Next(256), Random.Shared.Next(256)),
                20d);

            if (StartingPointRadioButton.IsChecked == true)
            {
                UtilityNetworkTraceTool.StartingPointSymbol = symbol;
            }
            else
            {
                UtilityNetworkTraceTool.BarrierSymbol = symbol;
            }
        }
    }
}
