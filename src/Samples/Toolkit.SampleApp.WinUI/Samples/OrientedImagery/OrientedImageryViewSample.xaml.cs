#nullable enable

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
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

    private OrientedImageryLayer? _orientedImageryLayer;

    public OrientedImageryViewSample()
    {
        InitializeComponent();
        _ = InitializeAsync();
    }

    public static bool HasSelectedImage(OrientedImage? selectedImage) => selectedImage != null;

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

            _orientedImageryLayer = orientedImageryLayer;
            MainMapView.Map ??= new Map(new Uri(BasemapUri));
            MainMapView.Map.OperationalLayers.Clear();
            MainMapView.Map.OperationalLayers.Add(orientedImageryLayer);

            MainOrientedImageryView.OrientedImageryLayer = orientedImageryLayer;
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

    private async void MainMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
    {
        if (e.Location == null || _orientedImageryLayer == null)
        {
            return;
        }

        try
        {
            if (MainOrientedImageryView.ViewModel.AllowAddingMarkers)
            {
                MainOrientedImageryView.ViewModel.AddMarkerLocation(e.Location);
                return;
            }

            var identifyResult = await MainMapView.IdentifyLayerAsync(_orientedImageryLayer, e.Position, 0, false);
            if (identifyResult.GeoElements.FirstOrDefault() is Feature feature)
            {
                MainOrientedImageryView.ViewModel.SelectedImage = await _orientedImageryLayer.FetchImageForFeatureAsync(feature);
                return;
            }

            var parameters = new OrientedImageSearchParameters { MaxResults = -1 };
            var images = await _orientedImageryLayer.SearchImagesAsync(e.Location, parameters) ?? new List<OrientedImage>();
            MainOrientedImageryView.ViewModel.SetImages(images.ToList(), e.Location);
            MainOrientedImageryView.ViewModel.SelectedImage = images.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = ex.Message;
        }
    }

    private void OpenSelectedImagePopupButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedImage = MainOrientedImageryView.ViewModel.SelectedImage;
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
}
