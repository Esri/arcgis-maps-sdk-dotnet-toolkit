# Search View

Search View enables searching using one or more locators, with support for suggestions, automatic zooming, and custom search sources.

![image](https://user-images.githubusercontent.com/29742178/142301018-4bbeb0f2-3021-49a7-b5ec-f642c5700bd0.png)

> **NOTE**: Search View uses metered ArcGIS services by default, so you will need to configure an API key. See [Security and authentication documentation](https://developers.arcgis.com/documentation/mapping-apis-and-services/security/#api-keys) for more information.


## Features

- Updates search suggestions as you type
- Supports using the Esri world geocoder or any other ArcGIS locators
- Supports searching using custom search sources
- Supports searching multiple sources simultaneously
- Allows for customization of the display of search results
- Allows you to repeat a search within a defined area, and shows a button to enable that search when the view's viewpoint changes
- Separates the behavior (`SearchViewModel`) and the display (`SearchView`) to allow you to create a custom UI if needed

## Customization

The following properties enable customization of the view:

- `EnableAutomaticConfiguration` - Controls whether view is automatically configured for the attached GeoView's map or scene. By default, this will set up a single World Geocoder search source. In future releases, this behavior may be extended to support other web map configuration options.
- `EnableRepeatSearchHereButton` - Controls whether a 'Repeat Search Here' button is shown when the user navigates the attached GeoView after a search is completed.
- `EnableResultListView` - Controls whether a result list is displayed.
- `EnableIndividualResultDisplay` - Controls whether the result list is shown when there is only one result.
- `MutlipleResultZoomBuffer` - Controls the buffer distance around collection results when a GeoView is attached and a search has multiple results.

## Usage - WPF

```xaml
<Grid>
    <esri:MapView x:Name="MyMapView" />
    <esri:SearchView GeoView="{Binding ElementName=MyMapView}" />
</Grid>
```

## Usage - UWP

```xaml
<Grid>
    <esri:MapView x:Name="MyMapView" />
    <toolkit:SearchView GeoView="{Binding ElementName=MyMapView}" />
</Grid>
```

## Usage - Xamarin.Forms

SearchView shows results in a list on top of underlying content, so it is best to position the view near the top of the page, on top of the MapView or SceneView.

```xaml
<Grid RowSpacing="0">
    <Grid.RowDefinitions>
        <RowDefinition Height="32" />
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <esri:MapView x:Name="MyMapView" Grid.Row="1" Grid.RowSpan="2" />
    <toolkit:SearchView GeoView="{Binding Source={Reference MyMapView}}" Grid.Row="0" Grid.RowSpan="2"   />
</Grid>
```