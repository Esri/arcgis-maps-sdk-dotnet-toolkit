# MeasureToolbar

Measure distances, areas, and features in a `MapView`.

![MeasureToolbar](https://user-images.githubusercontent.com/1378165/73389958-0f6dde00-428a-11ea-8c78-7192d49ea605.png)

## Features

- Allows selection and changing of unit of measure.
- Enables manually drawing and measuring lines (distances) and polygons (areas).
- Supports selecting and measuring features.
- Binds to a `MapView`.

## Usage

### UWP/WinUI:

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="50" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <toolkit:MeasureToolbar HorizontalAlignment="Center" MapView="{Binding ElementName=mapView}" />
    <esri:MapView x:Name="mapView" Grid.Row="1" />
</Grid>
```

### WPF:

The usage in WPF is identical to UWP/WinUI minus one important distinction. The `MeasureToolbar` should be accessed with the same prefix as the `GeoView`.
