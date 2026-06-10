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
using Esri.ArcGISRuntime.Toolkit.Internal;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    internal enum PreplannedMapModelStatus
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
    internal sealed partial class PreplannedMapModel : OfflineBindableObject, IOfflineMapAreaItem
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
        private bool _isOpen;
        private readonly DelegateCommand _downloadCommand;
        private readonly DelegateCommand _removeDownloadCommand;
        private readonly DelegateCommand _stopDownloadCommand;
        private readonly System.Windows.Input.ICommand _openMapCommand;

        internal PreplannedMapModel(
            Func<Task<OfflineMapTask>> offlineMapTaskFactory,
            PreplannedMapArea preplannedMapArea,
            string portalItemId,
            string preplannedMapAreaId,
            Action<PreplannedMapModel> onRemoveDownload,
            System.Windows.Input.ICommand openMapCommand,
            Action<Action> dispatcher) : this(offlineMapTaskFactory, portalItemId, preplannedMapAreaId,
                preplannedMapArea.PortalItem?.Title ?? preplannedMapAreaId, preplannedMapArea.PortalItem?.Description, true, onRemoveDownload, openMapCommand,  dispatcher)
        {
            _preplannedMapArea = preplannedMapArea;
        }

        internal PreplannedMapModel(
            Func<Task<OfflineMapTask>> offlineMapTaskFactory,
            string portalItemId,
            string preplannedMapAreaId,
            string title,
            string? description,
            byte[]? thumbnailData,
            bool supportsRedownloading,
            Action<PreplannedMapModel> onRemoveDownload,
            System.Windows.Input.ICommand openMapCommand,
            Action<Action> dispatcher) : this(offlineMapTaskFactory, portalItemId, preplannedMapAreaId, title, description, supportsRedownloading, onRemoveDownload, openMapCommand, dispatcher)
        {
            _thumbnailData = thumbnailData;
        }

        private PreplannedMapModel(
            Func<Task<OfflineMapTask>> offlineMapTaskFactory,
            string portalItemId, string preplannedMapAreaId,
            string title, string? description,
            bool supportsRedownloading, Action<PreplannedMapModel> onRemoveDownload,
            System.Windows.Input.ICommand openMapCommand,
            Action<Action> dispatcher): base(dispatcher)
        {
            _offlineMapTaskFactory = offlineMapTaskFactory;
            _portalItemId = portalItemId;
            _preplannedMapAreaId = preplannedMapAreaId;
            _title = title;
            _description = HtmlUtility.StripHtml(description);
            _supportsRedownloading = supportsRedownloading;
            _downloadCommand = new DelegateCommand((o) => _ = DownloadPreplannedMapAreaAsync(), () => _preplannedMapArea != null && AllowsDownload && Status != PreplannedMapModelStatus.Downloading);
            _removeDownloadCommand = new DelegateCommand((o) => RemoveDownloadedArea(), () => IsDownloaded && !IsOpen || Error is not null);
            _stopDownloadCommand = new DelegateCommand((o) => Job?.CancelAsync());
            _openMapCommand = openMapCommand;
            _onRemoveDownloadAction = onRemoveDownload;
            _mmpkDirectoryPath = OfflineMapAreaStorage.GetPreplannedAreaDirectory(portalItemId, preplannedMapAreaId);
        }

        public string PreplannedMapAreaId => _preplannedMapAreaId;

        public string Title
        {
            get => _title;
            private set => SetProperty(ref _title, value);
        }

        public string Description
        {
            get => _description ?? string.Empty;
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
            private set
            {
                if (_job is not null)
                    _job.ProgressChanged -= JobProgressChanged;
                SetProperty(ref _job, value);
                OnPropertyChanged(nameof(IOfflineMapAreaItem.DownloadProgress));
                if (_job is not null)
                    _job.ProgressChanged += JobProgressChanged;
            }
        }

        private void JobProgressChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IOfflineMapAreaItem.DownloadProgress));
        }

        public PreplannedMapModelStatus Status
        {
            get => _status;
            private set
            {
                SetProperty(ref _status, value);
                Dispatcher.Invoke(() =>
                {
                    _downloadCommand.NotifyCanExecuteChanged();
                    _removeDownloadCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(IOfflineMapAreaItem.IsDownloading));
                    OnPropertyChanged(nameof(IOfflineMapAreaItem.IsDownloaded));
                    OnPropertyChanged(nameof(IOfflineMapAreaItem.AllowsDownload));
                });
            }
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

        public bool AllowsDownload => Status == PreplannedMapModelStatus.Packaged;

        public bool IsDownloaded => Status == PreplannedMapModelStatus.Downloaded;

        public bool IsOpen
        {
            get { return _isOpen; }
            internal set
            {
                if (_isOpen != value)
                {
                    SetProperty(ref _isOpen, value);
                    _removeDownloadCommand.NotifyCanExecuteChanged();
                }
            }
        }


        public System.Windows.Input.ICommand DownloadCommand => _downloadCommand;
        public System.Windows.Input.ICommand RemoveDownloadCommand => _removeDownloadCommand;
        public System.Windows.Input.ICommand StopDownloadCommand => _stopDownloadCommand;
        public System.Windows.Input.ICommand OpenCommand => _openMapCommand;

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
            Dispatcher.Invoke(() => _downloadCommand.NotifyCanExecuteChanged());
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
                var offlineMapTask = await _offlineMapTaskFactory();
                var parameters = await offlineMapTask.CreateDefaultDownloadPreplannedOfflineMapParametersAsync(_preplannedMapArea);
                parameters.UpdateMode = OfflineManager.Shared.Configuration.PreplannedUpdateMode;
                parameters.ContinueOnErrors = false;

                OfflineMapAreaUtilities.TryDeleteDirectory(_mmpkDirectoryPath);
                Directory.CreateDirectory(_mmpkDirectoryPath);

                var job = offlineMapTask.DownloadPreplannedOfflineMap(parameters, _mmpkDirectoryPath);
                var portalItem = offlineMapTask.PortalItem ?? throw new InvalidOperationException("The offline map task does not reference a portal item.");
                await OfflineManager.Shared.StartJobAsync(job, portalItem, Title);
                ObserveJob(job);
            }
            catch (Exception ex)
            {
                Error = ex;
                Status = PreplannedMapModelStatus.DownloadFailure;
            }
            Dispatcher.Invoke(() => _downloadCommand.NotifyCanExecuteChanged());
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

        public bool IsDownloading => Status == PreplannedMapModelStatus.Downloading;

        public double DownloadProgress => Job is null ? 0d : Job.Progress / 100d;

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
                Description = HtmlUtility.StripHtml(_preplannedMapArea.PortalItem?.Description) ?? Description;
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
                Description = HtmlUtility.StripHtml(mobileMapPackage.Item?.Description) ?? Description;
                ThumbnailData ??= await OfflineMapAreaUtilities.LoadImageBytesAsync(mobileMapPackage.Item?.Thumbnail).ConfigureAwait(false);
                Error = null;
                Status = PreplannedMapModelStatus.Downloaded;
            }
            catch (Exception ex)
            {
                Error = ex;
                mobileMapPackage.Close();
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
            Action<PreplannedMapModel> onRemoveDownload, System.Windows.Input.ICommand openCommand, Action<Action> dispatcher)
        {
            var offlineModels = await LoadOfflinePreplannedMapModelsAsync(offlineMapTaskFactory, portalItemId, onRemoveDownload, openCommand, dispatcher).ConfigureAwait(false);

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
                        onRemoveDownload, openCommand, dispatcher))
                    .ToList();

                await Task.WhenAll(models.Select(m => m.LoadAsync())).ConfigureAwait(false);

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
            Action<PreplannedMapModel> onRemoveDownload, System.Windows.Input.ICommand openCommand, Action<Action> dispatcher)
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

                MobileMapPackage? mobileMapPackage = null;
                try
                {
                    mobileMapPackage = new MobileMapPackage(mapAreaDirectory);
                    await mobileMapPackage.LoadAsync().ConfigureAwait(false);

                    var model = new PreplannedMapModel(
                        offlineMapTaskFactory,
                        portalItemId,
                        preplannedMapAreaId,
                        mobileMapPackage.Item?.Title ?? preplannedMapAreaId,
                        HtmlUtility.StripHtml(mobileMapPackage.Item?.Description),
                        await OfflineMapAreaUtilities.LoadImageBytesAsync(mobileMapPackage.Item?.Thumbnail).ConfigureAwait(false),
                        false,
                        onRemoveDownload, openCommand, dispatcher);

                    await model.LoadAndUpdateMobileMapPackageAsync(mobileMapPackage).ConfigureAwait(false);
                    models.Add(model);
                }
                catch
                {
                    // Skip invalid local packages.
                    mobileMapPackage?.Close();
                }
            }

            return models.OrderBy(model => model.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
        }
    }
}

#pragma warning restore SA1402
#pragma warning restore CS1591
