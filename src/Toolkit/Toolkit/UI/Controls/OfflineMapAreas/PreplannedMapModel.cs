// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

#nullable enable

#pragma warning disable CS1591
#pragma warning disable SA1402

using System.IO;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Tasks.Offline;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    public enum PreplannedMapModelStatus
    {
        NotLoaded,
        Loading,
        LoadFailure,
        Packaging,
        Packaged,
        PackageFailure,
        Downloading,
        Downloaded,
        DownloadFailure,
        MmpkLoadFailure
    }

    internal readonly struct PreplannedMapModelsLoadResult
    {
        public PreplannedMapModelsLoadResult(IReadOnlyList<PreplannedMapModel> models, bool onlyOfflineModelsAreAvailable, Exception? error)
        {
            Models = models;
            OnlyOfflineModelsAreAvailable = onlyOfflineModelsAreAvailable;
            Error = error;
        }

        public IReadOnlyList<PreplannedMapModel> Models { get; }

        public bool OnlyOfflineModelsAreAvailable { get; }

        public Exception? Error { get; }
    }

#if WINUI
    [WinRT.GeneratedBindableCustomProperty]
#endif
    public sealed partial class PreplannedMapModel : OfflineBindableObject
    {
        private readonly Func<Task<OfflineMapTask>> _offlineMapTaskFactory;
        private readonly Action<PreplannedMapModel> _onRemoveDownloadAction;
        private readonly string _portalItemId;
        private readonly string _preplannedMapAreaId;
        private readonly string _mmpkDirectoryPath;
        private readonly PreplannedMapArea? _preplannedMapArea;
        private string _title;
        private string? _description;
        private byte[]? _thumbnailData;
        private long _sizeInBytes;
        private DownloadPreplannedOfflineMapJob? _job;
        private PreplannedMapModelStatus _status = PreplannedMapModelStatus.NotLoaded;
        private MobileMapPackage? _mobileMapPackage;
        private Map? _map;
        private Exception? _error;
        private bool _supportsRedownloading;

        internal PreplannedMapModel(
            Func<Task<OfflineMapTask>> offlineMapTaskFactory,
            PreplannedMapArea preplannedMapArea,
            string portalItemId,
            string preplannedMapAreaId,
            Action<PreplannedMapModel> onRemoveDownload)
        {
            _offlineMapTaskFactory = offlineMapTaskFactory;
            _preplannedMapArea = preplannedMapArea;
            _portalItemId = portalItemId;
            _preplannedMapAreaId = preplannedMapAreaId;
            _onRemoveDownloadAction = onRemoveDownload;
            _mmpkDirectoryPath = OfflineMapAreaStorage.GetPreplannedAreaDirectory(portalItemId, preplannedMapAreaId);
            _title = preplannedMapArea.PortalItem?.Title ?? preplannedMapAreaId;
            _description = OfflineMapAreaUtilities.StripHtml(preplannedMapArea.PortalItem?.Description);
            _supportsRedownloading = true;
        }

        internal PreplannedMapModel(
            Func<Task<OfflineMapTask>> offlineMapTaskFactory,
            string portalItemId,
            string preplannedMapAreaId,
            string title,
            string? description,
            byte[]? thumbnailData,
            bool supportsRedownloading,
            Action<PreplannedMapModel> onRemoveDownload)
        {
            _offlineMapTaskFactory = offlineMapTaskFactory;
            _portalItemId = portalItemId;
            _preplannedMapAreaId = preplannedMapAreaId;
            _title = title;
            _description = description;
            _thumbnailData = thumbnailData;
            _supportsRedownloading = supportsRedownloading;
            _onRemoveDownloadAction = onRemoveDownload;
            _mmpkDirectoryPath = OfflineMapAreaStorage.GetPreplannedAreaDirectory(portalItemId, preplannedMapAreaId);
        }

        public string PreplannedMapAreaId => _preplannedMapAreaId;

        public string Title
        {
            get => _title;
            private set => SetProperty(ref _title, value);
        }

        public string? Description
        {
            get => _description;
            private set => SetProperty(ref _description, value);
        }

        public byte[]? ThumbnailData
        {
            get => _thumbnailData;
            private set => SetProperty(ref _thumbnailData, value);
        }

        public long SizeInBytes
        {
            get => _sizeInBytes;
            private set => SetProperty(ref _sizeInBytes, value);
        }

        public DownloadPreplannedOfflineMapJob? Job
        {
            get => _job;
            private set => SetProperty(ref _job, value);
        }

        public PreplannedMapModelStatus Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        public MobileMapPackage? MobileMapPackage
        {
            get => _mobileMapPackage;
            private set => SetProperty(ref _mobileMapPackage, value);
        }

        public Map? Map
        {
            get => _map;
            private set => SetProperty(ref _map, value);
        }

        public Exception? Error
        {
            get => _error;
            private set => SetProperty(ref _error, value);
        }

        public bool SupportsRedownloading
        {
            get => _supportsRedownloading;
            private set => SetProperty(ref _supportsRedownloading, value);
        }

        public bool AllowsDownload => Status == PreplannedMapModelStatus.Packaged || Status == PreplannedMapModelStatus.DownloadFailure;

        public bool IsDownloaded => Status == PreplannedMapModelStatus.Downloaded;

        public async Task LoadAsync()
        {
            if (Job is null && TryLookupDownloadJob() is DownloadPreplannedOfflineMapJob existingJob)
            {
                ObserveJob(existingJob);
            }
            else if (MobileMapPackage is null && Directory.Exists(_mmpkDirectoryPath) && Directory.GetFiles(_mmpkDirectoryPath).Any())
            {
                await LoadAndUpdateMobileMapPackageAsync(new MobileMapPackage(_mmpkDirectoryPath)).ConfigureAwait(false);
            }
            else if (CanLoadPreplannedMapArea)
            {
                await LoadPreplannedMapAreaAsync().ConfigureAwait(false);
            }
        }

        public async Task DownloadPreplannedMapAreaAsync()
        {
            if (_preplannedMapArea is null)
            {
                throw new InvalidOperationException("The preplanned map area is not available for download.");
            }

            if (!AllowsDownload || !SupportsRedownloading)
            {
                throw new InvalidOperationException("The preplanned map area is not in a downloadable state.");
            }

            Status = PreplannedMapModelStatus.Downloading;
            Error = null;

            try
            {
                var offlineMapTask = await _offlineMapTaskFactory().ConfigureAwait(false);
                var parameters = await offlineMapTask.CreateDefaultDownloadPreplannedOfflineMapParametersAsync(_preplannedMapArea).ConfigureAwait(false);
                parameters.UpdateMode = OfflineManager.Shared.Configuration.PreplannedUpdateMode;
                parameters.ContinueOnErrors = false;

                if (Directory.Exists(_mmpkDirectoryPath))
                {
                }
                Directory.CreateDirectory(_mmpkDirectoryPath);

                var job = offlineMapTask.DownloadPreplannedOfflineMap(parameters, _mmpkDirectoryPath);
                var portalItem = offlineMapTask.PortalItem ?? throw new InvalidOperationException("The offline map task does not reference a portal item.");
                await OfflineManager.Shared.StartJobAsync(job, portalItem, Title).ConfigureAwait(false);
                ObserveJob(job);
            }
            catch (Exception ex)
            {
                Error = ex;
                Status = PreplannedMapModelStatus.DownloadFailure;
            }
        }

        public void RemoveDownloadedArea()
        {
            MobileMapPackage?.Close();
            OfflineMapAreaUtilities.TryDeleteDirectory(_mmpkDirectoryPath);
            MobileMapPackage = null;
            Map = null;
            SizeInBytes = 0;
            Error = null;
            Status = PreplannedMapModelStatus.NotLoaded;
            _ = LoadAsync();
            _onRemoveDownloadAction(this);
        }

        public async Task CancelJobAsync()
        {
            if (Job is not null)
            {
                await Job.CancelAsync().ConfigureAwait(false);
            }
        }

        private bool CanLoadPreplannedMapArea =>
            _preplannedMapArea is not null &&
            (Status == PreplannedMapModelStatus.NotLoaded ||
             Status == PreplannedMapModelStatus.LoadFailure ||
             Status == PreplannedMapModelStatus.PackageFailure);

        private async Task LoadPreplannedMapAreaAsync()
        {
            if (_preplannedMapArea is null)
            {
                return;
            }

            Status = PreplannedMapModelStatus.Loading;
            Error = null;

            try
            {
                await _preplannedMapArea.RetryLoadAsync().ConfigureAwait(false);
                Title = _preplannedMapArea.PortalItem?.Title ?? Title;
                Description = OfflineMapAreaUtilities.StripHtml(_preplannedMapArea.PortalItem?.Description) ?? Description;
                ThumbnailData ??= await OfflineMapAreaUtilities.LoadImageBytesAsync(_preplannedMapArea.PortalItem?.Thumbnail).ConfigureAwait(false);
                SizeInBytes = _preplannedMapArea.PackageItems.Sum(static item => item.Size);
                SupportsRedownloading = true;
                Status = _preplannedMapArea.PackagingStatus switch
                {
                    PreplannedPackagingStatus.Complete => PreplannedMapModelStatus.Packaged,
                    PreplannedPackagingStatus.Processing => PreplannedMapModelStatus.Packaging,
                    PreplannedPackagingStatus.Failed => PreplannedMapModelStatus.PackageFailure,
                    _ => PreplannedMapModelStatus.Packaged
                };
            }
            catch (Exception ex)
            {
                Error = ex;
                Status = _preplannedMapArea.PackagingStatus == PreplannedPackagingStatus.Failed
                    ? PreplannedMapModelStatus.PackageFailure
                    : PreplannedMapModelStatus.LoadFailure;
            }
        }

        private async Task LoadAndUpdateMobileMapPackageAsync(MobileMapPackage mobileMapPackage)
        {
            try
            {
                await mobileMapPackage.LoadAsync().ConfigureAwait(false);
                MobileMapPackage = mobileMapPackage;
                Map = mobileMapPackage.Maps.FirstOrDefault();
                SizeInBytes = OfflineMapAreaUtilities.GetDirectorySize(_mmpkDirectoryPath);
                Title = mobileMapPackage.Item?.Title ?? Title;
                Description = OfflineMapAreaUtilities.StripHtml(mobileMapPackage.Item?.Description) ?? Description;
                ThumbnailData ??= await OfflineMapAreaUtilities.LoadImageBytesAsync(mobileMapPackage.Item?.Thumbnail).ConfigureAwait(false);
                Error = null;
                Status = PreplannedMapModelStatus.Downloaded;
            }
            catch (Exception ex)
            {
                Error = ex;
                MobileMapPackage = null;
                Map = null;
                SizeInBytes = 0;
                Status = PreplannedMapModelStatus.MmpkLoadFailure;
            }
        }

        private DownloadPreplannedOfflineMapJob? TryLookupDownloadJob()
        {
            var expectedPath = Path.GetFullPath(_mmpkDirectoryPath);
            return OfflineManager.Shared.Jobs
                .OfType<DownloadPreplannedOfflineMapJob>()
                .FirstOrDefault(job => string.Equals(Path.GetFullPath(job.DownloadDirectoryPath), expectedPath, StringComparison.OrdinalIgnoreCase));
        }

        private void ObserveJob(DownloadPreplannedOfflineMapJob job)
        {
            Job = job;
            Error = null;
            Status = PreplannedMapModelStatus.Downloading;
            _ = ObserveJobAsync(job);
        }

        private async Task ObserveJobAsync(DownloadPreplannedOfflineMapJob job)
        {
            try
            {
                var result = (DownloadPreplannedOfflineMapResult)await job.GetResultAsync().ConfigureAwait(false);
                Error = null;
                if (result.MobileMapPackage is not null)
                {
                    await LoadAndUpdateMobileMapPackageAsync(result.MobileMapPackage).ConfigureAwait(false);
                }
                Status = PreplannedMapModelStatus.Downloaded;
            }
            catch (Exception ex)
            {
                if (OfflineMapAreaUtilities.IsCancellation(job, ex))
                {
                    Error = null;
                    Status = SupportsRedownloading ? PreplannedMapModelStatus.Packaged : PreplannedMapModelStatus.NotLoaded;
                }
                else
                {
                    Error = job.Error ?? ex;
                    Status = PreplannedMapModelStatus.DownloadFailure;
                }

                OfflineMapAreaUtilities.TryDeleteDirectory(_mmpkDirectoryPath);
            }
            finally
            {
                Job = null;
            }
        }

        internal static async Task<PreplannedMapModelsLoadResult> LoadPreplannedMapModelsAsync(
            Func<Task<OfflineMapTask>> offlineMapTaskFactory,
            string portalItemId,
            Action<PreplannedMapModel> onRemoveDownload)
        {
            var offlineModels = await LoadOfflinePreplannedMapModelsAsync(offlineMapTaskFactory, portalItemId, onRemoveDownload).ConfigureAwait(false);

            try
            {
                var offlineMapTask = await offlineMapTaskFactory().ConfigureAwait(false);
                var mapAreas = await offlineMapTask.GetPreplannedMapAreasAsync().ConfigureAwait(false);
                var models = mapAreas
                    .Where(area => !string.IsNullOrWhiteSpace(area.PortalItem?.ItemId))
                    .OrderBy(area => area.PortalItem?.Title, StringComparer.CurrentCultureIgnoreCase)
                    .Select(area => new PreplannedMapModel(
                        offlineMapTaskFactory,
                        area,
                        portalItemId,
                        area.PortalItem!.ItemId!,
                        onRemoveDownload))
                    .ToList();

                foreach (var model in models)
                {
                    await model.LoadAsync().ConfigureAwait(false);
                }

                return new PreplannedMapModelsLoadResult(models, false, null);
            }
            catch (Exception ex)
            {
                var onlyOfflineModelsAreAvailable = offlineModels.Count > 0 || !OfflineMapAreaUtilities.IsNetworkAvailable();
                return new PreplannedMapModelsLoadResult(
                    offlineModels,
                    onlyOfflineModelsAreAvailable,
                    onlyOfflineModelsAreAvailable ? null : ex);
            }
        }

        private static async Task<IReadOnlyList<PreplannedMapModel>> LoadOfflinePreplannedMapModelsAsync(
            Func<Task<OfflineMapTask>> offlineMapTaskFactory,
            string portalItemId,
            Action<PreplannedMapModel> onRemoveDownload)
        {
            var directory = OfflineMapAreaStorage.GetPreplannedAreasDirectory(portalItemId);
            if (!Directory.Exists(directory))
            {
                return Array.Empty<PreplannedMapModel>();
            }

            var models = new List<PreplannedMapModel>();
            foreach (var mapAreaDirectory in Directory.GetDirectories(directory))
            {
                var preplannedMapAreaId = Path.GetFileName(mapAreaDirectory);
                if (string.IsNullOrWhiteSpace(preplannedMapAreaId))
                {
                    continue;
                }

                try
                {
                    var mobileMapPackage = new MobileMapPackage(mapAreaDirectory);
                    await mobileMapPackage.LoadAsync().ConfigureAwait(false);

                    var model = new PreplannedMapModel(
                        offlineMapTaskFactory,
                        portalItemId,
                        preplannedMapAreaId,
                        mobileMapPackage.Item?.Title ?? preplannedMapAreaId,
                        OfflineMapAreaUtilities.StripHtml(mobileMapPackage.Item?.Description),
                        await OfflineMapAreaUtilities.LoadImageBytesAsync(mobileMapPackage.Item?.Thumbnail).ConfigureAwait(false),
                        false,
                        onRemoveDownload);

                    await model.LoadAsync().ConfigureAwait(false);
                    models.Add(model);
                }
                catch
                {
                    // Skip invalid local packages.
                }
            }

            return models.OrderBy(model => model.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
        }
    }
}

#pragma warning restore SA1402
#pragma warning restore CS1591
