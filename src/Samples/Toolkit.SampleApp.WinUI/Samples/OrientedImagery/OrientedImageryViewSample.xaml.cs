#nullable enable

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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

    private OrientedImageryViewModel _orientedImageryViewModel;

    public OrientedImageryViewSample()
    {
        InitializeComponent();

        _orientedImageryViewModel = new OrientedImageryViewModel();
        MainOrientedImageryView.ViewModel = _orientedImageryViewModel;

        MainOrientedImageryView.Loaded += (_, _) => ConfigureToolbarTemplates();

        _ = InitializeAsync();
    }

    private void ConfigureToolbarTemplates()
    {
        if (MainOrientedImageryView.ToolbarItemTemplateSelector is not OrientedImageryViewTemplateSelector selector)
            return;

        // This is currently a bit broken and it crowds out the default controls. Comment out to fix
        _orientedImageryViewModel.ToolbarItems.Add(new OrientedImageryPopupToolbarItem());
        if (this.Resources.TryGetValue("PopupToolbarItemTemplate", out object popupItemVMObject) && popupItemVMObject is DataTemplate popupItemVM)
            selector.TypeTemplatePairs.Add(new() { Type = typeof(OrientedImageryPopupToolbarItem), Template = popupItemVM });
    }

    private async Task InitializeAsync()
    {
        MainMapView.Map = new Map(new Uri(BasemapUri));
        await ApplyLayerAsync(new Uri(LayerUriTextBox.Text));
    }

    private async Task ApplyLayerAsync(Uri layerUri)
    {
        StatusTextBlock.Text = string.Empty;

        try
        {
            var orientedImageryLayer = new OrientedImageryLayer(layerUri);
            await orientedImageryLayer.LoadAsync();

            if (orientedImageryLayer.LoadStatus == LoadStatus.FailedToLoad)
            {
                StatusTextBlock.Text = orientedImageryLayer.LoadError?.Message ?? "The oriented imagery layer failed to load.";
                return;
            }

            MainMapView.Map ??= new Map(new Uri(BasemapUri));
            MainMapView.Map.OperationalLayers.Clear();
            MainMapView.Map.OperationalLayers.Add(orientedImageryLayer);

            _orientedImageryViewModel.OrientedImageryLayer = orientedImageryLayer;
            if (orientedImageryLayer.FullExtent != null)
            {
                MainMapView.SetViewpoint(new Viewpoint(orientedImageryLayer.FullExtent));
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = ex.Message;
        }
    }

    private async void ApplyLayerButton_Click(object sender, RoutedEventArgs e)
    {
        if (Uri.TryCreate(LayerUriTextBox.Text, UriKind.Absolute, out var layerUri))
        {
            await ApplyLayerAsync(layerUri);
        }
        else
        {
            StatusTextBlock.Text = "Enter a valid oriented imagery layer URL.";
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

    public static bool HasSelectedImage(OrientedImage? selectedImage) => selectedImage != null;
}
