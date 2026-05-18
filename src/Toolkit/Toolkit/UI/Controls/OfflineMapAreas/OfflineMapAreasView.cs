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
using TextBox = Microsoft.Maui.Controls.Entry;
#elif WPF
using System.Windows.Controls.Primitives;
using BaseItemsControl = System.Windows.Controls.ItemsControl;
using Esri.ArcGISRuntime.Toolkit.Primitives;
#elif WINDOWS_XAML
using BaseItemsControl = Microsoft.UI.Xaml.Controls.ItemsControl;
using Esri.ArcGISRuntime.Toolkit.Primitives;
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
    /// </remarks>
    public partial class OfflineMapAreasView
    {
        private readonly DelegateCommand _openMapCommand;
        private OfflineMapViewModel? _vm;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureFormView"/> class.
        /// </summary>
        public OfflineMapAreasView()
            : base()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
            ItemTemplate = DefaultItemTemplate;
#else
            DefaultStyleKey = typeof(OfflineMapAreasView);
#endif
            TemplateSettings = new OfflineMapAreasTemplateSettings();
            _openMapCommand = new DelegateCommand((map) => SelectedMap = map as Map, (map) => map != SelectedMap);
            _goOnlineCommand = new DelegateCommand((o) => SelectedMap = OnlineMap, () => SelectedMap != OnlineMap && OnlineMap != null);
        }

        /// <inheritdoc/>
#if WINDOWS_XAML || MAUI
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {

#if WINDOWS_XAML || WPF
            if(GetTemplateChild("RefreshMapAreasButton") is ButtonBase refreshAreasButton)
            {
                refreshAreasButton.Click += (s,e) => _vm?.LoadModelsAsync();
            }
            if (GetTemplateChild("NoInternetRefreshButton") is ButtonBase refreshButton)
            {
                refreshButton.Click += (s, e) => _vm?.LoadModelsAsync();
            }
            if (GetTemplateChild("AddMapAreaButton") is ButtonBase addMapAreaButton)
            {
                addMapAreaButton.Click += (s, e) => InitAddOnDemandArea();
            }
            if (GetTemplateChild("AcceptAddOnDemandAreaButton") is ButtonBase acceptMapAreaButton)
            {
                acceptMapAreaButton.Click += (s, e) => AddOnDemandArea();
            }
            if (GetTemplateChild("CancelAddOnDemandAreaButton") is ButtonBase cancelMapAreaButton)
            {
                cancelMapAreaButton.Click += (s, e) => CloseAddOnDemandArea();
            }

#elif MAUI
            base.OnApplyTemplate();
            OnApplyTemplateMaui();
#else
            base.OnApplyTemplate();
#endif
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
                            // Subtract the 40px dark buffer around the view.
                            double buffer = mv.UnitsPerPixel * mv.ViewInsets.Left; // Assume insets are equidistant
                            var clippedArea = GeometryEngine.Buffer(vp.TargetGeometry, -buffer);
                            var image = await mv.ExportImageAsync();
                            using var ms = new MemoryStream();
                            using var s = await image.GetEncodedBufferAsync();
                            s.CopyTo(ms);
                            await _vm.AddOnDemandMapAreaAsync(new OnDemandMapAreaConfiguration(name, clippedArea, 0, mv.MapScale, ms.ToArray()));
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

        private readonly DelegateCommand _goOnlineCommand;

        /// <summary>
        /// Sets the selected map back to the <see cref="OnlineMap"/>
        /// </summary>
        public ICommand GoOnlineCommand => _goOnlineCommand;

        /// <summary>
        /// Gets or sets Online map to display areas for in the list.
        /// </summary>
        public Map? OnlineMap
        {
            get => GetValue(OnlineMapProperty) as Map;
            set => SetValue(OnlineMapProperty, value);
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
            if (map.Item is null && map.Uri is not null && map.LoadStatus != LoadStatus.Loaded)
            {
                try
                {
                    await map.LoadAsync();
                }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace(map.Item?.ItemId))
            {
                _vm = new OfflineMapViewModel(map, DispatchAction, _openMapCommand);
                SetVM(_vm);
            }
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
                SelectedOfflineMapInfo = newOfflineMap;
                // TODO: Load new VM based on new OfflineMapInfo...
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="SelectedOfflineMapInfo"/>.
        /// </summary>
        public OfflineMapInfo? SelectedOfflineMapInfo
        {
            get => GetValue(SelectedOfflineMapInfoProperty) as OfflineMapInfo;
            set => SetValue(SelectedOfflineMapInfoProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedOfflineMapInfo"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty SelectedOfflineMapInfoProperty =
            BindableProperty.Create(nameof(SelectedOfflineMapInfo), typeof(OfflineMapInfo), typeof(OfflineMapAreasView), propertyChanged: (s, oldValue, newValue) => ((OfflineMapAreasView)s).OnSelectedOfflineMapInfoPropertyChanged(oldValue as OfflineMapInfo, newValue as OfflineMapInfo));
#else
        public static readonly DependencyProperty SelectedOfflineMapInfoProperty =
            DependencyProperty.Register(nameof(SelectedOfflineMapInfo), typeof(OfflineMapInfo), typeof(OfflineMapAreasView), new PropertyMetadata(null, (s, d) => ((OfflineMapAreasView)s).OnSelectedOfflineMapInfoPropertyChanged(d.OldValue as OfflineMapInfo, d.NewValue as OfflineMapInfo)));
#endif

        private void OnSelectedOfflineMapInfoPropertyChanged(OfflineMapInfo? oldMap, OfflineMapInfo? newOfflineMap)
        {

        }


        private void OnSelectedMapPropertyChanged(Map? map)
        {
            _openMapCommand.NotifyCanExecuteChanged();
            _goOnlineCommand.NotifyCanExecuteChanged();
        }

#if MAUI
        private Map? _selectedMap;

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
#elif WINDOWS_XAML
        public Map? SelectedMap
        {
            get => GetValue(SelectedMapProperty) as Map;
            private set => SetValue(SelectedMapProperty, value);
        }
        
        /// <summary>
        /// Identifies the <see cref="SelectedMap"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedMapProperty =
            DependencyProperty.Register(nameof(SelectedMap), typeof(Map), typeof(OfflineMapAreasView), new PropertyMetadata(null, (s,e) => ((OfflineMapAreasView)s).OnSelectedMapPropertyChanged(e.NewValue as Map)));


#elif WPF
        public Map? SelectedMap
        {
            get => GetValue(SelectedMapPropertyKey.DependencyProperty) as Map; 
            private set => SetValue(SelectedMapPropertyKey, value);
        }

        private static readonly DependencyPropertyKey SelectedMapPropertyKey =
                DependencyProperty.RegisterReadOnly(
                  name: nameof(SelectedMap),
                  propertyType: typeof(Map),
                  ownerType: typeof(OfflineMapAreasView),
                  typeMetadata: new FrameworkPropertyMetadata(null, (s,e) => ((OfflineMapAreasView)s).OnSelectedMapPropertyChanged(e.NewValue as Map)));
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
            BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(OfflineMapAreasView), null);
#else
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(OfflineMapAreasView), new PropertyMetadata(null));
#endif
    }

    // TODO: Should probably be internal for MAUI
    public interface IOfflineMapAreaItem : INotifyPropertyChanged
    {
        string Title { get; }

        string Description { get; }

        public byte[]? ThumbnailData { get; }

        long SizeInBytes { get; }

        Exception? Error { get; }

        bool MapIsOfflineDisabled { get; }

        bool AllowsDownload { get; }

        bool IsDownloaded { get; }

        bool SupportsRedownloading { get; }

        bool IsDownloading { get; }

        double DownloadProgress { get; }

        System.Windows.Input.ICommand DownloadCommand { get; }

        System.Windows.Input.ICommand RemoveDownloadCommand { get; }

        System.Windows.Input.ICommand StopDownloadCommand { get; }

        System.Windows.Input.ICommand OpenCommand { get; }

        Map? Map { get; }
    }
}
