# Legend

Display a legend for a map or scene view.

<img src="https://user-images.githubusercontent.com/1378165/73389924-011fc200-428a-11ea-91bf-4ea1c2bf6683.png" width="105" title="Legend" />

## Features

- Binds to a `GeoView`.
- Enables filtering out layers that are hidden because they are out of scale via the `FilterByVisibleScaleRange` property.
- Enables filtering out hidden layers via the `FilterHiddenLayers` property.
- Supports reversing the order of displayed layers via the `ReverseLayerOrder` property.

## Usage

### .NET MAUI:

```xml
<Grid ColumnDefinitions="*,300">
    <esri:MapView x:Name="mapView"/>
    <toolkit:Legend GeoView="{x:Reference mapView}" Grid.Column="1" />
</Grid>
```

### UWP/WinUI:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="300"/>
    </Grid.ColumnDefinitions>
    <esri:MapView x:Name="mapView"/>
    <toolkit:Legend GeoView="{Binding ElementName=mapView}" Grid.Column="1" />
</Grid>
```

### WPF:

The usage in WPF is identical to UWP/WinUI minus one important distinction. The `Legend` should be accessed with the same prefix as the `GeoView`.
