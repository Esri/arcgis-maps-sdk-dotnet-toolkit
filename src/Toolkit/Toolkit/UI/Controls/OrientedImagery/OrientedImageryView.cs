#if WPF

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using System.Collections.ObjectModel;
using static System.Net.Mime.MediaTypeNames;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

public partial class OrientedImageryView
{
    private const string ImageDisplayName = "PART_ImageDisplay";

    private OrientedImageDisplay? _display;
    private List<OrientedImage> _images;
    private ObservableCollection<OrientedImageMarker> _markers;
    private GraphicsOverlay _markersOverlay;

    public OrientedImageryView() : base()
    {
        _markersOverlay = new GraphicsOverlay() { Id = "OrientedImageryView_Markers_Overlay" };
        _display = new OrientedImageDisplay();
        AutoUpdateFootprint = true;
        NewMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Red, 20);
        SearchPointMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Blue, 20);
        _markers = new ObservableCollection<OrientedImageMarker>();
        _markers.CollectionChanged += (_, _) => UpdateMarkerGraphics();

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

        if (_display != null)
            _display.ImageClicked -= Display_ImageClicked;

        _display = GetTemplateChild(ImageDisplayName) as OrientedImageDisplay;

        if (_display == null)
            return;

        _display.AutoUpdateFootprint = AutoUpdateFootprint;
        _display.Markers = _markers;
        _display.Footprint = SelectedImage == null ? null : new OrientedImageFootprint(SelectedImage);
        UpdateVisibleFootprints();
        _display.ImageClicked += Display_ImageClicked;
    }

    private async void Display_ImageClicked(object? sender, OrientedImageDisplay.ImageClickedEventArgs e)
    {
        if (e.Marker != null)
            return; // do not add a new marker if there is already one in proximity to the click location

        var location = await SelectedImage!.ImageToLocationAsync(e.ImagePoint);
        _markers.Add(new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(location), NewMarkerSymbol));
    }

    #region GeoViewStuff
    private OrientedImageryLayer _oiLayer;
    public OrientedImageryLayer OrientedImageryLayer
    {
        get => _oiLayer;
        set
        {
            if (_oiLayer == value) return;

            if (_oiLayer != null)
            {
                _oiLayer.VisibleFootprints.Clear();
            }

            _oiLayer = value;
            UpdateVisibleFootprints();
        }
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
            oldGeoView.GraphicsOverlays?.Remove(_markersOverlay);
        }

        if (newGeoView != null)
        {
            if (newGeoView.GraphicsOverlays == null)
                newGeoView.GraphicsOverlays = new GraphicsOverlayCollection();
            newGeoView.GraphicsOverlays.Add(_markersOverlay);
        }
    }
    #endregion

    #region ImageManagement
    /// <summary>
    /// Sets the images to display in the control. Optionally, a search point can be provided to indicate the origin of the search.
    /// </summary>
    public void SetImages(Collection<OrientedImage> images, MapPoint? searchPoint = null)
    {
        UpdateSearchPointMarker(searchPoint);

        _images = images.ToList();
        _footprints = _images.Select((img) => new OrientedImageFootprint(img)).ToList();
        UpdateVisibleFootprints();
    }

    private OrientedImage? _selectedImage;
    public OrientedImage? SelectedImage
    {
        get => _selectedImage;
        set
        {
            if (value == _selectedImage) return;

            _selectedImage = value;

            if (_display == null) return;

            if (_selectedImage != null)
            {
                var footprint = _footprints.FirstOrDefault((fpt) => fpt.OrientedImage == _selectedImage);
                _display.Footprint = footprint ?? new OrientedImageFootprint(_selectedImage);
            }
            else
            {
                _display.Footprint = null;
            }
            UpdateVisibleFootprints();
        }
    }

    // Commands for move to next/previous image. Use Images.IndexOf, no need to track index as a field
    #endregion ImageManagement

    #region FootprintManagement
    private List<OrientedImageFootprint> _footprints = new();

    // These booleans need to be bindable
    public bool ShowSelectedFootprint = true;
    public bool ShowUnselectedFootprints = false;

    private bool _autoUpdateFootprint;
    public bool AutoUpdateFootprint
    {
        get => _autoUpdateFootprint;
        set
        {
            _autoUpdateFootprint = value;
            if (_display != null)
            {
                _display.AutoUpdateFootprint = value;
            }
        }
    }

    // These colors should be bindable
    public System.Drawing.Color SelectedFootprintFillColor = System.Drawing.Color.Orange;
    public System.Drawing.Color SelectedFootprintOutlineColor = System.Drawing.Color.Black;
    public System.Drawing.Color UnselectedFootprintFillColor = System.Drawing.Color.Blue;
    public System.Drawing.Color UnselectedFootprintOutlineColor = System.Drawing.Color.Orange;

    private void UpdateVisibleFootprints()
    {
        if (OrientedImageryLayer == null) return;

        // Not performant but simple
        bool selectedFootprintInFootprints = false;
        foreach (var ftp in _footprints)
        {
            if (ftp != _display?.Footprint)
            {
                ftp.FillColor = SelectedFootprintFillColor;
                ftp.OutlineColor = SelectedFootprintOutlineColor;
            }
            else
            {
                ftp.FillColor = UnselectedFootprintFillColor;
                ftp.OutlineColor = UnselectedFootprintOutlineColor;
                selectedFootprintInFootprints = true;
            }
        }

        OrientedImageryLayer.VisibleFootprints.Clear();
        if (ShowUnselectedFootprints)
        {
            foreach (var ftp in _footprints)
            {
                if (ftp == _display?.Footprint && !ShowSelectedFootprint)
                    continue;

                OrientedImageryLayer.VisibleFootprints.Add(ftp);
            }
        }
        if (ShowSelectedFootprint && (!selectedFootprintInFootprints && _display?.Footprint != null))
            OrientedImageryLayer.VisibleFootprints.Add(_display.Footprint);
    }
    #endregion FootprintManagement

    #region Markers
    /// <summary>
    /// Gets or sets the default symbology to use when adding new markers.
    /// </summary>
    public MarkerSymbol NewMarkerSymbol { get; set; }


    /// <summary>
    /// Gets or sets the symbol to use for the search point marker.
    /// </summary>
    public MarkerSymbol SearchPointMarkerSymbol { get; set; }

    private const string SearchPointMarkerTag = "SearchPointMarker";
    private void UpdateSearchPointMarker(MapPoint? location)
    {
        var existingMarker = _markers.FirstOrDefault((marker) => marker?.Tag is string tag && tag == SearchPointMarkerTag, null);

        if (existingMarker != null)
            _markers.Remove(existingMarker);

        if (location != null)
        {
            _markers.Add(new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(location), SearchPointMarkerSymbol) { Tag = SearchPointMarkerTag });
        }
    }

    /// <summary>
    /// Add a marker from a geographic location. The new marker is added uses the <cref="NewMarkerSymbol"/> unless
    /// overriden using the <paramref name="symbol"/> parameter.
    /// </summary>
    public void AddMarkerLocation(MapPoint location, MarkerSymbol? symbol)
    {
        _markers.Add(new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(location), symbol ?? NewMarkerSymbol));
    }

    /// <summary>
    /// Clears all markers on the image. Saves the search point marker if it exists.
    /// </summary>
    public void ClearMarkers()
    {
        var searchPointMarker = _markers.FirstOrDefault((marker) => marker.Tag is string tag && tag == SearchPointMarkerTag);
        _markers.Clear();
        if (searchPointMarker != null)
            _markers.Add(searchPointMarker);
    }

    private void UpdateMarkerGraphics()
    {
        _markersOverlay.Graphics.Clear();
        _markersOverlay.Graphics.AddRange(_markers.Select((mk) => new Graphic(mk.Position.Location!, mk.Symbol)));
    }
    #endregion Markers
}

#endif