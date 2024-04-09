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
<Grid xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013">
    <esri:MapView x:Name="MyMapView" />
    <esri:Compass Margin="15"
                     AutoHide="False"
                     GeoView="{x:Reference MyMapView}"
                     HeightRequest="50"
                     WidthRequest="50" />
</Grid>
```

### UWP/WinUI:

```xml
<Grid xmlns:esri="using:Esri.ArcGISRuntime.UI.Controls" 
      xmlns:toolkit="using:Esri.ArcGISRuntime.Toolkit.UI.Controls">
    <esri:MapView x:Name="MyMapView" />
    <toolkit:Compass Width="50"
                     Height="50"
                     Margin="15"
                     AutoHide="False"
                     GeoView="{Binding ElementName=MyMapView}" />
</Grid>
```

### WPF:

```xml
<Grid xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013">
    <esri:MapView x:Name="MyMapView" />
    <esri:Compass Width="50"
                     Height="50"
                     Margin="15"
                     AutoHide="False"
                     GeoView="{Binding ElementName=MyMapView}" />
</Grid>
```
