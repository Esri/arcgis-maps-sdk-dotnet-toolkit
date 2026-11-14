using System.Collections.ObjectModel;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Maui;
using Esri.ArcGISRuntime.Toolkit.Maui.OrientedImagery;
using Graphic = Esri.ArcGISRuntime.UI.Graphic;

namespace Toolkit.SampleApp.Maui.Samples;

/// <summary>
/// Interaction logic for OrientedImageryViewerSample.xaml
/// </summary>
[SampleInfo(Category = "Oriented Imagery", DisplayName = "Oriented imagery viewer",
    Description = "Search and select oriented imagery, manage overlays and markers, and navigate image sequences.")]
public partial class OrientedImageryViewerSample : ContentPage
{
    #region Sample state and setup

    private readonly ObservableCollection<string> _layerUrls =
    [
        "https://services.arcgis.com/2Pv4ow3pE6NFC9SW/ArcGIS/rest/services/Oakwood_camori_corrected/FeatureServer/0",
        "https://services8.arcgis.com/joda3ARQ9znLf0hf/arcgis/rest/services/AerialsphereScottsdale_360/FeatureServer/1",
        "https://services5.arcgis.com/N82JbI5EYtAkuUKU/ArcGIS/rest/services/EsriCampus_sequenceTest_attachment/FeatureServer/1",
    ];
    private readonly MapView _mapView = new()
    {
        Map = new Map(new Uri("https://runtime.maps.arcgis.com/home/item.html?id=67372ff42cd145319639a99152b15bc3")),
    };
    private readonly SceneView _sceneView = new()
    {
        Scene = new Scene(new Uri("https://runtime.maps.arcgis.com/home/item.html?id=0560e29930dc4d5ebeb58c635c0909c9")),
    };
    private bool _usingMapView = true;
    private readonly ElevationSource _elevationSource = new ArcGISTiledElevationSource(
        new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer"));
    private readonly ToolbarSelector? _defaultToolbarSelector;
    private bool _usingAlternateToolbar;
    private bool _overrideClicks;
    private bool _active;
    private int _loadVersion;
    private GeoView CurrentGeoView => _usingMapView ? _mapView : _sceneView;
    private GeoModel CurrentGeoModel => _usingMapView ? _mapView.Map! : _sceneView.Scene!;

    public OrientedImageryViewerSample()
    {
        InitializeComponent();
        _defaultToolbarSelector = ImageryView.ToolbarItemTemplateSelector;
        LayerPicker.ItemsSource = _layerUrls;
        GeoViewContainer.Add(CurrentGeoView);
        LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(0)));
        SizeChanged += (_, _) => UpdateViewerLayout();
    }

    #endregion

    #region Layer and map selection

    private void AddLayerClicked(object? sender, EventArgs e)
    {
        try
        {
            var url = LayerUriEntry.Text;
            _ = new OrientedImageryLayer(new Uri(url));
            _layerUrls.Add(url);
            LoadError.IsVisible = false;
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void LayerSelected(object? sender, EventArgs e)
    {
        int version = ++_loadVersion;
        // A newer selection or navigation away makes this load's result obsolete.
        var view = CurrentGeoView;
        var model = CurrentGeoModel;
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
            ImageryView.ViewModel.OrientedImageryLayer = layer;
            if (layer.FullExtent is { } extent)
                view.SetViewpoint(new Viewpoint(extent));
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
        ImageryView.GeoView = null;
        CurrentGeoModel.OperationalLayers.Clear();
        _usingMapView = !_usingMapView;
        CurrentGeoModel.OperationalLayers.Clear();
        GeoViewContainer.Children.Clear();
        GeoViewContainer.Add(CurrentGeoView);
        LoadError.IsVisible = false;
        if (_active && ImageryView.IsVisible)
            ImageryView.GeoView = CurrentGeoView;
    }

    #endregion

    #region Testing controls

    private void ToggleElevationClicked(object? sender, EventArgs e)
    {
        if (_sceneView.Scene?.BaseSurface is not Surface surface)
            return;

        if (surface.ElevationSources.Count > 0)
            surface.ElevationSources.Clear();
        else
            surface.ElevationSources.Add(_elevationSource);
    }

    private void UpdateToolbarClicked(object? sender, EventArgs e)
    {
        var overlays = ImageryView.ViewModel.ToolbarCategories.First(
            static category => category.Name == ToolbarCategory.OverlaysCategoryName);
        _usingAlternateToolbar = !_usingAlternateToolbar;
        if (_usingAlternateToolbar)
        {
            ImageryView.ToolbarItemTemplateSelector = (ToolbarSelector)Resources["AlternateToolbarSelector"];
            overlays.ToolbarItems.Add(new ShowSelectedImagePopupVM());
        }
        else
        {
            var popupItem = overlays.ToolbarItems.OfType<ShowSelectedImagePopupVM>().FirstOrDefault();
            if (popupItem != null)
                overlays.ToolbarItems.Remove(popupItem);
            ImageryView.ToolbarItemTemplateSelector = _defaultToolbarSelector;
            CloseSelectedImagePopup(this, EventArgs.Empty);
        }
    }

    private void PopupToolbarButtonLoaded(object? sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.SetBinding(Button.IsEnabledProperty,
                static (ShowSelectedImagePopupVM item) => item.ViewModel?.SelectedImage,
                converter: new CommunityToolkit.Maui.Converters.IsNotNullConverter());
        }
    }

    private void OpenSelectedImagePopup(object? sender, EventArgs e)
    {
        if (ImageryView.ViewModel.SelectedImage is not { } image)
            return;

        var graphic = new Graphic(image.Geometry);
        foreach (var attribute in image.Attributes)
            graphic.Attributes[attribute.Key] = attribute.Value;

        SelectedImagePopupViewer.Popup = Popup.FromGeoElement(graphic);
        SelectedImagePopupBackground.IsVisible = true;
    }

    private void CloseSelectedImagePopup(object? sender, EventArgs e)
    {
        SelectedImagePopupBackground.IsVisible = false;
        SelectedImagePopupViewer.Popup = null;
    }

    private void OverrideClicksClicked(object? sender, EventArgs e)
    {
        _overrideClicks = !_overrideClicks;
        if (_overrideClicks)
        {
            ImageryView.GeoViewTapped += ImageryViewGeoViewTapped;
            ImageryView.ImageTapped += ImageryViewImageTapped;
        }
        else
        {
            ImageryView.GeoViewTapped -= ImageryViewGeoViewTapped;
            ImageryView.ImageTapped -= ImageryViewImageTapped;
        }
    }

    private void ImageryViewGeoViewTapped(object? sender, OrientedImageryViewerGeoViewTappedArgs e)
    {
        e.Handled = true;
        if (e.GeoViewInputEventArgs.Location is { } location)
        {
            var symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Green, 20);
            ImageryView.ViewModel.AddMarkerLocation(location, symbol);
        }
    }

    private async void ImageryViewImageTapped(object? sender, OrientedImageryViewerImageTappedArgs e)
    {
        e.Handled = true;
        int version = _loadVersion;
        try
        {
            var location = await e.Image.ImageToLocationAsync(e.ImagePoint);
            if (version != _loadVersion || !_active)
                return;

            var symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Pink, 20);
            ImageryView.ViewModel.AddMarkerLocation(location, symbol);
        }
        catch (Exception ex)
        {
            if (version == _loadVersion && _active)
                ShowError(ex);
        }
    }

    private void DetachReattachGeoViewClicked(object? sender, EventArgs e)
    {
        if (ImageryView.GeoView != null)
        {
            ++_loadVersion;
            LayerPicker.SelectedItem = null;
            ImageryView.GeoView = null;
            CloseSelectedImagePopup(this, EventArgs.Empty);
        }
        else if (_active && ImageryView.IsVisible)
        {
            ImageryView.GeoView = CurrentGeoView;
        }
    }

    private void ShowError(Exception exception)
    {
        LoadError.Text = exception.Message;
        LoadError.IsVisible = true;
    }

    #endregion

    #region Page lifecycle

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _active = true;
        if (ImageryView.IsVisible)
            ImageryView.GeoView = CurrentGeoView;
    }

    /// <inheritdoc/>
    protected override void OnDisappearing()
    {
        _active = false;
        CloseSelectedImagePopup(this, EventArgs.Empty);
        ++_loadVersion;
        LayerPicker.SelectedItem = null;
        LoadError.IsVisible = false;
        ImageryView.GeoView = null;
        base.OnDisappearing();
    }

    #endregion

    #region Viewer visibility and layout

    private void OpenClicked(object? sender, EventArgs e) => SetViewerOpen(true);

    private void CloseClicked(object? sender, EventArgs e)
    {
        SetViewerOpen(false);
        OpenButton.Focus();
    }

    private void SetViewerOpen(bool open)
    {
        if (!open)
        {
            ++_loadVersion;
            LayerPicker.SelectedItem = null;
            LoadError.IsVisible = false;
        }

        ImageryView.IsVisible = open;
        UpdateViewerLayout();
        OpenButton.IsVisible = !open;
        CloseButton.IsVisible = open;
        ImageryView.GeoView = open && _active ? CurrentGeoView : null;
    }

    private void UpdateViewerLayout()
    {
#if WINDOWS
        bool sideBySide = true;
#else
        bool sideBySide = Width > Height;
#endif
        Grid.SetRow(ImageryView, sideBySide ? 0 : 1);
        Grid.SetColumn(ImageryView, sideBySide ? 1 : 0);
        bool open = ImageryView.IsVisible;
        LayoutRoot.RowDefinitions[1].Height = open && !sideBySide ? GridLength.Star : new GridLength(0);
        LayoutRoot.ColumnDefinitions[1].Width = open && sideBySide ? GridLength.Star : new GridLength(0);
    }

    #endregion
}

public class ShowSelectedImagePopupVM : ToolbarItemBase
{
}
