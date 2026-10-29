# OrientedImageryView

<<<<<<< HEAD
The `OrientedImageryView` displays oriented imagery with a built-in toolbar for image navigation, footprint visibility, camera location markers, and image markers. It takes a `GeoView`, and must be assigned an `OrientedImageryLayer` via its `ViewModel` property.

<img width="937" height="391" alt="oiview-screenshot" src="https://github.com/user-attachments/assets/638563c3-19f8-4d2f-88c6-26594aa24919" />

## Minimal implementation

The following WPF example places a `MapView` next to an `OrientedImageryView`. Set `GeoView` so the control can display its managed marker overlay on the map, and set `ViewModel.OrientedImageryLayer` so the view model can manage footprints and image-to-map calculations.

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

### Example

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
||||||| parent of bf053a5c (restore accidentally deleted doc)
=======
The `OrientedImageryView` displays oriented imagery with a built-in toolbar for image navigation, footprint visibility, camera location markers, and image markers. It works with an `OrientedImageryLayer` and `GeoView`. The control manages footprints and marker overlays, while your app remains responsible for searching for images and choosing which image is selected.

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

## Customizing the toolbar

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
