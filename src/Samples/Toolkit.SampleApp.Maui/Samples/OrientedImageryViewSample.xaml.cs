using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.UI;
using MauiGrid = Microsoft.Maui.Controls.Grid;

namespace Toolkit.SampleApp.Maui.Samples;

[XamlCompilation(XamlCompilationOptions.Compile)]
[SampleInfo(Category = "OrientedImageryView", Description = "Displays and interacts with oriented imagery.")]
public partial class OrientedImageryViewSample : ContentPage
{
    private const string BasemapUri = "https://runtime.maps.arcgis.com/home/item.html?id=d8c5e76fb2cc4bb6955a6783a5f577b7";

    private OrientedImageryLayer? _orientedImageryLayer;
    private bool _isInitialized;

    public OrientedImageryViewSample()
    {
        InitializeComponent();
        SizeChanged += OrientedImageryViewSample_SizeChanged;
        Loaded += OrientedImageryViewSample_Loaded;
        Unloaded += OrientedImageryViewSample_Unloaded;
    }

    private async void OrientedImageryViewSample_Loaded(object? sender, EventArgs e)
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        MainMapView.Map = new Map(new Uri(BasemapUri));
        MainMapView.GeoViewTapped += MainMapView_GeoViewTapped;

        try
        {
            await ApplyLayerAsync(new Uri(LayerUriEntry.Text ?? throw new InvalidOperationException("A layer URL is required.")));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(ex.GetType().Name, ex.Message, "OK");
        }
    }

    private void OrientedImageryViewSample_Unloaded(object? sender, EventArgs e)
    {
        MainMapView.GeoViewTapped -= MainMapView_GeoViewTapped;
        _isInitialized = false;
    }

    private void OrientedImageryViewSample_SizeChanged(object? sender, EventArgs e)
    {
        bool useSideBySideLayout = Width >= 800;
        ViewerGrid.ColumnDefinitions[0].Width = useSideBySideLayout ? new GridLength(3, GridUnitType.Star) : GridLength.Star;
        ViewerGrid.ColumnDefinitions[1].Width = useSideBySideLayout ? new GridLength(2, GridUnitType.Star) : new GridLength(0);
        ViewerGrid.RowDefinitions[0].Height = GridLength.Star;
        ViewerGrid.RowDefinitions[1].Height = useSideBySideLayout ? new GridLength(0) : GridLength.Star;

        MauiGrid.SetColumn(MainOrientedImageryView, useSideBySideLayout ? 1 : 0);
        MauiGrid.SetRow(MainOrientedImageryView, useSideBySideLayout ? 0 : 1);
    }

    private async Task ApplyLayerAsync(Uri layerUri)
    {
        OrientedImageryLayer layer = new(layerUri);
        await layer.LoadAsync();
        if (layer.LoadStatus == LoadStatus.FailedToLoad)
            throw new InvalidOperationException("The oriented imagery layer could not be loaded.", layer.LoadError);

        _orientedImageryLayer = layer;
        ShowDetailsButton.IsEnabled = false;
        SelectedImagePopupBackground.IsVisible = false;
        SelectedImagePopupViewer.Popup = null;
        MainMapView.Map ??= new Map(new Uri(BasemapUri));
        MainMapView.Map.OperationalLayers.Clear();
        MainMapView.Map.OperationalLayers.Add(layer);
        MainOrientedImageryView.OrientedImageryLayer = layer;

        if (layer.FullExtent != null)
            MainMapView.SetViewpoint(new Viewpoint(layer.FullExtent));
    }

    private async void ApplyLayerButton_Clicked(object sender, EventArgs e)
    {
        if (!Uri.TryCreate(LayerUriEntry.Text, UriKind.Absolute, out Uri? layerUri))
        {
            await DisplayAlertAsync("Invalid URL", "Enter a valid oriented imagery layer URL.", "OK");
            return;
        }

        try
        {
            await ApplyLayerAsync(layerUri);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(ex.GetType().Name, ex.Message, "OK");
        }
    }

    private async void MainMapView_GeoViewTapped(object? sender, GeoViewInputEventArgs e)
    {
        if (e.Location == null || _orientedImageryLayer == null)
            return;

        try
        {
            if (MainOrientedImageryView.ViewModel.AllowAddingMarkers)
            {
                MainOrientedImageryView.ViewModel.AddMarkerLocation(e.Location);
                return;
            }

            IdentifyLayerResult identifyResult = await MainMapView.IdentifyLayerAsync(_orientedImageryLayer, e.Position, 0, false);
            if (identifyResult.GeoElements.FirstOrDefault() is Feature feature)
            {
                MainOrientedImageryView.ViewModel.SelectedImage = await _orientedImageryLayer.FetchImageForFeatureAsync(feature);
            }
            else
            {
                OrientedImageSearchParameters parameters = new() { MaxResults = -1 };
                List<OrientedImage> images = (await _orientedImageryLayer.SearchImagesAsync(e.Location, parameters) ?? []).ToList();
                MainOrientedImageryView.ViewModel.SetImages(images, e.Location);
                MainOrientedImageryView.ViewModel.SelectedImage = images.FirstOrDefault();
            }

            ShowDetailsButton.IsEnabled = MainOrientedImageryView.ViewModel.SelectedImage != null;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(ex.GetType().Name, ex.Message, "OK");
        }
    }

    private void ShowDetailsButton_Clicked(object sender, EventArgs e)
    {
        OrientedImage? selectedImage = MainOrientedImageryView.ViewModel.SelectedImage;
        if (selectedImage == null)
            return;

        SelectedImagePopupViewer.Popup = CreatePopup(selectedImage);
        SelectedImagePopupBackground.IsVisible = true;
    }

    private static Popup CreatePopup(OrientedImage orientedImage)
    {
        Graphic graphic = new(orientedImage.Geometry);
        foreach ((string key, object? value) in orientedImage.Attributes)
            graphic.Attributes[key] = value;

        return Popup.FromGeoElement(graphic);
    }

    private void CloseDetailsButton_Clicked(object sender, EventArgs e)
    {
        SelectedImagePopupBackground.IsVisible = false;
        SelectedImagePopupViewer.Popup = null;
    }
}
