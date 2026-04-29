using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Tasks.Offline;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.Jobs
{
    // [SampleInfo(ApiKeyRequired = true)]
    public partial class OfflineMapAreasSample : UserControl
    {
        // (Preplanned) Naperville water network - It has 4 map areas 
        string naperville_preplanned = "https://www.arcgis.com/home/item.html?id=acc027394bc84c2fb04d1ed317aac674";

        // (On-demand) US Breweries for Offline Testing
        // It doesn't have preplanned map areas
        // When creating a download extent, try to specify a smaller extent to avoid the export error caused by the max tile export limit on the service
        string usBreweries = "https://www.arcgis.com/home/item.html?id=3da658f2492f4cfd8494970ef489d2c5";

        // (On-demand) Naperville water network with less layers
        // It doesn't have preplanned map areas
        string naperville_ondemand = "https://www.arcgis.com/home/item.html?id=b95fe18073bc4f7788f0375af2bb445e";

        public OfflineMapAreasSample()
        {
            InitializeComponent();
            OfflineMapAreasView.OnlineMap = new Map(new Uri(naperville_preplanned));
        }

        //private async void GenerateGdb_Click(object sender, System.Windows.RoutedEventArgs e)
        //{
        //    var url = new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/Sync/WildfireSync/FeatureServer");
        //    var job = await CreateGenerateGeodatabaseJob(url: url, syncModel: SyncModel.Layer);
        //    JobManager.Shared.Jobs.Add(job);
        //    job.Start();
        //}

        //private async void GenerateMap_Click(object sender, System.Windows.RoutedEventArgs e)
        //{
        //    var item = await Esri.ArcGISRuntime.Portal.PortalItem.CreateAsync(new Uri("https://www.arcgis.com/home/item.html?id=acc027394bc84c2fb04d1ed317aac674"));

        //    var map = new Map(item);
        //    var naperville = new Envelope(-9813416.487598, 5126112.596989, -9812775.435463, 5127101.526749, SpatialReferences.WebMercator);
        //    var job = await CreateOfflineMapJob(map: map, extent: naperville);
        //    JobManager.Shared.Jobs.Add(job);
        //    job.Start();
        //}

        //private async Task<GenerateOfflineMapJob> CreateOfflineMapJob(Map map, Envelope extent)
        //{
        //    var task = await OfflineMapTask.CreateAsync(map);
        //    var parameters = await task.CreateDefaultGenerateOfflineMapParametersAsync(areaOfInterest: extent);
        //    var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Guid.NewGuid().ToString());
        //    return task.GenerateOfflineMap(parameters, path);
        //}


        //private async Task<GenerateGeodatabaseJob> CreateGenerateGeodatabaseJob(Uri url, SyncModel syncModel, Envelope? extent = null)
        //{
        //    var task = await GeodatabaseSyncTask.CreateAsync(url);

        //    var parameters = new GenerateGeodatabaseParameters();
        //    parameters.Extent = extent ?? task.ServiceInfo?.FullExtent;
        //    parameters.OutSpatialReference = extent?.SpatialReference ?? SpatialReferences.WebMercator;
        //    parameters.SyncModel = syncModel;


        //    if (syncModel == SyncModel.Layer)
        //    {
        //        foreach (var info in task.ServiceInfo!.LayerInfos)
        //        {
        //            parameters.LayerOptions.Add(new GenerateLayerOption(info.Id));
        //        }
        //    }

        //    var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Guid.NewGuid().ToString() + ".geodatabase");
        //    var job = task.GenerateGeodatabase(parameters, path);
        //    return job;
        //}

        //private void Clear_Click(object sender, System.Windows.RoutedEventArgs e)
        //{
        //    foreach (var job in JobManager.Shared.Jobs.ToList())
        //    {
        //        if (job.Status == Tasks.JobStatus.Failed ||
        //            job.Status == Tasks.JobStatus.Succeeded)
        //        {
        //            JobManager.Shared.Jobs.Remove(job);
        //        }
        //    }
        //}
    }
}
