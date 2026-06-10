using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Tasks.Offline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.Jobs
{
    [SampleInfo(ApiKeyRequired = true)]
    public partial class OfflineMapAreasSample : UserControl
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
            // You can change the cache folder that manages downloaded data and pending jobs
            // OfflineManager.Shared.CacheFolder = "e:\\temp\\offlinecache\\";
            InitializeComponent();
            Maps = new List<MapAreaViewModel>();
            Maps.Add(new MapAreaViewModel() { Name = "Naperville - Preplanned", Map = new Map(new Uri(naperville_preplanned)) });
            Maps.Add(new MapAreaViewModel() { Name = "US Breweries - On-Demand", Map = new Map(new Uri(usBreweries)) });
            Maps.Add(new MapAreaViewModel() { Name = "Naperville - Offline Disabled", Map = new Map(new Uri(naperville_ondemand)) });
            Maps.Add(new MapAreaViewModel() { Name = "No Map", Map = null! });
            this.DataContext = this;
        }

        public List<MapAreaViewModel> Maps { get; }
    }

    public class MapAreaViewModel
    {
        public string Name { get; set; }
        public Map Map { get; set; }
        public override string ToString() => Name;
    }
}
