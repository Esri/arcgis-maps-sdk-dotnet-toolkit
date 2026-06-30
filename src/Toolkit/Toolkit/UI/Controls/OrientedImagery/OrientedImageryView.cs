#if WPF

using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using System.Collections.ObjectModel;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

public partial class OrientedImageryView
{
    private const string ImageDisplayName = "PART_ImageDisplay";

    private OrientedImageDisplay? _display;
    private OrientedImageryViewModel _viewModel;

    public OrientedImageryView() : base()
    {
        _display = new OrientedImageDisplay();

        ViewModel = new OrientedImageryViewModel();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        ItemsSource = GetDefaultToolbarItems();

#if MAUI
        // MAUI layout containers are not tab stops by default, so no IsTabStop is needed here.
        ControlTemplate = DefaultControlTemplate;
#else
        DefaultStyleKey = typeof(OrientedImageryView);
#endif
    }

    /// <inheritdoc/>
#if WINDOWS_XAML || MAUI
    protected override void OnApplyTemplate()
#elif WPF
    public override void OnApplyTemplate()
#endif
    {
        base.OnApplyTemplate();

        DataContext = _viewModel;

        if (_display != null)
            _display.ImageClicked -= Display_ImageClicked;

        _display = GetTemplateChild(ImageDisplayName) as OrientedImageDisplay;

        if (_display == null)
            return;

        // These should probably be proper bindings
        _display.AutoUpdateFootprint = ViewModel.AutoUpdateFootprint;
        _display.Markers = ViewModel.Markers;
        _display.Footprint = ViewModel.SelectedImageFootprint;
        _display.ImageClicked += Display_ImageClicked;
    }

    /// <summary>
    /// Gets the default toolbar items for the OrientedImageryView.
    /// </summary>
    public static Collection<object> GetDefaultToolbarItems()
    {
        var markerSymbolPickerVM = new SelectNewMarkerSymbolVM(new Collection<MarkerSymbol>()
        {
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, System.Drawing.Color.Purple, 10),
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Yellow, 10),
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 10)
        });
        return new() { new AutoUpdateFootprintVM(), new ShowSelectedFootprintVM(), new ShowUnselectedFootprintsVM(), new ShowCameraMarkersVM(), new AllowAddingMarkersVM(), markerSymbolPickerVM, new ClearMarkersVM() };
    }

    // Setter should eventually be public and handle event wiring
    public OrientedImageryViewModel ViewModel
    {
        get { return _viewModel; }
        private set { _viewModel = value; }
    }

    // This should be overridable, probably as an Action dependency property or something. Or maybe it should be its own event with this as the default handler.
    private async void Display_ImageClicked(object? sender, OrientedImageDisplay.ImageClickedEventArgs e)
    {
        if (e.Marker != null)
            return; // do not add a new marker if there is already one in proximity to the click location

        if (sender is OrientedImageDisplay display)
        {
            var location = await display.Footprint!.OrientedImage.ImageToLocationAsync(e.ImagePoint);
            ViewModel.AddMarkerLocation(location);
        }
    }

    #region GisProperties
    // This should be made a dependency property
    public OrientedImageryLayer OrientedImageryLayer
    {
        get => ViewModel.OrientedImageryLayer;
        set { ViewModel.OrientedImageryLayer = value; }
    }

    public GeoView? GeoView
    {
        get => (GeoView?)GetValue(GeoViewProperty);
        set => SetValue(GeoViewProperty, value);
    }

    public static readonly DependencyProperty GeoViewProperty =
        PropertyHelper.CreateProperty<GeoView?, OrientedImageryView>(nameof(GeoView), null, (s, oldValue, newValue) => s.UpdateGeoView(oldValue, newValue));

    private void UpdateGeoView(GeoView? oldGeoView, GeoView? newGeoView)
    {
        if (oldGeoView == newGeoView) return;

        if (oldGeoView != null)
        {
            oldGeoView.GraphicsOverlays?.Remove(ViewModel.MarkersOverlay);
        }

        if (newGeoView != null)
        {
            if (newGeoView.GraphicsOverlays == null)
                newGeoView.GraphicsOverlays = new GraphicsOverlayCollection();
            newGeoView.GraphicsOverlays.Add(ViewModel.MarkersOverlay);
        }
    }
    #endregion GisProperties

    #region ViewModelPropertyHandling
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(OrientedImageryViewModel.SelectedImageFootprint):
                if (_display != null)
                    _display.Footprint = ViewModel.SelectedImageFootprint;
                break;
            case nameof(OrientedImageryViewModel.AutoUpdateFootprint):
                if (_display != null)
                    _display.AutoUpdateFootprint = ViewModel.AutoUpdateFootprint;
                break;
            case nameof(OrientedImageryViewModel.Markers):
                if (_display != null)
                    _display.Markers = ViewModel.Markers;
                break;
        }
    }
    #endregion ViewModelPropertyHandling
}

#endif