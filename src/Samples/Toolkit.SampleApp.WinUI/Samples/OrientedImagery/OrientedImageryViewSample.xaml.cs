#nullable enable

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
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

    private readonly ObservableCollection<string> _exampleLayers =
    [
        "https://services.arcgis.com/2Pv4ow3pE6NFC9SW/ArcGIS/rest/services/Oakwood_camori_corrected/FeatureServer/0",
        "https://services3.arcgis.com/juvie9NFXORsK38U/ArcGIS/rest/services/WalkerOrientedImagery/FeatureServer/0",
        "https://services5.arcgis.com/N82JbI5EYtAkuUKU/ArcGIS/rest/services/EsriCampus_sequenceTest_attachment/FeatureServer/1"
    ];

    private OrientedImageryViewModel _orientedImageryViewModel;

    public OrientedImageryViewSample()
    {
        InitializeComponent();

        _orientedImageryViewModel = new OrientedImageryViewModel();
        MainOrientedImageryView.ViewModel = _orientedImageryViewModel;

        MainOrientedImageryView.Loaded += (_, _) => ConfigureToolbarTemplates();

        MainMapView.Map = new Map(new Uri(BasemapUri));
        SelectLayerDropdown.ItemsSource = _exampleLayers;
    }

    private void ConfigureToolbarTemplates()
    {
        if (MainOrientedImageryView.ToolbarItemTemplateSelector is not OrientedImageryViewToolbarSelector selector)
            return;

        var popupTemplate = (DataTemplate)Resources["PopupToolbarItemTemplate"];
        var overlays = _orientedImageryViewModel.ToolbarCategories.Single(static c => c.Name == OrientedImageryToolbarCategory.OverlaysCategoryName);
        overlays.ToolbarItems.Add(new OrientedImageryPopupToolbarItem());
        if (this.Resources.TryGetValue("PopupToolbarItemTemplate", out object popupItemTempalteObject) && popupItemTempalteObject is DataTemplate popupItemTemplate)
            selector.SelectionPairs.Add(new OrientedImageryToolbarItemSelectorPair() { ItemType = typeof(OrientedImageryPopupToolbarItem), Template = popupItemTemplate });
    }

    private async Task ApplyLayerAsync(Uri layerUri)
    {
        StatusTextBlock.Text = string.Empty;
        var orientedImageryLayer = new OrientedImageryLayer(layerUri);

        try
        {
            MainMapView.Map ??= new Map(new Uri(BasemapUri));
            MainMapView.Map.OperationalLayers.Clear();
            MainMapView.Map.OperationalLayers.Add(orientedImageryLayer);

            _orientedImageryViewModel.OrientedImageryLayer = orientedImageryLayer;
            await orientedImageryLayer.LoadAsync();
            if (ReferenceEquals(_orientedImageryViewModel.OrientedImageryLayer, orientedImageryLayer) &&
                orientedImageryLayer.FullExtent != null)
            {
                MainMapView.SetViewpoint(new Viewpoint(orientedImageryLayer.FullExtent));
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
