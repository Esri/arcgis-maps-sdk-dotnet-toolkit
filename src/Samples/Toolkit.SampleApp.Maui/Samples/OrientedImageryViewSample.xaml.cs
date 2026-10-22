using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Maui;

namespace Toolkit.SampleApp.Maui.Samples;

/// <summary>
/// Interaction logic for OrientedImageryViewSample.xaml
/// </summary>
[SampleInfo(Category = "Oriented Imagery", DisplayName = "Oriented imagery view",
    Description = "Search and select oriented imagery, manage overlays and markers, and navigate image sequences.")]
public partial class OrientedImageryViewSample : ContentPage
{
    private readonly string[] _layerUrls =
    {
        "https://services.arcgis.com/2Pv4ow3pE6NFC9SW/ArcGIS/rest/services/Oakwood_camori_corrected/FeatureServer/0",
        "https://services8.arcgis.com/joda3ARQ9znLf0hf/ArcGIS/rest/services/HopefulHeights_360/FeatureServer/1",
        "https://services5.arcgis.com/N82JbI5EYtAkuUKU/ArcGIS/rest/services/EsriCampus_sequenceTest_attachment/FeatureServer/1",
    };
    private readonly MapView _mapView = new()
    {
        Map = new Map(new Uri("https://runtime.maps.arcgis.com/home/item.html?id=67372ff42cd145319639a99152b15bc3")),
    };
    private readonly SceneView _sceneView = new()
    {
        Scene = new Scene(new Uri("https://runtime.maps.arcgis.com/home/item.html?id=0560e29930dc4d5ebeb58c635c0909c9")),
    };
    private bool _usingMapView = true;
    private bool _active;
    private bool _layerReady;
    private int _loadVersion;
    private GeoView CurrentGeoView => _usingMapView ? _mapView : _sceneView;
    private GeoModel CurrentGeoModel => _usingMapView ? _mapView.Map! : _sceneView.Scene!;

    public OrientedImageryViewSample()
    {
        InitializeComponent();
        LayerPicker.ItemsSource = _layerUrls;
        GeoViewContainer.Add(CurrentGeoView);
#if WINDOWS
        LayoutRoot.RowDefinitions.Clear();
        LayoutRoot.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(0)));
        Grid.SetRow(ImageryView, 0);
        Grid.SetColumn(ImageryView, 1);
#endif
    }

    private async void LayerSelected(object? sender, EventArgs e) => await ApplySelectedLayerAsync();

    private async Task ApplySelectedLayerAsync()
    {
        int version = ++_loadVersion;
        var view = CurrentGeoView;
        var model = CurrentGeoModel;
        _layerReady = false;
        ImageryView.SelectedLayer = null;
        // The closed viewer is disconnected, so clear its previous layer state explicitly.
        ImageryView.ViewModel.OrientedImageryLayer = null;
        model.OperationalLayers.Clear();
        LoadError.IsVisible = false;
        if (!_active || LayerPicker.SelectedItem is not string url)
            return;

        try
        {
            await model.LoadAsync();
            if (version != _loadVersion || !_active) return;
            var layer = new OrientedImageryLayer(new Uri(url));
            if (!_usingMapView)
                layer.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
            model.OperationalLayers.Clear();
            model.OperationalLayers.Add(layer);
            await layer.LoadAsync();
            if (version != _loadVersion || !_active) return;
            ImageryView.SelectedLayer = layer;
            if (layer.FullExtent is { } extent)
                view.SetViewpoint(new Viewpoint(extent));
            _layerReady = true;
        }
        catch (Exception ex)
        {
            if (version != _loadVersion || !_active) return;
            LayerPicker.SelectedItem = null;
            LoadError.Text = ex.Message;
            LoadError.IsVisible = true;
        }
    }

    private void ToggleViewClicked(object? sender, EventArgs e)
    {
        ++_loadVersion;
        LayerPicker.SelectedItem = null;
        ImageryView.SelectedLayer = null;
        ImageryView.ViewModel.OrientedImageryLayer = null;
        ImageryView.GeoView = null;
        CurrentGeoModel.OperationalLayers.Clear();
        _usingMapView = !_usingMapView;
        CurrentGeoModel.OperationalLayers.Clear();
        GeoViewContainer.Children.Clear();
        GeoViewContainer.Add(CurrentGeoView);
        LoadError.IsVisible = false;
        _layerReady = false;
        if (_active && ImageryView.IsVisible)
            ImageryView.GeoView = CurrentGeoView;
    }

    /// <inheritdoc/>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _active = true;
        if (ImageryView.IsVisible)
            ImageryView.GeoView = CurrentGeoView;
        if (!_layerReady && LayerPicker.SelectedItem != null)
            await ApplySelectedLayerAsync();
    }

    /// <inheritdoc/>
    protected override void OnDisappearing()
    {
        _active = false;
        ++_loadVersion;
        ImageryView.GeoView = null;
        base.OnDisappearing();
    }

    private void OpenClicked(object? sender, EventArgs e) => SetViewerOpen(true);

    private void CloseClicked(object? sender, EventArgs e)
    {
        SetViewerOpen(false);
        OpenButton.Focus();
    }

    private void SetViewerOpen(bool open)
    {
        ImageryView.IsVisible = open;
#if WINDOWS
        LayoutRoot.ColumnDefinitions[1].Width = open ? GridLength.Star : new GridLength(0);
#else
        LayoutRoot.RowDefinitions[1].Height = open ? GridLength.Star : new GridLength(0);
#endif
        OpenButton.IsVisible = !open;
        CloseButton.IsVisible = open;
        ImageryView.GeoView = open && _active ? CurrentGeoView : null;
    }
}
