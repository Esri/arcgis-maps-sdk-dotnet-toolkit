# FloorFilter

FloorFilter allows users to take advantage of floor-aware maps and scenes by surfacing and then filtering floor data.

![image](https://user-images.githubusercontent.com/29742178/158746908-71a39e28-596f-44b6-9230-e2a04bdaeb9e.png)

## Features

- Automatically hides the floor browsing view when the associated map or scene is not floor-aware
- Selects the facility in view automatically
    - This behavior can be configured through the `AutomaticSelectionMode` property
- View is space-efficient and can be expanded/collapsed (WPF only)
- Shows the selected facility's levels in proper vertical order
- Filters the map/scene content to show the selected level
- Allows browsing the full floor-aware hierarchy of sites, facilities, and levels
    - When consuming simpler maps, simplifies the UI as needed; for example, the site browsing experience is hidden when there is only one (or no) sites
- When browsing, allows dynamic filtering of lists
- Shows the ground floor of all facilities when there is no active selection
- Updates the visibility of floor levels across all facilities (e.g. if you are looking at floor 3 in building A, floor 3 will be shown in neighboring buildings)
- Exposes a full range of template and style properties for easy customization
- Adjusts layout and presentation to work well regardless of positioning - left/right and top/bottom
- Keeps the selected facility visible in the list while the selection is changing in response to map navigation

## Customization

> **NOTE**: Some properties aren't available on all platforms due to platform-specific differences in how the view is implemented.

The following properties enable customization of the view:

- `ZoomToButtonStyle`
- `BrowseButtonStyle`
- `ExpandCollapseButtonStyle`
- `CommonListStyle` - style applied to all lists (levels, sites, facilities)
- `LevelDataTemplate` - displays levels in the non-browsing floor filter experience
- `SiteDataTemplate` - displays sites in the browsing view
- `FacilityDataTemplate` - displays facilities in the browsing view
- `DifferentiatingFacilityDataTemplate` - displays facilities in the list of all facilities from all sites

The following properties enable customization or localization of text displayed in the view:

- `BackButtonLabel`
- `AllFacilitiesLabel`
- `AllFloorsLabel`
- `CloseLabel`
- `ExpandLabel`
- `CollapseLabel`
- `ZoomToLabel`
- `BrowseLabel`
- `NoResultsMessage`
- `SearchPlaceholder`

## Usage

### .NET MAUI:

```xml
<Grid>
    <esri:MapView x:Name="mapView" />
    <toolkit:FloorFilter GeoView="{x:Reference mapView}" />
</Grid>
```

### UWP/WinUI:

```xml
<Grid>
    <esri:MapView x:Name="mapView" />
    <esri:FloorFilter GeoView="{Binding ElementName=mapView}" />
</Grid>
```

### WPF:

The usage in WPF is identical to UWP/WinUI minus one important distinction. The `FloorFilter` should be accessed with the same prefix as the `GeoView`. 

```xml
xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013"
```
