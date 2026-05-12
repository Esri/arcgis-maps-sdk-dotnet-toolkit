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
        private static readonly DependencyProperty TemplateSettingsProperty =
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
    public sealed partial class OfflineMapAreasTemplateSettings : DependencyObject
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
            IsLoadingModels = _vm?.IsLoadingModels ?? false;
            IsShowingOnlyOfflineModels = _vm?.IsShowingOnlyOfflineModels ?? false;
            MapIsOfflineDisabled = _vm?.MapIsOfflineDisabled ?? false;
            OnDemandMapModels = _vm?.OnDemandMapModels;
            OnlineMap = _vm?.OnlineMap;
            PreplannedMapModels = _vm?.PreplannedMapModels;
            PreplannedMapModelsError = _vm?.PreplannedMapModelsError;
        }

        /// <summary>
        /// Gets a value that indicates whether the current view model is loading map area models.
        /// </summary>
        public bool IsLoadingModels
        {
#if WPF
            get => (bool)GetValue(IsLoadingModelsPropertyKey.DependencyProperty);
            private set => SetValue(IsLoadingModelsPropertyKey, value);
#else
            get => (bool)GetValue(IsLoadingModelsProperty);
            private set => SetValue(IsLoadingModelsProperty, value);
#endif
        }

#if WINDOWS_XAML
        private static readonly DependencyProperty IsLoadingModelsProperty =
            DependencyProperty.Register(nameof(IsLoadingModels), typeof(bool), typeof(OfflineMapAreasTemplateSettings), new PropertyMetadata(false));
#else
        private static readonly DependencyPropertyKey IsLoadingModelsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsLoadingModels), typeof(bool), typeof(OfflineMapAreasTemplateSettings), new FrameworkPropertyMetadata(false));
#endif

        /// <summary>
        /// Gets a value that indicates whether only offline models are available.
        /// </summary>
        public bool IsShowingOnlyOfflineModels
        {
#if WPF
            get => (bool)GetValue(IsShowingOnlyOfflineModelsPropertyKey.DependencyProperty);
            private set => SetValue(IsShowingOnlyOfflineModelsPropertyKey, value);
#else
            get => (bool)GetValue(IsShowingOnlyOfflineModelsProperty);
            private set => SetValue(IsShowingOnlyOfflineModelsProperty, value);
#endif
        }

#if WINDOWS_XAML
        private static readonly DependencyProperty IsShowingOnlyOfflineModelsProperty =
            DependencyProperty.Register(nameof(IsShowingOnlyOfflineModels), typeof(bool), typeof(OfflineMapAreasTemplateSettings), new PropertyMetadata(false));
#else
        private static readonly DependencyPropertyKey IsShowingOnlyOfflineModelsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsShowingOnlyOfflineModels), typeof(bool), typeof(OfflineMapAreasTemplateSettings), new FrameworkPropertyMetadata(false));
#endif

        /// <summary>
        /// Gets a value that indicates whether offline map areas are disabled for the current map.
        /// </summary>
        public bool MapIsOfflineDisabled
        {
#if WPF
            get => (bool)GetValue(MapIsOfflineDisabledPropertyKey.DependencyProperty);
            private set => SetValue(MapIsOfflineDisabledPropertyKey, value);
#else
            get => (bool)GetValue(MapIsOfflineDisabledProperty);
            private set => SetValue(MapIsOfflineDisabledProperty, value);
#endif
        }

#if WINDOWS_XAML
        private static readonly DependencyProperty MapIsOfflineDisabledProperty =
            DependencyProperty.Register(nameof(MapIsOfflineDisabled), typeof(bool), typeof(OfflineMapAreasTemplateSettings), new PropertyMetadata(false));
#else
        private static readonly DependencyPropertyKey MapIsOfflineDisabledPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(MapIsOfflineDisabled), typeof(bool), typeof(OfflineMapAreasTemplateSettings), new FrameworkPropertyMetadata(false));
#endif

        /// <summary>
        /// Gets the current on-demand map area models.
        /// </summary>
        public IReadOnlyList<IOfflineMapAreaItem>? OnDemandMapModels
        {
#if WPF
            get => (IReadOnlyList<IOfflineMapAreaItem>?)GetValue(OnDemandMapModelsPropertyKey.DependencyProperty);
            private set => SetValue(OnDemandMapModelsPropertyKey, value);
#else
            get => (IReadOnlyList<IOfflineMapAreaItem>?)GetValue(OnDemandMapModelsProperty);
            private set => SetValue(OnDemandMapModelsProperty, value);
#endif
        }

#if WINDOWS_XAML
        private static readonly DependencyProperty OnDemandMapModelsProperty =
            DependencyProperty.Register(nameof(OnDemandMapModels), typeof(IReadOnlyList<IOfflineMapAreaItem>), typeof(OfflineMapAreasTemplateSettings), new PropertyMetadata(null));
#else
        private static readonly DependencyPropertyKey OnDemandMapModelsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(OnDemandMapModels), typeof(IReadOnlyList<IOfflineMapAreaItem>), typeof(OfflineMapAreasTemplateSettings), new FrameworkPropertyMetadata(null));
#endif

        /// <summary>
        /// Gets the current online map.
        /// </summary>
        public Map? OnlineMap
        {
#if WPF
            get => (Map?)GetValue(OnlineMapPropertyKey.DependencyProperty);
            private set => SetValue(OnlineMapPropertyKey, value);
#else
            get => (Map?)GetValue(OnlineMapProperty);
            private set => SetValue(OnlineMapProperty, value);
#endif
        }

#if WINDOWS_XAML
        private static readonly DependencyProperty OnlineMapProperty =
            DependencyProperty.Register(nameof(OnlineMap), typeof(Map), typeof(OfflineMapAreasTemplateSettings), new PropertyMetadata(null));
#else
        private static readonly DependencyPropertyKey OnlineMapPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(OnlineMap), typeof(Map), typeof(OfflineMapAreasTemplateSettings), new FrameworkPropertyMetadata(null));
#endif

        /// <summary>
        /// Gets the current preplanned map area models.
        /// </summary>
        public IReadOnlyList<IOfflineMapAreaItem>? PreplannedMapModels
        {
#if WPF
            get => (IReadOnlyList<IOfflineMapAreaItem>?)GetValue(PreplannedMapModelsPropertyKey.DependencyProperty);
            private set => SetValue(PreplannedMapModelsPropertyKey, value);
#else
            get => (IReadOnlyList<IOfflineMapAreaItem>?)GetValue(PreplannedMapModelsProperty);
            private set => SetValue(PreplannedMapModelsProperty, value);
#endif
        }

#if WINDOWS_XAML
        private static readonly DependencyProperty PreplannedMapModelsProperty =
            DependencyProperty.Register(nameof(PreplannedMapModels), typeof(IReadOnlyList<IOfflineMapAreaItem>), typeof(OfflineMapAreasTemplateSettings), new PropertyMetadata(null));
#else
        private static readonly DependencyPropertyKey PreplannedMapModelsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(PreplannedMapModels), typeof(IReadOnlyList<IOfflineMapAreaItem>), typeof(OfflineMapAreasTemplateSettings), new FrameworkPropertyMetadata(null));
#endif

        /// <summary>
        /// Gets the current error raised while loading preplanned map models.
        /// </summary>
        public Exception? PreplannedMapModelsError
        {
#if WPF
            get => (Exception?)GetValue(PreplannedMapModelsErrorPropertyKey.DependencyProperty);
            private set => SetValue(PreplannedMapModelsErrorPropertyKey, value);
#else
            get => (Exception?)GetValue(PreplannedMapModelsErrorProperty);
            private set => SetValue(PreplannedMapModelsErrorProperty, value);
#endif
        }

#if WINDOWS_XAML
        private static readonly DependencyProperty PreplannedMapModelsErrorProperty =
            DependencyProperty.Register(nameof(PreplannedMapModelsError), typeof(Exception), typeof(OfflineMapAreasTemplateSettings), new PropertyMetadata(null));
#else
        private static readonly DependencyPropertyKey PreplannedMapModelsErrorPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(PreplannedMapModelsError), typeof(Exception), typeof(OfflineMapAreasTemplateSettings), new FrameworkPropertyMetadata(null));
#endif

        private void OnVMPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(OfflineMapViewModel.IsLoadingModels):
                    IsLoadingModels = _vm?.IsLoadingModels ?? false;
                    break;
                case nameof(OfflineMapViewModel.IsShowingOnlyOfflineModels):
                    IsShowingOnlyOfflineModels = _vm?.IsShowingOnlyOfflineModels ?? false;
                    break;
                case nameof(OfflineMapViewModel.MapIsOfflineDisabled):
                    MapIsOfflineDisabled = _vm?.MapIsOfflineDisabled ?? false;
                    break;
                case nameof(OfflineMapViewModel.OnDemandMapModels):
                    OnDemandMapModels = _vm?.OnDemandMapModels is null ? null : new ReadonlyCollection<IOfflineMapAreaItem>(_vm.OnDemandMapModels);
                    break;
                case nameof(OfflineMapViewModel.OnlineMap):
                    OnlineMap = _vm?.OnlineMap;
                    break;
                case nameof(OfflineMapViewModel.PreplannedMapModels):
                    PreplannedMapModels = _vm?.PreplannedMapModels is null ? null : new ReadonlyCollection<IOfflineMapAreaItem>(_vm.PreplannedMapModels);
                    break;
                case nameof(OfflineMapViewModel.PreplannedMapModelsError):
                    PreplannedMapModelsError = _vm?.PreplannedMapModelsError;
                    break;
                default:
                    UpdateProperties();
                    break;
            }
        }

        // Wrapper to avoid covariance issues in .NET
#if WINUI
        [WinRT.GeneratedBindableCustomProperty]
#endif
        private partial class ReadonlyCollection<T> : IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged
        {
            private readonly IList _list;

            public ReadonlyCollection(IList list)
            {
                _list = list;
                if(list is INotifyCollectionChanged incc)
                    incc.CollectionChanged += OnCollectionChanged;
                if(list is INotifyPropertyChanged inpc)
                    inpc.PropertyChanged += OnPropertyChanged;
            }

            private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);

            private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

            public T this[int index] => (T)_list[index]!;

            public int Count => _list.Count;

            public IEnumerator<T> GetEnumerator()
            {
                foreach(var item in _list)
                {
                    yield return (T)item!;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public event NotifyCollectionChangedEventHandler? CollectionChanged;

            public event PropertyChangedEventHandler? PropertyChanged;
        }
    }
}
#endif
