using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UtilityNetworks;
using UtilityNetworkTraceToolControl = Esri.ArcGISRuntime.Toolkit.UI.Controls.UtilityNetworkTraceTool;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.UtilityNetworkTraceTool
{
    public partial class UNTraceSimple : UserControl
    {
        private const string FeatureServiceURL = "https://sampleserver7.arcgisonline.com/server/rest/services/UtilityNetwork/NapervilleElectricV5/FeatureServer";
        private const string WebmapURL = "https://www.arcgis.com/home/item.html?id=471eb0bf37074b1fbb972b1da70fb310";

        public UNTraceSimple()
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
                MessageBox.Show($"Initializing sample failed: {ex.Message}", ex.GetType().Name);
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
                MessageBox.Show($"Loading named traces failed: {ex.Message}", ex.GetType().Name);
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
                MessageBox.Show($"Switching maps failed: {ex.Message}", ex.GetType().Name);
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
                MessageBox.Show($"Adding a trace location failed: {ex.Message}", ex.GetType().Name);
            }
        }

        private void UpdateSymbolButton_Click(object sender, RoutedEventArgs e)
        {
            var symbol = new SimpleMarkerSymbol(GetRandomSymbolStyle(), GetRandomColor(), 20d);
            if (StartingPointRadioButton.IsChecked == true)
            {
                UtilityNetworkTraceTool.StartingPointSymbol = symbol;
            }
            else
            {
                UtilityNetworkTraceTool.BarrierSymbol = symbol;
            }
        }

        private void ToggleItemTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            var isStartingPointSelected = StartingPointRadioButton.IsChecked == true;
            var itemTemplateProperty = isStartingPointSelected
                ? UtilityNetworkTraceToolControl.StartingPointItemTemplateProperty
                : UtilityNetworkTraceToolControl.BarrierItemTemplateProperty;

            if (UtilityNetworkTraceTool.ReadLocalValue(itemTemplateProperty) == DependencyProperty.UnsetValue)
            {
                var resourceKey = isStartingPointSelected
                    ? "AlternateStartingPointItemTemplate"
                    : "AlternateBarrierItemTemplate";
                UtilityNetworkTraceTool.SetValue(itemTemplateProperty, Resources[resourceKey]);
            }
            else
            {
                UtilityNetworkTraceTool.ClearValue(itemTemplateProperty);
            }
        }

        private SimpleMarkerSymbolStyle GetRandomSymbolStyle()
        {
            var styles = Enum.GetValues<SimpleMarkerSymbolStyle>();
            return styles[new Random().Next(styles.Length)];
        }

        private System.Drawing.Color GetRandomColor()
        {
            var random = new Random();
            return System.Drawing.Color.FromArgb(
                255,
                random.Next(0, 256),
                random.Next(0, 256),
                random.Next(0, 256));
        }
    }
}