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

#if !XAMARIN && !WINDOWS_UWP

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Floor;
using Esri.ArcGISRuntime.UI.Controls;

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
    public partial class FloorFilter : Control, INotifyPropertyChanged
    {
        private readonly FloorFilterController _controller = new FloorFilterController();

        // View wraps everything else, to enable FloorFilter to automatically hide itself when used with a non-floor-aware map/scene.
        private UIElement? _autoVisibilityWrapper;

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

            ZoomCommand = new NotifyingDelegateCommand(() => _controller?.ForceZoomToSelection());
            CloseBrowseCommand = new NotifyingDelegateCommand(() => IsBrowseOpen = false);
            GoBackCommand = new NotifyingDelegateCommand(() => SetTabSelection(0));
            ShowAllFacilitiesCommand = new NotifyingDelegateCommand(() => SetTabSelection(2));

            DataContext = this;

            _controller.AutomaticSelectionMode = AutomaticSelectionMode;
            _controller.PropertyChanged += HandleControllerPropertyChanges;
        }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Remove old handlers
            if (_allFacilitiesListView != null)
            {
                _allFacilitiesListView.SelectionChanged -= HandleControlDrivenFacilitySelection;
                _allFacilitiesListView = null;
            }

            if (_facilitiesListView != null)
            {
                _facilitiesListView.SelectionChanged -= HandleControlDrivenFacilitySelection;
                _facilitiesListView = null;
            }

            if (_facilitiesNoSitesListView != null)
            {
                _facilitiesNoSitesListView.SelectionChanged -= HandleControlDrivenFacilitySelection;
                _facilitiesNoSitesListView = null;
            }

            if (_siteListView != null)
            {
                _siteListView.SelectionChanged -= HandleControlDrivenSiteSelection;
                _siteListView = null;
            }

            // Add and subscribe
            if (GetTemplateChild("PART_AutoVisibilityWrapper") is UIElement autoVisibilityWrapper)
            {
                _autoVisibilityWrapper = autoVisibilityWrapper;
            }

            if (GetTemplateChild("PART_SiteListView") is ListView siteListView)
            {
                _siteListView = siteListView;
                _siteListView.SelectionChanged += HandleControlDrivenSiteSelection;
            }

            if (GetTemplateChild("PART_FaciltiesListView") is ListView facilitiesListView)
            {
                _facilitiesListView = facilitiesListView;
                _facilitiesListView.SelectionChanged += HandleControlDrivenFacilitySelection;
            }

            if (GetTemplateChild("PART_AllFacilitiesListView") is ListView allFacilitiesListView)
            {
                _allFacilitiesListView = allFacilitiesListView;
                _allFacilitiesListView.SelectionChanged += HandleControlDrivenFacilitySelection;
            }

            if (GetTemplateChild("PART_FacilitiesNoSitesListView") is ListView facilitiesNoSitesListView)
            {
                _facilitiesNoSitesListView = facilitiesNoSitesListView;
                _facilitiesNoSitesListView.SelectionChanged += HandleControlDrivenFacilitySelection;
            }
        }

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
                    SetValue(AllFacilitiesPropertyKey, _controller.AllFacilities);
                    SetTabSelection(0);
                    break;
                case nameof(_controller.DisplaySites):
                    SetValue(AllSitesPropertyKey, _controller.DisplaySites);
                    SetTabSelection(0);
                    break;
                case nameof(_controller.DisplayLevels):
                    SetValue(AllLevelsPropertyKey, _controller.DisplayLevels);
                    break;
                case nameof(_controller.RequestedViewpoint):
                    SetViewpointFromControllerAsync();
                    break;
                case nameof(_controller.ShouldDisplayFloorPicker):
                    _autoVisibilityWrapper?.SetValue(VisibilityProperty, _controller.ShouldDisplayFloorPicker ? Visibility.Visible : Visibility.Collapsed);
                    break;
                case nameof(_controller.AllLevelsSelect):
                    OnPropertyChanged(nameof(AllLevelsSelected));
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
                System.Diagnostics.Debug.WriteLine($"Esri Toolkit - error seting viewpoint: {ex}");
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
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).RemoveValueChanged(mapview, HandleGeoModelChanged);
                }
                else if (oldView is SceneView sceneview)
                {
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).RemoveValueChanged(sceneview, HandleGeoModelChanged);
                }

                oldView.ViewpointChanged -= HandleGeoViewViewpointChanged;
                oldView.NavigationCompleted -= HandleGeoViewNavigationCompleted;
            }

            if (newView != null)
            {
                if (newView is MapView mapview)
                {
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).AddValueChanged(mapview, HandleGeoModelChanged);
                }
                else if (newView is SceneView sceneview)
                {
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).AddValueChanged(sceneview, HandleGeoModelChanged);
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
                // Listen for load completion
                // mapLoadable.Loaded
                // var listener = new Internal.WeakEventListener<ILoadable, object?, EventArgs>(mapLoadable);
                // listener.OnEventAction = (instance, source, eventArgs) => Doc_Loaded(source, eventArgs);
                // listener.OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent;
                // mapLoadable.Loaded += listener.OnEvent;
                if (mapLoadable.LoadStatus == LoadStatus.Loaded)
                {
                    HandleGeoModelLoaded();
                }
                else
                {
                    mapLoadable.Loaded += ForwardGeoModelLoaded;

                    // Ensure event is raised even if already loaded
                    _ = mapLoadable.LoadAsync();
                }
            }
            else if (GeoView is SceneView sv && sv.Scene is ILoadable sceneLoadable)
            {
                // Listen for load completion
                // var listener = new Internal.WeakEventListener<ILoadable, object?, EventArgs>(sceneLoadable);
                // listener.OnEventAction = (instance, source, eventArgs) => Doc_Loaded(source, eventArgs);
                // listener.OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent;
                // sceneLoadable.Loaded += listener.OnEvent;
                if (sceneLoadable.LoadStatus == LoadStatus.Loaded)
                {
                    HandleGeoModelLoaded();
                }
                else
                {
                    sceneLoadable.Loaded += ForwardGeoModelLoaded;

                    // Ensure event is raised even if already loaded
                    _ = sv.Scene.LoadAsync();
                }
            }
        }

        private void ForwardGeoModelLoaded(object? sender, EventArgs e) => HandleGeoModelLoaded();

        private void HandleGeoModelLoaded()
        {
            Dispatcher.Invoke(() =>
            {
                if (GeoView is MapView mv)
                {
                    _controller.FloorManager = mv.Map?.FloorManager;
                }
                else if (GeoView is SceneView sv)
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
        public bool AllLevelsSelected
        {
            get => _controller.AllLevelsSelect;
            set
            {
                if (value)
                {
                    _controller.SelectAllLevels();
                }
                else
                {
                    _controller.UndoSelectAllLevels();
                }
            }
        }
        #endregion Selection

        #region Read-only list properties

        /// <summary>
        /// Gets the list of available sites.
        /// </summary>
        public IList<FloorSite>? AllSites => GetValue(AllSitesPropertyKey.DependencyProperty) as IList<FloorSite>;

        private static readonly DependencyPropertyKey AllSitesPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(AllSites), typeof(IList<FloorSite>), typeof(FloorFilter), null);

        /// <summary>
        /// Gets the list of available facilities across all sites.
        /// </summary>
        /// <remarks>
        /// To get a list of all facilities in the selected site, see <see cref="SelectedSite"/> and <see cref="FloorSite.Facilities"/>.
        /// </remarks>
        public IList<FloorFacility>? AllFacilities => GetValue(AllFacilitiesPropertyKey.DependencyProperty) as IList<FloorFacility>;

        private static readonly DependencyPropertyKey AllFacilitiesPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(AllFacilities), typeof(IList<FloorFacility>), typeof(FloorFilter), null);

        /// <summary>
        /// Gets the list of available levels in the currently selected facility.
        /// </summary>
        public IList<FloorLevel>? AllLevels => GetValue(AllLevelsPropertyKey.DependencyProperty) as IList<FloorLevel>;

        private static readonly DependencyPropertyKey AllLevelsPropertyKey =
DependencyProperty.RegisterReadOnly(nameof(AllLevels), typeof(IList<FloorLevel>), typeof(FloorFilter), null);
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
            DependencyProperty.RegisterAttached("IsExpanded", typeof(bool), typeof(FloorFilter), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
        #endregion Attached Properties

        #region INPC

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Commands

        /// <summary>
        /// Gets the command that zooms to the currently-selected item.
        /// </summary>
        public ICommand ZoomCommand { get; }

        /// <summary>
        /// Gets the command that closes the browsing view.
        /// </summary>
        public ICommand CloseBrowseCommand { get; }

        /// <summary>
        /// Gets the command that navigates back to the sites view in the browsing view.
        /// </summary>
        public ICommand GoBackCommand { get; }

        /// <summary>
        /// Gets the command that navigates to the all sites view in the browsing view.
        /// </summary>
        public ICommand ShowAllFacilitiesCommand { get; }

        private class NotifyingDelegateCommand : ICommand
        {
            private Action _action;
            private bool _canExecute = true;

            public NotifyingDelegateCommand(Action action)
            {
                _action = action;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => _canExecute;

            public void Execute(object? parameter) => _action();

            internal void NotifyExecuteState(bool newValue)
            {
                _canExecute = newValue;
                CanExecuteChanged?.Invoke(this, new EventArgs());
            }
        }
        #endregion Commands

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

        private int _selectedTab = 0;

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
                (GoBackCommand as NotifyingDelegateCommand)?.NotifyExecuteState(true);
            }
            else
            {
                (GoBackCommand as NotifyingDelegateCommand)?.NotifyExecuteState(false);
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

                    break;
                case 1:
                    if (_facilitiesListView?.SelectedItem != null)
                    {
                        _facilitiesListView.ScrollIntoView(_facilitiesListView.SelectedItem);
                    }

                    break;
                case 2:
                    if (_allFacilitiesListView?.SelectedItem != null)
                    {
                        _allFacilitiesListView.ScrollIntoView(_allFacilitiesListView.SelectedItem);
                    }

                    break;
                case 3:
                    if (_facilitiesNoSitesListView?.SelectedItem != null)
                    {
                        _facilitiesNoSitesListView.ScrollIntoView(_facilitiesNoSitesListView.SelectedItem);
                    }

                    break;
            }
        }

        #endregion UI State Management
    }
}
#endif