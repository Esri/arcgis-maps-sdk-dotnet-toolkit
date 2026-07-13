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
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
    internal sealed partial class OfflineMapViewModel : OfflineBindableObject
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
        private System.Windows.Input.ICommand _openMapCommand;

        public OfflineMapViewModel(OfflineMapInfo offlineMap, Action<Action> dispatcher, System.Windows.Input.ICommand openMapCommand) : base(dispatcher)
        {
            if (offlineMap is null)
            {
                throw new ArgumentNullException(nameof(offlineMap));
            }
            _onlineMap = new Map(offlineMap.PortalItemUrl);
            _portalItemId = offlineMap.Id;
            _openMapCommand = openMapCommand;
            PreplannedMapModels = new ReadOnlyObservableCollection<PreplannedMapModel>(_preplannedMapModels);
            OnDemandMapModels = new ReadOnlyObservableCollection<OnDemandMapModel>(_onDemandMapModels);
            this._isShowingOnlyOfflineModels = true;
        }

        public OfflineMapViewModel(Map onlineMap, Action<Action> dispatcher, System.Windows.Input.ICommand openMapCommand) : base(dispatcher)
        {
            if (onlineMap is null)
            {
                throw new ArgumentNullException(nameof(onlineMap));
            }

            string? itemId = onlineMap.Item?.ItemId;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                itemId = PortalHelper.GetPortalItemId(onlineMap.Uri);

                if (string.IsNullOrWhiteSpace(itemId))
                {
                    throw new ArgumentException("The map must reference a portal item with an item ID.", nameof(onlineMap));
                }
            }
            _onlineMap = onlineMap;
            _portalItemId = itemId;
            _openMapCommand = openMapCommand;
            PreplannedMapModels = new ReadOnlyObservableCollection<PreplannedMapModel>(_preplannedMapModels);
            OnDemandMapModels = new ReadOnlyObservableCollection<OnDemandMapModel>(_onDemandMapModels);
        }

        internal enum Mode
        {
            Ambiguous,
            Preplanned,
            OnDemand,
            NoInternetAvailable
        }

        public Map OnlineMap => _onlineMap;

        public string PortalItemId => _portalItemId;

        internal ReadOnlyObservableCollection<PreplannedMapModel> PreplannedMapModels { get; }

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

        internal ReadOnlyObservableCollection<OnDemandMapModel> OnDemandMapModels { get; }

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

            var model = new OnDemandMapModel(GetOfflineMapTaskAsync, configuration, PortalItemId, OnRemoveDownloadOfOnDemandArea, _openMapCommand, Dispatcher);
            Dispatcher(() => InsertSorted(_onDemandMapModels, model, static item => item.Title));
            await model.DownloadOnDemandMapAreaAsync().ConfigureAwait(false);
        }

        public string NextOnDemandAreaTitle()
        {
            static string MakeTitle(int index) => string.Format(
                Properties.Resources.GetString("OfflineMapAreasDefaultAreaTitle")!,
                index);

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
            var result = await PreplannedMapModel.LoadPreplannedMapModelsAsync(GetOfflineMapTaskAsync, PortalItemId, OnRemoveDownloadOfPreplannedArea, _openMapCommand, Dispatcher).ConfigureAwait(false);

            TaskCompletionSource tcs = new TaskCompletionSource();
            Dispatcher(() =>
            {
                try
                {
                    ReplaceCollection(_preplannedMapModels, result.Models);
                    PreplannedMapModelsError = result.Error;
                    IsShowingOnlyOfflineModels = result.OnlyOfflineModelsAreAvailable;
                }
                finally
                {
                    tcs.SetResult();
                }
            });
            await tcs.Task.ConfigureAwait(false);
        }

        private async Task LoadOnDemandMapModelsAsync()
        {
            var models = await OnDemandMapModel.LoadOnDemandMapModelsAsync(PortalItemId, OnRemoveDownloadOfOnDemandArea, _openMapCommand, Dispatcher).ConfigureAwait(false);
            TaskCompletionSource tcs = new TaskCompletionSource();
            Dispatcher(() =>
            {
                try
                {
                    ReplaceCollection(_onDemandMapModels, models);
                }
                finally
                {
                    tcs.SetResult();
                }
            });
            await tcs.Task.ConfigureAwait(false);
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
                Dispatcher(() =>
                {
                    OfflineManager.Shared.RemoveMapInfo(PortalItemId);
                });
            }

            if (IsShowingOnlyOfflineModels && !model.SupportsRedownloading)
            {
                Dispatcher(() => _preplannedMapModels.Remove(model));
            }
        }

        private void OnRemoveDownloadOfOnDemandArea(OnDemandMapModel model)
        {
            Dispatcher(() =>
            {
                _onDemandMapModels.Remove(model);

                if (!HasDownloadedMapAreas)
                {
                    OfflineManager.Shared.RemoveMapInfo(PortalItemId);
                }
            });
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

    internal abstract class OfflineBindableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected Action<Action> Dispatcher { get; }

        public OfflineBindableObject(Action<Action> dispatcher)
        {
            Dispatcher = dispatcher;
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName!);
            return true;
        }

        protected void OnPropertyChanged(string propertyName)
            => Dispatcher(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }

    internal static class OfflineMapAreaStorage
    {
        private const string MapAreasFolderName = "MapAreas";
        private const string PreplannedFolderName = "Preplanned";
        private const string OnDemandFolderName = "OnDemand";
        private const string MobileMapPackageFolderName = "mmpk";
        private const string ThumbnailFileName = "thumbnail.bin";

        public static string GetPortalItemAreasDirectory(string portalItemId)
            => Path.Combine(OfflineManager.Shared.GetOfflineManagerDirectory(), MapAreasFolderName, portalItemId);

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
}
