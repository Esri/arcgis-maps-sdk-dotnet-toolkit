# BookmarksView

## Features

* Associate the bookmarks control with a `MapView` or `SceneView` (`GeoView` property), through binding on supported platforms (WPF, UWP, Forms) or plain properties otherwise.
* Display a list of bookmarks, defined by the `Map` or `Scene` from the associated `GeoView` or the `BookmarksOverride` if set.
* Navigates the associated `GeoView` to the selected bookmark.
* Customize the display of the list with the `ItemTemplate` property on UWP and WPF.
* Supports observable collections for `BookmarksOverride` and handles changes to the `Map`/`Scene` properties.

## Usage

For bookmarks to appear in the control, you must set either `GeoModel.Bookmarks` or `BookmarksView.BookmarksOverride`.

### .NET MAUI:

```xml
<Grid ColumnDefinitions="*,300">
    <esri:MapView x:Name="mapView" />
    <toolkit:BookmarksView Grid.Column="1" GeoView="{x:Reference mapView}" />
</Grid>
```

To customize the item template:

```xml
<toolkit:BookmarksView Grid.Column="1" GeoView="{x:Reference mapView}">
    <toolkit:BookmarksView.ItemTemplate>
        <DataTemplate>
            <TextCell Text="{Binding Name}" TextColor="Red" />
        </DataTemplate>
    </toolkit:BookmarksView.ItemTemplate>
</toolkit:BookmarksView>
```

### UWP/WinUI:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="300" />
    </Grid.ColumnDefinitions>
    <esri:MapView x:Name="mapView" />
    <toolkit:BookmarksView Grid.Column="1" GeoView="{Binding ElementName=mapView}" />
</Grid>
```

You can customize the `BookmarksView` bookmark display by setting the `ItemTemplate` property with a customized `DataTemplate`. 

```xml
<toolkit:BookmarksView Grid.Column="1" GeoView="{Binding ElementName=mapView}">
    <toolkit:BookmarksView.ItemTemplate>
        <DataTemplate>
            <TextBlock Foreground="Red" Text="{Binding Name}" />
        </DataTemplate>
    </toolkit:BookmarksView.ItemTemplate>
</toolkit:BookmarksView>
```

### WPF:

The usage in WPF is identical to UWP/WinUI with one important distinction: `BookmarksView` is accessed with the same namespace prefix as `GeoView`. 

```xml
xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013"
```
