using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Offline;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Jobs
{
    [SampleInfo(ApiKeyRequired = true)]
    public sealed partial class OfflineMapAreasSample : Page
    {
        // (Preplanned) Naperville water network - It has 4 map areas 
        string naperville_preplanned = "https://www.arcgis.com/home/item.html?id=acc027394bc84c2fb04d1ed317aac674";

        // (On-demand) US Breweries for Offline Testing
        // It doesn't have preplanned map areas
        // When creating a download extent, try to specify a smaller extent to avoid the export error caused by the max tile export limit on the service
        string usBreweries = "https://www.arcgis.com/home/item.html?id=3da658f2492f4cfd8494970ef489d2c5";

        // No offline support
        string naperville_ondemand = "https://www.arcgis.com/home/item.html?id=b95fe18073bc4f7788f0375af2bb445e";

        public OfflineMapAreasSample()
        {
            Maps = new List<MapAreaViewModel>();
            Maps.Add(new MapAreaViewModel() { Name = "Naperville - Preplanned", Map = new Map(new Uri(naperville_preplanned)) });
            Maps.Add(new MapAreaViewModel() { Name = "US Breweries - On-Demand", Map = new Map(new Uri(usBreweries)) });
            Maps.Add(new MapAreaViewModel() { Name = "Naperville - Offline Disabled", Map = new Map(new Uri(naperville_ondemand)) });
            Maps.Add(new MapAreaViewModel() { Name = "No Map", Map = null! });
            this.InitializeComponent();
        }

        public List<MapAreaViewModel> Maps { get; }

        public ReadOnlyObservableCollection<OfflineMapInfo> OfflineMapInfos => Esri.ArcGISRuntime.Toolkit.OfflineManager.Shared.OfflineMapInfos;

        private void OnlineMapSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox)?.SelectedItem is MapAreaViewModel mapVM)
            {
                OfflineMapAreasView.OnlineMap = mapVM.Map;
                OfflineMapSelector.SelectedItem = null;
            }
        }

        private void OfflineMapSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox)?.SelectedItem is OfflineMapInfo offlineMapInfo)
            {
                OfflineMapAreasView.OfflineMapInfo = offlineMapInfo;
                MapSelector.SelectedItem = null;
            }
        }

        public static ImageSource? BytesToImage(byte[]? imageData)
        {
            if (imageData is null || imageData.Length == 0)
                return null;
            var bmi = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
            using var ms = new MemoryStream(imageData);
            bmi.SetSource(ms.AsRandomAccessStream());
            return bmi;
        }
    }
    public class MapAreaViewModel
    {
        public string Name { get; set; }
        public Map Map { get; set; }
        public override string ToString() => Name;
    }
}
