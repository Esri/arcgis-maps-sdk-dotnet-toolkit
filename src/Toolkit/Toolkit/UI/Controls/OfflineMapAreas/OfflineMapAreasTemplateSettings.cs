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
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    /// <summary>
    /// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for a <see cref="OfflineMapAreasView"/> control. Not intended for general use.
    /// </summary>
#if WPF || WINDOWS_XAML
    public sealed partial class OfflineMapAreasTemplateSettings : DependencyObject, INotifyPropertyChanged
#elif MAUI
    internal sealed partial class OfflineMapAreasTemplateSettings : BindableObject, INotifyPropertyChanged
#endif
    {
        internal OfflineMapAreasTemplateSettings()
        {
        }

        private OfflineMapViewModel? _vm;

        internal OfflineMapViewModel? VM
        {
            get { return _vm; }
            set {
                if (_vm != value)
                {
                    if (_vm is not null)
                    {
                        _vm.PropertyChanged -= OnVMPropertyChanged;
                        if(_vm.OnDemandMapModels is INotifyCollectionChanged incc)
                            incc.CollectionChanged -= OnDemandMapModels_CollectionChanged;
                        foreach (var area in _vm.PreplannedMapModels)
                            area.IsOpen = false;
                        foreach (var area in _vm.OnDemandMapModels)
                            area.IsOpen = false;
                    }
                    _vm = value;
                    if (_vm is not null)
                    {
                        _vm.PropertyChanged += OnVMPropertyChanged;
                        if (_vm.OnDemandMapModels is INotifyCollectionChanged incc)
                            incc.CollectionChanged += OnDemandMapModels_CollectionChanged;
                    }
                    UpdateProperties();
                }
            }
        }

        private void OnDemandMapModels_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasNoAreas)));
        }

        private void UpdateProperties()
        {
            OnPropertyChanged(new PropertyChangedEventArgs(null)); // Raise for all props
        }

        /// <summary>
        /// Gets a value that indicates whether the current view model is loading map area models.
        /// </summary>
        public bool IsLoadingModels => _vm?.IsLoadingModels ?? false;

        /// <summary>
        /// Gets a value that indicates whether only already offline models are currently available.
        /// </summary>
        public bool IsShowingOnlyOfflineModels => _vm?.IsShowingOnlyOfflineModels ?? false;

        /// <summary>
        /// Gets a value that indicates whether offline map areas are disabled for the current map.
        /// </summary>
        public bool MapIsOfflineDisabled => _vm?.MapIsOfflineDisabled ?? false;

        /// <summary>
        /// Gets a value indicating whether an internet connection is not available.
        /// </summary>
        public bool IsInternetNotAvailable => _vm?.DisplayMode == OfflineMapViewModel.Mode.NoInternetAvailable;

        /// <summary>
        /// Gets a value indicating whether the offline map is operating in on-demand mode.
        /// </summary>
        public bool IsOnDemandMode =>  _vm is not null && !_vm.IsLoadingModels && !MapIsOfflineDisabled && (_vm.DisplayMode == OfflineMapViewModel.Mode.OnDemand || _vm.DisplayMode == OfflineMapViewModel.Mode.Ambiguous);

        /// <summary>
        /// Gets a value indicating whether the offline map can add new map areas.
        /// </summary>
        public bool CanAddMapArea => _vm is not null && !_vm.IsLoadingModels && IsOnDemandMode && !IsInternetNotAvailable;

        /// <summary>
        /// Gets a value indicating wheteher the Add On-Demand Map Area UI is currently active. This is used to toggle visibility of the Add On-Demand Map Area UI in the control template.
        /// </summary>
        public bool IsAddOnDemandMode
        {
            get;
#if MAUI // Maui's code-generator needs this to be two-way bindable, so we need an accessible setter https://github.com/dotnet/maui/issues/26578
            internal set;
#else
            private set;
#endif
        }

        internal void SetIsAddOnDemandMode(bool value)
        {
            if (IsAddOnDemandMode != value)
            {
                IsAddOnDemandMode = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsAddOnDemandMode)));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the offline map is operating in preplanned mode.
        /// </summary>
        public bool IsPreplannedMode => _vm is not null && !_vm.IsLoadingModels && !MapIsOfflineDisabled && _vm.DisplayMode == OfflineMapViewModel.Mode.Preplanned;

        /// <summary>
        /// Gets a value indicating whether there are no map areas available for the current map after it has loaded.
        /// </summary>
        public bool HasNoAreas => MapAreas is not null && _vm?.IsLoadingModels == false && !IsInternetNotAvailable && !MapIsOfflineDisabled && (MapAreas?.Count ??  0) == 0;

        /// <summary>
        /// Gets the current map areas.
        /// </summary>
        public IReadOnlyCollection<IOfflineMapAreaItem>? MapAreas => _vm is null ? null : (_vm.PreplannedMapModels.Count > 0 ? _vm.PreplannedMapModels : _vm.OnDemandMapModels);

        /// <summary>
        /// Gets the current error raised while loading preplanned map models.
        /// </summary>
        public Exception? PreplannedMapModelsError => _vm?.PreplannedMapModelsError;

        private void OnVMPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(OfflineMapViewModel.IsLoadingModels):
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(MapAreas)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasNoAreas)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsOnDemandMode)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsPreplannedMode)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CanAddMapArea)));
                    break;
                case nameof(OfflineMapViewModel.DisplayMode):
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsInternetNotAvailable)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsOnDemandMode)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CanAddMapArea)));                    
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsPreplannedMode)));
                    break;
                case nameof(OfflineMapViewModel.MapIsOfflineDisabled):
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsOnDemandMode)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsPreplannedMode)));
                    break;
            }
            OnPropertyChanged(e);
        }

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            this.Dispatch(() => _handler?.Invoke(this, e));
        }

        PropertyChangedEventHandler? _handler;
        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add => _handler += value;
            remove => _handler -= value;
        }

    }
}
