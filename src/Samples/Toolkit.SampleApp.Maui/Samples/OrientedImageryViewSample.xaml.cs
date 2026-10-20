using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.SampleApp.Maui.Samples;

/// <summary>
/// Interaction logic for OrientedImageryViewSample.xaml
/// </summary>
[SampleInfo(Category = "Oriented Imagery", DisplayName = "Oriented imagery view",
    Description = "Search and select oriented imagery, manage overlays and markers, and navigate image sequences.")]
public partial class OrientedImageryViewSample : ContentPage
{
    private readonly OrientedImageryLayer[] _layers =
    {
        new(new Uri("https://services.arcgis.com/2Pv4ow3pE6NFC9SW/ArcGIS/rest/services/Oakwood_camori_corrected/FeatureServer/0")),
        new(new Uri("https://services.arcgis.com/gfpXnknjcY6QHKhU/arcgis/rest/services/CDOT00R20021_360_IMAGERY/FeatureServer/3")),
        new(new Uri("https://services5.arcgis.com/N82JbI5EYtAkuUKU/ArcGIS/rest/services/EsriCampus_sequenceTest_attachment/FeatureServer/1")),
    };
    private bool _initialized;

    public OrientedImageryViewSample()
    {
        InitializeComponent();
        LayerPicker.ItemDisplayBinding = Binding.Create(static (OrientedImageryLayer layer) => layer.Name);
        MyMapView.Map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=67372ff42cd145319639a99152b15bc3"));
        foreach (var layer in _layers)
            MyMapView.Map.OperationalLayers.Add(layer);
#if WINDOWS
        LayoutRoot.RowDefinitions.Clear();
        LayoutRoot.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(0)));
        Grid.SetRow(ImageryView, 0);
        Grid.SetColumn(ImageryView, 1);
#endif
    }

    private void LayerSelected(object? sender, EventArgs e) =>
        ImageryView.SelectedLayer = LayerPicker.SelectedItem as OrientedImageryLayer;

    /// <inheritdoc/>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ImageryView.IsVisible)
            ImageryView.GeoView = MyMapView;
        if (_initialized) return;
        _initialized = true;
        try
        {
            await Task.WhenAll(_layers.Select(layer => layer.LoadAsync()));
            LayerPicker.ItemsSource = _layers;
            LayerPicker.SelectedItem = _layers[0];
            if (_layers[0].FullExtent is { } extent)
                MyMapView.SetViewpoint(new Viewpoint(extent));
        }
        catch (Exception ex)
        {
            LoadError.Text = ex.Message;
            LoadError.IsVisible = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnDisappearing()
    {
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
        ImageryView.GeoView = open ? MyMapView : null;
    }
}
