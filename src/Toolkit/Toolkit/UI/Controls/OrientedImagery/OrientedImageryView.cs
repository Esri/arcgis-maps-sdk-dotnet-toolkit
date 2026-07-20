#if WPF || WINDOWS_XAML

using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using System.Collections.ObjectModel;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

/// <summary>
/// Displays oriented imagery and provides controls for managing image selection, footprints, and markers.
/// </summary>
public partial class OrientedImageryView
{
    private const string ImageDisplayName = "PART_ImageDisplay";

    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryView"/> class.
    /// </summary>
    public OrientedImageryView() : base()
    {
#if WINDOWS_XAML
        InitializePlatform();
#endif

        ViewModel = new OrientedImageryViewModel();

#if WPF
        ItemsSource = GetDefaultToolbarItems();
#endif

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

        DataContext = ViewModel;

        var oldDisplay = _display;
        if (oldDisplay != null)
            oldDisplay.ImageClicked -= Display_ImageClicked;

        _display = GetTemplateChild(ImageDisplayName) as OrientedImageDisplay;

#if WINDOWS_XAML
        UpdateDisplayStateSubscriptions(oldDisplay, _display);
#endif

        if (_display == null)
            return;

        _display.ImageClicked += Display_ImageClicked;
        WireDisplayProperties();
    }

#region ViewModel
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
        return new() {new ShowSelectedFootprintVM(), new ShowUnselectedFootprintsVM(), new ShowCameraMarkersVM(), new AllowAddingMarkersVM(), markerSymbolPickerVM, new ClearMarkersVM() };
    }

    /// <summary>
    /// Gets or sets the view model for the oriented imagery view.
    /// </summary>
    public OrientedImageryViewModel ViewModel
    {
        get => (OrientedImageryViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ViewModel" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty ViewModelProperty =
        PropertyHelper.CreateProperty<OrientedImageryViewModel?, OrientedImageryView>(nameof(ViewModel), null, (s, oldValue, newValue) => s.OnViewModelChanged(oldValue, newValue));

    private void OnViewModelChanged(OrientedImageryViewModel? oldValue, OrientedImageryViewModel? newValue)
    {
        if (oldValue != null)
        {
            oldValue.PropertyChanged -= ViewModel_PropertyChanged;

            if (GeoView?.GraphicsOverlays != null)
                GeoView.GraphicsOverlays.Remove(oldValue.MarkersOverlay);
        }

        if (newValue == null)
        {
            SetValue(ViewModelProperty, new OrientedImageryViewModel());
            return;
        }

        newValue.PropertyChanged += ViewModel_PropertyChanged;
        DataContext = newValue;
        SetToolbarViewModels(newValue);

        if (GeoView != null)
        {
            if (GeoView.GraphicsOverlays == null)
                GeoView.GraphicsOverlays = new GraphicsOverlayCollection();
            GeoView.GraphicsOverlays.Add(newValue.MarkersOverlay);
        }

        WireDisplayProperties();
    }

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
#endregion ViewModel

#region Display
    private OrientedImageDisplay? _display;

    private void WireDisplayProperties()
    {
        if (_display == null)
            return;

        _display.AutoUpdateFootprint = ViewModel.AutoUpdateFootprint;
        _display.Markers = ViewModel.Markers;
        _display.Footprint = ViewModel.SelectedImageFootprint;
        _display.DisplayBackgroundColor = DisplayBackgroundColor;
    }

    // This should be overridable, probably as an Action dependency property or something. Or maybe it should be its own event with this as the default handler.
    private async void Display_ImageClicked(object? sender, OrientedImageDisplay.ImageClickedEventArgs e)
    {
        // Do not add a new marker if there is already one in proximity to the click location
        if (sender is not OrientedImageDisplay display || e.Marker != null)
            return;

        try
        {
            var location = await display.Footprint!.OrientedImage.ImageToLocationAsync(e.ImagePoint);
            ViewModel.AddMarkerLocation(location);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error converting image point to location: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets or sets the background color shown where the image does not fill the display.
    /// </summary>
    public System.Drawing.Color DisplayBackgroundColor
    {
        get => (System.Drawing.Color)GetValue(DisplayBackgroundColorProperty);
        set => SetValue(DisplayBackgroundColorProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="DisplayBackgroundColor" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty DisplayBackgroundColorProperty =
        PropertyHelper.CreateProperty<System.Drawing.Color, OrientedImageryView>(nameof(DisplayBackgroundColor), System.Drawing.Color.White, (s, oldValue, newValue) => s.UpdateDisplayBackgroundColor(newValue));

    private void UpdateDisplayBackgroundColor(System.Drawing.Color displayBackgroundColor)
    {
        if (_display != null)
            _display.DisplayBackgroundColor = displayBackgroundColor;
    }

#endregion Display

#region GeoModel
    /// <summary>
    /// Gets or sets the oriented imagery layer associated with this view.
    /// </summary>
    /// <remarks>
    /// This is a thin wrapper over the <see cref="OrientedImageryViewModel.OrientedImageryLayer"/> property.
    /// </remarks>
    public OrientedImageryLayer? OrientedImageryLayer
    {
        get => ViewModel.OrientedImageryLayer;
        set { ViewModel.OrientedImageryLayer = value; }
    }

    /// <summary>
    /// Gets or sets the <see cref="Esri.ArcGISRuntime.UI.Controls.GeoView"/> on which marker graphics are displayed.
    /// </summary>
    public GeoView? GeoView
    {
        get => (GeoView?)GetValue(GeoViewProperty);
        set => SetValue(GeoViewProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="GeoView"/> dependency property.
    /// </summary>
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
#endregion GeoModel
}

#endif
