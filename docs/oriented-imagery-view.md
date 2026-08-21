# OrientedImageryView

The `OrientedImageryView` displays oriented imagery with a built-in toolbar for image navigation, footprint visibility, camera location markers, and image markers on WPF and .NET MAUI. It works with an `OrientedImageryLayer` and `GeoView`. The control manages footprints and marker overlays, while your app remains responsible for searching for images and choosing which image is selected.

<img width="1108" height="656" alt="oriented-imagery" src="https://github.com/user-attachments/assets/d1199b2b-a9e5-4fce-89e6-0a3d85d05457" />

## Minimal implementation

The following WPF example places a `MapView` next to an `OrientedImageryView`. Set `GeoView` so the control can display its managed marker overlay on the map, and set `OrientedImageryLayer` so the view model can manage footprints and image-to-map calculations.

```xml
<Grid xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="400" />
    </Grid.ColumnDefinitions>

    <esri:MapView x:Name="MainMapView" />
    <esri:OrientedImageryView x:Name="MainOrientedImageryView"
                              Grid.Column="1"
                              GeoView="{Binding ElementName=MainMapView}" />
</Grid>
```

Create the map and oriented imagery layer. Search for oriented images when the map is tapped.

```cs
private const string OrientedImageryLayerUrl = "https://example.com/arcgis/rest/services/OrientedImagery/FeatureServer";

private OrientedImageryLayer? _orientedImageryLayer;

private async Task InitializeAsync()
{
    MainMapView.Map = new Map(BasemapStyle.ArcGISImageryStandard);

    _orientedImageryLayer = new OrientedImageryLayer(new Uri(OrientedImageryLayerUrl));
    MainMapView.Map.OperationalLayers.Add(_orientedImageryLayer);

    MainOrientedImageryView.OrientedImageryLayer = _orientedImageryLayer;

    await _orientedImageryLayer.LoadAsync();

    if (_orientedImageryLayer.FullExtent is not null)
    {
        await MainMapView.SetViewpointGeometryAsync(_orientedImageryLayer.FullExtent);
    }

    MainMapView.GeoViewTapped += MainMapView_GeoViewTapped;
}

private async void MainMapView_GeoViewTapped(object? sender, GeoViewInputEventArgs e)
{
    if (_orientedImageryLayer is null || e.Location is null)
    {
        return;
    }

    // Add a marker if the `OrientedImageryViewModel.AllowAddingMarkers` is enabled.
    if (MainOrientedImageryView.ViewModel.AllowAddingMarkers)
    {
        MainOrientedImageryView.ViewModel.AddMarkerLocation(e.Location);
        return;
    }

    // Otherwise search for images that view the clicked location.
    var parameters = new OrientedImageSearchParameters { MaxResults = -1 };
    var images = await _orientedImageryLayer.SearchImagesAsync(e.Location, parameters) ?? new List<OrientedImage>();

    MainOrientedImageryView.ViewModel.SetImages(images, e.Location);
    MainOrientedImageryView.ViewModel.SelectedImage = images.FirstOrDefault();
}
```

See the `OrientedImageryView` sample for a full example implementation.

## Customizing the WPF toolbar

The .NET MAUI control currently uses a fixed toolbar with the same built-in actions.

`OrientedImageryView` is structured as an `ItemsControl` whose toolbar items are defined by its `ItemsSource` property. The default toolbar items are returned by `OrientedImageryView.GetDefaultToolbarItems()`, which includes controls for auto-updating the footprint, showing selected and unselected footprints, cycling camera marker display modes, enabling marker creation, selecting the marker symbol, and clearing markers.

Toolbar items are rendered by the `ItemTemplateSelector`. Use an `OrientedImageryViewTemplateSelector` to map toolbar item types to data templates. When you assign a custom selector to `OrientedImageryView.ItemTemplateSelector`, the control merges in the default toolbar templates so built-in toolbar items continue to render unless you override their templates.

Custom toolbar items can be any type, but deriving from `OrientedImageryToolbarItemBase` automatically provides access to the current `OrientedImageryViewModel` through the `MainViewModel` property.

### Example

Define a toolbar item class deriving from `OrientedImageryToolbarItemBase`.

```cs
internal sealed class ShowImageDetailsToolbarItem : OrientedImageryToolbarItemBase
{
}
```

Define a data template for the custom toolbar item, then create an `OrientedImageryViewTemplateSelector` that maps the item type to the template.

```xml
<UserControl.Resources>
    <ResourceDictionary>
        <DataTemplate x:Key="ShowImageDetailsToolbarTemplate"
                      DataType="{x:Type local:ShowImageDetailsToolbarItem}">
            <Button Click="ShowImageDetails_Click"
                    ToolTip="Show selected image details">
                <Button.Style>
                    <Style TargetType="{x:Type Button}">
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding MainViewModel.SelectedImage}" Value="{x:Null}">
                                <Setter Property="IsEnabled" Value="False" />
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </Button.Style>
                <TextBlock Text="Info" />
            </Button>
        </DataTemplate>

        <esri:OrientedImageryViewTemplateSelector x:Key="CustomToolbarSelector">
            <esri:OrientedImageryViewTemplateSelectorItem Type="{x:Type local:ShowImageDetailsToolbarItem}"
                                                          Template="{StaticResource ShowImageDetailsToolbarTemplate}" />
        </esri:OrientedImageryViewTemplateSelector>
    </ResourceDictionary>
</UserControl.Resources>
```

Assign the custom selector to `OrientedImageryView.ItemTemplateSelector`.

```xml
<esri:OrientedImageryView x:Name="MainOrientedImageryView"
                          GeoView="{Binding ElementName=MainMapView}"
                          ItemTemplateSelector="{StaticResource CustomToolbarSelector}" />
```

Add the custom item to the toolbar collection.

```cs
public OrientedImagerySample()
{
    InitializeComponent();
    ConfigureToolbar();
}

private void ConfigureToolbar()
{
    var toolbarItems = OrientedImageryView.GetDefaultToolbarItems();
    toolbarItems.Add(new ShowImageDetailsToolbarItem());
    MainOrientedImageryView.ItemsSource = toolbarItems;
}

private void ShowImageDetails_Click(object sender, RoutedEventArgs e)
{
    var selectedImage = MainOrientedImageryView.ViewModel.SelectedImage;
    if (selectedImage is null)
    {
        return;
    }

    // Show details for selectedImage.
}
```
