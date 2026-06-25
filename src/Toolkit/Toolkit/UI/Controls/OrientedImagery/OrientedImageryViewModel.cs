using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

#if WPF
public class OrientedImageryViewModel : INotifyPropertyChanged
{
    public OrientedImageryViewModel() : base()
    {
        _markersOverlay = new GraphicsOverlay() { Id = "OrientedImageryView_Markers_Overlay" };
        NewMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Red, 20);
        SearchPointMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Blue, 20);
        _markers = new ObservableCollection<OrientedImageMarker>();
        _markers.CollectionChanged += (_, _) => UpdateMarkerGraphics();
        _images = new List<OrientedImage>();
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
    #endregion

    #region ImageManagement
    private List<OrientedImage> _images;

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

            SetProperty(ref _selectedImage, value);

            if (_selectedImage != null)
            {
                var footprint = _footprints.FirstOrDefault((fpt) => fpt.OrientedImage == _selectedImage);
                SelectedImageFootprint = footprint ?? new OrientedImageFootprint(_selectedImage);
            }
            else
            {
                SelectedImageFootprint = null;
            }
            UpdateVisibleFootprints();
        }
    }

    private OrientedImageFootprint? _selectedImageFootprint;

    /// <summary>
    /// Gets the footprint of the currently selected image. This is updated based on the <see cref="SelectedImage"/> property.
    /// </summary>
    public OrientedImageFootprint? SelectedImageFootprint
    {
        get => _selectedImageFootprint;
        private set => SetProperty(ref _selectedImageFootprint, value);
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
        set => SetProperty(ref _autoUpdateFootprint, value);
    }

    // These colors should be bindable
    public System.Drawing.Color SelectedFootprintFillColor = System.Drawing.Color.Orange;
    public System.Drawing.Color SelectedFootprintOutlineColor = System.Drawing.Color.Black;
    public System.Drawing.Color UnselectedFootprintFillColor = System.Drawing.Color.Blue;
    public System.Drawing.Color UnselectedFootprintOutlineColor = System.Drawing.Color.Orange;

    private void UpdateVisibleFootprints()
    {
        if (OrientedImageryLayer == null) return;

        OrientedImageryLayer.VisibleFootprints.Clear();

        foreach (var ftp in _footprints)
        {
            if (ftp != SelectedImageFootprint && ShowUnselectedFootprints)
            {
                ftp.FillColor = UnselectedFootprintFillColor;
                ftp.OutlineColor = UnselectedFootprintOutlineColor;
                OrientedImageryLayer.VisibleFootprints.Add(ftp);
            }
        }

        if (ShowSelectedFootprint && SelectedImageFootprint != null)
        {
            OrientedImageryLayer.VisibleFootprints.Add(SelectedImageFootprint);
        }
    }
    #endregion FootprintManagement

    #region Markers
    // This viewmodel manages the markers collection and graphics overlay, the view is responsible for connecting them to displays
    private ObservableCollection<OrientedImageMarker> _markers;
    public ObservableCollection<OrientedImageMarker> Markers => _markers;

    private GraphicsOverlay _markersOverlay;
    public GraphicsOverlay MarkersOverlay => _markersOverlay;

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
    public void AddMarkerLocation(MapPoint location, MarkerSymbol? symbol = null)
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

    #region INotifyPropertyChanged
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #endregion INotifyPropertyChanged
}
#endif
