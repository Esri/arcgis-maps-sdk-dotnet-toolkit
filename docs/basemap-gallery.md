# BasemapGallery

BasemapGallery displays a collection of basemaps from ArcGIS Online, a user-defined portal, or a custom collection. When a basemap is selected from the gallery, the basemap used in the connected GeoModel is replaced.

![BasemapGallery List View](https://user-images.githubusercontent.com/29742178/124198151-f2dc6380-da84-11eb-8e78-4e705d14c33d.png)

![BasemapGallery Grid View](https://user-images.githubusercontent.com/29742178/124198175-ff60bc00-da84-11eb-9a41-6b85a6ed89fd.png)

> **NOTE**: BasemapGallery uses metered ArcGIS basemaps by default, so you will need to configure an API key. See [Security and authentication documentation](https://developers.arcgis.com/documentation/mapping-apis-and-services/security/#api-keys) for more information.

## Features

BasemapGallery:

- Can be configured to use a list, grid, or automatic layout. When using an automatic layout, list or grid presentation is chosen based on a defined width threshold.
    - Note: Grid layout is not supported on MAUI WinUI. Regardless of settings, the list layout will always be used.
- Shows basemaps from a portal, and allows manually adding and removing basemaps from the collection.
- Shows a representation of the map or scene's current basemap if that basemap exists in the gallery.
- Shows a name and thumbnail for each basemap.
- Shows a tooltip on hover on WPF and UWP.
- Supports templating.

## Key properties

BasemapGallery exposes the following properties:

- `GeoModel` - References the connected Map or Scene (optional).
- `Portal` - Controls which portal is used to find basemaps. Setting the portal will reset the basemap collection.
- `AvailableBasemaps` - Collection of basemaps being shown.

The following properties enable customization of the gallery's appearance:

- `GalleryViewStyle` - Controls whether the gallery is displayed as a list or a grid.
- `GridItemTemplate` - Template used to display basemaps when using a grid presentation.
- `ListItemTemplate` - Template used to display basemaps when using a list presentation.
- `ListItemContainerStyle` - Container style used when displaying items as a list. Does not apply to MAUI.
- `GridItemContainerStyle` - Container style used when displaying items as a grid. Does not apply to MAUI.

## Usage

Ensure that your `MapView.Map` is not null before selecting a basemap with the `BasemapGallery`.

### UWP/WinUI:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="300" />
    </Grid.ColumnDefinitions>
    <esri:MapView x:Name="mapView" />
    <toolkit:BasemapGallery Grid.Column="1" GeoModel="{Binding ElementName=mapView, Path=Map}" />
</Grid>
```

### WPF:

The usage in WPF is identical to UWP/WinUI minus one important distinction. The `BasemapGallery` toolkit component should be accessed with the same prefix as the `MapView`. 

```xml
xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013"
```

### .NET MAUI:

```xml
<Grid ColumnDefinitions="*,300">
    <esri:MapView x:Name="mapView" />
    <toolkit:BasemapGallery Grid.Column="1" GeoModel="{Binding Source={x:Reference mapView}, Path=Map}"/>
</Grid>
```
