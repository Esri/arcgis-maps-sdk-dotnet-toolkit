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

using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
#if WINUI
    [WinRT.GeneratedBindableCustomProperty]
#endif
    internal sealed partial class OnDemandMapModel : OfflineBindableObject, IOfflineMapAreaItem
    {
        private readonly Func<Task<OfflineMapTask>>? _offlineMapTaskFactory;
        private readonly Action<OnDemandMapModel> _onRemoveDownloadAction;
        private readonly string _portalItemId;
        private readonly string _areaId;
        private readonly string _directoryPath;
        private readonly string _mmpkDirectoryPath;
        private readonly string _thumbnailPath;
        private readonly OnDemandMapAreaConfiguration? _configuration;
        private string _title;
        private byte[]? _thumbnailData;
        private long _sizeInBytes;
        private GenerateOfflineMapJob? _job;
        private OnDemandMapModelStatus _status = OnDemandMapModelStatus.Initialized;
        private MobileMapPackage? _mobileMapPackage;
        private Map? _map;
        private Exception? _error;
        private bool _isOpen;
        private readonly DelegateCommand _downloadCommand;
        private readonly DelegateCommand _removeDownloadCommand;
        private readonly DelegateCommand _stopDownloadCommand;
        private readonly ICommand _openMapCommand;

        internal OnDemandMapModel(
            Func<Task<OfflineMapTask>> offlineMapTaskFactory,
            OnDemandMapAreaConfiguration configuration,
            string portalItemId,
            Action<OnDemandMapModel> onRemoveDownload,
            ICommand openMapCommand,
            Action<Action> dispatcher) : this(configuration.AreaId, portalItemId, configuration.Title, configuration.ThumbnailData, onRemoveDownload, openMapCommand, dispatcher)
        {
            _offlineMapTaskFactory = offlineMapTaskFactory;
            _configuration = configuration;
            if (_thumbnailData is { Length: > 0 })
            {
                Directory.CreateDirectory(_directoryPath);
                File.WriteAllBytes(_thumbnailPath, _thumbnailData);
            }
        }

        internal OnDemandMapModel(
            GenerateOfflineMapJob job,
            string areaId,
            string portalItemId,
            Action<OnDemandMapModel> onRemoveDownload,
            ICommand openMapCommand,
            Action<Action> dispatcher) : this(areaId, portalItemId, job.Parameters.ItemInfo?.Title, null, onRemoveDownload, openMapCommand, dispatcher)
        {
            _job = job;
            ObserveJob(job);
        }

        private OnDemandMapModel(
            string areaId,
            string portalItemId,
            string? title,
            byte[]? thumbnailData,
            Action<OnDemandMapModel> onRemoveDownload,
            ICommand openMapCommand,
            Action<Action> dispatcher) : base(dispatcher)
        {
            _areaId = areaId;
            _portalItemId = portalItemId;
            _title = title ?? string.Empty;
            _thumbnailData = thumbnailData;
            _onRemoveDownloadAction = onRemoveDownload;
            _downloadCommand = new DelegateCommand((o) => { }, () => false); // Currently doesn't allow re-download
            _removeDownloadCommand = new DelegateCommand((o) => RemoveDownloadedArea(), () => IsDownloaded && !IsOpen || Error != null);
            _stopDownloadCommand = new DelegateCommand((o) => Job?.CancelAsync());
            _openMapCommand = openMapCommand;
            _directoryPath = OfflineMapAreaStorage.GetOnDemandAreaDirectory(portalItemId, _areaId);
            _mmpkDirectoryPath = OfflineMapAreaStorage.GetOnDemandMmpkDirectory(portalItemId, _areaId);
            _thumbnailPath = OfflineMapAreaStorage.GetOnDemandThumbnailPath(portalItemId, _areaId);
            if (File.Exists(_thumbnailPath) && _thumbnailData is null)
            {
                _thumbnailData = File.ReadAllBytes(_thumbnailPath);
            }
        }

        public string AreaId => _areaId;

        public string Title
        {
            get => _title;
            private set => SetProperty(ref _title, value);
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

        public GenerateOfflineMapJob? Job
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

        public OnDemandMapModelStatus Status
        {
            get => _status;
            private set
            {
                SetProperty(ref _status, value);
                Dispatcher.Invoke(() =>
                {
                    OnPropertyChanged(nameof(IOfflineMapAreaItem.IsDownloading));
                    OnPropertyChanged(nameof(IOfflineMapAreaItem.IsDownloaded));
                    OnPropertyChanged(nameof(IOfflineMapAreaItem.Description));
                    _removeDownloadCommand.NotifyCanExecuteChanged();
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
            private set
            {
                SetProperty(ref _error, value);
                Dispatcher.Invoke(() =>
                {
                    _removeDownloadCommand.NotifyCanExecuteChanged();
                });
            }
        }

        public bool AllowsDownload => Status == OnDemandMapModelStatus.Initialized;

        public bool IsDownloaded => Status == OnDemandMapModelStatus.Downloaded;

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

        static IValueConverter _converter = new FileSizeConverter();
        public string Description => SizeInBytes == 0 ? string.Empty : string.Format(
            Properties.Resources.GetString("OfflineMapAreasDownloadSize")!,
            _converter.Convert(SizeInBytes, typeof(string), null, null!));

        public bool SupportsRedownloading => false;
        
        public ICommand DownloadCommand => _downloadCommand;

        public ICommand RemoveDownloadCommand => _removeDownloadCommand;

        public ICommand StopDownloadCommand => _stopDownloadCommand;
        
        public System.Windows.Input.ICommand OpenCommand => _openMapCommand;

        public bool IsDownloading => Status == OnDemandMapModelStatus.Downloading;

        public double DownloadProgress => Job is null ? 0d : Job.Progress / 100d;

        public async Task DownloadOnDemandMapAreaAsync()
        {
            if (_offlineMapTaskFactory is null || _configuration is null)
            {
                throw new InvalidOperationException("The on-demand map area does not have download configuration.");
            }

            if (!AllowsDownload)
            {
                throw new InvalidOperationException("The on-demand map area is not in a downloadable state.");
            }

            Status = OnDemandMapModelStatus.Downloading;
            Error = null;

            try
            {
                var offlineMapTask = await _offlineMapTaskFactory();
                var parameters = await offlineMapTask.CreateDefaultGenerateOfflineMapParametersAsync(
                    _configuration.AreaOfInterest,
                    _configuration.MinScale,
                    _configuration.MaxScale);

                parameters.UpdateMode = OfflineManager.Shared.Configuration.OnDemandUpdateMode;
                parameters.ContinueOnErrors = false;

                if (parameters.ItemInfo is not null)
                {
                    parameters.ItemInfo.Title = _configuration.Title;
                    parameters.ItemInfo.Description = string.Empty;
                }

                Directory.CreateDirectory(_mmpkDirectoryPath);

                var job = offlineMapTask.GenerateOfflineMap(parameters, _mmpkDirectoryPath);
                var portalItem = offlineMapTask.PortalItem ?? throw new InvalidOperationException("The offline map task does not reference a portal item.");
                await OfflineManager.Shared.StartJobAsync(job, portalItem, Title);
                ObserveJob(job);
            }
            catch (Exception ex)
            {
                MobileMapPackage?.Close();
                OfflineMapAreaUtilities.TryDeleteDirectory(_mmpkDirectoryPath);
                Error = ex;
                Status = OnDemandMapModelStatus.DownloadFailure;
                Trace.WriteLine("Failed to download on-demand map area: " + ex.Message, "ArcGIS Toolkit");
            }
        }

        public void RemoveDownloadedArea()
        {
            MobileMapPackage?.Close();
            MobileMapPackage = null;
            OfflineMapAreaUtilities.TryDeleteDirectory(_directoryPath);
            Map = null;
            SizeInBytes = 0;
            Error = null;
            Status = OnDemandMapModelStatus.Initialized;
            _onRemoveDownloadAction(this);
        }

        public async Task CancelJobAsync()
        {
            if (Job is not null)
            {
                await Job.CancelAsync().ConfigureAwait(false);
                OfflineMapAreaUtilities.TryDeleteDirectory(_mmpkDirectoryPath);
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
                ThumbnailData ??= await OfflineMapAreaUtilities.LoadImageBytesAsync(mobileMapPackage.Item?.Thumbnail).ConfigureAwait(false);
                Error = null;
                Status = OnDemandMapModelStatus.Downloaded;
            }
            catch (Exception ex)
            {
                mobileMapPackage.Close();
                MobileMapPackage = null;
                Error = ex;
                Map = null;
                SizeInBytes = 0;
                Status = OnDemandMapModelStatus.MmpkLoadFailure;
                Trace.WriteLine("Failed to load mobile map package: " + ex.Message, "ArcGIS Toolkit");
            }
        }

        private void ObserveJob(GenerateOfflineMapJob job)
        {
            Job = job;
            Error = null;
            Status = OnDemandMapModelStatus.Downloading;
            _ = ObserveJobAsync(job);
        }

        private async Task ObserveJobAsync(GenerateOfflineMapJob job)
        {
            try
            {
                var result = (GenerateOfflineMapResult)await job.GetResultAsync().ConfigureAwait(false);
                Error = null;
                if (result.MobileMapPackage is not null)
                {
                    await LoadAndUpdateMobileMapPackageAsync(result.MobileMapPackage).ConfigureAwait(false);
                }
                else
                {
                    Status = OnDemandMapModelStatus.DownloadFailure;
                }
            }
            catch (Exception ex)
            {
                if (OfflineMapAreaUtilities.IsCancellation(job, ex))
                {
                    Error = null;
                    Status = OnDemandMapModelStatus.DownloadCancelled;
                    RemoveDownloadedArea();
                }
                else
                {
                    Error = job.Error ?? ex;
                    Status = OnDemandMapModelStatus.DownloadFailure;
                }

                OfflineMapAreaUtilities.TryDeleteDirectory(_mmpkDirectoryPath);
            }
            finally
            {
                Job = null;
            }
        }

        internal static async Task<IReadOnlyList<OnDemandMapModel>> LoadOnDemandMapModelsAsync(
            string portalItemId,
            Action<OnDemandMapModel> onRemoveDownload, System.Windows.Input.ICommand openCommand, Action<Action> dispatcher)
        {
            var models = new List<OnDemandMapModel>();

            foreach (var job in OfflineManager.Shared.Jobs.OfType<GenerateOfflineMapJob>())
            {
                if (!string.Equals(job.OnlineMap?.Item?.ItemId, portalItemId, StringComparison.Ordinal))
                {
                    continue;
                }

                var areaId = OfflineMapAreaStorage.GetOnDemandAreaIdFromMmpkDirectory(job.DownloadDirectoryPath);
                if (string.IsNullOrWhiteSpace(areaId))
                {
                    continue;
                }

                models.Add(new OnDemandMapModel(job, areaId, portalItemId, onRemoveDownload, openCommand, dispatcher));
            }

            var onDemandDirectory = OfflineMapAreaStorage.GetOnDemandAreasDirectory(portalItemId);
            if (Directory.Exists(onDemandDirectory))
            {
                foreach (var areaDirectory in Directory.GetDirectories(onDemandDirectory))
                {
                    var areaId = Path.GetFileName(areaDirectory);
                    if (string.IsNullOrWhiteSpace(areaId) || models.Any(model => string.Equals(model.AreaId, areaId, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    var model = await CreateDownloadedAsync(areaId, portalItemId, onRemoveDownload, openCommand, dispatcher).ConfigureAwait(false);
                    if (model is not null)
                    {
                        models.Add(model);
                    }
                    else
                    {
                        OfflineMapAreaUtilities.TryDeleteDirectory(areaDirectory);
                    }
                }
            }

            return models.OrderBy(model => model.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
        }

        private static async Task<OnDemandMapModel?> CreateDownloadedAsync(
            string areaId,
            string portalItemId,
            Action<OnDemandMapModel> onRemoveDownload, System.Windows.Input.ICommand openCommand, Action<Action> dispatcher)
        {
            var mmpkDirectory = OfflineMapAreaStorage.GetOnDemandMmpkDirectory(portalItemId, areaId);
            if (!Directory.Exists(mmpkDirectory))
            {
                return null;
            }

            var thumbnailPath = OfflineMapAreaStorage.GetOnDemandThumbnailPath(portalItemId, areaId);
            byte[]? thumbnailData = File.Exists(thumbnailPath) ? File.ReadAllBytes(thumbnailPath) : null;
            var mobileMapPackage = new MobileMapPackage(mmpkDirectory);

            try
            {
                await mobileMapPackage.LoadAsync().ConfigureAwait(false);
            }
            catch
            {
                // Allow the model to surface the load failure state.
            }

            var model = new OnDemandMapModel(
                areaId,
                portalItemId,
                mobileMapPackage.Item?.Title,
                thumbnailData,
                onRemoveDownload, openCommand, dispatcher);

            await model.LoadAndUpdateMobileMapPackageAsync(mobileMapPackage).ConfigureAwait(false);
            return model;
        }
    }

    internal enum OnDemandMapModelStatus
    {
        Initialized,
        Downloading,
        Downloaded,
        DownloadFailure,
        DownloadCancelled,
        MmpkLoadFailure
    }

    internal sealed class OnDemandMapAreaConfiguration
    {
        public OnDemandMapAreaConfiguration(string title, Esri.ArcGISRuntime.Geometry.Geometry areaOfInterest, double minScale, double maxScale, byte[]? thumbnailData = null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title cannot be null or whitespace.", nameof(title));
            }

            Title = title;
            AreaOfInterest = areaOfInterest ?? throw new ArgumentNullException(nameof(areaOfInterest));
            MinScale = minScale;
            MaxScale = maxScale;
            ThumbnailData = thumbnailData;
            AreaId = Guid.NewGuid().ToString("D");
        }

        public string AreaId { get; }

        public string Title { get; }

        public double MinScale { get; }

        public double MaxScale { get; }

        public Esri.ArcGISRuntime.Geometry.Geometry AreaOfInterest { get; }

        public byte[]? ThumbnailData { get; }
    }
}
