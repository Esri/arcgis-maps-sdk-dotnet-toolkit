# UtilityNetworkTraceTool

Use named trace configurations defined in a web map to perform connected trace operations and compare results.

<img width="400" src="https://user-images.githubusercontent.com/29742178/173907143-0226ddcf-1104-4745-96a5-eef74fd02197.png">

## Features

- Load networks and named trace configurations from a web map
- Support for templating
- (UWP, WPF) Identify starting point candidates, then use the inspection view to narrow the selection:

    <img width="400" src="https://user-images.githubusercontent.com/29742178/173909691-57d8310d-264f-41dc-9cdd-6bd6f0495fd2.png">

- (UWP, WPF) Run multiple trace scenarios, then use color and name to compare results:

    <img width="400" src="https://user-images.githubusercontent.com/29742178/173907143-0226ddcf-1104-4745-96a5-eef74fd02197.png">

- User-friendly warnings help avoid common mistakes, including specifying too many starting points or running the same trace configuration multiple times:

    | Duplicated trace | Too few starting points | Extra starting points |
    |------------------|-------------------------|-----------------------|
    | <img width="300" src="https://user-images.githubusercontent.com/29742178/173909348-f7fd09b9-0443-4c7c-9bcf-01f3d28e6db7.png"> | <img width="299" src="https://user-images.githubusercontent.com/29742178/173910092-f3d481aa-85b3-4462-8613-671c5601bcf7.png">| <img width="298" src="https://user-images.githubusercontent.com/29742178/173909521-88f1ebe0-3eb4-46e8-8305-d4f64ac3d21a.png"> |

## Customization

The following properties enable customization (UWP, WPF only):

- `ResultItemTemplate` - override the display of results
- `StartingPointItemTemplate` - override the display of starting points
- `TraceTypeItemTemplate` - override the display of the trace configuration choices
- `UtilityNetworkItemTemplate` - override the display of the Utility Network choices

All platforms:

- `Template` - allows overriding the appearance of the entire control

The following properties enable customizing symbology:

- `StartingPointSymbol`
- `ResultFillSymbol`
- `ResultLineSymbol`
- `ResultPointSymbol`

## Usage

The default template for this control is optimized for a panel or side-by-side presentation with a width of around 300 dip.

### .NET MAUI:

```xml
<Grid xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013"
      ColumnDefinitions="*,300">
    <esri:MapView x:Name="MyMapView" />
    <esri:UtilityNetworkTraceTool GeoView="{x:Reference MyMapView}"
                                  Grid.Column="1" />
</Grid>
```

### UWP/WinUI:

```xml
<Grid xmlns:esri="using:Esri.ArcGISRuntime.UI.Controls"
      xmlns:toolkit="using:Esri.ArcGISRuntime.Toolkit.UI.Controls">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="300" />
    </Grid.ColumnDefinitions>
    <esri:MapView x:Name="MyMapView" />
    <toolkit:UtilityNetworkTraceTool GeoView="{x:Bind MyMapView}"
                                     Grid.Column="1" />
</Grid>
```

### WPF:

```xml
<Grid xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="300" />
    </Grid.ColumnDefinitions>
    <esri:MapView x:Name="MyMapView" />
    <esri:UtilityNetworkTraceTool GeoView="{Binding ElementName=MyMapView}"
                                  Grid.Column="1" />
</Grid>
```
