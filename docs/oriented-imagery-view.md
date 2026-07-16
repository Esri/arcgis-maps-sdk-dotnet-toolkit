# OrientedImageryView

`OrientedImageryView` displays oriented imagery with a built-in toolbar for image navigation, footprint visibility, camera location markers, and image markers. It works with an `OrientedImageryLayer` and `GeoView`. The control manages footprints and marker overlays, while your app remains responsible for searching for images and choosing which image is selected.

![Example Image](images/oriented-imagery.png)

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

## Customizing the toolbar

`OrientedImageryView` is structured as an `ItemsControl` whose toolbar items are defined by its `ItemsSource` property. The default toolbar items are defined by `OrientedImageryView.GetDefaultToolbarItems()`, which includes controls for auto-updating the footprint, showing selected and unselected footprints, cycling camera marker display modes, enabling marker creation, selecting the marker symbol, and clearing markers.

Toolbar items are styled using the `OrientedImageryViewTemplateSelector` which maps object types to data templates. Custom toolbar items can be any type, but users may choose to derive from `OrientedImageryViewToolbarViewModelBase` to automatically gain access to the current `OrientedImageryViewModel` via the `OrientedImageryViewToolbarViewModelBase.MainViewModel` property.

### Example

Define a toolbar item class deriving from `OrientedImageryViewToolbarViewModelBase`.

```cs
internal sealed class ShowImageDetailsToolbarItem : OrientedImageryViewToolbarViewModelBase { }
```

Define a data template for the custom toolbar item and create a new `OrientedImageyViewTemplateSelector` item to map it to the `ShowImageDetailsToolbarItem` type.

```xml
<UserControl.Resources>
    <DataTemplate x:Key="ShowImageDetailsToolbarTemplate"
                  DataType="{x:Type local:ShowImageDetailsToolbarItem}">
        <Button Content="Info"
                MinWidth="48"
                Padding="8,4"
                ToolTip="Show selected image details"
                Click="ShowImageDetails_Click">
            <Button.Style>
                <Style TargetType="{x:Type Button}">
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding MainViewModel.SelectedImage}" Value="{x:Null}">
                            <Setter Property="IsEnabled" Value="False" />
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </Button.Style>
        </Button>
    </DataTemplate>

    <esri:OrientedImageryViewTemplateSelectorItem x:Key="ShowImageDetailsToolbarSelectorItem"
                                                  Type="{x:Type local:ShowImageDetailsToolbarItem}"
                                                  Template="{StaticResource ShowImageDetailsToolbarTemplate}" />
</UserControl.Resources>
```

Add the custom item to the toolbar collection and register its template after the control has loaded.

```cs
public OrientedImagerySample()
{
    InitializeComponent();
    MainOrientedImageryView.Loaded += (_, _) => ConfigureToolbar();
}

private void ConfigureToolbar()
{
    // Register the selector item.
    if (MainOrientedImageryView.ItemTemplateSelector is OrientedImageryViewTemplateSelector selector &&
        TryFindResource("ShowImageDetailsToolbarSelectorItem") is OrientedImageryViewTemplateSelectorItem selectorItem)
    {
        selector.TypeTemplatePairs.Add(selectorItem);
    }

    // Reuse the default toolbar items (optional).
    var toolbarItems = OrientedImageryView.GetDefaultToolbarItems();

    // Add the custom item to the list.
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