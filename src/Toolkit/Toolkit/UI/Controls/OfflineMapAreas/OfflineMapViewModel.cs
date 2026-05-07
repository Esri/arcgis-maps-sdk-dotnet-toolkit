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

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
#if WINUI
    [WinRT.GeneratedBindableCustomProperty]
#endif
    public sealed class OfflineMapViewModel : OfflineBindableObject
    {
        private readonly ObservableCollection<PreplannedMapModel> _preplannedMapModels = new ObservableCollection<PreplannedMapModel>();
        private readonly ObservableCollection<OnDemandMapModel> _onDemandMapModels = new ObservableCollection<OnDemandMapModel>();
        private readonly object _offlineMapTaskLock = new object();
        private readonly Map _onlineMap;
        private readonly string _portalItemId;
        private Task<OfflineMapTask>? _offlineMapTaskTask;
        private Mode _mode = Mode.Ambiguous;
        private bool _isShowingOnlyOfflineModels;
        private bool _isLoadingModels;
        private bool _mapIsOfflineDisabled;
        private Exception? _preplannedMapModelsError;

        public OfflineMapViewModel(Map onlineMap)
        {
            if (onlineMap is null)
            {
                throw new ArgumentNullException(nameof(onlineMap));
            }

            if (onlineMap.Item is null && onlineMap.Uri is not null && onlineMap.LoadStatus != LoadStatus.Loaded)
            {
                throw new ArgumentException("The map must be loaded or created with an item.", nameof(onlineMap));
            }

            if (string.IsNullOrWhiteSpace(onlineMap.Item?.ItemId))
            {
                throw new ArgumentException("The map must reference a portal item with an item ID.", nameof(onlineMap));
            }

            _onlineMap = onlineMap;
            _portalItemId = onlineMap.Item!.ItemId!;
            PreplannedMapModels = new ReadOnlyObservableCollection<PreplannedMapModel>(_preplannedMapModels);
            OnDemandMapModels = new ReadOnlyObservableCollection<OnDemandMapModel>(_onDemandMapModels);
        }

        public enum Mode
        {
            Ambiguous,
            Preplanned,
            OnDemand,
            NoInternetAvailable
        }

        public Map OnlineMap => _onlineMap;

        public string PortalItemId => _portalItemId;

        public ReadOnlyObservableCollection<PreplannedMapModel> PreplannedMapModels { get; }

        public Exception? PreplannedMapModelsError
        {
            get => _preplannedMapModelsError;
            private set => SetProperty(ref _preplannedMapModelsError, value);
        }

        public bool IsShowingOnlyOfflineModels
        {
            get => _isShowingOnlyOfflineModels;
            private set => SetProperty(ref _isShowingOnlyOfflineModels, value);
        }

        public ReadOnlyObservableCollection<OnDemandMapModel> OnDemandMapModels { get; }

        public Mode DisplayMode
        {
            get => _mode;
            private set => SetProperty(ref _mode, value);
        }

        public bool IsLoadingModels
        {
            get => _isLoadingModels;
            private set => SetProperty(ref _isLoadingModels, value);
        }

        public bool MapIsOfflineDisabled
        {
            get => _mapIsOfflineDisabled;
            private set => SetProperty(ref _mapIsOfflineDisabled, value);
        }

        public async Task LoadModelsAsync()
        {
            IsLoadingModels = true;

            try
            {
                try
                {
                    if (OnlineMap.LoadStatus != LoadStatus.Loaded)
                    {
                        await OnlineMap.RetryLoadAsync().ConfigureAwait(false);
                    }
                }
                catch
                {
                    // Ignore load failures here. We still attempt to load local models.
                }

                MapIsOfflineDisabled = OnlineMap.LoadStatus == LoadStatus.Loaded && OnlineMap.OfflineSettings is null;
                if (MapIsOfflineDisabled)
                {
                    return;
                }

                switch (DisplayMode)
                {
                    case Mode.Preplanned:
                        await LoadPreplannedMapModelsAsync().ConfigureAwait(false);
                        break;
                    case Mode.OnDemand:
                        await LoadOnDemandMapModelsAsync().ConfigureAwait(false);
                        break;
                    case Mode.Ambiguous:
                    case Mode.NoInternetAvailable:
                        await DetermineModeAsync().ConfigureAwait(false);
                        break;
                }
            }
            finally
            {
                IsLoadingModels = false;
            }
        }

        public async Task AddOnDemandMapAreaAsync(OnDemandMapAreaConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (DisplayMode != Mode.OnDemand && DisplayMode != Mode.Ambiguous)
            {
                return;
            }

            var model = new OnDemandMapModel(GetOfflineMapTaskAsync, configuration, PortalItemId, OnRemoveDownloadOfOnDemandArea);
            RunOnCapturedContext(() => InsertSorted(_onDemandMapModels, model, static item => item.Title));
            await model.DownloadOnDemandMapAreaAsync().ConfigureAwait(false);
        }

        public string NextOnDemandAreaTitle()
        {
            static string MakeTitle(int index) => $"Area {index}";

            var index = OnDemandMapModels.Count + 1;
            while (OnDemandMapModels.Any(model => string.Equals(model.Title, MakeTitle(index), StringComparison.Ordinal)))
            {
                index++;
            }

            return MakeTitle(index);
        }

        public bool IsProposedOnDemandAreaTitleUnique(string proposedTitle)
        {
            if (string.IsNullOrWhiteSpace(proposedTitle))
            {
                return false;
            }
            return !OnDemandMapModels.Any(model => string.Equals(model.Title, proposedTitle, StringComparison.Ordinal));
        }

        private bool HasDownloadedPreplannedMapAreas => PreplannedMapModels.Any(model => model.IsDownloaded);

        private bool HasDownloadedOnDemandMapAreas => OnDemandMapModels.Any(model => model.IsDownloaded);

        private bool HasDownloadedMapAreas => HasDownloadedPreplannedMapAreas || HasDownloadedOnDemandMapAreas;

        private async Task DetermineModeAsync()
        {
            await LoadPreplannedMapModelsAsync().ConfigureAwait(false);
            if (PreplannedMapModels.Count > 0)
            {
                DisplayMode = Mode.Preplanned;
                return;
            }

            await LoadOnDemandMapModelsAsync().ConfigureAwait(false);
            if (OnDemandMapModels.Count > 0)
            {
                DisplayMode = Mode.OnDemand;
                return;
            }

            DisplayMode = IsShowingOnlyOfflineModels ? Mode.NoInternetAvailable : Mode.Ambiguous;
        }

        private async Task LoadPreplannedMapModelsAsync()
        {
            var result = await PreplannedMapModel.LoadPreplannedMapModelsAsync(GetOfflineMapTaskAsync, PortalItemId, OnRemoveDownloadOfPreplannedArea).ConfigureAwait(false);

            RunOnCapturedContext(() =>
            {
                ReplaceCollection(_preplannedMapModels, result.Models);
                PreplannedMapModelsError = result.Error;
                IsShowingOnlyOfflineModels = result.OnlyOfflineModelsAreAvailable;
            });
        }

        private async Task LoadOnDemandMapModelsAsync()
        {
            var models = await OnDemandMapModel.LoadOnDemandMapModelsAsync(PortalItemId, OnRemoveDownloadOfOnDemandArea).ConfigureAwait(false);
            RunOnCapturedContext(() => ReplaceCollection(_onDemandMapModels, models));
        }

        private async Task<OfflineMapTask> GetOfflineMapTaskAsync()
        {
            Task<OfflineMapTask>? task;
            lock (_offlineMapTaskLock)
            {
                _offlineMapTaskTask ??= OfflineMapTask.CreateAsync(OnlineMap);
                task = _offlineMapTaskTask;
            }

            try
            {
                return await task.ConfigureAwait(false);
            }
            catch
            {
                lock (_offlineMapTaskLock)
                {
                    if (_offlineMapTaskTask == task)
                    {
                        _offlineMapTaskTask = null;
                    }
                }

                throw;
            }
        }

        private void OnRemoveDownloadOfPreplannedArea(PreplannedMapModel model)
        {
            if (!HasDownloadedMapAreas)
            {
                OfflineManager.Shared.RemoveMapInfo(PortalItemId);
            }

            if (IsShowingOnlyOfflineModels && !model.SupportsRedownloading)
            {
                RunOnCapturedContext(() => _preplannedMapModels.Remove(model));
            }
        }

        private void OnRemoveDownloadOfOnDemandArea(OnDemandMapModel model)
        {
            RunOnCapturedContext(() => _onDemandMapModels.Remove(model));

            if (!HasDownloadedMapAreas)
            {
                OfflineManager.Shared.RemoveMapInfo(PortalItemId);
            }
        }

        private static void ReplaceCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
        {
            collection.Clear();
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

        private static void InsertSorted<T>(ObservableCollection<T> collection, T item, Func<T, string> selector)
        {
            var index = 0;
            while (index < collection.Count &&
                string.Compare(selector(collection[index]), selector(item), StringComparison.CurrentCultureIgnoreCase) <= 0)
            {
                index++;
            }

            collection.Insert(index, item);
        }
    }

    public abstract class OfflineBindableObject : INotifyPropertyChanged
    {
        private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            RunOnCapturedContext(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
            return true;
        }

        protected void RunOnCapturedContext(Action action)
        {
            if (_synchronizationContext is null || SynchronizationContext.Current == _synchronizationContext)
            {
                action();
                return;
            }

            _synchronizationContext.Send(static state => ((Action)state!).Invoke(), action);
        }
    }

    internal static class OfflineMapAreaStorage
    {
        private const string MapAreasFolderName = "MapAreas";
        private const string PreplannedFolderName = "Preplanned";
        private const string OnDemandFolderName = "OnDemand";
        private const string MobileMapPackageFolderName = "mmpk";
        private const string ThumbnailFileName = "thumbnail.bin";

        public static string GetPortalItemAreasDirectory(string portalItemId)
            => Path.Combine(OfflineManager.GetOfflineManagerDirectory(), MapAreasFolderName, portalItemId);

        public static string GetPreplannedAreasDirectory(string portalItemId)
            => Path.Combine(GetPortalItemAreasDirectory(portalItemId), PreplannedFolderName);

        public static string GetPreplannedAreaDirectory(string portalItemId, string preplannedMapAreaId)
            => Path.Combine(GetPreplannedAreasDirectory(portalItemId), preplannedMapAreaId);

        public static string GetOnDemandAreasDirectory(string portalItemId)
            => Path.Combine(GetPortalItemAreasDirectory(portalItemId), OnDemandFolderName);

        public static string GetOnDemandAreaDirectory(string portalItemId, string areaId)
            => Path.Combine(GetOnDemandAreasDirectory(portalItemId), areaId);

        public static string GetOnDemandMmpkDirectory(string portalItemId, string areaId)
            => Path.Combine(GetOnDemandAreaDirectory(portalItemId, areaId), MobileMapPackageFolderName);

        public static string GetOnDemandThumbnailPath(string portalItemId, string areaId)
            => Path.Combine(GetOnDemandAreaDirectory(portalItemId, areaId), ThumbnailFileName);

        public static string? GetOnDemandAreaIdFromMmpkDirectory(string mmpkDirectoryPath)
        {
            var parent = Directory.GetParent(Path.GetFullPath(mmpkDirectoryPath));
            return parent?.Name;
        }
    }

    internal static class OfflineMapAreaUtilities
    {
        private static readonly Regex HtmlTagExpression = new Regex("<[^>]+>", RegexOptions.Compiled);

        public static string UnknownAreaTitle => "Unknown";

        public static long GetDirectorySize(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            return Directory
                .EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .Select(file => new FileInfo(file).Length)
                .Sum();
        }

        public static bool IsNetworkAvailable() => NetworkInterface.GetIsNetworkAvailable();

        public static bool IsCancellation(IJob job, Exception ex)
            => ex is OperationCanceledException || ex is TaskCanceledException || job.Status == JobStatus.Canceling;

        public static async Task<byte[]?> LoadImageBytesAsync(RuntimeImage? image)
        {
            if (image is null)
            {
                return null;
            }

            if (image.LoadStatus != LoadStatus.Loaded)
            {
                await image.LoadAsync().ConfigureAwait(false);
            }

            if (image.LoadStatus != LoadStatus.Loaded)
            {
                return null;
            }

            using var stream = await image.GetEncodedBufferAsync().ConfigureAwait(false);
            var bytes = new byte[stream.Length];
            await stream.ReadExactlyAsync(bytes).ConfigureAwait(false);
            return bytes;
        }

        public static string? StripHtml(string? value)
            => string.IsNullOrWhiteSpace(value) ? value : HtmlTagExpression.Replace(value, string.Empty);

        public static void TryDeleteDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch
            {
                // Best effort cleanup only.
            }
        }
    }
}
