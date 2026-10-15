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
    private double _panelFraction = 0.55;
    private double _dragStartHeight;
    private double _landscapePanelFraction = 0.5;
    private bool _landscape;
#if !WINDOWS
    private bool _layoutUpdatePending;
#endif
    private bool _mobileLayoutInitialized;

    public OrientedImageryViewSample()
    {
        InitializeComponent();
        MyMapView.Map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=67372ff42cd145319639a99152b15bc3"));
        foreach (var layer in _layers)
            MyMapView.Map.OperationalLayers.Add(layer);
#if WINDOWS
        ResizeHandle.IsVisible = false;
        ViewerHost.RowDefinitions[0].Height = 0;
        LayoutRoot.RowDefinitions.Clear();
        LayoutRoot.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        LayoutRoot.ColumnDefinitions.Clear();
        LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(0)));
        Grid.SetRow(ViewerHost, 0);
        Grid.SetColumn(ViewerHost, 1);
#else
        LayoutRoot.SizeChanged += (_, _) =>
        {
            if (_layoutUpdatePending) return;
            _layoutUpdatePending = true;
            // SizeChanged runs during arrange; change grid tracks only after that pass has completed.
            Dispatcher.Dispatch(() =>
            {
                _layoutUpdatePending = false;
                UpdateMobileLayout();
            });
        };
#endif
    }

    /// <inheritdoc/>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ViewerHost.IsVisible)
            ImageryView.GeoView = MyMapView;
        if (_initialized) return;
        _initialized = true;
        try
        {
            await _layers[0].LoadAsync();
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

    private void CloseClicked(object? sender, EventArgs e)
    {
        ImageryView.GeoView = null;
        ViewerHost.IsVisible = false;
#if WINDOWS
        LayoutRoot.ColumnDefinitions[1].Width = 0;
#else
        if (_landscape)
            LayoutRoot.ColumnDefinitions[1].Width = 0;
        else
            LayoutRoot.RowDefinitions[1].Height = 0;
#endif
        OpenButton.IsVisible = true;
        CloseButton.IsVisible = false;
        OpenButton.Focus();
    }

    private void OpenClicked(object? sender, EventArgs e)
    {
        ViewerHost.IsVisible = true;
#if WINDOWS
        LayoutRoot.ColumnDefinitions[1].Width = 440;
#else
        UpdateMobileLayout();
#endif
        OpenButton.IsVisible = false;
        CloseButton.IsVisible = true;
        ImageryView.GeoView = MyMapView;
    }

    private void ResizePanel(object? sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType == GestureStatus.Started)
            _dragStartHeight = _landscape ? ViewerHost.Width : ViewerHost.Height;
        else if (e.StatusType == GestureStatus.Running)
        {
            if (_landscape)
                SetPanelWidth(_dragStartHeight - e.TotalX);
            else
                SetPanelHeight(_dragStartHeight - e.TotalY);
        }
    }

    private void UpdateMobileLayout()
    {
        if (LayoutRoot.Width <= 0 || LayoutRoot.Height <= 0) return;
        bool landscape = LayoutRoot.Width > LayoutRoot.Height;
        if (landscape != _landscape || !_mobileLayoutInitialized)
        {
            _landscape = landscape;
            _mobileLayoutInitialized = true;
            if (landscape)
            {
                LayoutRoot.RowDefinitions[0].Height = GridLength.Star;
                LayoutRoot.RowDefinitions[1].Height = 0;
                ViewerHost.RowDefinitions[0].Height = GridLength.Star;
                ViewerHost.RowDefinitions[1].Height = 0;
                ViewerHost.ColumnDefinitions[0].Width = 20;
                ViewerHost.ColumnDefinitions[1].Width = GridLength.Star;
            }
            else
            {
                LayoutRoot.ColumnDefinitions[0].Width = GridLength.Star;
                LayoutRoot.ColumnDefinitions[1].Width = 0;
                ViewerHost.RowDefinitions[0].Height = 20;
                ViewerHost.RowDefinitions[1].Height = GridLength.Star;
                ViewerHost.ColumnDefinitions[0].Width = GridLength.Star;
                ViewerHost.ColumnDefinitions[1].Width = 0;
            }
            Grid.SetRow(ViewerHost, landscape ? 0 : 1);
            Grid.SetColumn(ViewerHost, landscape ? 1 : 0);
            Grid.SetRow(ImageryView, landscape ? 0 : 1);
            Grid.SetColumn(ImageryView, landscape ? 1 : 0);
            ResizeGrip.WidthRequest = landscape ? 3 : 28;
            ResizeGrip.HeightRequest = landscape ? 28 : 3;
        }
        if (!ViewerHost.IsVisible)
        {
            if (landscape) LayoutRoot.ColumnDefinitions[1].Width = 0;
            else LayoutRoot.RowDefinitions[1].Height = 0;
        }
        else if (landscape)
            SetPanelWidth(LayoutRoot.Width * _landscapePanelFraction);
        else
            SetPanelHeight(LayoutRoot.Height * _panelFraction);
    }

    private void SetPanelWidth(double width)
    {
        if (LayoutRoot.Width <= 0 || !ViewerHost.IsVisible) return;
        double minimum = Math.Min(300, LayoutRoot.Width * 0.6);
        double maximum = Math.Max(minimum, LayoutRoot.Width - 160);
        double panelWidth = Math.Clamp(width, minimum, maximum);
        _landscapePanelFraction = panelWidth / LayoutRoot.Width;
        LayoutRoot.ColumnDefinitions[0].Width = new GridLength(1 - _landscapePanelFraction, GridUnitType.Star);
        LayoutRoot.ColumnDefinitions[1].Width = new GridLength(_landscapePanelFraction, GridUnitType.Star);
    }

    private void SetPanelHeight(double height)
    {
        if (LayoutRoot.Height <= 0 || !ViewerHost.IsVisible) return;
        double minimum = Math.Min(260, LayoutRoot.Height * 0.65);
        double maximum = Math.Max(minimum, LayoutRoot.Height - 100);
        double panelHeight = Math.Clamp(height, minimum, maximum);
        _panelFraction = panelHeight / LayoutRoot.Height;
        LayoutRoot.RowDefinitions[0].Height = new GridLength(1 - _panelFraction, GridUnitType.Star);
        LayoutRoot.RowDefinitions[1].Height = new GridLength(_panelFraction, GridUnitType.Star);
    }

}
