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
<Grid>
    <esri:MapView x:Name="mapView" />
    <toolkit:ScaleLine Margin="20" MapView="{x:Reference mapView}" />
</Grid>
```

### UWP/WinUI:

```xml
<Grid>
    <esri:MapView x:Name="mapView" />
    <toolkit:ScaleLine Margin="20" MapView="{Binding ElementName=mapView}" />
</Grid>
```

### WPF:

The usage in WPF is identical to UWP/WinUI minus one important distinction. The `ScaleLine` should be accessed with the same prefix as the `GeoView`. 

```xml
xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013"
```
