using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UtilityNetworks;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "UtilityNetworkTraceTool", Description = "Use named trace configurations defined in a web map to perform connected trace operations and compare results.")]
    public partial class UtilityNetworkTraceToolSample : ContentPage
    {
        private const string FeatureServiceURL = "https://sampleserver7.arcgisonline.com/server/rest/services/UtilityNetwork/NapervilleElectricV5/FeatureServer";
        private const string WebmapURL = "https://www.arcgis.com/home/item.html?id=471eb0bf37074b1fbb972b1da70fb310";
        private bool _isInitialized;

        public UtilityNetworkTraceToolSample()
        {
            InitializeComponent();
            _isInitialized = true;

            MyTraceTool.UtilityNetworkChanged += MyTraceTool_UtilityNetworkChanged;
            MyTraceTool.UtilityNetworkTraceCompleted += MyTraceTool_UtilityNetworkTraceCompleted;

            Initialize();
        }

        private async void MyTraceTool_UtilityNetworkTraceCompleted(object? sender, Esri.ArcGISRuntime.Toolkit.Maui.UtilityNetworkTraceCompletedEventArgs e)
        {
            if (Dispatcher.IsDispatchRequired)
            {
                Dispatcher.Dispatch(() => MyTraceTool_UtilityNetworkTraceCompleted(sender, e));
                return;
            }

            await DisplayAlertAsync(
                "Trace completed",
                $"Trace completed with {e.Parameters.StartingLocations.Count} starting points, " +
                    $"{e.Parameters.Barriers.Count} barriers, and " +
                    $"{e.Parameters.FilterBarriers.Count} filter barriers.",
                "OK");
        }

        private void MyTraceTool_UtilityNetworkChanged(object? sender, Esri.ArcGISRuntime.Toolkit.Maui.UtilityNetworkChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Network changed. New selection: {e.UtilityNetwork?.Name}");
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
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async void LoadNamedTracesButton_Clicked(object? sender, EventArgs e)
        {
            try
            {
                var traceNames = NamedTraceNamesEntry.Text?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    ?? Array.Empty<string>();
                await MyTraceTool.LoadAsync(traceNames);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(ex.GetType().Name, $"Loading named traces failed: {ex.Message}", "OK");
            }
        }

        private async void MapSourceRadioButton_CheckedChanged(object? sender, CheckedChangedEventArgs e)
        {
            if (!_isInitialized || !e.Value)
            {
                return;
            }

            try
            {
                if (PortalMapRadioButton.IsChecked)
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
                await DisplayAlertAsync(ex.GetType().Name, $"Switching maps failed: {ex.Message}", "OK");
            }
        }

        private async void AddTraceLocationButton_Clicked(object? sender, EventArgs e)
        {
            try
            {
                var deviceWhereClause = DeviceWhereClauseEntry.Text;
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

                if (StartingPointRadioButton.IsChecked)
                {
                    MyTraceTool.AddStartingPoint(device, device.Geometry as MapPoint);
                }
                else
                {
                    MyTraceTool.AddBarrier(device, device.Geometry as MapPoint);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(ex.GetType().Name, $"Adding a trace location failed: {ex.Message}", "OK");
            }
        }

        private void UpdateSymbolButton_Clicked(object? sender, EventArgs e)
        {
            var styles = Enum.GetValues<SimpleMarkerSymbolStyle>();
            var symbol = new SimpleMarkerSymbol(
                styles[Random.Shared.Next(styles.Length)],
                System.Drawing.Color.FromArgb(255, Random.Shared.Next(256), Random.Shared.Next(256), Random.Shared.Next(256)),
                20d);

            if (StartingPointRadioButton.IsChecked)
            {
                MyTraceTool.StartingPointSymbol = symbol;
            }
            else
            {
                MyTraceTool.BarrierSymbol = symbol;
            }
        }
    }
}