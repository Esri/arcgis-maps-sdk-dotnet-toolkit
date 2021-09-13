# BasemapGallery

BasemapGallery displays a collection of basemaps from ArcGIS Online, a user-defined portal, or a custom collection. When a basemap is selected from the gallery, the basemap used in the connected GeoModel is replaced.

![BasemapGallery List View](https://user-images.githubusercontent.com/29742178/124198151-f2dc6380-da84-11eb-8e78-4e705d14c33d.png)

![BasemapGallery Grid View](https://user-images.githubusercontent.com/29742178/124198175-ff60bc00-da84-11eb-9a41-6b85a6ed89fd.png)

> **NOTE**: BasemapGallery uses metered ArcGIS basemaps by default, so you will need to configure an API key. See [Security and authentication documentation](https://developers.arcgis.com/documentation/mapping-apis-and-services/security/#api-keys) for more information.

## Features

BasemapGallery:

- Can be configured to use a list, grid, or automatic layout. When using an automatic layout, list or grid presentation is chosen based on a defined width threshold.
    - Note: Grid layout is not supported on Xamarin.Forms UWP. Regardless of settings, the list layout will always be used.
- Shows basemaps from a portal, and allows manually adding and removing basemaps from the collection.
- Shows a representation of the map or scene's current basemap if that basemap exists in the gallery.
- Shows a name and thumbnail for each basemap.
- Shows a tooltip on hover on WPF and UWP.
- Supports templating.

## Key properties

BasemapGallery exposes the following properties:

- `GeoModel` - References the connected Map or Scene (optional).
- `Portal` - Controls which portal is used to find basemaps. Setting the portal will reset the basemap collection.
- `AvailableBasemaps` - Collection of basemaps being shown.

The following properties enable customization of the gallery's appearance:

- `GalleryViewStyle` - Controls whether the gallery is displayed as a list or a grid.
- `GridItemTemplate` - Template used to display basemaps when using a grid presentation.
- `ListItemTemplate` - Template used to display basemaps when using a list presentation.
- `ListItemContainerStyle` - Container style used when displaying items as a list. Does not apply to Xamarin.Forms.
- `GridItemContainerStyle` - Container style used when displaying items as a grid. Does not apply to Xamarin.Forms.

## Usage

UWP:

```xml
<Page
    x:Class="Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.BasemapGallery.BasemapGallerySample"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:esri="using:Esri.ArcGISRuntime.UI.Controls"
    xmlns:basemapgallery="using:Esri.ArcGISRuntime.Toolkit.UI.Controls.BasemapGallery">
    <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <esri:MapView x:Name="mapView" />
        <basemapgallery:BasemapGallery
            Width="200"
            Height="400"
            Margin="4"
            HorizontalAlignment="Right"
            VerticalAlignment="Top"
            GeoModel="{Binding ElementName=mapView, Path=Map}" />
    </Grid>
</Page>
```

WPF:

```xml
<UserControl
    x:Class="Esri.ArcGISRuntime.Toolkit.Samples.BasemapGallery.BasemapGalleryWithSceneSample"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013"
    xmlns:basemapgallery="clr-namespace:Esri.ArcGISRuntime.Toolkit.UI.Controls.BasemapGallery;assembly=Esri.ArcGISRuntime.Toolkit">
    <Grid>
        <esri:SceneView x:Name="MySceneView" />
        <basemapgallery:BasemapGallery
            Width="200"
            Height="400"
            Margin="8"
            HorizontalAlignment="Right"
            VerticalAlignment="Top"
            GeoModel="{Binding ElementName=MySceneView, Path=Scene}" />
    </Grid>
</UserControl>
```

Xamarin.Forms:

```xml
<ContentPage x:Class="Toolkit.Samples.Forms.Samples.BasemapGalleryWithSceneSample"
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
                <esriTK:BasemapGallery GeoModel="{Binding Source={x:Reference sceneView}, Path=Scene}"
                                    HeightRequest="200"
                                    WidthRequest="400" />
            </StackLayout>
        </Grid>
    </ContentPage.Content>
</ContentPage>
```