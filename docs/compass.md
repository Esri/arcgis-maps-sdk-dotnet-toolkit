# Compass

Show a compass direction for a `GeoView`. Resets the view orientation when tapped/clicked.

![compass](https://user-images.githubusercontent.com/1378165/73389839-d9c8f500-4289-11ea-923c-18232489b3e0.png)

## Features

- Supports binding to a `GeoView`.
- Can be configured to hide itself automatically when the heading is 0 via the `AutoHide` property.
- Allows display of a manually-set heading via the `Heading` property.

## Usage

### .NET MAUI:

```xml
<Grid>
    <esri:MapView x:Name="mapView" />
    <toolkit:Compass Margin="15"
                     AutoHide="False"
                     GeoView="{x:Reference mapView}"
                     HeightRequest="50"
                     WidthRequest="50" />
</Grid>
```

### UWP/WinUI:

```xml
<Grid>
    <esri:MapView x:Name="mapView" />
    <toolkit:Compass Width="50"
                     Height="50"
                     Margin="15"
                     AutoHide="False"
                     GeoView="{Binding ElementName=mapView}" />
</Grid>
```

### WPF:

The usage in WPF is identical to UWP/WinUI minus one important distinction. The `Compass` should be accessed with the same prefix as the `GeoView`. 

```xml
xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013"
```
