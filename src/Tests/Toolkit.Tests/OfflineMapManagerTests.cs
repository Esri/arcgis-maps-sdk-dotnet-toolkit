using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Toolkit.Tests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class OfflineMapManagerTests
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

        public static void CloseVM(OfflineMapViewModel vm)
        {
            foreach (var item in vm.PreplannedMapModels.ToArray())
            {
                item.RemoveDownloadedArea();
            }
            foreach (var item in vm.OnDemandMapModels.ToArray())
            {
                item.RemoveDownloadedArea();
            }
            OfflineManager.Shared.Jobs.Select(j => _ = j.CancelAsync()).ToList();
            OfflineManager.Shared.Jobs.Clear();
            OfflineManager.Shared.RemoveAllDownloads();
        }

        [TestMethod]
        public async Task PortalItemMustHaveID()
        {
            var map = new Map(new Uri(naperville_preplanned));
            Assert.ThrowsExactly<ArgumentException>(() => new OfflineMapViewModel(map));
        }

        [TestMethod]
        public async Task CreateVMFromUnloadedMapWithItem()
        {
            var item = await Esri.ArcGISRuntime.Portal.PortalItem.CreateAsync(new Uri(naperville_preplanned));
            var vm = new OfflineMapViewModel(new Map(item));
            CloseVM(vm);
        }

        [TestMethod]
        public async Task LoadPreplannedModels()
        {
            var map = new Map(new Uri(naperville_preplanned));
            await map.LoadAsync();
            var vm = new OfflineMapViewModel(map);
            try
            {
                Assert.AreEqual(map, vm.OnlineMap);
                Assert.IsEmpty(vm.OnDemandMapModels);
                Assert.IsEmpty(vm.PreplannedMapModels);
                Assert.IsFalse(vm.IsLoadingModels);
                var task = vm.LoadModelsAsync();
                Assert.IsTrue(vm.IsLoadingModels);
                await task;
                Assert.IsFalse(vm.MapIsOfflineDisabled);
                Assert.IsEmpty(vm.OnDemandMapModels);
                Assert.HasCount(4, vm.PreplannedMapModels);
            }
            finally
            {
                CloseVM(vm);
            }
        }

        [TestMethod]
        public async Task LoadOnDemandModels()
        {
            var map = new Map(new Uri(naperville_ondemand));
            await map.LoadAsync();
            var vm = new OfflineMapViewModel(map);
            try
            {
                Assert.AreEqual(map, vm.OnlineMap);
                Assert.IsEmpty(vm.OnDemandMapModels);
                Assert.IsEmpty(vm.PreplannedMapModels);
                await vm.LoadModelsAsync();
                Assert.IsTrue(vm.MapIsOfflineDisabled);
                Assert.IsEmpty(vm.OnDemandMapModels);
                Assert.IsEmpty(vm.PreplannedMapModels);
            }
            finally
            {
                CloseVM(vm);
            }
        }

        [TestMethod]
        public async Task LoadUSBreweriesModel()
        {
            var map = new Map(new Uri(usBreweries));
            await map.LoadAsync();
            var vm = new OfflineMapViewModel(map);
            try
            {
                Assert.AreEqual(map, vm.OnlineMap);
                Assert.IsEmpty(vm.OnDemandMapModels);
                Assert.IsEmpty(vm.PreplannedMapModels);
                await vm.LoadModelsAsync();
                Assert.IsFalse(vm.MapIsOfflineDisabled);
                Assert.IsEmpty(vm.OnDemandMapModels);
                Assert.IsEmpty(vm.PreplannedMapModels);
            }
            finally
            {
                CloseVM(vm);
            }
        }

        [TestMethod]
        public async Task DownloadPreplannedArea()
        {
            var map = new Map(new Uri(naperville_preplanned));
            await map.LoadAsync();
            var vm = new OfflineMapViewModel(map);
            try
            {
                await vm.LoadModelsAsync();
                if (vm.PreplannedMapModels[0].Status == PreplannedMapModelStatus.Downloaded)
                {
                    vm.PreplannedMapModels[0].RemoveDownloadedArea();
                }
                if (vm.PreplannedMapModels[0].AllowsDownload == false)
                {
                    System.Diagnostics.Debugger.Break();
                }
                Assert.IsTrue(vm.PreplannedMapModels[0].AllowsDownload);
                Assert.IsFalse(vm.PreplannedMapModels[0].IsDownloaded);
                Assert.IsNull(vm.PreplannedMapModels[0].Map);
                Assert.IsNull(vm.PreplannedMapModels[0].Job);
                Assert.IsNull(vm.PreplannedMapModels[0].MobileMapPackage);
                Assert.IsNotNull(vm.PreplannedMapModels[0].Description);
                Assert.IsNull(vm.PreplannedMapModels[0].Error);
                Assert.IsTrue(vm.PreplannedMapModels[0].SupportsRedownloading);
                Assert.AreEqual("City Hall Area", vm.PreplannedMapModels[0].Title);
                Assert.IsNotNull(vm.PreplannedMapModels[0].ThumbnailData);
                Assert.AreEqual(PreplannedMapModelStatus.Packaged, vm.PreplannedMapModels[0].Status);
                Assert.IsGreaterThan(0, vm.PreplannedMapModels[0].SizeInBytes);
                await vm.PreplannedMapModels[0].DownloadPreplannedMapAreaAsync();
                Assert.AreEqual(PreplannedMapModelStatus.Downloading, vm.PreplannedMapModels[0].Status);
                Assert.IsNotNull(vm.PreplannedMapModels[0].Job);
                await AwaitDownload(vm.PreplannedMapModels[0]);
                if (vm.PreplannedMapModels[0].Error is not null)
                {
                    System.Diagnostics.Debugger.Break();
                }
                Assert.AreEqual(PreplannedMapModelStatus.Downloaded, vm.PreplannedMapModels[0].Status);
                Assert.IsNull(vm.PreplannedMapModels[0].Error);
                Assert.IsTrue(vm.PreplannedMapModels[0].IsDownloaded);
                Assert.IsNotNull(vm.PreplannedMapModels[0].Map);
                Assert.IsNotNull(vm.PreplannedMapModels[0].MobileMapPackage);
            }
            finally
            {
                CloseVM(vm);
            }

        }

        [TestMethod]
        public async Task AddAndDownloadOnDemandArea()
        {
            var map = new Map(new Uri(naperville_ondemand));
            await map.LoadAsync();
            var vm = new OfflineMapViewModel(map);
            await vm.AddOnDemandMapAreaAsync(
                new OnDemandMapAreaConfiguration(
                    "Redlands",
                    new Envelope(-88.15127312743896, 41.771367718680104, -88.14870389797277, 41.76880460601543, SpatialReferences.Wgs84), 10000, 1000, null));
            try
            {
                Assert.HasCount(1, vm.OnDemandMapModels);
                Assert.IsNull(vm.OnDemandMapModels[0].Error);
                Assert.IsNull(vm.OnDemandMapModels[0].Map);
                Assert.IsNotNull(vm.OnDemandMapModels[0].Job);
                Assert.IsNull(vm.OnDemandMapModels[0].MobileMapPackage);
                Assert.IsNotNull(vm.OnDemandMapModels[0].AreaId);
                Assert.IsNull(vm.OnDemandMapModels[0].Map);
                Assert.IsFalse(vm.OnDemandMapModels[0].AllowsDownload);
                Assert.IsFalse(vm.OnDemandMapModels[0].IsDownloaded);
                Assert.AreEqual(OnDemandMapModelStatus.Downloading, vm.OnDemandMapModels[0].Status);
                Assert.AreEqual("Redlands", vm.OnDemandMapModels[0].Title);
                await AwaitDownload(vm.OnDemandMapModels[0]);
                Assert.IsTrue(vm.OnDemandMapModels[0].IsDownloaded);
            }
            finally
            {
                CloseVM(vm);
            }
        }

        [TestMethod]
        public async Task AddOnDemandAreaError()
        {
            var map = new Map(new Uri(usBreweries));
            await map.LoadAsync();
            var vm = new OfflineMapViewModel(map);
            try
            {
                await vm.AddOnDemandMapAreaAsync(
                    new OnDemandMapAreaConfiguration(
                        "Redlands",
                        new Envelope(-117.2697, 33.9823, -117.0953, 34.1272, SpatialReferences.Wgs84), 10000, 1000000, null));
                Assert.HasCount(1, vm.OnDemandMapModels);
                Assert.IsNotNull(vm.OnDemandMapModels[0].Error); // Max scale is less than min scale will cause an error
            }
            finally
            {
                CloseVM(vm);
            }
        }

        private static async Task AwaitDownload(PreplannedMapModel model)
        {
            TaskCompletionSource downloadCompletionSource = new TaskCompletionSource();
            System.ComponentModel.PropertyChangedEventHandler? handler = null;
            handler = (s, e) =>
            {
                if (e.PropertyName == nameof(PreplannedMapModel.Status) || model.Status == PreplannedMapModelStatus.Downloaded)
                {
                    downloadCompletionSource.TrySetResult();
                }
                else if (e.PropertyName == nameof(PreplannedMapModel.Error) && model.Error != null)
                {
                    downloadCompletionSource.TrySetException(model.Error);
                }
            };
            model.PropertyChanged += handler;
            try
            {
                await downloadCompletionSource.Task;
            }
            finally
            {
                model.PropertyChanged -= handler;
            }
        }

        private static async Task AwaitDownload(OnDemandMapModel model)
        {
            TaskCompletionSource downloadCompletionSource = new TaskCompletionSource();
            System.ComponentModel.PropertyChangedEventHandler? handler = null;
            handler = (s, e) =>
            {
                if (e.PropertyName == nameof(OnDemandMapModel.Status))
                {
                    if (model.Status == OnDemandMapModelStatus.Downloaded)
                    {
                        downloadCompletionSource.TrySetResult();
                    }
                    else if (model.Status == OnDemandMapModelStatus.DownloadCancelled)
                    {
                        downloadCompletionSource.TrySetCanceled();
                    }
                }
                else if (e.PropertyName == nameof(OnDemandMapModel.Error) && model.Error != null)
                {
                    downloadCompletionSource.TrySetException(model.Error);
                }
            };
            model.PropertyChanged += handler;
            try
            {
                await downloadCompletionSource.Task;
            }
            finally
            {
                model.PropertyChanged -= handler;
            }
        }
    }
}