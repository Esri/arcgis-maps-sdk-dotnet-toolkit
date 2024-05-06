# OverviewMap

OverviewMap is a small, secondary map (sometimes called an inset map), that can be superimposed on an existing `MapView`/`SceneView`. OverviewMap shows a representation of the viewpoint of the `GeoView` it is connected to.

![OverviewMap](https://user-images.githubusercontent.com/29742178/121975740-34f07000-cd37-11eb-9162-462925cb3fe7.png)

> **NOTE**: OverviewMap uses metered ArcGIS basemaps by default, so you will need to configure an API key. See [Security and authentication documentation](https://developers.arcgis.com/documentation/mapping-apis-and-services/security/#api-keys) for more information.

## Features

OverviewMap:

- Displays a representation of the current viewpoint for a connected GeoView
- Supports a configurable scaling factor for setting the overview map's zoom level relative to the connected view.
- Supports a configurable symbol for visualizing the current viewpoint
- Supports two-way navigation, so the user can navigate the connected GeoView by panning and zooming the overview.
- Exposes a `Map` property to allow use of a custom basemap. Defaults to an empty map with a topographic basemap.

## Key properties

OverviewMap has the following bindable properties:

- `AreaSymbol` - Defines the symbol used to visualize the current viewpoint when connected to a map. This is a red rectangle by default.
- `GeoView` - References the connected MapView or SceneView
- `Map` - Defines the map shown in the inset/overview.
- `PointSymbol` - Defines the symbol used to visualize the current viewpoint when connected to a scene. This is a red cross by default.
- `ScaleFactor` - Defines the scale of the OverviewMap relative to the scale of the connected `GeoView`. The default is 25.

## Usage

### .NET MAUI:

```xml
<Grid xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013">
    <esri:MapView x:Name="MyMapView" />
    <esri:OverviewMap Margin="4"
                      GeoView="{x:Reference MyMapView}" />
</Grid>
```

### WinUI/UWP:

```xml
<Grid xmlns:esri="using:Esri.ArcGISRuntime.UI.Controls"
      xmlns:toolkit="using:Esri.ArcGISRuntime.Toolkit.UI.Controls">
    <esri:MapView x:Name="MyMapView" />
    <toolkit:OverviewMap Margin="4"
                         GeoView="{x:Bind MyMapView}" />
</Grid>
```

### WPF:

```xml
<Grid xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013">
    <esri:MapView x:Name="MyMapView" />
    <esri:OverviewMap Margin="4"
                      GeoView="{Binding ElementName=MyMapView}" />
</Grid>
```
