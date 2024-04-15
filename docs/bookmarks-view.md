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
<Grid xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013" 
      ColumnDefinitions="*,300">
    <esri:MapView x:Name="MyMapView" />
    <esri:BookmarksView Grid.Column="1" 
                        GeoView="{x:Reference MyMapView}" />
</Grid>
```

To customize the item template:

```xml
<esri:BookmarksView xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013" 
                    Grid.Column="1" 
                    GeoView="{x:Reference MyMapView}">
    <esri:BookmarksView.ItemTemplate>
        <DataTemplate>
            <TextCell Text="{Binding Name}"
                      TextColor="Red" />
        </DataTemplate>
    </esri:BookmarksView.ItemTemplate>
</esri:BookmarksView>
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
    <toolkit:BookmarksView Grid.Column="1" 
                           GeoView="{Binding ElementName=MyMapView}" />
</Grid>
```

You can customize the `BookmarksView` bookmark display by setting the `ItemTemplate` property with a customized `DataTemplate`. 

```xml
<toolkit:BookmarksView xmlns:toolkit="using:Esri.ArcGISRuntime.Toolkit.UI.Controls"
                       Grid.Column="1"
                       GeoView="{Binding ElementName=MyMapView}">
    <toolkit:BookmarksView.ItemTemplate>
        <DataTemplate>
            <TextBlock Foreground="Red" 
                       Text="{Binding Name}" />
        </DataTemplate>
    </toolkit:BookmarksView.ItemTemplate>
</toolkit:BookmarksView>
```

### WPF:

```xml
<Grid xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="300" />
    </Grid.ColumnDefinitions>
    <esri:MapView x:Name="MyMapView" />
    <esri:BookmarksView Grid.Column="1" 
                        GeoView="{Binding ElementName=MyMapView}" />
</Grid>
```
