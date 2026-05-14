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

#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [TemplatePart(Name = ItemsViewName, Type = typeof(ItemsControl))]
    public partial class OfflineMapAreasView : Control
    {
        private const string ItemsViewName = "ItemsView";

        private void SetVM(OfflineMapViewModel? vm)
        {
            TemplateSettings.VM = vm;
            if (vm is not null)
                _ = vm.LoadModelsAsync();
        }

        // Template settings class.
        // See https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/template-settings-classes for more information about template settings and why we use them.

        /// <summary>
        /// Gets an object that provides calculated values that can be referenced as TemplateBinding sources when defining templates for a <see cref="OfflineMapAreasView" /> control.
        /// </summary>
        public OfflineMapAreasTemplateSettings TemplateSettings
        {
#if WPF
            get => (OfflineMapAreasTemplateSettings)GetValue(TemplateSettingsPropertyKey.DependencyProperty);
            private set => SetValue(TemplateSettingsPropertyKey, value);
#elif WINUI
            get => (OfflineMapAreasTemplateSettings)GetValue(TemplateSettingsProperty);
            private set => SetValue(TemplateSettingsProperty, value);
#endif
        }

#if WINUI
        public static readonly DependencyProperty TemplateSettingsProperty =
            DependencyProperty.Register(nameof(TemplateSettings), typeof(OfflineMapAreasTemplateSettings), typeof(OfflineMapAreasView), new PropertyMetadata(null));
#elif WPF
        private static readonly DependencyPropertyKey TemplateSettingsPropertyKey =
                DependencyProperty.RegisterReadOnly(
                  name: nameof(TemplateSettings),
                  propertyType: typeof(OfflineMapAreasTemplateSettings),
                  ownerType: typeof(OfflineMapAreasView),
                  typeMetadata: new FrameworkPropertyMetadata());
#endif
    }

    /// <summary>
    /// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for a <see cref="OfflineMapAreasView"/> control. Not intended for general use.
    /// </summary>
    public sealed partial class OfflineMapAreasTemplateSettings : DependencyObject, INotifyPropertyChanged
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
                    }
                    _vm = value;
                    if (_vm is not null)
                    {
                        _vm.PropertyChanged += OnVMPropertyChanged;
                    }
                    UpdateProperties();
                }
            }
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
        /// Gets a value that indicates whether only offline models are available.
        /// </summary>
//        public bool IsShowingOnlyOfflineModels => _vm?.IsShowingOnlyOfflineModels ?? false;

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
        public bool IsOnDemandMode => _vm?.DisplayMode == OfflineMapViewModel.Mode.OnDemand || _vm?.DisplayMode == OfflineMapViewModel.Mode.Ambiguous;

        /// <summary>
        /// Gets a value indicating whether the offline map is operating in preplanned mode.
        /// </summary>
        public bool IsPreplannedMode => _vm?.DisplayMode == OfflineMapViewModel.Mode.Preplanned;

        /// <summary>
        /// Gets a value indicating whether there are no map areas available for the current map after it has loaded.
        /// </summary>
        public bool HasNoAreas => MapAreas is not null && _vm?.IsLoadingModels == false && !MapIsOfflineDisabled && (MapAreas?.Count ??  0) == 0;

        /// <summary>
        /// Gets the current map areas.
        /// </summary>
        public IReadOnlyCollection<IOfflineMapAreaItem>? MapAreas => _vm is null ? null : (_vm.PreplannedMapModels.Count > 0 ? _vm.PreplannedMapModels : _vm.OnDemandMapModels);

        /// <summary>
        /// Gets the current error raised while loading preplanned map models.
        /// </summary>
        public Exception? PreplannedMapModelsError => _vm?.PreplannedMapModelsError;

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnVMPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(OfflineMapViewModel.IsLoadingModels):
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(MapAreas)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasNoAreas)));
                    break;
                case nameof(OfflineMapViewModel.DisplayMode):
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsInternetNotAvailable)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsOnDemandMode)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsPreplannedMode)));
                    break;
            }
            OnPropertyChanged(e);
        }

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            this.Dispatch(() => PropertyChanged?.Invoke(this, e));
        }
    }
}
#endif
