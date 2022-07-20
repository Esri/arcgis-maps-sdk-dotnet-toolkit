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

#if WINDOWS

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Floor;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI.Controls;

#if WPF
using System.Windows;
using System.Windows.Controls;
using PropertyMetadata = System.Windows.FrameworkPropertyMetadata;
#elif WINDOWS_UWP
using Esri.ArcGISRuntime.Toolkit.Internal;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#elif WINDOWS_WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// FloorFilter provides a browsing experience for floor-aware maps and scenes, and allows you to filter the view to show a single floor/level.
    /// </summary>
    [TemplatePart(Name = "PART_AutoVisibilityWrapper", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_SiteListView", Type = typeof(ListView))]
    [TemplatePart(Name = "PART_FacilitiesListView", Type = typeof(ListView))]
    [TemplatePart(Name = "PART_AllFacilitiesListView", Type = typeof(ListView))]
    [TemplatePart(Name = "PART_FacilitiesNoSitesListView", Type = typeof(ListView))]
    [TemplatePart(Name = "PART_ZoomButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_CloseBrowseButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_BackButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_AllButton", Type = typeof(Button))]
    public partial class FloorFilter : Control, INotifyPropertyChanged
    {
        private readonly FloorFilterController _controller = new FloorFilterController();

        // View wraps everything else, to enable FloorFilter to automatically hide itself when used with a non-floor-aware map/scene.
        private UIElement? _autoVisibilityWrapper;

        private Button? _zoomButton;
        private Button? _closeBrowseButton;
        private Button? _backButton;
        private Button? _allButton;

        // Most ListView selection properties are set in XAML, but special event behavior is needed for the browsing view navigation experience.
        private ListView? _siteListView;
        private ListView? _facilitiesListView;
        private ListView? _allFacilitiesListView;
        private ListView? _facilitiesNoSitesListView;

        private bool _blockObservationOfViewpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="FloorFilter"/> class.
        /// </summary>
        public FloorFilter()
        {
            DefaultStyleKey = typeof(FloorFilter);

            DataContext = this;

            _controller.AutomaticSelectionMode = AutomaticSelectionMode;
            _controller.PropertyChanged += HandleControllerPropertyChanges;
        }

        /// <inheritdoc/>
#if WPF
        public override void OnApplyTemplate()
#elif WINDOWS_UWP || WINDOWS_WINUI
        protected override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            // Remove old handlers
            if (_allFacilitiesListView != null)
            {
#if WPF
                _allFacilitiesListView.SelectionChanged -= HandleControlDrivenFacilitySelection;
#elif WINDOWS_UWP
                if (_allFacilitiesListView is FilteringListView faflv)
                {
                    faflv.SelectionChanged2 -= HandleControlDrivenFacilitySelection;
                }
                else
                {
                    _allFacilitiesListView.SelectionChanged -= HandleControlDrivenFacilitySelection;
                }
#endif
                _allFacilitiesListView = null;
            }

            if (_facilitiesListView != null)
            {
#if WPF
                _facilitiesListView.SelectionChanged -= HandleControlDrivenFacilitySelection;
#elif WINDOWS_UWP
                if (_facilitiesListView is FilteringListView fflv)
                {
                    fflv.SelectionChanged2 -= HandleControlDrivenFacilitySelection;
                }
                else
                {
                    _facilitiesListView.SelectionChanged -= HandleControlDrivenFacilitySelection;
                }
#endif
                _facilitiesListView = null;
            }

            if (_facilitiesNoSitesListView != null)
            {
#if WPF
                _facilitiesNoSitesListView.SelectionChanged -= HandleControlDrivenFacilitySelection;
#elif WINDOWS_UWP
                if (_facilitiesListView is FilteringListView fnsflv)
                {
                    fnsflv.SelectionChanged2 -= HandleControlDrivenFacilitySelection;
                }
                else
                {
                    _facilitiesNoSitesListView.SelectionChanged -= HandleControlDrivenFacilitySelection;
                }
#endif
                _facilitiesNoSitesListView = null;
            }

            if (_siteListView != null)
            {
#if WPF
                _siteListView.SelectionChanged -= HandleControlDrivenSiteSelection;
#elif WINDOWS_UWP
                if (_siteListView is FilteringListView fslv)
                {
                    fslv.SelectionChanged2 -= HandleControlDrivenSiteSelection;
                }
                else
                {
                    _siteListView.SelectionChanged -= HandleControlDrivenSiteSelection;
                }
#endif
                _siteListView = null;
            }

            if (_zoomButton != null)
            {
                _zoomButton.Click -= HandleZoomButtonClick;
                _zoomButton = null;
            }

            if (_closeBrowseButton != null)
            {
                _closeBrowseButton.Click -= HandleCloseBrowseClick;
                _closeBrowseButton = null;
            }

            if (_backButton != null)
            {
                _backButton.Click -= HandleBackClick;
                _backButton = null;
            }

            if (_allButton != null)
            {
                _allButton.Click -= HandleAllButtonClick;
                _allButton = null;
            }

            // Add and subscribe
            _autoVisibilityWrapper = GetTemplateChild("PART_AutoVisibilityWrapper") as UIElement;

            if (GetTemplateChild("PART_SiteListView") is ListView siteListView)
            {
                _siteListView = siteListView;
#if WPF
                _siteListView.SelectionChanged += HandleControlDrivenSiteSelection;
#elif WINDOWS_UWP
                if (_siteListView is FilteringListView nfslv)
                {
                    nfslv.SelectionChanged2 += HandleControlDrivenSiteSelection;
                }
                else
                {
                    _siteListView.SelectionChanged += HandleControlDrivenSiteSelection;
                }
#endif
            }

            if (GetTemplateChild("PART_FaciltiesListView") is ListView facilitiesListView)
            {
                _facilitiesListView = facilitiesListView;
#if WPF
                _facilitiesListView.SelectionChanged += HandleControlDrivenFacilitySelection;
#elif WINDOWS_UWP
                if (_facilitiesListView is FilteringListView nfflv)
                {
                    nfflv.SelectionChanged2 += HandleControlDrivenFacilitySelection;
                }
                else
                {
                    _facilitiesListView.SelectionChanged += HandleControlDrivenFacilitySelection;
                }
#endif
            }

            if (GetTemplateChild("PART_AllFacilitiesListView") is ListView allFacilitiesListView)
            {
                _allFacilitiesListView = allFacilitiesListView;
#if WPF
                _allFacilitiesListView.SelectionChanged += HandleControlDrivenFacilitySelection;
#elif WINDOWS_UWP
                if (_allFacilitiesListView is FilteringListView nfaflv)
                {
                    nfaflv.SelectionChanged2 += HandleControlDrivenFacilitySelection;
                }
                else
                {
                    _allFacilitiesListView.SelectionChanged += HandleControlDrivenFacilitySelection;
                }

#endif
            }

            if (GetTemplateChild("PART_FacilitiesNoSitesListView") is ListView facilitiesNoSitesListView)
            {
                _facilitiesNoSitesListView = facilitiesNoSitesListView;
#if WPF
                _facilitiesNoSitesListView.SelectionChanged += HandleControlDrivenFacilitySelection;
#elif WINDOWS_UWP || WINDOWS_WINUI
                if (_facilitiesNoSitesListView is FilteringListView nfnslv)
                {
                    nfnslv.SelectionChanged2 += HandleControlDrivenFacilitySelection;
                }
                else
                {
                    _facilitiesNoSitesListView.SelectionChanged += HandleControlDrivenFacilitySelection;
                }
#endif
            }

            if (GetTemplateChild("PART_ZoomButton") is Button zoomButton)
            {
                _zoomButton = zoomButton;
                _zoomButton.Click += HandleZoomButtonClick;
            }

            if (GetTemplateChild("PART_CloseBrowseButton") is Button closeButton)
            {
                _closeBrowseButton = closeButton;
                _closeBrowseButton.Click += HandleCloseBrowseClick;
            }

            if (GetTemplateChild("PART_BackButton") is Button backButton)
            {
                _backButton = backButton;
                _backButton.Click += HandleBackClick;
            }

            if (GetTemplateChild("PART_AllButton") is Button allButton)
            {
                _allButton = allButton;
                _allButton.Click += HandleAllButtonClick;
            }
        }

        private void HandleAllButtonClick(object sender, RoutedEventArgs e) => SetTabSelection(2);

        private void HandleBackClick(object sender, RoutedEventArgs e) => SetTabSelection(0);

        private void HandleCloseBrowseClick(object sender, RoutedEventArgs e) => IsBrowseOpen = false;

        private void HandleZoomButtonClick(object sender, RoutedEventArgs e) => _controller?.ForceZoomToSelection();

        private void HandleControllerPropertyChanges(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_controller.SelectedSite):
                    if (_controller.SelectedSite != SelectedSite)
                    {
                        SelectedSite = _controller.SelectedSite;
                    }

                    break;
                case nameof(_controller.SelectedFacility):
                    if (_controller.SelectedFacility != SelectedFacility)
                    {
                        SelectedFacility = _controller.SelectedFacility;
                    }

                    break;
                case nameof(_controller.SelectedLevel):
                    if (_controller.SelectedLevel != SelectedLevel)
                    {
                        SelectedLevel = _controller.SelectedLevel;
                    }

                    break;
                case nameof(_controller.AllFacilities):
                    AllFacilities = _controller.AllFacilities;
                    SetTabSelection(0);
                    break;
                case nameof(_controller.AllSites):
                    AllSites = _controller.AllSites;
                    SetTabSelection(0);
                    break;
                case nameof(_controller.DisplayLevels):
                    DisplayLevels = _controller.DisplayLevels;
                    break;
                case nameof(_controller.RequestedViewpoint):
                    SetViewpointFromControllerAsync();
                    break;
                case nameof(_controller.ShouldDisplayFloorPicker):
                    _autoVisibilityWrapper?.SetValue(VisibilityProperty, _controller.ShouldDisplayFloorPicker ? Visibility.Visible : Visibility.Collapsed);
                    break;
                case nameof(_controller.AllDisplayLevelsSelected):
                    OnPropertyChanged(nameof(AllDisplayLevelsSelecteded));
                    break;
            }
        }

        private async void SetViewpointFromControllerAsync()
        {
            try
            {
                _blockObservationOfViewpoint = true;
                if (GeoView is MapView mv && _controller.RequestedViewpoint != null)
                {
                    GeoView.ViewpointChanged -= HandleGeoViewViewpointChanged;
                    GeoView.NavigationCompleted += HandleGeoViewNavigationCompleted;
                    await mv.SetViewpointAsync(_controller.RequestedViewpoint);
                }
                else if (GeoView is SceneView sv && _controller.RequestedViewpoint != null)
                {
                    GeoView.ViewpointChanged -= HandleGeoViewViewpointChanged;
                    GeoView.NavigationCompleted += HandleGeoViewNavigationCompleted;
                    await sv.SetViewpointAsync(_controller.RequestedViewpoint);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Esri Toolkit - error setting viewpoint: {ex}");
            }
        }

        private void HandleControlDrivenSiteSelection(object sender, SelectionChangedEventArgs e)
        {
            // Note: custom listview has special behavior so that selection change events are raised when the existing item is selected.
            if (e.AddedItems.Count > 0 && SelectedBrowseTab == 0)
            {
                SetTabSelection(1);
            }
        }

        private void HandleControlDrivenFacilitySelection(object sender, SelectionChangedEventArgs e)
        {
            // Note: custom listview has special behavior so that selection change events are raised when the existing item is selected.
            if (e.AddedItems.Count > 0)
            {
                IsBrowseOpen = false;
            }
        }

        #region GeoView, GeoModel, Viewpoint management

        /// <summary>
        /// Gets or sets the GeoView associated with this view.
        /// </summary>
        public GeoView? GeoView
        {
            get => GetValue(GeoViewProperty) as GeoView;
            set => SetValue(GeoViewProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(FloorFilter), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((FloorFilter)d).SetGeoView(e.OldValue as GeoView, e.NewValue as GeoView);

#if WINDOWS_UWP || WINDOWS_WINUI
        private long _propertyChangedCallbackToken = 0;
#endif

        private void SetGeoView(GeoView? oldView, GeoView? newView)
        {
            if (oldView == newView)
            {
                return;
            }

            if (oldView != null)
            {
                if (oldView is MapView mapview)
                {
#if WINDOWS_UWP || WINDOWS_WINUI
                    mapview.UnregisterPropertyChangedCallback(MapView.MapProperty, _propertyChangedCallbackToken);
#else
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).RemoveValueChanged(mapview, HandleGeoModelChanged);
#endif
                }
                else if (oldView is SceneView sceneview)
                {
#if WINDOWS_UWP || WINDOWS_WINUI
                    sceneview.UnregisterPropertyChangedCallback(SceneView.SceneProperty, _propertyChangedCallbackToken);
#else
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).RemoveValueChanged(sceneview, HandleGeoModelChanged);
#endif
                }

                oldView.ViewpointChanged -= HandleGeoViewViewpointChanged;
                oldView.NavigationCompleted -= HandleGeoViewNavigationCompleted;
            }

            if (newView != null)
            {
                if (newView is MapView mapview)
                {
#if WINDOWS_UWP || WINDOWS_WINUI
                    _propertyChangedCallbackToken = mapview.RegisterPropertyChangedCallback(MapView.MapProperty, HandleGeoModelChanged);
#else
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).AddValueChanged(mapview, HandleGeoModelChanged);
#endif
                }
                else if (newView is SceneView sceneview)
                {
#if WINDOWS_UWP || WINDOWS_WINUI
                    _propertyChangedCallbackToken = sceneview.RegisterPropertyChangedCallback(SceneView.SceneProperty, HandleGeoModelChanged);
#else
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).AddValueChanged(sceneview, HandleGeoModelChanged);
#endif
                }

                newView.ViewpointChanged += HandleGeoViewViewpointChanged;
                newView.NavigationCompleted += HandleGeoViewNavigationCompleted;

                // Handle case where geoview loads map while events are being set up
                HandleGeoModelChanged(null, null);
            }

            OnPropertyChanged(nameof(ShowAllFloorsButton));
        }

        private void HandleGeoViewViewpointChanged(object? sender, EventArgs e)
        {
            if (AutomaticSelectionMode != AutomaticSelectionMode.Never && !_blockObservationOfViewpoint)
            {
                _controller.ObservedViewpoint = GeoView!.GetCurrentViewpoint(ViewpointType.CenterAndScale);
            }
        }

        private void HandleGeoViewNavigationCompleted(object? sender, EventArgs e)
        {
            // Restore GeoView listening once the animation completes
            if (GeoView != null)
            {
                GeoView.ViewpointChanged += HandleGeoViewViewpointChanged;
                GeoView.NavigationCompleted -= HandleGeoViewNavigationCompleted;
                _blockObservationOfViewpoint = false;
            }
        }

        private void HandleGeoModelChanged(object? sender, object? e)
        {
            _controller.Reset();
            IsBrowseOpen = false;
            OnPropertyChanged(nameof(ShowAllFloorsButton));
            if (GeoView is MapView mv && mv.Map is ILoadable mapLoadable)
            {
                if (mapLoadable.LoadStatus == LoadStatus.Loaded)
                {
                    HandleGeoModelLoaded();
                }
                else
                {
                    mapLoadable.Loaded += ForwardGeoModelLoaded;
                    HandleGeoModelLoaded();
                }
            }
            else if (GeoView is SceneView sv && sv.Scene is ILoadable sceneLoadable)
            {
                if (sceneLoadable.LoadStatus == LoadStatus.Loaded)
                {
                    HandleGeoModelLoaded();
                }
                else
                {
                    sceneLoadable.Loaded += ForwardGeoModelLoaded;
                    HandleGeoModelLoaded();
                }
            }
        }

        private void ForwardGeoModelLoaded(object? sender, EventArgs e) => HandleGeoModelLoaded();

        private void HandleGeoModelLoaded()
        {
#if WINDOWS_UWP || WINDOWS_WINUI
            _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
#else
            Dispatcher.Invoke(() =>
#endif
            {
                if (GeoView is MapView mv && mv.Map is Map mapLoadable && mapLoadable.LoadStatus == LoadStatus.Loaded)
                {
                    _controller.FloorManager = mapLoadable.FloorManager;
                }
                else if (GeoView is SceneView sv && sv.Scene is Scene sceneLoadable && sceneLoadable.LoadStatus == LoadStatus.Loaded)
                {
                    _controller.FloorManager = sv.Scene?.FloorManager;
                }
            });
        }
        #endregion GeoView, GeoModel, Viewpoint management

        #region Selection

        /// <summary>
        /// Gets or sets the currently selected site.
        /// </summary>
        public FloorSite? SelectedSite
        {
            get => GetValue(SelectedSiteProperty) as FloorSite;
            set => SetValue(SelectedSiteProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedSite"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedSiteProperty =
            DependencyProperty.Register(nameof(SelectedSite), typeof(FloorSite), typeof(FloorFilter), new PropertyMetadata(null, OnSelectedSitePropertyChanged));

        /// <summary>
        /// Gets or sets the selected facility.
        /// </summary>
        public FloorFacility? SelectedFacility
        {
            get => GetValue(SelectedFacilityProperty) as FloorFacility;
            set => SetValue(SelectedFacilityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedFacility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedFacilityProperty =
            DependencyProperty.Register(nameof(SelectedFacility), typeof(FloorFacility), typeof(FloorFilter), new PropertyMetadata(null, OnSelectedFacilityPropertyChanged));

        /// <summary>
        /// Gets or sets the selected level.
        /// </summary>
        public FloorLevel? SelectedLevel
        {
            get => GetValue(SelectedLevelProperty) as FloorLevel;
            set => SetValue(SelectedLevelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedLevel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedLevelProperty =
            DependencyProperty.Register(nameof(SelectedLevel), typeof(FloorLevel), typeof(FloorFilter), new PropertyMetadata(null, OnSelectedLevelPropertyChanged));

        private static void OnSelectedSitePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FloorFilter floorFilter)
            {
                FloorSite? newSite = e.NewValue as FloorSite;
                floorFilter._controller.SetSelectedSite(newSite, false);

                if (newSite == null && floorFilter.AllSites?.Count > 1)
                {
                    floorFilter.SetTabSelection(0);
                }
            }
        }

        private static void OnSelectedFacilityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FloorFilter filter)
            {
                FloorFacility? newFacility = e.NewValue as FloorFacility;
                if (newFacility?.Site is FloorSite newSite)
                {
                    filter._controller.SetSelectedSite(newSite, false);
                }

                if (newFacility != null)
                {
                    filter.IsBrowseOpen = false;
                }

                filter._controller.SetSelectedFacility(newFacility, false);
                filter.OnPropertyChanged(nameof(ShowAllFloorsButton));
            }
        }

        private static void OnSelectedLevelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((FloorFilter)d)._controller.SetSelectedLevel(e.NewValue as FloorLevel, false);

        /// <summary>
        /// Sets the <see cref="SelectedSite"/> without triggering a GeoView navigation.
        /// </summary>
        public void SetSelectedSiteWithoutZoom(FloorSite newSelection)
        {
            _controller.SetSelectedSite(newSelection, true);
        }

        /// <summary>
        /// Sets the <see cref="SelectedFacility"/> without triggering a GeoView navigation.
        /// </summary>
        public void SetSelectedFacilityWithoutZoom(FloorFacility newSelection)
        {
            _controller.SetSelectedFacility(newSelection, true);
        }

        /// <summary>
        /// Sets the <see cref="SelectedLevel"/> without triggering a GeoView navigation.
        /// </summary>
        public void SetSelectedLevelWithoutZoom(FloorLevel newLevel)
        {
            _controller.SetSelectedLevel(newLevel, true);
        }

        /// <summary>
        /// Gets or sets a value indicating whether all of the levels for the selected facility should be enabled for display.
        /// </summary>
        /// <remarks>
        /// This is used for showing an entire facility in 3D.
        /// </remarks>
        public bool AllDisplayLevelsSelecteded
        {
            get => _controller.AllDisplayLevelsSelected;
            set
            {
                if (value)
                {
                    _controller.SelectAllDisplayLevels();
                }
                else
                {
                    _controller.UndoSelectAllDisplayLevels();
                }
            }
        }
        #endregion Selection

        #region Read-only list properties

#if WINDOWS_UWP || WINDOWS_WINUI

        /// <summary>
        /// Gets the list of available sites.
        /// </summary>
        public IList<FloorSite>? AllSites
        {
            get { return (IList<FloorSite>?)GetValue(AllSitesProperty); }
            private set { SetValue(AllSitesProperty, value); }
        }

        /// <summary>
        /// Gets the list of all facilities in the floor-aware map or scene.
        /// </summary>
        public IList<FloorFacility>? AllFacilities
        {
            get => GetValue(AllFacilitiesProperty) as IList<FloorFacility>;
            private set => SetValue(AllFacilitiesProperty, value);
        }

        /// <summary>
        /// Gets the list of levels in the currently-selected facility.
        /// </summary>
        public IList<FloorLevel>? DisplayLevels
        {
            get => GetValue(DisplayLevelsProperty) as IList<FloorLevel>;
            private set => SetValue(DisplayLevelsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AllSites"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllSitesProperty =
            DependencyProperty.Register(nameof(AllSites), typeof(IList<FloorSite>), typeof(FloorFilter), null);

        /// <summary>
        /// Identiies the <see cref="AllFacilities"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllFacilitiesProperty =
            DependencyProperty.Register(nameof(AllFacilities), typeof(IList<FloorFacility>), typeof(FloorFilter), null);

        /// <summary>
        /// Identifies the <see cref="DisplayLevels"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayLevelsProperty =
            DependencyProperty.Register(nameof(DisplayLevels), typeof(IList<FloorLevel>), typeof(FloorFilter), null);
#else
        /// <summary>
        /// Gets the list of available sites.
        /// </summary>
        public IList<FloorSite>? AllSites
        {
            get => GetValue(AllSitesPropertyKey.DependencyProperty) as IList<FloorSite>;
            private set => SetValue(AllSitesPropertyKey, value);
        }

        private static readonly DependencyPropertyKey AllSitesPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(AllSites), typeof(IList<FloorSite>), typeof(FloorFilter), null);

        /// <summary>
        /// Gets the list of available facilities across all sites.
        /// </summary>
        /// <remarks>
        /// To get a list of all facilities in the selected site, see <see cref="SelectedSite"/> and <see cref="FloorSite.Facilities"/>.
        /// </remarks>
        public IList<FloorFacility>? AllFacilities
        {
            get => GetValue(AllFacilitiesPropertyKey.DependencyProperty) as IList<FloorFacility>;
            private set => SetValue(AllFacilitiesPropertyKey, value);
        }

        private static readonly DependencyPropertyKey AllFacilitiesPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(AllFacilities), typeof(IList<FloorFacility>), typeof(FloorFilter), null);

        /// <summary>
        /// Gets the list of available levels in the currently selected facility.
        /// </summary>
        public IList<FloorLevel>? DisplayLevels
        {
            get => GetValue(DisplayLevelsPropertyKey.DependencyProperty) as IList<FloorLevel>;
            private set => SetValue(DisplayLevelsPropertyKey, value);
        }

        private static readonly DependencyPropertyKey DisplayLevelsPropertyKey =
DependencyProperty.RegisterReadOnly(nameof(DisplayLevels), typeof(IList<FloorLevel>), typeof(FloorFilter), null);
#endif
        #endregion Read-only list properties

        #region Configuration

        /// <summary>
        /// Gets or sets the value that defines how the <see cref="SelectedFacility"/> is updated as the <see cref="GeoView"/>'s viewpoint changes.
        /// </summary>
        public AutomaticSelectionMode AutomaticSelectionMode
        {
            get => (AutomaticSelectionMode)GetValue(AutomaticSelectionModeProperty);
            set => SetValue(AutomaticSelectionModeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AutomaticSelectionMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutomaticSelectionModeProperty =
            DependencyProperty.Register(nameof(AutomaticSelectionMode), typeof(AutomaticSelectionMode), typeof(FloorFilter), new PropertyMetadata(AutomaticSelectionMode.Always, OnAutomaticSelectionModePropertyChanged));

        private static void OnAutomaticSelectionModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((FloorFilter)d)._controller.AutomaticSelectionMode = (AutomaticSelectionMode)e.NewValue;
        #endregion Configuration

        #region Attached Properties

        /// <summary>
        /// Gets a value indicating whether the given dependency object is in an expanded state.
        /// </summary>
        public static bool GetIsExpanded(DependencyObject obj) => (bool)obj.GetValue(IsExpandedProperty);

        /// <summary>
        /// Sets a value indicating whether the given dependency object is in an expanded state.
        /// </summary>
        public static void SetIsExpanded(DependencyObject obj, bool value) => obj.SetValue(IsExpandedProperty, value);

        /// <summary>
        /// Identifies the "IsExpanded" attached property.
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty =
#if WINDOWS_UWP || WINDOWS_WINUI
            DependencyProperty.RegisterAttached("IsExpanded", typeof(bool), typeof(FloorFilter), new PropertyMetadata(false));
#else
            DependencyProperty.RegisterAttached("IsExpanded", typeof(bool), typeof(FloorFilter), new PropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
#endif
        #endregion Attached Properties

        #region INPC

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region UI State Management

        /// <summary>
        /// Gets a value indicating whether the floor filter should display an 'All Floors' button.
        /// </summary>
        /// <remarks>The 'All Floors' button is useful in 3D.</remarks>
        public bool ShowAllFloorsButton => GeoView is SceneView sv && sv.Scene is not null && SelectedFacility != null && SelectedFacility.Levels.Count > 1;

        /// <summary>
        /// Gets a value indicating whether the selected site's name should be displayed in the browse experience.
        /// </summary>
        /// <remarks>
        /// The site name label is redundant when browsing all sites and when on the site tab.
        /// </remarks>
        public bool ShowSiteNameSubtitle { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the browse view should show <see cref="BrowseFacilitiesLabel"/> rather than <see cref="BrowseSitesLabel"/>.
        /// </summary>
        public bool ShowFacilityBrowseLabel { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the browsing view is open.
        /// </summary>
        public bool IsBrowseOpen
        {
            get => (bool)GetValue(IsBrowseOpenProperty);
            set => SetValue(IsBrowseOpenProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsBrowseOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsBrowseOpenProperty =
            DependencyProperty.Register(nameof(IsBrowseOpen), typeof(bool), typeof(FloorFilter), new PropertyMetadata(false));

        private int _selectedTab = -1;

        /// <summary>
        /// Gets or sets the tab in the browsing experience that should be active.
        /// </summary>
        public int SelectedBrowseTab
        {
            get => _selectedTab;
            set
            {
                if (_selectedTab != value)
                {
                    _selectedTab = value;
                    OnPropertyChanged();
#if WINDOWS_UWP
                    switch (_selectedTab)
                    {
                        case 0:
                            VisualStateManager.GoToState(this, "Sites0", false);
                            break;
                        case 1:
                            VisualStateManager.GoToState(this, "Facilities1", false);
                            break;
                        case 2:
                            VisualStateManager.GoToState(this, "AllFacilities2", false);
                            break;
                        case 3:
                            VisualStateManager.GoToState(this, "Simple3", false);
                            break;
                    }
#endif
                }
            }
        }

        private void SetTabSelection(int index)
        {
            // Force selection of single facility tab
            if (AllSites == null || AllSites?.Count == 1)
            {
                index = 3;
            }

            if (index != 0 && index != 3)
            {
                _backButton?.SetValue(VisibilityProperty, Visibility.Visible);
            }
            else
            {
                _backButton?.SetValue(VisibilityProperty, Visibility.Collapsed);
            }

            SelectedBrowseTab = index;

            if (index == 1 && !string.IsNullOrWhiteSpace(SelectedSite?.Name))
            {
                ShowSiteNameSubtitle = true;
                OnPropertyChanged(nameof(ShowSiteNameSubtitle));
            }
            else
            {
                ShowSiteNameSubtitle = false;
                OnPropertyChanged(nameof(ShowSiteNameSubtitle));
            }

            switch (index)
            {
                case 0:
                    if (_siteListView?.SelectedItem != null)
                    {
                        _siteListView.ScrollIntoView(_siteListView.SelectedItem);
                    }

                    ShowFacilityBrowseLabel = false;
                    OnPropertyChanged(nameof(ShowFacilityBrowseLabel));

                    break;
                case 1:
                    if (_facilitiesListView?.SelectedItem != null)
                    {
                        _facilitiesListView.ScrollIntoView(_facilitiesListView.SelectedItem);
                    }

                    ShowFacilityBrowseLabel = true;
                    OnPropertyChanged(nameof(ShowFacilityBrowseLabel));

                    break;
                case 2:
                    if (_allFacilitiesListView?.SelectedItem != null)
                    {
                        _allFacilitiesListView.ScrollIntoView(_allFacilitiesListView.SelectedItem);
                    }

                    ShowFacilityBrowseLabel = true;
                    OnPropertyChanged(nameof(ShowFacilityBrowseLabel));

                    break;
                case 3:
                    if (_facilitiesNoSitesListView?.SelectedItem != null)
                    {
                        _facilitiesNoSitesListView.ScrollIntoView(_facilitiesNoSitesListView.SelectedItem);
                    }

                    ShowFacilityBrowseLabel = true;
                    OnPropertyChanged(nameof(ShowFacilityBrowseLabel));

                    break;
            }
        }

        #endregion UI State Management
    }
}
#endif