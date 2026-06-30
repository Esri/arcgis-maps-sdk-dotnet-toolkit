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
using System.Windows.Input;

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
        NewMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 15);
        SearchPointMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Red, 20);
        AllCamerasMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.FromArgb(200,0,0,255), 15);
        CurrentCameraMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Yellow, 15);

        SelectedFootprintFillColor = System.Drawing.Color.FromArgb(128, System.Drawing.Color.Orange);
        SelectedFootprintOutlineColor = System.Drawing.Color.Orange;
        UnselectedFootprintFillColor = System.Drawing.Color.FromArgb(128, System.Drawing.Color.Blue);
        UnselectedFootprintOutlineColor = System.Drawing.Color.Blue;

        _markers = new ObservableCollection<OrientedImageMarker>();
        _markers.CollectionChanged += (_, _) => UpdateMarkerGraphics();
        _images = new List<OrientedImage>();

        SelectNextImageCommand = new Command(
            execute: () => SelectNextImage(),
            canExecute: () => _images.Count > 0 && (SelectedImage == null || _images.IndexOf(SelectedImage) < _images.Count - 1));
        SelectPreviousImageCommand = new Command(
            execute: () => SelectPreviousImage(),
            canExecute: () => _images.Count > 0 && (SelectedImage != null && _images.IndexOf(SelectedImage) > 0));
        ClearMarkersCommand = new Command(
            execute: () => ClearMarkers(),
            canExecute: () => true);
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
    public void SetImages(List<OrientedImage> images, MapPoint? searchPoint = null)
    {

        _images = images.ToList();
        _footprints = _images.Select((img) => new OrientedImageFootprint(img)).ToList();

        UpdateSearchPointMarker(searchPoint);
        UpdateCameraMarkers();
        UpdateVisibleFootprints();

        ((Command)SelectNextImageCommand).ChangeCanExecute();
        ((Command)SelectPreviousImageCommand).ChangeCanExecute();
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
            UpdateCameraMarkers();
            UpdateVisibleFootprints();
            ((Command)SelectNextImageCommand).ChangeCanExecute();
            ((Command)SelectPreviousImageCommand).ChangeCanExecute();
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

    /// <summary>
    /// Selects the next image in the list of images.
    /// </summary>
    /// <remarks>
    /// If the currently selected image is either <c>null</c> or not in the list, this command will select the first image in the list.
    /// </remarks>
    public ICommand SelectNextImageCommand { get; private set; }

    /// <summary>
    /// Selects the previous image in the list of images.
    /// </summary>
    /// <remarks>
    /// This command will not select an image if the currently selected image is either <c>null</c> or not in the list.
    /// </remarks>
    public ICommand SelectPreviousImageCommand { get; private set; }

    private void SelectNextImage()
    {
        if (_images.Count == 0)
            return;

        var currentIndex = SelectedImage != null ? _images.IndexOf(SelectedImage) : -1;
        if (currentIndex < _images.Count - 1)
        {
            SelectedImage = _images[currentIndex + 1];
        }
    }

    private void SelectPreviousImage()
    {
        if (_images.Count == 0)
            return;

        var currentIndex = SelectedImage != null ? _images.IndexOf(SelectedImage) : _images.Count;
        if (currentIndex > 0)
        {
            SelectedImage = _images[currentIndex - 1];
        }
    }

    #endregion ImageManagement

    #region FootprintManagement
    private List<OrientedImageFootprint> _footprints = new();

    private bool _showSelectedFootprint = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show the footprint for the selected oriented image.
    /// </summary>
    public bool ShowSelectedFootprint
    {
        get => _showSelectedFootprint;
        set
        {
            if (_showSelectedFootprint == value) { return; }

            SetProperty(ref _showSelectedFootprint, value);
            UpdateVisibleFootprints();
        }
    }

    private bool _showUnselectedFootprints;

    /// <summary>
    /// Gets or sets a value indicating whether to show footprints for non-selected oriented images.
    /// </summary>
    public bool ShowUnselectedFootprints
    {
        get => _showUnselectedFootprints;
        set
        {
            if (_showUnselectedFootprints == value) { return; }

            SetProperty(ref _showUnselectedFootprints, value);
            UpdateVisibleFootprints();
        }
    }

    private bool _autoUpdateFootprint;
    public bool AutoUpdateFootprint
    {
        get => _autoUpdateFootprint;
        set => SetProperty(ref _autoUpdateFootprint, value);
    }

    private System.Drawing.Color _selectedFootprintFillColor;

    /// <summary>
    /// Gets or sets the fill color used for the selected image footprint.
    /// </summary>
    public System.Drawing.Color SelectedFootprintFillColor
    {
        get => _selectedFootprintFillColor;
        set
        {
            if (_selectedFootprintFillColor == value) { return; }

            SetProperty(ref _selectedFootprintFillColor, value);
            UpdateVisibleFootprints();
        }
    }

    private System.Drawing.Color _selectedFootprintOutlineColor;

    /// <summary>
    /// Gets or sets the outline color used for the selected image footprint.
    /// </summary>
    public System.Drawing.Color SelectedFootprintOutlineColor
    {
        get => _selectedFootprintOutlineColor;
        set
        {
            if (_selectedFootprintOutlineColor == value) { return; }

            SetProperty(ref _selectedFootprintOutlineColor, value);
            UpdateVisibleFootprints();
        }
    }

    private System.Drawing.Color _unselectedFootprintFillColor;

    /// <summary>
    /// Gets or sets the fill color used for unselected image footprints.
    /// </summary>
    public System.Drawing.Color UnselectedFootprintFillColor
    {
        get => _unselectedFootprintFillColor;
        set
        {
            if (_unselectedFootprintFillColor == value) { return; }

            SetProperty(ref _unselectedFootprintFillColor, value);
            UpdateVisibleFootprints();
        }
    }

    private System.Drawing.Color _unselectedFootprintOutlineColor;

    /// <summary>
    /// Gets or sets the outline color used for unselected image footprints.
    /// </summary>
    public System.Drawing.Color UnselectedFootprintOutlineColor
    {
        get => _unselectedFootprintOutlineColor;
        set
        {
            if (_unselectedFootprintOutlineColor == value) { return; }

            SetProperty(ref _unselectedFootprintOutlineColor, value);
            UpdateVisibleFootprints();
        }
    }

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
            SelectedImageFootprint.FillColor = SelectedFootprintFillColor;
            SelectedImageFootprint.OutlineColor = SelectedFootprintOutlineColor;
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

    /// <summary>
    /// Gets or sets the symbol to use for the current camera marker.
    /// </summary>
    public MarkerSymbol CurrentCameraMarkerSymbol { get; set; }
    private const string CurrentCameraMarkerTag = "CurrentCamerasMarker";

    /// <summary>
    /// Gets or sets the symbol to use for all camera markers.
    /// </summary>
    public MarkerSymbol AllCamerasMarkerSymbol { get; set; }
    private const string SelectedCamerasMarkerTag = "SelectedCamerasMarker";

    /// <summary>
    /// Gets or sets a value indicating whether to show markers for the locations of the cameras associated with the images in the control.
    /// </summary>
    public bool ShowSelectedCameraLocations
    {
        get => _showSelectedCameraLocations;
        set
        {
            if (_showSelectedCameraLocations == value) { return; }

            SetProperty(ref _showSelectedCameraLocations, value);
            UpdateCameraMarkers();
        }
    }
    private bool _showSelectedCameraLocations = true;


    /// <summary>
    /// Gets or sets a value indicating whether to show camera locations on the display. This property only has an effect if <see cref="ShowSelectedCameraLocations"/> is set to <c>true</c>.
    /// </summary>
    public bool ShowSelectedCameraLocationsOnDisplay
    {
        get => _showSelectedCameraLocationsOnDisplay;
        set
        {
            if (_showSelectedCameraLocationsOnDisplay == value) { return; }

            SetProperty(ref _showSelectedCameraLocationsOnDisplay, value);
            UpdateCameraMarkers();
        }
    }
    private bool _showSelectedCameraLocationsOnDisplay = false;

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
    /// Clears all extant markers save the search point marker.
    /// </summary>
    public ICommand ClearMarkersCommand { get; private set; }

    private void ClearMarkers()
    {
        var searchPointMarker = _markers.FirstOrDefault((marker) => marker.Tag is string tag && tag == SearchPointMarkerTag);
        var currentCameraMarker = _markers.FirstOrDefault((marker) => marker.Tag is string tag && tag == CurrentCameraMarkerTag);
        _markers.Clear();
        UpdateCameraMarkers();
        if (searchPointMarker != null)
            _markers.Add(searchPointMarker);
        if (currentCameraMarker != null)
            _markers.Add(currentCameraMarker);
    }

    private void UpdateCameraMarkers()
    {
        var existingCameraMarkers = _markers.Where((marker) => marker.Tag is string tag && tag == SelectedCamerasMarkerTag).ToList();
        foreach (var mrk in existingCameraMarkers)
        {
            _markers.Remove(mrk);
        }

        if (ShowSelectedCameraLocations)
        {
            foreach (var img in _images.Where((img) => img.Geometry is MapPoint))
            {
                _markers.Add(new OrientedImageMarker( OrientedImageMarkerPosition.FromLocation((MapPoint)img.Geometry!), AllCamerasMarkerSymbol)
                {
                    Tag = SelectedCamerasMarkerTag,
                    IsVisible = ShowSelectedCameraLocationsOnDisplay
                });
            }
        }

        UpdateSelectedImageMarker();
    }

    private void UpdateSelectedImageMarker()
    {
        var existingMarker = _markers.FirstOrDefault((marker) => marker?.Tag is string tag && tag == CurrentCameraMarkerTag, null);

        if (existingMarker != null)
            _markers.Remove(existingMarker);

        if (SelectedImage?.Geometry is MapPoint point)
        {
            _markers.Add(new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(point), CurrentCameraMarkerSymbol) { Tag = CurrentCameraMarkerTag });
        }
    }

    private void UpdateMarkerGraphics()
    {
        _markersOverlay.Graphics.Clear();
        _markersOverlay.Graphics.AddRange(_markers.Select((mk) => new Graphic(mk.Position.Location!, mk.Symbol)));

        if (SelectedImage?.Geometry is MapPoint currentImageLocation)
            _markersOverlay.Graphics.Add(new Graphic(currentImageLocation, CurrentCameraMarkerSymbol));
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
