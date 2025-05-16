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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Floor;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI;
#endif

/// <summary>
/// FloorFilter provides a browsing experience for floor-aware maps and scenes, and allows you to filter the view to show a single floor/level.
/// </summary>
public partial class FloorFilter : TemplatedView
{
    private int _navigationStackCounter;
    private readonly FloorFilterController _controller = new FloorFilterController();

    // View wraps everything else, to enable FloorFilter to automatically hide itself when used with a non-floor-aware map/scene.
#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SX1309 // Field names should begin with underscore
    private View? PART_VisibilityWrapper;
    private View? PART_LevelListContainer;

    private Button? PART_ZoomButton;
    private Button? PART_AllButton;
    private Button? PART_BrowseButton;

    // Most ListView selection properties are set in XAML, but special event behavior is needed for the browsing view navigation experience.
    private CollectionView? PART_LevelListView;
#pragma warning restore SX1309 // Field names should begin with underscore
#pragma warning restore SA1310 // Field names should not contain underscore
#pragma warning restore SA1306 // Field names should begin with lower-case letter

    private bool _blockObservationOfViewpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="FloorFilter"/> class.
    /// </summary>
    public FloorFilter()
    {
        LevelDataTemplate = DefaultLevelDataTemplate;
        FacilityDataTemplate = DefaultFacilityDataTemplate;
        SiteDataTemplate = DefaultSiteDataTemplate;
        DifferentiatingFacilityDataTemplate = DefaultDifferentiatingFacilityDataTemplate;
        ControlTemplate = DefaultControlTemplate;

        // TODO = confirm this is needed
        BindingContext = this;

        _controller.AutomaticSelectionMode = AutomaticSelectionMode;
        _controller.PropertyChanged += HandleControllerPropertyChanges;
    }

    private void InitializeLocalizedStrings()
    {
        BrowseLabel = Properties.Resources.GetString("FloorFilterBrowse");
        BrowseSitesLabel = Properties.Resources.GetString("FloorFilterBrowseSites");
        BrowseFacilitiesLabel = Properties.Resources.GetString("FloorFilterBrowseFacilities");
        NoResultsMessage = Properties.Resources.GetString("FloorFilterNoResultsFound");
        AllFacilitiesLabel = Properties.Resources.GetString("FloorFilterAllFacilities");
        SearchPlaceholder = Properties.Resources.GetString("FloorFilterFilter");
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        if (PART_ZoomButton != null)
        {
            PART_ZoomButton.Clicked -= HandleZoomButtonClick;
            PART_ZoomButton = null;
        }

        if (PART_BrowseButton != null)
        {
            PART_BrowseButton.Clicked -= PART_BrowseButton_Clicked;
            PART_BrowseButton = null;
        }

        PART_AllButton = null;
        PART_LevelListContainer = null;

        base.OnApplyTemplate();

        PART_VisibilityWrapper = GetTemplateChild(nameof(PART_VisibilityWrapper)) as View;
        PART_VisibilityWrapper?.SetValue(IsVisibleProperty, _controller.ShouldDisplayFloorPicker);

        if (GetTemplateChild(nameof(PART_ZoomButton)) is Button zoomButton)
        {
            PART_ZoomButton = zoomButton;
            PART_ZoomButton.Clicked += HandleZoomButtonClick;
        }

        PART_LevelListView = GetTemplateChild(nameof(PART_LevelListView)) as CollectionView;
        PART_BrowseButton = GetTemplateChild(nameof(PART_BrowseButton)) as Button;
        PART_AllButton = GetTemplateChild(nameof(PART_AllButton)) as Button;
        PART_LevelListContainer = GetTemplateChild(nameof(PART_LevelListContainer)) as View;
        PART_LevelListContainer?.SetValue(IsVisibleProperty, false);
        if (PART_BrowseButton != null)
        {
            PART_BrowseButton.Clicked += PART_BrowseButton_Clicked;
        }

        if (PART_LevelListView != null)
        {
            PART_LevelListView.ItemTemplate = LevelDataTemplate;
            PART_LevelListView.BindingContext = this;
            PART_LevelListView.SetBinding(CollectionView.ItemsSourceProperty, nameof(DisplayLevels), BindingMode.OneWay);
            PART_LevelListView.SetBinding(CollectionView.SelectedItemProperty, static (FloorFilter filter) => filter.SelectedLevel, BindingMode.TwoWay);
        }
    }

    private void PART_BrowseButton_Clicked(object? sender, EventArgs e)
    {
        if (AllSites?.Count > 1)
        {
            NavigateForward(new FloorFilterBrowseSitesPage(this));
        }
        else
        {
            NavigateForward(new FloorFilterBrowseFacilitiesPage(this, true, false));
        }
    }

    internal async void NavigateForward(ContentPage page)
    {
        try
        {
            await Navigation.PushAsync(page);
            _navigationStackCounter++;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Toolkit exception while navigating - {ex}");
        }
    }

    internal async void CloseBrowsing()
    {
        while (_navigationStackCounter > 0)
        {
            try
            {
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Toolkit exception while navigating - {ex}");
            }
            finally
            {
                _navigationStackCounter--;
            }
        }
    }

    internal async void GoBack()
    {
        try
        {
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Toolkit exception while navigating - {ex}");
        }
        finally
        {
            _navigationStackCounter--;
        }
    }

    private void HandleZoomButtonClick(object? sender, EventArgs e) => _controller?.ForceZoomToSelection();

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
                OnPropertyChanged(nameof(SelectedLevel));
                UpdateLevelListSize();
                break;
            case nameof(_controller.AllFacilities):
                AllFacilities = _controller.AllFacilities;
                break;
            case nameof(_controller.AllSites):
                AllSites = _controller.AllSites;
                break;
            case nameof(_controller.DisplayLevels):
                DisplayLevels = _controller.DisplayLevels;
                break;
            case nameof(_controller.RequestedViewpoint):
                SetViewpointFromControllerAsync();
                break;
            case nameof(_controller.ShouldDisplayFloorPicker):
                PART_VisibilityWrapper?.SetValue(IsVisibleProperty, _controller.ShouldDisplayFloorPicker);
                break;
            case nameof(_controller.AllDisplayLevelsSelected):
                OnPropertyChanged(nameof(AllDisplayLevelsSelected));
                break;
        }
    }

    private void HandleLevelsListChanges()
    {
        PART_AllButton?.SetValue(IsVisibleProperty, ShowAllFloorsButton);

        if (DisplayLevels == null || DisplayLevels.Count == 0)
        {
            PART_LevelListContainer?.SetValue(IsVisibleProperty, false);
        }
        else
        {
            PART_LevelListContainer?.SetValue(IsVisibleProperty, true);
        }

        UpdateLevelListSize();
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

    private void HandleControlDrivenFacilitySelection(object? sender, ItemTappedEventArgs e)
    {
        Navigation.PopModalAsync();
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
    public static readonly BindableProperty GeoViewProperty =
        BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(FloorFilter), propertyChanged: OnGeoViewPropertyChanged);

    private static void OnGeoViewPropertyChanged(BindableObject d, object oldValue, object newValue) =>
        ((FloorFilter)d).SetGeoView(oldValue as GeoView, newValue as GeoView);

    private void SetGeoView(GeoView? oldView, GeoView? newView)
    {
        if (oldView == newView)
        {
            return;
        }

        if (oldView != null)
        {
            oldView.PropertyChanged -= Handle_GeoModelChanged;
            oldView.ViewpointChanged -= HandleGeoViewViewpointChanged;
            oldView.NavigationCompleted -= HandleGeoViewNavigationCompleted;
        }

        if (newView != null)
        {
            newView.PropertyChanged += Handle_GeoModelChanged;

            newView.ViewpointChanged += HandleGeoViewViewpointChanged;
            newView.NavigationCompleted += HandleGeoViewNavigationCompleted;

            // Handle case where geoview loads map while events are being set up
            HandleGeoModelChanged(nameof(MapView.Map));
        }

        PART_AllButton?.SetValue(IsVisibleProperty, ShowAllFloorsButton);
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

    private void Handle_GeoModelChanged(object? sender, PropertyChangedEventArgs e) => HandleGeoModelChanged(e.PropertyName);

    private void HandleGeoModelChanged(string? propertyName)
    {
        if (propertyName != nameof(MapView.Map) && propertyName != nameof(SceneView.Scene))
        {
            return;
        }

        _controller.Reset();
        CloseBrowsing();
        PART_AllButton?.SetValue(IsVisibleProperty, ShowAllFloorsButton);
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
        Dispatcher.Dispatch(() =>
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
    public static readonly BindableProperty SelectedSiteProperty =
        BindableProperty.Create(nameof(SelectedSite), typeof(FloorSite), typeof(FloorFilter), null, propertyChanged: OnSelectedSitePropertyChanged);

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
    public static readonly BindableProperty SelectedFacilityProperty =
        BindableProperty.Create(nameof(SelectedFacility), typeof(FloorFacility), typeof(FloorFilter), null, propertyChanged: OnSelectedFacilityPropertyChanged);

    /// <summary>
    /// Gets or sets the selected level.
    /// </summary>
    public FloorLevel? SelectedLevel
    {
        get => _controller.SelectedLevel;
        set => _controller.SetSelectedLevel(value, false);
    }

    private static void OnSelectedSitePropertyChanged(BindableObject d, object oldValue, object newValue)
    {
        if (d is FloorFilter floorFilter)
        {
            FloorSite? newSite = newValue as FloorSite;
            floorFilter._controller.SetSelectedSite(newSite, false);
        }
    }

    private static void OnSelectedFacilityPropertyChanged(BindableObject d, object oldValue, object newValue)
    {
        if (d is FloorFilter filter)
        {
            FloorFacility? newFacility = newValue as FloorFacility;
            if (newFacility?.Site is FloorSite newSite)
            {
                filter._controller.SetSelectedSite(newSite, false);
            }

            filter._controller.SetSelectedFacility(newFacility, false);
            filter.PART_AllButton?.SetValue(IsVisibleProperty, filter.ShowAllFloorsButton);
        }
    }

    /// <summary>
    /// Sets the <see cref="SelectedSite"/> without triggering a GeoView navigation.
    /// </summary>
    public void SetSelectedSiteWithoutZoom(FloorSite newSelection) => _controller.SetSelectedSite(newSelection, true);

    /// <summary>
    /// Sets the <see cref="SelectedFacility"/> without triggering a GeoView navigation.
    /// </summary>
    public void SetSelectedFacilityWithoutZoom(FloorFacility newSelection) => _controller.SetSelectedFacility(newSelection, true);

    /// <summary>
    /// Sets the <see cref="SelectedLevel"/> without triggering a GeoView navigation.
    /// </summary>
    public void SetSelectedLevelWithoutZoom(FloorLevel newLevel) => _controller.SetSelectedLevel(newLevel, true);

    /// <summary>
    /// Gets or sets a value indicating whether all of the levels for the selected facility should be enabled for display.
    /// </summary>
    /// <remarks>
    /// This is used for showing an entire facility in 3D.
    /// </remarks>
    public bool AllDisplayLevelsSelected
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
    private IList<FloorLevel>? _displayLevels;
    private IList<FloorSite>? _allSites;
    private IList<FloorFacility>? _allFacilities;

    /// <summary>
    /// Gets the list of available sites.
    /// </summary>
    public IList<FloorSite>? AllSites
    {
        get => _allSites;
        private set
        {
            if (value != _allSites)
            {
                _allSites = value;
                OnPropertyChanged(nameof(AllSites));
            }
        }
    }

    /// <summary>
    /// Gets the list of available facilities across all sites.
    /// </summary>
    /// <remarks>
    /// To get a list of all facilities in the selected site, see <see cref="SelectedSite"/> and <see cref="FloorSite.Facilities"/>.
    /// </remarks>
    public IList<FloorFacility>? AllFacilities
    {
        get => _allFacilities;
        private set
        {
            if (value != _allFacilities)
            {
                _allFacilities = value;
                OnPropertyChanged(nameof(AllFacilities));
            }
        }
    }

    /// <summary>
    /// Gets the list of available levels in the currently selected facility.
    /// </summary>
    public IList<FloorLevel>? DisplayLevels
    {
        get => _displayLevels;
        private set
        {
            if (value != _displayLevels)
            {
                _displayLevels = value;
                HandleLevelsListChanges();
                OnPropertyChanged(nameof(DisplayLevels));
            }
        }
    }

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
    public static readonly BindableProperty AutomaticSelectionModeProperty =
        BindableProperty.Create(nameof(AutomaticSelectionMode), typeof(AutomaticSelectionMode), typeof(FloorFilter), AutomaticSelectionMode.Always, propertyChanged: OnAutomaticSelectionModePropertyChanged);

    private static void OnAutomaticSelectionModePropertyChanged(BindableObject d, object oldValue, object newValue) =>
        ((FloorFilter)d)._controller.AutomaticSelectionMode = (AutomaticSelectionMode)newValue;
    #endregion Configuration

    #region UI State Management

    /// <summary>
    /// Updates the size of the level list and manages scroll position and scroll bar visibility
    /// to ensure a good experience regardless of whether the list should be scrollable.
    /// </summary>
    private void UpdateLevelListSize()
    {
        if (PART_LevelListView != null)
        {
            if (DisplayLevels == null || DisplayLevels.Count < 1)
            {
                PART_LevelListContainer?.SetValue(IsVisibleProperty, false);
            }

            const int maxHeight = 320;
            var desiredHeight = (_displayLevels?.Count ?? 0) * 48;
#if _IOS__ || __MACCATALYST__
            var limitedHeight = Math.Min(maxHeight, desiredHeight);
            PART_LevelListView.VerticalOptions = LayoutOptions.End;
            PART_LevelListView.HeightRequest = limitedHeight;
            PART_LevelListContainer?.SetValue(MaximumHeightRequestProperty, limitedHeight + 2);
#endif
            if (desiredHeight > maxHeight)
            {
                PART_LevelListView.VerticalScrollBarVisibility = ScrollBarVisibility.Always;

                // ScrollTo causes fail fast excepion in kernelbase.dll
#if !WINDOWS
                if (SelectedLevel != null && DisplayLevels != null)
                {
                    PART_LevelListView?.ScrollTo(DisplayLevels.IndexOf(SelectedLevel));
                }
#endif
            }
            else if (DisplayLevels?.Any() == true)
            {
                PART_LevelListView.VerticalScrollBarVisibility = ScrollBarVisibility.Never;
#if !WINDOWS
                PART_LevelListView?.ScrollTo(DisplayLevels.Count - 1);
#endif
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the floor filter should display an 'All Floors' button.
    /// </summary>
    /// <remarks>The 'All Floors' button is useful in 3D.</remarks>
    public bool ShowAllFloorsButton => GeoView is SceneView sv && sv.Scene is not null && SelectedFacility != null && SelectedFacility.Levels.Count > 1;
#endregion UI State Management
}