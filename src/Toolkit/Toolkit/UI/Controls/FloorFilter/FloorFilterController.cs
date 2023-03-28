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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Floor;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI
#endif
{
    internal class FloorFilterController : INotifyPropertyChanged
    {
        // Internal configuration properties - currently not exposed anywhere
        private const ExternalLevelSelectionMode _levelSelectionMode = ExternalLevelSelectionMode.EquivalentLevelOrHide;
        private const NullSelectionLevelMode _nullSelectionMode = NullSelectionLevelMode.ShowGround;
        private const SelectionDrivenViewpointUpdateMode _requestedViewpointMode = SelectionDrivenViewpointUpdateMode.AlwaysSiteOrFacility;

        private FloorManager? _floorManager;
        private bool _displayFloorPicker;

        // Viewpoint delivered from the GeoView
        private Viewpoint? _observedViewpoint;

        // Viewpoint published to the GeoView
        private Viewpoint? _requestedViewpoint;

        private FloorLevel? _selectedLevel;
        private FloorFacility? _selectedFacility;
        private FloorSite? _selectedSite;

        private IList<FloorLevel>? _levelsForSelectedFacility;
        private IList<FloorFacility>? _facilitiesForSelectedSite;
        private IList<FloorSite>? _allSites;
        private IList<FloorFacility>? _allFacilities;

        private AutomaticSelectionMode _automaticSelectionMode = AutomaticSelectionMode.Always;

        // Enable easy toggle between all levels and single selected level in 3D
        private bool _allDisplayLevelsSelected;
        private FloorLevel? _previousSelection;

        public FloorManager? FloorManager
        {
            get => _floorManager;
            set => SetPropertyChanged(value, ref _floorManager, nameof(FloorManager), () => _ = HandleLoad());
        }

        /// <summary>
        /// Gets or sets a value indicating whether there is enough information to successfully display a FloorPicker.
        /// Observe this to dynamically hide/show the view as needed.
        /// </summary>
        public bool ShouldDisplayFloorPicker
        {
            get => _displayFloorPicker;
            set => SetPropertyChanged(value, ref _displayFloorPicker);
        }

        /// <summary>
        /// Gets a value indicating whether all levels should be visible simultaneously.
        /// </summary>
        public bool AllDisplayLevelsSelected => _allDisplayLevelsSelected;

        /// <summary>
        /// Gets or sets the viewpoint observed from an associated MapView or SceneView.
        /// Depending on configuration, setting this property will update selection.
        /// </summary>
        public Viewpoint? ObservedViewpoint
        {
            get => _observedViewpoint;
            set => SetPropertyChanged(value, ref _observedViewpoint, nameof(ObservedViewpoint), () => UpdateSelectionIfNeeded(true));
        }

        /// <summary>
        /// Gets the viewpoint requested by the floorfilter.
        /// Observe this property to handle viewpoint changes driven by selection or manual zoom.
        /// </summary>
        public Viewpoint? RequestedViewpoint
        {
            get => _requestedViewpoint;
            private set => SetPropertyChanged(value, ref _requestedViewpoint, nameof(RequestedViewpoint));
        }

        /// <summary>
        /// Gets the selected site. See <see cref="SetSelectedSite(FloorSite?, bool)"/> to change this.
        /// </summary>
        public FloorSite? SelectedSite => _selectedSite;

        /// <summary>
        /// Gets the selected level.
        /// </summary>
        public FloorLevel? SelectedLevel => _selectedLevel;

        /// <summary>
        /// Gets the levels for the currently selected facility. May be null or empty; not all facilites have levels.
        /// This collection is displayed in a level/floor list.
        /// </summary>
        public IList<FloorLevel>? DisplayLevels => _levelsForSelectedFacility;

        /// <summary>
        /// Gets the facilities for the selected site. Can be null or empty; not all sites have facilities.
        /// </summary>
        public IList<FloorFacility>? DisplayFacilities => _facilitiesForSelectedSite;

        /// <summary>
        /// Gets all of the facilities in the floor manager, for all sites.
        /// </summary>
        public IList<FloorFacility>? AllFacilities => _allFacilities;

        /// <summary>
        /// Gets all of the sites in the floor manager.
        /// </summary>
        public IList<FloorSite>? AllSites => _allSites;

        public AutomaticSelectionMode AutomaticSelectionMode
        {
            get => _automaticSelectionMode;
            set => SetPropertyChanged(value, ref _automaticSelectionMode);
        }

        /// <summary>
        /// Gets the selected facility.
        /// If sites are present, the set facility must be part of the already-selected site. Callers must enforce this requirement, or controller will be left in an inconsistent state.
        /// </summary>
        public FloorFacility? SelectedFacility => _selectedFacility;

        public void Reset()
        {
            // Clear all selections
            // Clear available lists
            SetSelectedLevel(null, false);
            SetDisplayLevels(null, false);
            SetSelectedFacility(null, false);
            SetDisplayFacilities(null, false);
            SetSelectedSite(null, false);
            SetAllSites(null, false);

            // Reset ShouldDisplayFloorPicker property
            ShouldDisplayFloorPicker = false;

            // Clear viewpoint
            RequestedViewpoint = null;
            ObservedViewpoint = null;
            _allDisplayLevelsSelected = false;
            _previousSelection = null;

            // Explicitly don't clear floor manager
        }

        /// <summary>
        /// Zoom to the current selection. Intended for use with an explicit "Zoom" button.
        /// </summary>
        public void ForceZoomToSelection()
        {
            Geometry.Geometry? geometry = null;
            if (SelectedLevel?.Geometry is Geometry.Geometry levelGeometry)
            {
                geometry = levelGeometry;
            }
            else if (SelectedFacility?.Geometry is Geometry.Geometry facilityGeometry)
            {
                geometry = facilityGeometry;
            }
            else if (SelectedSite?.Geometry is Geometry.Geometry siteGeometry)
            {
                geometry = siteGeometry;
            }

            // Consider zoom to facility if there is only one and there has been no selection
            // .
            if (geometry != null)
            {
                var newViewpoint = new Viewpoint(geometry);
                RequestedViewpoint = newViewpoint;
            }
        }

        /// <summary>
        /// Zoom to the new selection when appropriate. Intended for use with automatic zooming behavior (e.g. automatic zoom to selected facility).
        /// </summary>
        public void ZoomToSelection(FloorFacility selectedFacility, bool blockAutomaticZoom)
        {
            if (blockAutomaticZoom
                || _requestedViewpointMode == SelectionDrivenViewpointUpdateMode.Never
                || _requestedViewpointMode == SelectionDrivenViewpointUpdateMode.AlwaysSiteOnly
                || selectedFacility.Geometry == null)
            {
                return;
            }

            if (_requestedViewpointMode == SelectionDrivenViewpointUpdateMode.NotIntersects)
            {
#pragma warning disable CS0162 // Unreachable code detected
                var newViewpoint = new Viewpoint(selectedFacility.Geometry);
                if (ObservedViewpoint?.TargetGeometry != null && !Geometry.GeometryEngine.Intersects(newViewpoint.TargetGeometry, ObservedViewpoint!.TargetGeometry))
                {
                    RequestedViewpoint = newViewpoint;
                }
#pragma warning restore CS0162 // Unreachable code detected
            }
            else
            {
                RequestedViewpoint = new Viewpoint(selectedFacility.Geometry);
            }
        }

        public void ZoomToSelection(FloorSite selectedSite, bool blockAutomaticZoom)
        {
            if (blockAutomaticZoom
                || _requestedViewpointMode == SelectionDrivenViewpointUpdateMode.FacilityOnly
                || _requestedViewpointMode == SelectionDrivenViewpointUpdateMode.Never
                || selectedSite.Geometry == null)
            {
                return;
            }

            if (_requestedViewpointMode == SelectionDrivenViewpointUpdateMode.NotIntersects)
            {
#pragma warning disable CS0162 // Unreachable code detected
                var newViewpoint = new Viewpoint(selectedSite.Geometry);
                if (ObservedViewpoint?.TargetGeometry != null && !Geometry.GeometryEngine.Intersects(newViewpoint.TargetGeometry, ObservedViewpoint!.TargetGeometry))
                {
                    RequestedViewpoint = newViewpoint;
                }
#pragma warning restore CS0162 // Unreachable code detected
            }
            else
            {
                RequestedViewpoint = new Viewpoint(selectedSite.Geometry);
            }
        }

        public void ZoomToSelection(FloorLevel selectedLevel, bool blockAutomaticZoom)
        {
            if (blockAutomaticZoom
                || _requestedViewpointMode == SelectionDrivenViewpointUpdateMode.Never
                || _requestedViewpointMode == SelectionDrivenViewpointUpdateMode.AlwaysSiteOnly
                || _requestedViewpointMode == SelectionDrivenViewpointUpdateMode.FacilityOnly
                || _requestedViewpointMode == SelectionDrivenViewpointUpdateMode.AlwaysSiteOrFacility)
            {
                return;
            }

            if (_requestedViewpointMode == SelectionDrivenViewpointUpdateMode.NotIntersects)
            {
#pragma warning disable CS0162 // Unreachable code detected
                var newViewpoint = new Viewpoint(selectedLevel.Geometry);
                if (ObservedViewpoint?.TargetGeometry != null && !Geometry.GeometryEngine.Intersects(newViewpoint.TargetGeometry, ObservedViewpoint!.TargetGeometry))
                {
                    RequestedViewpoint = newViewpoint;
                }
#pragma warning restore CS0162 // Unreachable code detected
            }
            else
            {
                RequestedViewpoint = new Viewpoint(SelectedSite.Geometry);
            }
        }

        public void SetSelectedFacility(FloorFacility? newValue, bool blockAutomaticZoom)
        {
            if (newValue == _selectedFacility)
            {
                return;
            }

            // Reset selected level
            SetSelectedLevel(null, blockAutomaticZoom);

            // Select facility
            _selectedFacility = newValue;
            OnPropertyChanged(nameof(SelectedFacility));

            // Display levels for selected facility
            SetDisplayLevels(_selectedFacility?.Levels?.Reverse().ToList(), blockAutomaticZoom);

            if (_selectedFacility != null)
            {
                ZoomToSelection(_selectedFacility, blockAutomaticZoom);
            }
        }

        public void SetSelectedSite(FloorSite? newValue, bool blockAutomaticZoom)
        {
            if (newValue == _selectedSite)
            {
                return;
            }

            // Reset facility selection
            SetSelectedFacility(null, blockAutomaticZoom);

            // Select new site, apply the site's facility list
            SetPropertyChanged(newValue, ref _selectedSite, nameof(SelectedSite), () => SetDisplayFacilities(_selectedSite?.Facilities?.ToList(), blockAutomaticZoom));

            // Zoom if needed
            if (_selectedSite != null)
            {
                ZoomToSelection(_selectedSite, blockAutomaticZoom);
            }
        }

        public void SetSelectedLevel(FloorLevel? newValue, bool blockAutomaticZoom)
        {
            if (newValue == _selectedLevel)
            {
                return;
            }

            _selectedLevel = newValue;

            if (_selectedLevel != null)
            {
                SetPropertyChanged(false, ref _allDisplayLevelsSelected, nameof(AllDisplayLevelsSelected));
            }

            OnPropertyChanged(nameof(SelectedLevel));

            UpdateLevelVisibility();

            if (_selectedLevel != null)
            {
                ZoomToSelection(_selectedLevel, blockAutomaticZoom);
            }
        }

        public void SelectAllDisplayLevels()
        {
            _previousSelection = _selectedLevel;
            SetPropertyChanged(true, ref _allDisplayLevelsSelected, nameof(AllDisplayLevelsSelected), () => SetSelectedLevel(null, false));
        }

        /// <summary>
        /// Deselects all levels, then reselects the level that was selected at the time that <see cref="SelectAllDisplayLevels"/> was called.
        /// </summary>
        public void UndoSelectAllDisplayLevels()
        {
            SetPropertyChanged(false, ref _allDisplayLevelsSelected, nameof(AllDisplayLevelsSelected), () => SetSelectedLevel(_previousSelection, false));
        }

        private async Task HandleLoad()
        {
            Reset();
            if (FloorManager == null)
            {
                return;
            }

            try
            {
                await FloorManager.LoadAsync();

                // Populate sites
                if (FloorManager.Sites.Any())
                {
                    SetAllSites(FloorManager.Sites.ToList(), true);
                }

                if (FloorManager.Facilities.Any())
                {
                    ShouldDisplayFloorPicker = true;
                    _allFacilities = FloorManager.Facilities.ToList();
                    OnPropertyChanged(nameof(AllFacilities));
                }

                // If viewpoint set and automatic selection enabled, select site, facility, and floor
                UpdateSelectionIfNeeded(true);
            }
            catch (Exception)
            {
                // Load failure is expected in some cases
                ShouldDisplayFloorPicker = false;
            }
        }

        private void UpdateLevelVisibility()
        {
            // Extra work is done to avoid rapid flickering/map updates.
            Dictionary<FloorLevel, bool> visibilityState = new Dictionary<FloorLevel, bool>();
            if (_floorManager == null)
            {
                return;
            }

            // Basis for all modes
            foreach (var level in _floorManager.Levels)
            {
                visibilityState[level] = false;
            }

            if (AllDisplayLevelsSelected && SelectedFacility != null)
            {
                foreach (var level in SelectedFacility.Levels)
                {
                    visibilityState[level] = true;
                }
            }
            else if (SelectedLevel == null)
            {
                if (_nullSelectionMode == NullSelectionLevelMode.ShowGround)
                {
                    foreach (var facility in _floorManager.Facilities)
                    {
                        if (GroundFloorForFacility(facility) is FloorLevel level)
                        {
                            visibilityState[level] = true;
                        }
                    }
                }
            }
            else
            {
                var eligibleFacilities = AllFacilities?.Where(facility => facility != SelectedFacility && facility != SelectedLevel?.Facility).ToList() ?? new List<FloorFacility> { };
#pragma warning disable CS0162 // Unreachable code detected
                switch (_levelSelectionMode)
                {
                    case ExternalLevelSelectionMode.Hide:
                        // Already done
                        break;
                    case ExternalLevelSelectionMode.Ground:
                        foreach (var facility in eligibleFacilities)
                        {
                            if (GroundFloorForFacility(facility) is FloorLevel level)
                            {
                                visibilityState[level] = true;
                            }
                        }

                        break;
                    case ExternalLevelSelectionMode.EquivalentLevelOrHide:
                        foreach (var facility in eligibleFacilities)
                        {
                            if (facility.Levels.FirstOrDefault(l => l.VerticalOrder == SelectedLevel.VerticalOrder) is FloorLevel level)
                            {
                                visibilityState[level] = true;
                            }
                        }

                        break;
                    case ExternalLevelSelectionMode.EquivalentLevelOrGround:
                        foreach (var facility in eligibleFacilities)
                        {
                            if (facility.Levels.FirstOrDefault(l => l.VerticalOrder == SelectedLevel.VerticalOrder) is FloorLevel level)
                            {
                                visibilityState[level] = true;
                            }
                            else if (GroundFloorForFacility(facility) is FloorLevel glevel)
                            {
                                visibilityState[glevel] = true;
                            }
                        }

                        break;
                }
#pragma warning restore CS0162 // Unreachable code detected
                visibilityState[SelectedLevel] = true;

                SelectedLevel.IsVisible = true;
            }

            foreach (var level in _floorManager.Levels)
            {
                if (level.IsVisible != visibilityState[level])
                {
                    level.IsVisible = visibilityState[level];
                }
            }
        }

        private void UpdateSelectionIfNeeded(bool blockAutomaticZoom)
        {
            try
            {
                // Expectation: viewpoint is center and scale
                if (ObservedViewpoint == null || double.IsNaN(ObservedViewpoint.TargetScale) || AutomaticSelectionMode == AutomaticSelectionMode.Never)
                {
                    return;
                }

                // Only take action if viewpoint is within minimum scale. Default minscale is 4300 or less (~zoom level 17 or greater)
                double targetScale = FloorManager?.SiteLayer?.MinScale ?? 0;
                if (targetScale == 0)
                {
                    targetScale = 4300;
                }

                // If viewpoint is out of range, reset selection (if not non-clearing) and return;
                if (ObservedViewpoint.TargetScale > targetScale)
                {
                    if (AutomaticSelectionMode == AutomaticSelectionMode.Always)
                    {
                        SetSelectedLevel(null, blockAutomaticZoom);
                        SetSelectedFacility(null, blockAutomaticZoom);
                        SetSelectedSite(null, blockAutomaticZoom);
                    }

                    // Assumption: if too zoomed out to see sites, also too zoomed out to see facilities
                    return;
                }

                // If the centerpoint is within a site's geometry, select that site.
                // This code gracefully skips selection if there are no sites or no matching sites
                var result = FloorManager?.Sites.FirstOrDefault(site => site.Geometry?.Extent != null
                                                                && Geometry.GeometryEngine.Intersects(site.Geometry.Extent, ObservedViewpoint.TargetGeometry));
                if (result != null)
                {
                    SetSelectedSite(result, blockAutomaticZoom);
                }
                else if (AutomaticSelectionMode == AutomaticSelectionMode.Always)
                {
                    SetSelectedSite(null, blockAutomaticZoom);
                }

                // Move on to facility selection. Default to map-authored Facility MinScale. If MinScale not specified or is 0, default to 1500.
                targetScale = FloorManager?.FacilityLayer?.MinScale ?? 0;
                if (targetScale == 0)
                {
                    targetScale = 1500;
                }

                // If out of scale, stop here
                if (ObservedViewpoint.TargetScale > targetScale)
                {
                    return;
                }

                var facilityResult = FloorManager?.Facilities?.FirstOrDefault(facility => facility?.Geometry?.Extent != null
                                                                                && Geometry.GeometryEngine.Intersects(facility.Geometry.Extent, ObservedViewpoint.TargetGeometry));
                if (facilityResult != null)
                {
                    SetSelectedFacility(facilityResult, blockAutomaticZoom);
                }
                else if (AutomaticSelectionMode == AutomaticSelectionMode.Always)
                {
                    SetSelectedFacility(null, blockAutomaticZoom);
                }
            }
            catch (Exception)
            {
                // Ignored
            }
        }

        private void SetDisplayLevels(IList<FloorLevel>? newValue, bool blockAutomaticZoom)
            => SetPropertyChanged(newValue, ref _levelsForSelectedFacility, nameof(DisplayLevels), () => SelectFirstLevel(blockAutomaticZoom));

        private void SelectFirstLevel(bool blockAutomaticZoom)
        {
            if (_levelsForSelectedFacility != null && _levelsForSelectedFacility.Count == 1)
            {
                SetSelectedLevel(_levelsForSelectedFacility[0], blockAutomaticZoom);
            }
            else if (_levelsForSelectedFacility != null && SelectedFacility != null)
            {
                SetSelectedLevel(GroundFloorForFacility(SelectedFacility), blockAutomaticZoom);
            }
        }

        private void SetDisplayFacilities(IList<FloorFacility>? newValue, bool blockAutomaticZoom)
            => SetPropertyChanged(newValue, ref _facilitiesForSelectedSite, nameof(_facilitiesForSelectedSite), () => SelectSingleFacility(blockAutomaticZoom));

        private void SetAllSites(IList<FloorSite>? newValue, bool blockAutomaticZoom)
            => SetPropertyChanged(newValue, ref _allSites, nameof(AllSites), () => SelectSingleSite(blockAutomaticZoom));

        private void SelectSingleFacility(bool blockAutomaticZoom)
        {
            if (_facilitiesForSelectedSite?.Count == 1)
            {
                SetSelectedFacility(_facilitiesForSelectedSite[0], blockAutomaticZoom);
            }
        }

        /// <summary>
        /// If there is only one site, and configuration allows, select that site.
        /// </summary>
        private void SelectSingleSite(bool blockAutomaticZoom)
        {
            // Future: expose property to control this behavior
            if (_allSites?.Count == 1)
            {
                SetSelectedSite(_allSites[0], blockAutomaticZoom);
            }
        }

        /// <summary>
        /// Gets the ground floor, or the only floor, in the input facility.
        /// May return null level; some facilities do not have any levels.
        /// </summary>
        private static FloorLevel? GroundFloorForFacility(FloorFacility inputFacility)
        {
            if (inputFacility.Levels.FirstOrDefault(level => level.VerticalOrder == 0) is FloorLevel groundLevel)
            {
                return groundLevel;
            }

            return inputFacility.Levels.FirstOrDefault();
        }

        private void SetPropertyChanged<T>(T value, ref T field, [CallerMemberName] string propertyName = "", Action? action = null)
        {
            if (!Equals(value, field))
            {
                field = value;
                OnPropertyChanged(propertyName);
                action?.Invoke();
            }
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    internal enum SelectionDrivenViewpointUpdateMode
    {
        Never,
        Always,

        // Only zoom if the observed viewpoint doesn't intersect with the selection's viewpoint
        NotIntersects,

        // Only zoom to the site level, not to facilities or floors
        AlwaysSiteOnly,

        // Only zoom to the site or facility level, not to floors
        AlwaysSiteOrFacility,

        FacilityOnly,
    }

    /// <summary>
    /// Defines how levels should be displayed when there is no selection.
    /// </summary>
    internal enum NullSelectionLevelMode
    {
        HideAll,
        ShowGround,
    }

    /// <summary>
    /// Defines FloorFilter manages the selection of levels in facilities that aren't currently selected.
    /// </summary>
    internal enum ExternalLevelSelectionMode
    {
        Hide,
        EquivalentLevelOrHide,
        EquivalentLevelOrGround,
        Ground,
    }
}
