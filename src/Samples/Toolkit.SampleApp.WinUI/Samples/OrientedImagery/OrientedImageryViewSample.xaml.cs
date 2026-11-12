#nullable enable

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Popup = Esri.ArcGISRuntime.Mapping.Popups.Popup;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.OrientedImagery;

[SampleInfo(
    Category = "OrientedImagery",
    DisplayName = "Oriented Imagery View",
    Description = "Display and interact with oriented imagery.",
    ApiKeyRequired = true)]
public sealed partial class OrientedImageryViewSample : Page
{
    private const string BasemapUri = "https://runtime.maps.arcgis.com/home/item.html?id=d8c5e76fb2cc4bb6955a6783a5f577b7";
    private const string SceneBasemapUri = "https://runtime.maps.arcgis.com/home/item.html?id=0560e29930dc4d5ebeb58c635c0909c9";
    private const string ElevationUrl = "https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer";

    private readonly ObservableCollection<string> _exampleLayers =
    [
        "https://services.arcgis.com/2Pv4ow3pE6NFC9SW/ArcGIS/rest/services/Oakwood_camori_corrected/FeatureServer/0",
        "https://services3.arcgis.com/juvie9NFXORsK38U/ArcGIS/rest/services/WalkerOrientedImagery/FeatureServer/0",
        "https://services5.arcgis.com/N82JbI5EYtAkuUKU/ArcGIS/rest/services/EsriCampus_sequenceTest_attachment/FeatureServer/1"
    ];

    private OrientedImageryViewModel _orientedImageryViewModel;
    private readonly SceneView _sceneView;
    private readonly ElevationSource _elevationSource;
    private bool _usingMapView = true;
    private GeoView CurrentGeoView => _usingMapView ? MainMapView : _sceneView;
    private bool _overrideTapEvents = false;

    public OrientedImageryViewSample()
    {
        InitializeComponent();

        _orientedImageryViewModel = new OrientedImageryViewModel();
        MainOrientedImageryView.ViewModel = _orientedImageryViewModel;

        MainOrientedImageryView.Loaded += (_, _) => ConfigureToolbarTemplates();

        MainMapView.Map = new Map(new Uri(BasemapUri));
        _sceneView = new SceneView { Scene = new Scene(new Uri(SceneBasemapUri)) };
        _elevationSource = new ArcGISTiledElevationSource(new Uri(ElevationUrl));
        MainOrientedImageryView.GeoView = CurrentGeoView;
        SelectLayerDropdown.ItemsSource = _exampleLayers;
    }

    private void ConfigureToolbarTemplates()
    {
        if (MainOrientedImageryView.ToolbarItemTemplateSelector is not ToolbarSelector selector)
            return;

        if (!selector.SelectionPairs.OfType<ToolbarItemSelectorPair>().Any(static pair => pair.ItemType == typeof(OrientedImageryPopupToolbarItem)) &&
            Resources.TryGetValue("PopupToolbarItemTemplate", out object popupItemTemplateObject) && popupItemTemplateObject is DataTemplate popupItemTemplate)
            selector.SelectionPairs.Add(new ToolbarItemSelectorPair() { ItemType = typeof(OrientedImageryPopupToolbarItem), Template = popupItemTemplate });
    }

    private async Task ApplyLayerAsync(Uri layerUri)
    {
        StatusTextBlock.Text = string.Empty;
        var orientedImageryLayer = new OrientedImageryLayer(layerUri);
        var geoView = CurrentGeoView;

        try
        {
            if (_usingMapView)
            {
                MainMapView.Map ??= new Map(new Uri(BasemapUri));
                MainMapView.Map.OperationalLayers.Clear();
                MainMapView.Map.OperationalLayers.Add(orientedImageryLayer);
            }
            else
            {
                orientedImageryLayer.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
                _sceneView.Scene!.OperationalLayers.Clear();
                _sceneView.Scene.OperationalLayers.Add(orientedImageryLayer);
            }

            _orientedImageryViewModel.OrientedImageryLayer = orientedImageryLayer;
            await orientedImageryLayer.LoadAsync();
            if (ReferenceEquals(_orientedImageryViewModel.OrientedImageryLayer, orientedImageryLayer) &&
                orientedImageryLayer.FullExtent != null)
            {
                geoView.SetViewpoint(new Viewpoint(orientedImageryLayer.FullExtent));
            }
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(_orientedImageryViewModel.OrientedImageryLayer, orientedImageryLayer))
                StatusTextBlock.Text = ex.Message;
        }
    }

    private async void SelectLayerDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectLayerDropdown.SelectedItem is string layerUrl)
            await ApplyLayerAsync(new Uri(layerUrl));
    }

    private void AddLayerButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _ = new OrientedImageryLayer(new Uri(LayerUriTextBox.Text));
            _exampleLayers.Add(LayerUriTextBox.Text);
            StatusTextBlock.Text = string.Empty;
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = ex.Message;
        }
    }

    private void OpenSelectedImagePopupButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedImage = _orientedImageryViewModel.SelectedImage;
        if (selectedImage == null)
        {
            return;
        }

        SelectedImagePopupViewer.Popup = CreatePopup(selectedImage);
        SelectedImagePopupBackground.Visibility = Visibility.Visible;
    }

    private static Popup CreatePopup(OrientedImage orientedImage)
    {
        var graphic = new Graphic(orientedImage.Geometry);
        foreach (var attribute in orientedImage.Attributes)
        {
            graphic.Attributes[attribute.Key] = attribute.Value;
        }

        return Popup.FromGeoElement(graphic);
    }

    private void SelectedImagePopupBackground_Tapped(object sender, TappedRoutedEventArgs e)
    {
        SelectedImagePopupBackground.Visibility = Visibility.Collapsed;
        SelectedImagePopupViewer.Popup = null;
    }

    private void Toggle2DButton_Click(object sender, RoutedEventArgs e)
    {
        GeoViewContainer.Children.Clear();
        _usingMapView = !_usingMapView;
        MainOrientedImageryView.GeoView = CurrentGeoView;
        GeoViewContainer.Children.Add(CurrentGeoView);
        SelectLayerDropdown.SelectedItem = null;
        _orientedImageryViewModel.OrientedImageryLayer = null;
    }

    private void ToggleElevationSurfaceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_sceneView.Scene?.BaseSurface is not Surface surface)
            return;

        if (surface.ElevationSources.Count > 0)
            surface.ElevationSources.Clear();
        else
            surface.ElevationSources.Add(_elevationSource);
    }

    private void UpdateToolbarButton_Click(object sender, RoutedEventArgs e)
    {
        ConfigureToolbarTemplates();
        var overlays = _orientedImageryViewModel.ToolbarCategories.First(static c => c.Name == ToolbarCategory.OverlaysCategoryName);
        var popupItem = overlays.ToolbarItems.OfType<OrientedImageryPopupToolbarItem>().FirstOrDefault();
        if (popupItem == null)
            overlays.ToolbarItems.Add(new OrientedImageryPopupToolbarItem());
        else
            overlays.ToolbarItems.Remove(popupItem);
    }

    private void OverrideClickHandlersButton_Click(object sender, RoutedEventArgs e)
    {
        _overrideTapEvents = !_overrideTapEvents;
        if (_overrideTapEvents)
        {
            MainOrientedImageryView.GeoViewTapped += MainOrientedImageryView_GeoViewTapped;
            MainOrientedImageryView.ImageTapped += MainOrientedImageryView_ImageTapped;
        }
        else
        {
            MainOrientedImageryView.GeoViewTapped -= MainOrientedImageryView_GeoViewTapped;
            MainOrientedImageryView.ImageTapped -= MainOrientedImageryView_ImageTapped;
        }
    }

    private void MainOrientedImageryView_GeoViewTapped(object? sender, OrientedImageryViewGeoViewTappedArgs e)
    {
        e.Handled = true;

        if (e.GeoViewInputEventArgs.Location == null)
            return;

        var symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Green, 20);
        _orientedImageryViewModel.AddMarkerLocation(e.GeoViewInputEventArgs.Location, symbol);
    }

    private async void MainOrientedImageryView_ImageTapped(object? sender, OrientedImageryViewImageTappedArgs e)
    {
        e.Handled = true;

        try
        {
            var location = await e.Image.ImageToLocationAsync(e.ImagePoint);
            var symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Pink, 20);
            _orientedImageryViewModel.AddMarkerLocation(location, symbol);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = ex.Message;
        }
    }

    private void DetachReattachGeoViewButton_Click(object sender, RoutedEventArgs e)
    {
        MainOrientedImageryView.GeoView = MainOrientedImageryView.GeoView == null ? CurrentGeoView : null;
    }

    private void PanelSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
        const double minimumPanelWidth = 120;
        var mapWidth = MapColumn.ActualWidth;
        var imageryWidth = ImageryColumn.ActualWidth;
        if (mapWidth + imageryWidth < 2 * minimumPanelWidth)
            return;

        var change = Math.Clamp(e.HorizontalChange, minimumPanelWidth - mapWidth, imageryWidth - minimumPanelWidth);
        MapColumn.Width = new GridLength(mapWidth + change, GridUnitType.Star);
        ImageryColumn.Width = new GridLength(imageryWidth - change, GridUnitType.Star);
    }

    public static bool HasSelectedImage(OrientedImage? selectedImage) => selectedImage != null;

    public static double PopupIconOpacity(OrientedImage? selectedImage) => selectedImage == null ? 0.35 : 1;
}

public sealed partial class PanelResizeHandle : Microsoft.UI.Xaml.Controls.Grid
{
    public PanelResizeHandle()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }
}
