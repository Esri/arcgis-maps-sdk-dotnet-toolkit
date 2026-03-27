# MeasureToolbar

Measure distances, areas, and features in a `MapView`.

![MeasureToolbar](https://user-images.githubusercontent.com/1378165/73389958-0f6dde00-428a-11ea-8c78-7192d49ea605.png)

## Features

- Allows selection and changing of unit of measure.
- Enables manually drawing and measuring lines (distances) and polygons (areas).
- Supports selecting and measuring features.
- Binds to a `MapView`.

## Usage

### WinUI:

```xml
<Grid xmlns:esri="using:Esri.ArcGISRuntime.UI.Controls"
      xmlns:toolkit="using:Esri.ArcGISRuntime.Toolkit.UI.Controls">
    <Grid.RowDefinitions>
        <RowDefinition Height="50" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <toolkit:MeasureToolbar HorizontalAlignment="Center"
                            MapView="{x:Bind MyMapView}" />
    <esri:MapView x:Name="MyMapView"
                  Grid.Row="1" />
</Grid>
```

### WPF:

```xml
<Grid xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013">
    <Grid.RowDefinitions>
        <RowDefinition Height="50" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <esri:MeasureToolbar HorizontalAlignment="Center"
                         MapView="{Binding ElementName=MyMapView}" />
    <esri:MapView x:Name="MyMapView"
                  Grid.Row="1" />
</Grid>
```
