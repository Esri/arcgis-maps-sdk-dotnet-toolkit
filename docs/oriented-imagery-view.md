# OrientedImageryView

The `OrientedImageryView` displays oriented imagery with a built-in toolbar for image navigation, footprint visibility, camera location markers, and image markers. It takes a `GeoView`, and must be assigned an `OrientedImageryLayer` via its `ViewModel` property.

<img width="937" height="391" alt="oiview-screenshot" src="https://github.com/user-attachments/assets/638563c3-19f8-4d2f-88c6-26594aa24919" />

## Minimal implementation

The following example places a `MapView` next to an `OrientedImageryView`. Set `GeoView` so the control can display its managed marker overlay on the map, and set `ViewModel.OrientedImageryLayer` so the view model can manage footprints and image-to-map calculations.

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

Create the map and oriented imagery layer. By default, the control searches for oriented images when the map is tapped.

```cs
private const string OrientedImageryLayerUrl = "https://example.com/arcgis/rest/services/OrientedImagery/FeatureServer";

private OrientedImageryLayer? _orientedImageryLayer;

private async Task InitializeAsync()
{
    MainMapView.Map = new Map(BasemapStyle.ArcGISImageryStandard);

    _orientedImageryLayer = new OrientedImageryLayer(new Uri(OrientedImageryLayerUrl));
    MainMapView.Map.OperationalLayers.Add(_orientedImageryLayer);

    MainOrientedImageryView.ViewModel.OrientedImageryLayer = _orientedImageryLayer;

    await _orientedImageryLayer.LoadAsync();
    if (_orientedImageryLayer.FullExtent is not null)
        await MainMapView.SetViewpointGeometryAsync(_orientedImageryLayer.FullExtent);
}
```

See the `OrientedImageryView` sample for a full example implementation.

## Customizing the toolbar

The `OrientedImageryView` toolbar is implemented an `ItemsControl` whose individual buttons and dropdowns are defined by the `OrientedImageryViewModel.ToolbarCategories` list. `ToolbarCategories` is a list of `OrientedImageryToolbarCategory`s that in turn contain lists of `OrientedImageryToolbarItem`s. Data templates for each toolbar item and category are selected using the `OrientedImageryView.ToolbarSelector`, which is a custom `DataTemplateSelector` containing a list of paired item types to data templates that can be modified as needed. See the below example.

### Example (WPF)

Define a toolbar item class deriving from `OrientedImageryToolbarItemBase`.

```cs
public sealed class ShowImageDetailsToolbarItem : OrientedImageryToolbarItemBase
{
}
```

Define a data template for the custom toolbar item, then create an `OrientedImageryViewToolbarSelector` that maps the item type to the template. As in the sample, import the default theme to reuse its toolbar button style. Here, `local` refers to your application's namespace and `esri` uses the namespace declaration from the minimal example.

```xml
<UserControl.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="/Esri.ArcGISRuntime.Toolkit.WPF;component/UI/Controls/OrientedImagery/OrientedImagery.Theme.xaml" />
        </ResourceDictionary.MergedDictionaries>

        <DataTemplate x:Key="ShowImageDetailsToolbarTemplate"
                      DataType="{x:Type local:ShowImageDetailsToolbarItem}">
            <Button Click="ShowImageDetails_Click"
                    ToolTip="Show selected image details">
                <Button.Style>
                    <Style TargetType="{x:Type Button}" BasedOn="{StaticResource ToolbarButtonStyle}">
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding ViewModel.SelectedImage}" Value="{x:Null}">
                                <Setter Property="IsEnabled" Value="False" />
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </Button.Style>
                <TextBlock Text="Info" />
            </Button>
        </DataTemplate>

        <esri:OrientedImageryViewToolbarSelector x:Key="CustomToolbarSelector">
            <esri:OrientedImageryToolbarItemSelectorPair ItemType="{x:Type local:ShowImageDetailsToolbarItem}"
                                                        Template="{StaticResource ShowImageDetailsToolbarTemplate}" />
        </esri:OrientedImageryViewToolbarSelector>
    </ResourceDictionary>
</UserControl.Resources>
```

Configure the toolbar when the control has loaded, so its default templates are available.

```xml
<esri:OrientedImageryView x:Name="MainOrientedImageryView"
                          GeoView="{Binding ElementName=MainMapView}"
                          Loaded="MainOrientedImageryView_Loaded" />
```

Add the custom item to the existing overlays category and assign the custom selector. The other categories and toolbar items remain unchanged.

```cs
private void MainOrientedImageryView_Loaded(object sender, RoutedEventArgs e)
{
    MainOrientedImageryView.Loaded -= MainOrientedImageryView_Loaded;

    var overlaysCategory = MainOrientedImageryView.ViewModel.ToolbarCategories
        .First(category => category.Name == OrientedImageryToolbarCategory.OverlaysCategoryName);
    overlaysCategory.ToolbarItems.Add(new ShowImageDetailsToolbarItem());
    MainOrientedImageryView.ToolbarItemTemplateSelector =
        (OrientedImageryViewToolbarSelector)FindResource("CustomToolbarSelector");
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
