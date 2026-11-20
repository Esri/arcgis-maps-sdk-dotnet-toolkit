# OrientedImageryViewer

The `OrientedImageryViewer` displays oriented imagery with a built-in toolbar for image navigation, footprint visibility, camera location markers, and image markers. It takes a `GeoView`, and must be assigned an `OrientedImageryLayer` via its `ViewModel` property.

<img width="937" height="391" alt="oiview-screenshot" src="https://github.com/user-attachments/assets/638563c3-19f8-4d2f-88c6-26594aa24919" />

## Minimal implementation

The following example places a `MapView` next to an `OrientedImageryViewer`. Set `GeoView` so the control can display its managed marker overlay on the map, and set `ViewModel.OrientedImageryLayer` so the view model can manage footprints and image-to-map calculations.

```xml
<Grid xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="400" />
    </Grid.ColumnDefinitions>

    <esri:MapView x:Name="MainMapView" />
    <esri:OrientedImageryViewer x:Name="MainOrientedImageryViewer"
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

    MainOrientedImageryViewer.ViewModel.OrientedImageryLayer = _orientedImageryLayer;

    await _orientedImageryLayer.LoadAsync();
    if (_orientedImageryLayer.FullExtent is not null)
        await MainMapView.SetViewpointGeometryAsync(_orientedImageryLayer.FullExtent);
}
```

See the `OrientedImageryViewer` sample for a full example implementation.

## Customizing the toolbar

The `OrientedImageryViewer` toolbar is implemented as an `ItemsControl` whose individual buttons and dropdowns are defined by the `OrientedImageryViewerModel.ToolbarCategories` collection. Each `ToolbarCategory` contains a collection of `ToolbarItemBase` objects. Data templates for each toolbar item and category are selected using `OrientedImageryViewer.ToolbarItemTemplateSelector`, a specialized `DataTemplateSelector` subclass containing configurable mappings from category names or item types to data templates. These toolbar customization types are in the `Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery` namespace.

### Example (WPF)

Define a toolbar item class deriving from `ToolbarItemBase`.

```cs
using Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;

public sealed class ShowImageDetailsToolbarItem : ToolbarItemBase
{
}
```

Define a data template for the custom toolbar item, then create a `ToolbarSelector` that maps the item type to the template. As in the sample, import the default theme to reuse its toolbar button style. Here, `local` refers to your application's namespace and `esri` uses the namespace declaration from the minimal example. Add an `oi` declaration as below to import the toolbar customization types:

```xml
xmlns:oi="clr-namespace:Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;assembly=Esri.ArcGISRuntime.Toolkit.WPF"
```

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

        <oi:ToolbarSelector x:Key="CustomToolbarSelector">
            <oi:ToolbarItemSelectorPair ItemType="{x:Type local:ShowImageDetailsToolbarItem}"
                                        Template="{StaticResource ShowImageDetailsToolbarTemplate}" />
        </oi:ToolbarSelector>
    </ResourceDictionary>
</UserControl.Resources>
```

Configure the toolbar when the control has loaded, so its default templates are available.

```xml
<esri:OrientedImageryViewer x:Name="MainOrientedImageryViewer"
                          GeoView="{Binding ElementName=MainMapView}"
                          Loaded="MainOrientedImageryViewer_Loaded" />
```

Add the custom item to the existing overlays category and assign the custom selector. The other categories and toolbar items remain unchanged.

```cs
private void MainOrientedImageryViewer_Loaded(object sender, RoutedEventArgs e)
{
    MainOrientedImageryViewer.Loaded -= MainOrientedImageryViewer_Loaded;

    var overlaysCategory = MainOrientedImageryViewer.ViewModel.ToolbarCategories
        .First(category => category.Name == ToolbarCategory.OverlaysCategoryName);
    overlaysCategory.ToolbarItems.Add(new ShowImageDetailsToolbarItem());
    MainOrientedImageryViewer.ToolbarItemTemplateSelector =
        (ToolbarSelector)FindResource("CustomToolbarSelector");
}

private void ShowImageDetails_Click(object sender, RoutedEventArgs e)
{
    var selectedImage = MainOrientedImageryViewer.ViewModel.SelectedImage;
    if (selectedImage is null)
    {
        return;
    }

    // Show details for selectedImage.
}
```
