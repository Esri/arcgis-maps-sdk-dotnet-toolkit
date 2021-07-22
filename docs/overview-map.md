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

UWP:

```xml
<Page
    x:Class="Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.OverviewMap.OverviewMapSample"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:esri="using:Esri.ArcGISRuntime.UI.Controls"
    xmlns:overviewmap="using:Esri.ArcGISRuntime.Toolkit.UI.Controls.OverviewMap">
    <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <esri:MapView x:Name="mapView" />
        <overviewmap:OverviewMap
            Width="100"
            Height="100"
            Margin="4"
            HorizontalAlignment="Right"
            VerticalAlignment="Top"
            GeoView="{Binding ElementName=mapView}" />
    </Grid>
</Page>
```

WPF:

```xml
<UserControl
    x:Class="Esri.ArcGISRuntime.Toolkit.Samples.OverviewMap.OverviewMapWithSceneSample"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013"
    xmlns:overviewmap="clr-namespace:Esri.ArcGISRuntime.Toolkit.UI.Controls.OverviewMap;assembly=Esri.ArcGISRuntime.Toolkit">
    <Grid>
        <esri:SceneView x:Name="MySceneView" />
        <overviewmap:OverviewMap
            Width="100"
            Height="100"
            Margin="8"
            HorizontalAlignment="Right"
            VerticalAlignment="Top"
            GeoView="{Binding ElementName=MySceneView}" />
    </Grid>
</UserControl>
```

Xamarin.Forms:

```xml
<ContentPage x:Class="Toolkit.Samples.Forms.Samples.OverviewMapWithSceneSample"
             xmlns="http://xamarin.com/schemas/2014/forms"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:esriTK="clr-namespace:Esri.ArcGISRuntime.Toolkit.Xamarin.Forms;assembly=Esri.ArcGISRuntime.Toolkit.Xamarin.Forms"
             xmlns:esriUI="clr-namespace:Esri.ArcGISRuntime.Xamarin.Forms;assembly=Esri.ArcGISRuntime.Xamarin.Forms">
    <ContentPage.Content>
        <Grid>
            <esriUI:SceneView x:Name="sceneView" />
            <StackLayout Margin="4"
                         HorizontalOptions="Start"
                         VerticalOptions="Start">
                <esriTK:OverviewMap GeoView="{Binding Source={x:Reference sceneView}}"
                                    HeightRequest="100"
                                    WidthRequest="100" />
            </StackLayout>

        </Grid>
    </ContentPage.Content>
</ContentPage>
```