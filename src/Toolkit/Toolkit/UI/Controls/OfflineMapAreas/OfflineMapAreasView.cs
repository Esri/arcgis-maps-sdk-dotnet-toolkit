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

using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using System.Net;
using System.IO;
using System.Windows.Input;
using System.Diagnostics;
using Esri.ArcGISRuntime.Geometry;




#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
using DependencyObject = Microsoft.Maui.Controls.BindableObject;
using ScrollViewer = Microsoft.Maui.Controls.ScrollView;
using BaseItemsControl = Microsoft.Maui.Controls.ItemsView;
using ButtonBase = Microsoft.Maui.Controls.Button;
using ScaleSelector = Microsoft.Maui.Controls.Picker;
using TextBox = Microsoft.Maui.Controls.Entry;
#elif WPF
using System.Windows.Controls.Primitives;
using BaseItemsControl = System.Windows.Controls.ItemsControl;
using Esri.ArcGISRuntime.Toolkit.Primitives;
using ScaleSelector = System.Windows.Controls.ComboBox;
#elif WINDOWS_XAML
using BaseItemsControl = Microsoft.UI.Xaml.Controls.ItemsControl;
using Esri.ArcGISRuntime.Toolkit.Primitives;
using ScaleSelector = Microsoft.UI.Xaml.Controls.ComboBox;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    /// <summary>
    /// The OfflineMapAreasView allows users to take a web map offline by downloading map areas.
    /// </summary>
    /// <remarks>
    /// The view supports both ahead-of-time(preplanned) and on-demand map areas for an offline enabled web map. The view:
    /// <para>
    /// Displays a list of map areas.
    /// <list type="bullet">
    /// <item>Shows download progress and status for map areas.</item>
    /// <item>Opens a map area for viewing when selected.</item>
    /// <item>Provides options to view details about downloaded map areas.</item>
    /// <item>Supports removing downloaded offline map areas files from the device.</item>
    /// </list>
    /// </para>
    /// <para>
    /// For preplanned workflows, the view:
    /// <list type="bullet">
    /// <item>Displays a list of available preplanned map areas from an offline-enabled web map that contains preplanned map areas when the network is connected.</item>
    /// <item>Downloads preplanned map areas in the list.</item>
    /// <item>Displays a list of downloaded preplanned map areas on the device when the network is disconnected.</item>
    /// </list>
    /// </para>
    /// <para>
    /// For on-demand workflows, the view:
    /// <list type="bullet">
    /// <item>Allows users to add and download on-demand map areas to the device by specifying an area of interest and level of detail.</item>
    /// <item>Displays a list of on-demand map areas available on the device that are tied to a specific web map</item>
    /// </list>
    /// </para>
    /// <para>
    /// If you're using the <see cref="OfflineMapAreasView"/> on iOS 26 or later, maps will be downloaded using a background transfer service which allows downloads to continue even if the app is suspended.
    /// However for that to work, you must register the background task in your <c>info.plist</c> and add the following entry:
    /// <code lang="xml">
    ///     &lt;key>BGTaskSchedulerPermittedIdentifiers&lt;/key>
    ///     &lt;array>
    ///         &lt;string>APP_BUNDLE_IDENTIFIER.cpt.jobs.*&lt;/string>
    ///     &lt;/array>
    /// </code>
    /// where <c>APP_BUNDLE_IDENTIFIER</c> is the bundle identifier of your app. If this entry is not added, downloads will still work but they may be paused when the app is suspended and may not resume until the app is opened again.
    /// </para>
    /// </remarks>
    public partial class OfflineMapAreasView
    {
        private readonly DelegateCommand _openMapCommand;
        private OfflineMapViewModel? _vm;
        private static readonly OnDemandScaleOption[] OnDemandScaleOptions = new[]
        {
            new OnDemandScaleOption("OfflineMapAreasScaleRoom", 70.5310735),
            new OnDemandScaleOption("OfflineMapAreasScaleRooms", 141.062147),
            new OnDemandScaleOption("OfflineMapAreasScaleHouseProperty", 282.124294),
            new OnDemandScaleOption("OfflineMapAreasScaleHouses", 564.248588),
            new OnDemandScaleOption("OfflineMapAreasScaleSmallBuilding", 1128.497175),
            new OnDemandScaleOption("OfflineMapAreasScaleBuilding", 2256.994353),
            new OnDemandScaleOption("OfflineMapAreasScaleBuildings", 4513.988705),
            new OnDemandScaleOption("OfflineMapAreasScaleStreet", 9027.977411),
            new OnDemandScaleOption("OfflineMapAreasScaleStreets", 18055.954822),
            new OnDemandScaleOption("OfflineMapAreasScaleNeighborhood", 36111.909643),
            new OnDemandScaleOption("OfflineMapAreasScaleTown", 72223.819286),
            new OnDemandScaleOption("OfflineMapAreasScaleCity", 144447.638572),
            new OnDemandScaleOption("OfflineMapAreasScaleCities", 288895.277144),
            new OnDemandScaleOption("OfflineMapAreasScaleMetropolitanArea", 577790.554289),
            new OnDemandScaleOption("OfflineMapAreasScaleCounty", 1155581.108577),
            new OnDemandScaleOption("OfflineMapAreasScaleCounties", 2311162.217155),
            new OnDemandScaleOption("OfflineMapAreasScaleStateProvince", 4622324.434309),
            new OnDemandScaleOption("OfflineMapAreasScaleStatesProvinces", 9244648.868618),
            new OnDemandScaleOption("OfflineMapAreasScaleCountriesSmall", 18489297.737236),
            new OnDemandScaleOption("OfflineMapAreasScaleCountriesBig", 36978595.474472),
            new OnDemandScaleOption("OfflineMapAreasScaleContinent", 73957190.948944),
            new OnDemandScaleOption("OfflineMapAreasScaleWorldSmall", 147914381.897889),
            new OnDemandScaleOption("OfflineMapAreasScaleWorldBig", 295828763.795777),
            new OnDemandScaleOption("OfflineMapAreasScaleWorld", 591657527.591555),
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="OfflineMapAreasView"/> class.
        /// </summary>
        public OfflineMapAreasView()
            : base()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
            ItemTemplate = new DataTemplate(() => BuildMapAreasItemTemplate());
#else
            TemplateSettings = new OfflineMapAreasTemplateSettings();
            DefaultStyleKey = typeof(OfflineMapAreasView);
#endif
            _openMapCommand = new DelegateCommand((map) => SelectedMap = map as Map, (map) => map != SelectedMap);
            _goOnlineCommand = new DelegateCommand((o) => SelectedMap = OnlineMap, () => SelectedMap != OnlineMap && OnlineMap != null);
        }

        private void CloseAddOnDemandArea()
        {
            TemplateSettings.SetIsAddOnDemandMode(false);
            if (GetTemplateChild("AddAreaMapView") is MapView mv)
                mv.Map = null;
        }

        private void InitAddOnDemandArea()
        {
            TemplateSettings.SetIsAddOnDemandMode(true);
            
            if (GetTemplateChild("AddAreaMapView") is MapView mv)
            {
                if(OnlineMap?.Item is not null)
                {
                    mv.Map = new Map(OnlineMap.Item);
                }
                else
                {
                    // Fallback map
                    mv.Map = new Map(BasemapStyle.ArcGISLightGray)
                    {
                        InitialViewpoint = OnlineMap?.InitialViewpoint
                    };
                }
            }
            if (GetTemplateChild("AddOnDemandAreaNameTextBox") is TextBox tb)
            {
                tb.Text = _vm?.NextOnDemandAreaTitle();
            }
            if (GetTemplateChild("AddOnDemandAreaScaleSelector") is ScaleSelector scaleSelector)
            {
                scaleSelector.ItemsSource = OnDemandScaleOptions;
                scaleSelector.SelectedItem = OnDemandScaleOptions.Where(s=>s.ResourceKey == "OfflineMapAreasScaleStreet").FirstOrDefault() ?? OnDemandScaleOptions[0];
            }
        }
        private async void AddOnDemandArea()
        {
            if (_vm is not null)
            {
                string name = _vm.NextOnDemandAreaTitle() ?? "Area";
                if (GetTemplateChild("AddOnDemandAreaNameTextBox") is TextBox tb)
                {
                    if (!string.IsNullOrWhiteSpace(tb.Text))
                        name = tb.Text;
                }
                if (GetTemplateChild("AddAreaMapView") is MapView mv)
                {
                    var vp = mv.GetCurrentViewpoint(ViewpointType.BoundingGeometry);
                    if (vp != null)
                    {
                        try
                        {
                            var image = await mv.ExportImageAsync();
                            using var ms = new MemoryStream();
                            using var s = await image.GetEncodedBufferAsync();
                            s.CopyTo(ms);
                            await _vm.AddOnDemandMapAreaAsync(new OnDemandMapAreaConfiguration(name, vp.TargetGeometry, 0, GetSelectedOnDemandScale(), ms.ToArray()));
                        }
                        catch (System.Exception ex)
                        {
                            Trace.WriteLine("Failed to add on-demand map area: " + ex.Message, "ArcGIS Toolkit");
                        }
                    }
                }
            }
            CloseAddOnDemandArea();
        }

        private double GetSelectedOnDemandScale()
        {
            if (GetTemplateChild("AddOnDemandAreaScaleSelector") is ScaleSelector scaleSelector &&
                scaleSelector.SelectedItem is OnDemandScaleOption selectedOption)
            {
                return selectedOption.Scale;
            }

            return 9027.977411; // Default to street scale
        }

        private readonly DelegateCommand _goOnlineCommand;

        /// <summary>
        /// Sets the selected map back to the <see cref="OnlineMap"/>
        /// </summary>
        public ICommand GoOnlineCommand => _goOnlineCommand;

        /// <summary>
        /// Gets or sets Online map to display areas for in the list.
        /// </summary>
        /// <remarks>
        /// <note>
        /// Setting this will set the <see cref="OfflineMapInfo"/> property to <c>null</c>.
        /// </note>
        /// </remarks>
        public Map? OnlineMap
        {
            get => GetValue(OnlineMapProperty) as Map;
            set => SetValue(OnlineMapProperty, value);
        }

        private sealed class OnDemandScaleOption
        {
            public OnDemandScaleOption(string resourceKey, double scale)
            {
                ResourceKey = resourceKey;
                Scale = scale;
            }

            public string ResourceKey { get; }

            public double Scale { get; }

            public string DisplayName => Properties.Resources.GetString(ResourceKey)!;

            public override string ToString() => DisplayName;
        }

        /// <summary>
        /// Identifies the <see cref="OnlineMap"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty OnlineMapProperty =
            BindableProperty.Create(nameof(OnlineMap), typeof(Map), typeof(OfflineMapAreasView), propertyChanged: (s, oldValue, newValue) => ((OfflineMapAreasView)s).OnOnlineMapPropertyChanged(oldValue as Map, newValue as Map));
#else
        public static readonly DependencyProperty OnlineMapProperty =
            DependencyProperty.Register(nameof(OnlineMap), typeof(Map), typeof(OfflineMapAreasView), new PropertyMetadata(null, (s, d) => ((OfflineMapAreasView)s).OnOnlineMapPropertyChanged(d.OldValue as Map, d.NewValue as Map)));
#endif

        private void OnOnlineMapPropertyChanged(Map? oldMap, Map? newMap)
        {
            SetVM(null);
            if (newMap is not null && OfflineMapInfo is not null)
            {
                OfflineMapInfo = null; // Only one of OnlineMap or OfflineMapInfo can be set at a time, so clear the other when one is set.
            }
            if (newMap is not null)
            {
                SelectedMap = newMap;
                InitVM(newMap);
            }
        }

        private async void InitVM(Map map)
        {
            TemplateSettings.SetIsAddOnDemandMode(false);
            _vm = new OfflineMapViewModel(map, DispatchAction, _openMapCommand);
            SetVM(_vm);
        }

        private void SetVM(OfflineMapViewModel? vm)
        {
            TemplateSettings.VM = vm;
            if (vm is not null)
                _ = vm.LoadModelsAsync();
        }

        private void DispatchAction(Action action) => this.Dispatch(action);

        /// <summary>
        /// Gets or sets OfflineMapInfo to display areas for in the list.
        /// </summary>
        /// <remarks>
        /// <para>A set of previously stored offline map infos can be obtained from <see cref="Esri.ArcGISRuntime.Toolkit.OfflineManager.OfflineMapInfos">OfflineManager.Shared.OfflineMapInfos</see>.
        /// When an <see cref="OfflineMapInfo"/> is set, the view will attempt to load the areas associated with that info and display the map areas for it in the list.
        /// </para>
        /// <note>
        /// Setting this will set the <see cref="OnlineMap"/> property to <c>null</c>
        /// </note>
        /// </remarks>
        public OfflineMapInfo? OfflineMapInfo
        {
            get => GetValue(OfflineMapInfoProperty) as OfflineMapInfo;
            set => SetValue(OfflineMapInfoProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OfflineMapInfo"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty OfflineMapInfoProperty =
            BindableProperty.Create(nameof(OfflineMapInfo), typeof(OfflineMapInfo), typeof(OfflineMapAreasView), propertyChanged: (s, oldValue, newValue) => ((OfflineMapAreasView)s).OnOfflineMapInfoPropertyChanged(oldValue as OfflineMapInfo, newValue as OfflineMapInfo));
#else
        public static readonly DependencyProperty OfflineMapInfoProperty =
            DependencyProperty.Register(nameof(OfflineMapInfo), typeof(OfflineMapInfo), typeof(OfflineMapAreasView), new PropertyMetadata(null, (s, d) => ((OfflineMapAreasView)s).OnOfflineMapInfoPropertyChanged(d.OldValue as OfflineMapInfo, d.NewValue as OfflineMapInfo)));
#endif

        private void OnOfflineMapInfoPropertyChanged(OfflineMapInfo? oldMap, OfflineMapInfo? newOfflineMap)
        {
            if (newOfflineMap is not null && OnlineMap is not null)
            {
                OnlineMap = null; // Only one of OnlineMap or OfflineMapInfo can be set at a time, so clear the other when one is set.
            }
            SetVM(null);
            if (newOfflineMap is not null)
            {
                _vm = new OfflineMapViewModel(newOfflineMap, DispatchAction, _openMapCommand);
                SetVM(_vm);
            }
        }

        private void OnSelectedMapPropertyChanged(Map? map)
        {
            if(_vm is not null)
            {
                foreach(var preplanned in _vm.PreplannedMapModels)
                {
                    preplanned.IsOpen = map is not null && preplanned.Map == map;
                }
                foreach (var onDemand in _vm.OnDemandMapModels)
                {
                    onDemand.IsOpen = map is not null && onDemand.Map == map;
                }
            }
            _openMapCommand.NotifyCanExecuteChanged();
            _goOnlineCommand.NotifyCanExecuteChanged();
        }

#if MAUI
        private Map? _selectedMap;

        /// <summary>
        /// Gets the currently selected map. This will be set to the map associated with a map area item when a map area is selected from the list in the view. This can be used to display the selected map in a MapView or to take other actions based on the selected map.
        /// </summary>
        /// <remarks>
        /// By default the <see cref="OnlineMap"/> will be the selected map, and the MapView's Map property can be bound to this property. The property will then update when an offline map area is selected.
        /// To go back to the online map, you can create a button that binds its Command property to the <see cref="GoOnlineCommand"/> which will set the selected map back to the online map.
        /// </remarks>
        public Map? SelectedMap
        {
            get => _selectedMap;
            private set
            {
                if (_selectedMap != value) {
                    _selectedMap = value;
                    OnPropertyChanged(nameof(SelectedMap));
                    OnSelectedMapPropertyChanged(_selectedMap);
                }
            }
        }
#elif WINDOWS_XAML || WPF
        /// <summary>
        /// Gets the currently selected map. This will be set to the map associated with a map area item when a map area is selected from the list in the view. This can be used to display the selected map in a MapView or to take other actions based on the selected map.
        /// </summary>
        /// <remarks>
        /// By default the <see cref="OnlineMap"/> will be the selected map, and the MapView's Map property can be bound to this property. The property will then update when an offline map area is selected.
        /// To go back to the online map, you can create a button that binds its Command property to the <see cref="GoOnlineCommand"/> which will set the selected map back to the online map.
        /// </remarks>
        public Map? SelectedMap
        {
#if WINDOWS_XAML
            get => GetValue(SelectedMapProperty) as Map;
            private set => SetValue(SelectedMapProperty, value);
#elif WPF
            get => GetValue(SelectedMapPropertyKey.DependencyProperty) as Map; 
            private set => SetValue(SelectedMapPropertyKey, value);
#endif
        }
        
#if WINDOWS_XAML
        /// <summary>
        /// Identifies the <see cref="SelectedMap"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedMapProperty =
            DependencyProperty.Register(nameof(SelectedMap), typeof(Map), typeof(OfflineMapAreasView), new PropertyMetadata(null, (s,e) => ((OfflineMapAreasView)s).OnSelectedMapPropertyChanged(e.NewValue as Map)));
#elif WPF
        private static readonly DependencyPropertyKey SelectedMapPropertyKey =
                DependencyProperty.RegisterReadOnly(
                  name: nameof(SelectedMap),
                  propertyType: typeof(Map),
                  ownerType: typeof(OfflineMapAreasView),
                  typeMetadata: new FrameworkPropertyMetadata(null, (s,e) => ((OfflineMapAreasView)s).OnSelectedMapPropertyChanged(e.NewValue as Map)));
#endif
#endif
        /// <summary>
        /// Gets or sets the vertical scrollbar visibility of the scrollviewer below the title.
        /// </summary>
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
            set => SetValue(VerticalScrollBarVisibilityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="VerticalScrollBarVisibility"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty VerticalScrollBarVisibilityProperty =
            BindableProperty.Create(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(OfflineMapAreasView), ScrollBarVisibility.Default);
#else
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(OfflineMapAreasView), new PropertyMetadata(ScrollBarVisibility.Auto));
#endif

        /// <summary>
        /// Gets or sets item template for the <see cref="IOfflineMapAreaItem"/> items in the list.
        /// </summary>
        public DataTemplate? ItemTemplate
        {
            get => GetValue(ItemTemplateProperty) as DataTemplate;
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ItemTemplate"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty ItemTemplateProperty =
            BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(OfflineMapAreasView));
#else
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(OfflineMapAreasView), new PropertyMetadata(null));
#endif
    }

    /// <summary>
    /// Represents an offline map area exposed by <see cref="OfflineMapAreasView.TemplateSettings"/>. This is the item type used for the list of map areas in the view and should contain all necessary information about a map area to be displayed in the list and interacted with (e.g. downloaded, opened, etc.).
    /// </summary>
#if MAUI
    internal
#else
    public 
#endif
    interface IOfflineMapAreaItem : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the Title of the offline area
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Gets the description of the offline area
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the thumbnail image data for the offline area.
        /// </summary>
        public byte[]? ThumbnailData { get; }

        /// <summary>
        /// The size of the downloaded area in bytes.
        /// </summary>
        long SizeInBytes { get; }

        /// <summary>
        /// Gets the error if there was an issue with downloading or opening the map area.
        /// </summary>
        Exception? Error { get; }

        /// <summary>
        /// Gets a value indicating whether the map area can be downloaded.
        /// </summary>
        bool AllowsDownload { get; }

        /// <summary>
        /// Gets a value indicating whether the map area has been downloaded.
        /// </summary>
        bool IsDownloaded { get; }

        /// <summary>
        /// Gets a value indicating whether the map area has been downloaded.
        /// </summary>
        bool IsDownloading { get; }

        /// <summary>
        /// Gets a value indicating whether the map area is currently the selected map.
        /// </summary>
        /// <seealso cref="OfflineMapAreasView.SelectedMap"/>
        bool IsOpen { get; }

        /// <summary>
        /// Gets the current download progress as a value between 0 (nothing downloaded) and 1 (fully downloaded).
        /// </summary>
        double DownloadProgress { get; }

        /// <summary>
        /// Gets a command that will attempt to download the map area when executed.
        /// </summary>
        System.Windows.Input.ICommand DownloadCommand { get; }

        /// <summary>
        /// Gets a command that will delete the downloaded map area from disk.
        /// </summary>
        System.Windows.Input.ICommand RemoveDownloadCommand { get; }

        /// <summary>
        /// Gets a command that will stop a download in progress.
        /// </summary>
        System.Windows.Input.ICommand StopDownloadCommand { get; }

        /// <summary>
        /// Gets a command that sets the <see cref="OfflineMapAreasView.SelectedMap"/> to this instance. The command parameter should be bound to the <see cref="Map"/> property.
        /// </summary>
        System.Windows.Input.ICommand OpenCommand { get; }

        /// <summary>
        /// Gets the map associated with this offline map area item. This should be passed as the <see cref="ButtonBase.CommandParameter"/> to the <see cref="OpenCommand"/> command.
        /// </summary>
        Map? Map { get; }
    }
}
