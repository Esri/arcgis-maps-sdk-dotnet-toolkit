using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

#if WPF
/// <summary>
/// Manages oriented imagery selection, footprints, and map markers for an oriented imagery view.
/// </summary>
public class OrientedImageryViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryViewModel"/> class.
    /// </summary>
    public OrientedImageryViewModel() : base()
    {
        _allowAddingMarkers = false;
        _markers = new ObservableCollection<OrientedImageMarker>();
        _markers.CollectionChanged += Markers_CollectionChanged;

        _markersOverlay = new GraphicsOverlay() { Id = "OrientedImageryView_Markers_Overlay" };
        NewMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 15);
        SearchPointMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Red, 12);
        AllCamerasMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.FromArgb(200,0,0,255), 15);
        SelectedCameraMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Yellow, 15);

        _images = new List<OrientedImage>();

        AutoUpdateFootprint = true;
        SelectedFootprintFillColor = System.Drawing.Color.FromArgb(32, System.Drawing.Color.Red);
        SelectedFootprintOutlineColor = System.Drawing.Color.Red;
        UnselectedFootprintFillColor = System.Drawing.Color.FromArgb(16, System.Drawing.Color.Blue);
        UnselectedFootprintOutlineColor = System.Drawing.Color.Blue;

        ToolbarItems = GetDefaultToolbarItems();
        ToolbarItems.CollectionChanged += ToolbarItems_CollectionChanged;

        SelectNextImageCommand = new Command(
            execute: async () => await SelectNextImageAsync(),
            canExecute: () => IsSequentialNavigation ? SupportsSequentialNavigation && !_isFetchingAdjacentImage && !_noNextImage && SelectedImage != null : _images.Count > 0 && (SelectedImage == null || _images.IndexOf(SelectedImage) < _images.Count - 1));
        SelectPreviousImageCommand = new Command(
            execute: async () => await SelectPreviousImageAsync(),
            canExecute: () => IsSequentialNavigation ? SupportsSequentialNavigation && !_isFetchingAdjacentImage && !_noPreviousImage && SelectedImage != null : _images.Count > 0 && (SelectedImage != null && _images.IndexOf(SelectedImage) > 0));
        ToggleSequentialNavigationCommand = new Command(
            execute: async () => await ToggleSequentialNavigationAsync(),
            canExecute: () => IsSequentialNavigation || (SupportsSequentialNavigation && SelectedImage != null && _isSelectedImageReady));
        ClearMarkersCommand = new Command(
            execute: () => ClearMarkers(),
            canExecute: () => HasUserMarkers);
    }

#region GeoModel
    private OrientedImageryLayer? _oiLayer;
    private LayerSceneProperties? _oiLayerSceneProperties;

    /// <summary>
    /// Gets or sets the oriented imagery layer whose visible footprints are managed by this view model.
    /// </summary>
    public OrientedImageryLayer? OrientedImageryLayer
    {
        get => _oiLayer;
        set
        {
            if (_oiLayer == value) return;

            if (_oiLayer != null)
            {
                _oiLayer.VisibleFootprints.Clear();
                _oiLayer.PropertyChanged -= OrientedImageryLayer_PropertyChanged;
                _oiLayerSceneProperties!.PropertyChanged -= OrientedImageryLayer_SceneProperties_PropertyChanged;
            }

            IsSequentialNavigation = false;
            ResetSequentialNavigationState();
            _imageBeforeSequentialNavigation = null;
            _images.Clear();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Images)));
            _footprints.Clear();
            SelectedImage = null;
            Markers.Clear();

            _oiLayer = value;
            if (_oiLayer != null)
            {
                _oiLayer.PropertyChanged += OrientedImageryLayer_PropertyChanged;
                _oiLayerSceneProperties = _oiLayer.SceneProperties;
                _oiLayerSceneProperties.PropertyChanged += OrientedImageryLayer_SceneProperties_PropertyChanged;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SupportsSequentialNavigation)));
            ChangeNavigationCommandCanExecute();
            MatchSceneProperties();
            UpdateVisibleFootprints();
        }
    }

    private void MatchSceneProperties()
    {
        _markersOverlay.SceneProperties.SurfacePlacement = _oiLayer?.SceneProperties.SurfacePlacement ?? SurfacePlacement.Relative;
        _markersOverlay.SceneProperties.AltitudeOffset = _oiLayer?.SceneProperties.AltitudeOffset ?? 0d;
    }

    private void OrientedImageryLayer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrientedImageryLayer.SupportsSequentialNavigation))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SupportsSequentialNavigation)));
            ChangeNavigationCommandCanExecute();
            return;
        }

        if (e.PropertyName != nameof(OrientedImageryLayer.SceneProperties))
            return;

        if (_oiLayerSceneProperties != null)
            _oiLayerSceneProperties.PropertyChanged -= OrientedImageryLayer_SceneProperties_PropertyChanged;

        _oiLayerSceneProperties = _oiLayer?.SceneProperties;
        MatchSceneProperties();

        if (_oiLayer != null)
            _oiLayerSceneProperties!.PropertyChanged += OrientedImageryLayer_SceneProperties_PropertyChanged;
    }

    private void OrientedImageryLayer_SceneProperties_PropertyChanged(object? sender, PropertyChangedEventArgs e) => MatchSceneProperties();
#endregion GeoModel

#region Images
    private List<OrientedImage> _images;
    private OrientedImage? _selectedImage;
    private OrientedImageFootprint? _selectedImageFootprint;
    private bool _isSequentialNavigation;
    private bool _isFetchingAdjacentImage;
    private OrientedImage? _nextSequentialImage;
    private OrientedImage? _previousSequentialImage;
    private CancellationTokenSource? _adjacentImagesCancellation;
    private bool _noNextImage;
    private bool _noPreviousImage;
    private int _sequentialNavigationVersion;
    private string? _sequentialNavigationMessage;
    private OrientedImage? _imageBeforeSequentialNavigation;
    private bool _isSelectedImageReady;

    /// <summary>
    /// Gets a value indicating whether the current layer supports sequential navigation.
    /// </summary>
    public bool SupportsSequentialNavigation => OrientedImageryLayer?.SupportsSequentialNavigation == true;

    /// <summary>
    /// Gets a value indicating whether sequential navigation is active.
    /// </summary>
    public bool IsSequentialNavigation
    {
        get => _isSequentialNavigation;
        private set
        {
            if (_isSequentialNavigation == value) return;
            ResetSequentialNavigationState();
            SetProperty(ref _isSequentialNavigation, value);
        }
    }

    /// <summary>
    /// Gets the error message for sequential navigation.
    /// </summary>
    public string? SequentialNavigationMessage
    {
        get => _sequentialNavigationMessage;
        private set => SetProperty(ref _sequentialNavigationMessage, value);
    }

    /// <summary>
    /// Gets or sets the currently selected oriented image.
    /// </summary>
    public OrientedImage? SelectedImage
    {
        get => _selectedImage;
        set
        {
            if (value == _selectedImage) return;

            SetSelectedImageReady(false);
            ResetSequentialNavigationState();
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
            UpdateSelectedCameraMarker();

            ChangeNavigationCommandCanExecute();
            if (IsSequentialNavigation)
                _ = PrefetchAdjacentImagesAsync();
        }
    }

    /// <summary>
    /// Gets a read-only view of the list of selected and unselected images currently assigned to the control.
    /// </summary>
    public IReadOnlyList<OrientedImage> Images
    {
        get => _images.AsReadOnly();
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

    /// <summary>
    /// Enters sequential navigation or returns to the current search results.
    /// </summary>
    public ICommand ToggleSequentialNavigationCommand { get; private set; }

    /// <summary>
    /// Sets the images to display in the control.
    /// </summary>
    /// <remarks>
    /// Pass the <paramref name="searchPoint"/> parameter to display a marker at the location from which the images were searched.
    /// Assigning results exits sequential navigation and clears the selected image and any previous navigation state.
    /// </remarks>
    /// <param name="images">The oriented images to display.</param>
    /// <param name="searchPoint">The point from which the images were searched.</param>
    public void SetImages(IEnumerable<OrientedImage> images, MapPoint? searchPoint = null)
    {
        var newImages = images.ToList();
        IsSequentialNavigation = false;
        ResetSequentialNavigationState();
        _imageBeforeSequentialNavigation = null;
        SelectedImage = null;
        _images = newImages;
        _footprints = _images.Select((img) => new OrientedImageFootprint(img)).ToList();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Images)));

        UpdateSearchPointMarker(searchPoint);
        UpdateCameraMarkers();
        UpdateVisibleFootprints();

        ChangeNavigationCommandCanExecute();
    }

    private async Task SelectNextImageAsync()
    {
        if (IsSequentialNavigation)
        {
            await FetchAdjacentImageAsync(SequenceStep.Next);
            return;
        }

        if (_images.Count == 0)
            return;

        var currentIndex = SelectedImage != null ? _images.IndexOf(SelectedImage) : -1;
        if (currentIndex < _images.Count - 1)
        {
            SelectedImage = _images[currentIndex + 1];
        }
    }

    private async Task SelectPreviousImageAsync()
    {
        if (IsSequentialNavigation)
        {
            await FetchAdjacentImageAsync(SequenceStep.Previous);
            return;
        }

        if (_images.Count == 0)
            return;

        var currentIndex = SelectedImage != null ? _images.IndexOf(SelectedImage) : _images.Count;
        if (currentIndex > 0)
        {
            SelectedImage = _images[currentIndex - 1];
        }
    }

    private Task ToggleSequentialNavigationAsync()
    {
        if (IsSequentialNavigation)
        {
            IsSequentialNavigation = false;
            SelectedImage = _imageBeforeSequentialNavigation;
            _imageBeforeSequentialNavigation = null;
            ChangeNavigationCommandCanExecute();
            return Task.CompletedTask;
        }

        if (!SupportsSequentialNavigation || SelectedImage == null || !_isSelectedImageReady)
            return Task.CompletedTask;

        _imageBeforeSequentialNavigation = SelectedImage;
        IsSequentialNavigation = true;
        return PrefetchAdjacentImagesAsync();
    }

    private async Task FetchAdjacentImageAsync(SequenceStep step)
    {
        if (!IsSequentialNavigation || _isFetchingAdjacentImage || !SupportsSequentialNavigation || SelectedImage == null || OrientedImageryLayer == null ||
            (step == SequenceStep.Next ? _noNextImage : _noPreviousImage))
            return;

        var adjacentImage = step == SequenceStep.Next ? _nextSequentialImage : _previousSequentialImage;
        if (adjacentImage == null)
        {
            var navigationVersion = _sequentialNavigationVersion;
            await PrefetchAdjacentImagesAsync();
            if (navigationVersion != _sequentialNavigationVersion)
                return;

            adjacentImage = step == SequenceStep.Next ? _nextSequentialImage : _previousSequentialImage;
        }

        if (adjacentImage != null)
            SelectedImage = adjacentImage;
    }

    private async Task PrefetchAdjacentImagesAsync()
    {
        if (!IsSequentialNavigation || !SupportsSequentialNavigation || SelectedImage == null || OrientedImageryLayer == null || _isFetchingAdjacentImage)
            return;

        var cancellation = new CancellationTokenSource();
        _adjacentImagesCancellation = cancellation;
        var navigationVersion = _sequentialNavigationVersion;
        _isFetchingAdjacentImage = true;
        SequentialNavigationMessage = null;
        ChangeNavigationCommandCanExecute();
        try
        {
            await Task.WhenAll(
                PrefetchAdjacentImageAsync(OrientedImageryLayer, SelectedImage, SequenceStep.Next, navigationVersion, cancellation.Token),
                PrefetchAdjacentImageAsync(OrientedImageryLayer, SelectedImage, SequenceStep.Previous, navigationVersion, cancellation.Token));
        }
        finally
        {
            if (navigationVersion == _sequentialNavigationVersion)
            {
                _adjacentImagesCancellation = null;
                _isFetchingAdjacentImage = false;
                ChangeNavigationCommandCanExecute();
            }
            cancellation.Dispose();
        }
    }

    private async Task PrefetchAdjacentImageAsync(OrientedImageryLayer layer, OrientedImage image, SequenceStep step, int navigationVersion, CancellationToken cancellationToken)
    {
        try
        {
            var adjacentImage = await layer.FetchAdjacentImageAsync(image, step, cancellationToken);
            if (navigationVersion != _sequentialNavigationVersion || cancellationToken.IsCancellationRequested)
                return;

            if (step == SequenceStep.Next)
                _nextSequentialImage = adjacentImage;
            else
                _previousSequentialImage = adjacentImage;

            if (adjacentImage == null)
                SetSequentialNavigationBoundary(step);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ArcGISException exception) when (IsEndOfSequenceError(exception.Message))
        {
            if (navigationVersion == _sequentialNavigationVersion && !cancellationToken.IsCancellationRequested)
                SetSequentialNavigationBoundary(step);
        }
        catch (Exception exception)
        {
            if (navigationVersion == _sequentialNavigationVersion && !cancellationToken.IsCancellationRequested)
                SequentialNavigationMessage = string.Format(Properties.Resources.GetString("OrientedImageryViewNavigationFailed") ?? "{0}", exception.Message);
        }
    }

    internal static bool IsEndOfSequenceError(string message) =>
        message.IndexOf("No adjacent image query result is available", StringComparison.OrdinalIgnoreCase) >= 0;

    private void SetSequentialNavigationBoundary(SequenceStep step)
    {
        if (step == SequenceStep.Next)
            _noNextImage = true;
        else
            _noPreviousImage = true;

        ChangeNavigationCommandCanExecute();
    }

    private void ResetSequentialNavigationState()
    {
        _sequentialNavigationVersion++;
        _adjacentImagesCancellation?.Cancel();
        _adjacentImagesCancellation = null;
        _isFetchingAdjacentImage = false;
        _nextSequentialImage = null;
        _previousSequentialImage = null;
        _noNextImage = false;
        _noPreviousImage = false;
        SequentialNavigationMessage = null;
    }

    internal void SetSelectedImageReady(bool isReady)
    {
        if (_isSelectedImageReady == isReady) return;
        _isSelectedImageReady = isReady;
        ((Command)ToggleSequentialNavigationCommand).ChangeCanExecute();
    }

    private void ChangeNavigationCommandCanExecute()
    {
        ((Command)SelectNextImageCommand).ChangeCanExecute();
        ((Command)SelectPreviousImageCommand).ChangeCanExecute();
        ((Command)ToggleSequentialNavigationCommand).ChangeCanExecute();
    }
#endregion Images

#region Footprints
    private List<OrientedImageFootprint> _footprints = new();

    private bool _showSelectedFootprint = true;

    /// <summary>
    /// Gets the footprint of the currently selected image. This is updated based on the <see cref="SelectedImage"/> property.
    /// </summary>
    public OrientedImageFootprint? SelectedImageFootprint
    {
        get => _selectedImageFootprint;
        private set => SetProperty(ref _selectedImageFootprint, value);
    }

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

    /// <summary>
    /// Gets or sets a value indicating whether the selected image footprint is automatically updated.
    /// </summary>
    public bool AutoUpdateFootprint
    {
        get => _autoUpdateFootprint;
        set => SetProperty(ref _autoUpdateFootprint, value);
    }

    private bool _allowAddingMarkers;

    /// <summary>
    /// Gets or sets a value indicating whether new markers may be added.
    /// </summary>
    public bool AllowAddingMarkers
    {
        get => _allowAddingMarkers;
        set => SetProperty(ref _allowAddingMarkers, value);
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
#endregion Footprints

#region Markers
    // This viewmodel manages the markers collection and graphics overlay, the view is responsible for connecting them to displays
    private ObservableCollection<OrientedImageMarker> _markers;
    private GraphicsOverlay _markersOverlay;
    private bool _showCameraLocations = true;
    private bool _showCameraLocationsOnDisplay = false;
    private static readonly MarkerTag SearchPointMarkerTag = new MarkerTag("SearchPointMarker");
    private static readonly MarkerTag SelectedImageMarkerTag = new MarkerTag("SelectedImageMarker", int.MaxValue);
    private static readonly MarkerTag AllCamerasMarkerTag = new MarkerTag("AllSelectedCamerasMarker", -1);

    /// <summary>
    /// Gets the collection of markers managed by this view model.
    /// </summary>
    public ObservableCollection<OrientedImageMarker> Markers
    {
        get { return _markers; }
    }

    private bool HasUserMarkers => Markers.Any(marker => marker.Tag is not MarkerTag);

    /// <summary>
    /// Gets the graphics overlay that contains the marker graphics.
    /// </summary>
    public GraphicsOverlay MarkersOverlay => _markersOverlay;

    /// <summary>
    /// Gets or sets the default symbology to use when adding new markers.
    /// </summary>
    public MarkerSymbol NewMarkerSymbol { get; set; }

    /// <summary>
    /// Gets or sets the symbol to use for the search point marker.
    /// </summary>
    public MarkerSymbol SearchPointMarkerSymbol { get; set; }

    /// <summary>
    /// Gets or sets the symbol to use for the current camera marker.
    /// </summary>
    public MarkerSymbol SelectedCameraMarkerSymbol { get; set; }

    /// <summary>
    /// Gets or sets the symbol to use for all camera markers.
    /// </summary>
    public MarkerSymbol AllCamerasMarkerSymbol { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show markers for the locations of the cameras associated with the images in the control.
    /// </summary>
    public bool ShowCameraLocations
    {
        get => _showCameraLocations;
        set
        {
            if (_showCameraLocations == value) { return; }

            SetProperty(ref _showCameraLocations, value);
            UpdateCameraMarkers();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show camera locations on the display.
    /// </summary>
    /// <remarks>
    /// This property only has an effect if <see cref="ShowCameraLocations"/> is set to <c>true</c>.
    /// </remarks>
    public bool ShowCameraLocationsOnDisplay
    {
        get => _showCameraLocationsOnDisplay;
        set
        {
            if (_showCameraLocationsOnDisplay == value) { return; }

            SetProperty(ref _showCameraLocationsOnDisplay, value);
            UpdateCameraMarkers();
        }
    }

    /// <summary>
    /// Clears all extant markers save the search point marker.
    /// </summary>
    public ICommand ClearMarkersCommand { get; private set; }

    /// <summary>
    /// Adds a marker at a geographic location. The marker uses <see cref="NewMarkerSymbol"/> unless
    /// overridden using the <paramref name="symbol"/> parameter.
    /// </summary>
    /// <remarks>
    /// New markers will be discarded if <see cref="AllowAddingMarkers"/> is <c>false</c>.
    /// </remarks>
    /// <param name="location">The geographic location of the marker.</param>
    /// <param name="symbol">The optional symbol to use for the marker.</param>
    public void AddMarkerLocation(MapPoint location, MarkerSymbol? symbol = null)
    {
        if (AllowAddingMarkers)
            Markers.Add(new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(location), symbol ?? NewMarkerSymbol));
    }

    private void Markers_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        ((Command)ClearMarkersCommand).ChangeCanExecute();

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                var addMarker = (OrientedImageMarker)e!.NewItems![0]!;
                _markersOverlay.Graphics.Insert(e.NewStartingIndex, new Graphic(addMarker.Position.Location!, addMarker.Symbol)
                {
                    ZIndex = addMarker.Tag is MarkerTag addMarkerTag ? addMarkerTag.ZIndex : 0
                });
                break;
            case NotifyCollectionChangedAction.Replace:
                var replaceMarker = (OrientedImageMarker)e!.NewItems![0]!;
                _markersOverlay.Graphics[e.OldStartingIndex] = new Graphic(replaceMarker.Position.Location!, replaceMarker.Symbol)
                {
                    ZIndex = replaceMarker.Tag is MarkerTag replaceMarkerTag ? replaceMarkerTag.ZIndex : 0
                };
                break;
            case NotifyCollectionChangedAction.Remove:
                _markersOverlay.Graphics.RemoveAt(e.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Move:
                var moveMarker = (OrientedImageMarker)e!.NewItems![0]!;
                _markersOverlay.Graphics.Move(e.OldStartingIndex, e.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Reset:
                _markersOverlay.Graphics.Clear();
                break;
        }
    }

    private void ClearMarkers()
    {
        if (!HasUserMarkers)
            return;

        var searchPointMarker = Markers.FirstOrDefault((marker) => marker.Tag is MarkerTag tag && tag.Identifier == SearchPointMarkerTag.Identifier);
        Markers.Clear();
        UpdateCameraMarkers();
        UpdateSelectedCameraMarker();
        if (searchPointMarker != null)
            Markers.Add(searchPointMarker);

        ((Command)ClearMarkersCommand).ChangeCanExecute();
    }

    private void UpdateSearchPointMarker(MapPoint? location)
    {
        var existingMarker = Markers.FirstOrDefault((marker) => marker?.Tag is MarkerTag tag && tag.Identifier == SearchPointMarkerTag.Identifier, null);

        if (existingMarker != null)
            Markers.Remove(existingMarker);

        if (location != null)
        {
            Markers.Add(new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(location), SearchPointMarkerSymbol) { Tag = SearchPointMarkerTag });
        }
    }

    private void UpdateCameraMarkers()
    {
        foreach (var marker in Markers.Where(mk => mk.Tag is MarkerTag tag && tag.Identifier == AllCamerasMarkerTag.Identifier).ToArray())
        {
            Markers.Remove(marker);
        }

        if (ShowCameraLocations)
        {
            foreach (var image in _images)
            {
                Markers.Add(new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation((MapPoint)image.Geometry!), AllCamerasMarkerSymbol)
                {
                    Tag = AllCamerasMarkerTag,
                    IsVisible = ShowCameraLocationsOnDisplay
                });
            }
        }
    }

    private void UpdateSelectedCameraMarker()
    {
        var currentMarker = Markers.FirstOrDefault(mk => mk.Tag is MarkerTag tag && tag.Identifier == SelectedImageMarkerTag.Identifier, null!);

        if (SelectedImage != null)
        {
            var newMarker = new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation((MapPoint)SelectedImage.Geometry!), SelectedCameraMarkerSymbol)
            {
                Tag = SelectedImageMarkerTag,
                IsVisible = false
            };
            if (currentMarker != null)
                Markers[Markers.IndexOf(currentMarker)] = newMarker;
            else
                Markers.Add(newMarker);
        }
        else if (currentMarker != null)
        {
            Markers.Remove(currentMarker);
        }
    }

    private struct MarkerTag(string identifier, int zIndex = 0)
    {
        public string Identifier = identifier;
        public int ZIndex = zIndex;
    }
#endregion Markers

#region ToolbarItems
    /// <summary>
    /// The collection of toolbar items to be displayed on the containing <see cref="OrientedImageryView"/>.
    /// </summary>
    /// <remarks>
    /// Each toolbar items is an <see cref="OrientedImageryToolbarItemBase"/> object with a reference to the containing <see cref="OrientedImageryViewModel"/>.
    /// The list is initially populated with a set of default toolbar items for the base control.
    /// </remarks>
    public ObservableCollection<OrientedImageryToolbarItemBase> ToolbarItems { get; private set; }

    /// <summary>
    /// Gets the default toolbar items for a <see cref="OrientedImageryViewModel"/>.
    /// </summary>
    private ObservableCollection<OrientedImageryToolbarItemBase> GetDefaultToolbarItems()
    {
        var markerSymbolPickerVM = new SelectNewMarkerSymbolVM(new Collection<MarkerSymbol>()
        {
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, System.Drawing.Color.Purple, 10),
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Yellow, 10),
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 10)
        });

        ObservableCollection<OrientedImageryToolbarItemBase> items =
        [
            new ShowSelectedFootprintVM(),
            new ShowUnselectedFootprintsVM(),
            new ShowCameraMarkersVM(),
            new AllowAddingMarkersVM(),
            markerSymbolPickerVM,
            new ClearMarkersVM(),
            new SequentialNavigationVM()
        ];

        foreach (var item in items)
            item.ViewModel = this;

        return items;
    }

    private void ToolbarItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var removedItem in e.OldItems?.OfType<OrientedImageryToolbarItemBase>() ?? [])
        {
            removedItem.ViewModel = null;
        }

        foreach (var newItem in ToolbarItems)
        {
            newItem.ViewModel = this;
        }
    }
#endregion ToolbarItems

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
