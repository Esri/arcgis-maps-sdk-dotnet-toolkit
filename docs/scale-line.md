# ScaleLine

Display the current scale reference for a map.

![ScaleLine](https://user-images.githubusercontent.com/1378165/73390077-3debb900-428a-11ea-8b2f-dfd4914a637e.png)

## Features

- Supports binding to a `MapView`.
- Supports display of an arbitrary scale via the `MapScale` property.
- Displays both metric and imperial units.

## Usage

Ensure that your `GeoModel` is not null before selecting a basemap with the `BasemapGallery`.

### .NET MAUI:

```xml
<Grid xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013">
    <esri:MapView x:Name="MyMapView" />
    <esri:ScaleLine Margin="20"
                    MapView="{x:Reference MyMapView}" />
</Grid>
```

### UWP/WinUI:

```xml
<Grid xmlns:esri="using:Esri.ArcGISRuntime.UI.Controls"
      xmlns:toolkit="using:Esri.ArcGISRuntime.Toolkit.UI.Controls">
    <esri:MapView x:Name="MyMapView" />
    <toolkit:ScaleLine Margin="20"
                       MapView="{Binding ElementName=MyMapView}" />
</Grid>
```

### WPF:

```xml
<Grid xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013">
    <esri:MapView x:Name="MyMapView" />
    <esri:ScaleLine Margin="20"
                    MapView="{Binding ElementName=MyMapView}" />
</Grid>
```
