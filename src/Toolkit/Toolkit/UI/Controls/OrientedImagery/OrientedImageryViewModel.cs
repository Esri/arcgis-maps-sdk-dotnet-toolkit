using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
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
        _appMarkers = new ObservableCollection<OrientedImageMarker>();
        _managedMarkers = new List<OrientedImageMarker>();
        _displayMarkers = new ResettableObservableCollection<OrientedImageMarker>();
        _appMarkers.CollectionChanged += Markers_CollectionChanged;

        var symbolOutline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.White, 1);
        _markersOverlay = new GraphicsOverlay() { Id = "OrientedImageryView_Markers_Overlay" };
        NewMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 12);
        SearchPointMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Red, 15) { Outline = symbolOutline };
        AllCamerasMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.FromArgb(200,0,0,255), 15) { Outline = symbolOutline };
        SelectedCameraMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 15) { Outline = symbolOutline };

        _images = new List<OrientedImage>();
        _readOnlyImages = _images.AsReadOnly();

        AutoUpdateFootprint = true;
        SelectedFootprintFillColor = System.Drawing.Color.FromArgb(128, System.Drawing.Color.Red);
        SelectedFootprintOutlineColor = System.Drawing.Color.Red;
        UnselectedFootprintFillColor = System.Drawing.Color.FromArgb(64, System.Drawing.Color.Blue);
        UnselectedFootprintOutlineColor = System.Drawing.Color.Blue;

        _interactionState = OrientedImageryViewInteractionState.FindImages;

        ToolbarCategories = GetDefaultToolbarCategories();
        ToolbarCategories.CollectionChanged += ToolbarCategories_CollectionChanged;
        SynchronizeToolbarCategories();

        SelectNextImageCommand = new Command(
            execute: () => Navigate(SequenceStep.Next),
            canExecute: () => CanNavigate(SequenceStep.Next));
        SelectPreviousImageCommand = new Command(
            execute: () => Navigate(SequenceStep.Previous),
            canExecute: () => CanNavigate(SequenceStep.Previous));
        ToggleSequentialNavigationCommand = new Command(
            execute: () => ToggleSequentialNavigation(),
            canExecute: () => IsSequentialNavigationEnabled || (SupportsSequentialNavigation && SelectedImage != null));
        ClearMarkersCommand = new Command(
            execute: () => Markers.Clear(),
            canExecute: () => Markers.Count > 0);
        OpenSelectedImagePopupCommand = new Command(
            execute: SetSelectedImagePopup,
            canExecute: () => SelectedImage != null );
        CloseSelectedImagePopupCommand = new Command(
            execute: () => SelectedImagePopup = null,
            canExecute: () => true);
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

            IsSequentialNavigationEnabled = false;
            ResetSequentialNavigationState();
            _images.Clear();
            _footprints.Clear();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Images)));
            SelectedImage = null;
            _managedMarkers.Clear();
            Markers.Clear();

            SetProperty(ref _oiLayer, value);
            if (_oiLayer != null)
            {
                _oiLayer.PropertyChanged += OrientedImageryLayer_PropertyChanged;
                _oiLayerSceneProperties = _oiLayer.SceneProperties;
                _oiLayerSceneProperties.PropertyChanged += OrientedImageryLayer_SceneProperties_PropertyChanged;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SupportsSequentialNavigation)));
            ChangeNavigationCommandsCanExecute();
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
            ChangeNavigationCommandsCanExecute();
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
    private ReadOnlyCollection<OrientedImage> _readOnlyImages;
    private OrientedImage? _selectedImage;
    private OrientedImageFootprint? _selectedImageFootprint;
    private bool _isSelectedFootprintVisible;
    private Popup? _selectedImagePopup;
    private bool _isSequentialNavigationEnabled;
    private bool _isFetchingAdjacentImage;
    private Task? _adjacentImagesFetchTask;
    private OrientedImage? _nextSequentialImage;
    private OrientedImage? _previousSequentialImage;
    private CancellationTokenSource? _adjacentImagesCancellation;
    private bool _hasNextImage;
    private bool _hasPreviousImage;
    private int _sequentialNavigationVersion;
    private Exception? _sequentialNavigationError;
    private MapPoint? _searchPoint;

    /// <summary>
    /// Gets or sets the currently selected oriented image.
    /// </summary>
    public OrientedImage? SelectedImage
    {
        get => _selectedImage;
        set
        {
            if (value == _selectedImage) return;

            SelectedImagePopup = null;
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
            UpdateCameraMarkers();

            ResetSequentialNavigationState();
            ChangeNavigationCommandsCanExecute();
            if (IsSequentialNavigationEnabled)
                _ = PrefetchAdjacentImagesAsync();

            ((Command)OpenSelectedImagePopupCommand).ChangeCanExecute();
        }
    }

    /// <summary>
    /// Gets the popup for the currently selected image, or <see langword="null"/> when the popup is closed.
    /// </summary>
    public Popup? SelectedImagePopup
    {
        get => _selectedImagePopup;
        private set => SetProperty(ref _selectedImagePopup, value);
    }

    /// <summary>
    /// Gets the command that opens the popup for the currently selected image.
    /// </summary>
    public ICommand OpenSelectedImagePopupCommand { get; }

    /// <summary>
    /// Gets the command that closes the selected image popup.
    /// </summary>
    public ICommand CloseSelectedImagePopupCommand { get; }

    private void SetSelectedImagePopup()
    {
        if (SelectedImage is null)
        {
            SelectedImagePopup = null;
            return;
        }

        var graphic = new Graphic(SelectedImage.Geometry);
        foreach (var attribute in SelectedImage.Attributes)
        {
            graphic.Attributes[attribute.Key] = attribute.Value;
        }

        SelectedImagePopup = Popup.FromGeoElement(graphic);
    }

    /// <summary>
    /// Gets a read-only view of the list of selected and unselected images currently assigned to the control.
    /// </summary>
    public IReadOnlyList<OrientedImage> Images
    {
        get => _readOnlyImages;
    }

    /// <summary>
    /// Gets the search location used to retrieve <see cref="Images"/> as passed into <see cref="SetImages(IEnumerable{OrientedImage}, MapPoint?)"/>.
    /// </summary>
    public MapPoint? SearchPoint
    {
        get => _searchPoint;
        private set
        {
            SetProperty(ref _searchPoint, value);
            UpdateSearchPointMarker(_searchPoint);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current layer supports sequential navigation.
    /// </summary>
    public bool SupportsSequentialNavigation => OrientedImageryLayer?.SupportsSequentialNavigation == true;

    /// <summary>
    /// Gets a value indicating whether sequential navigation is active.
    /// </summary>
    public bool IsSequentialNavigationEnabled
    {
        get => _isSequentialNavigationEnabled;
        set
        {
            if (_isSequentialNavigationEnabled == value) return;
            SetProperty(ref _isSequentialNavigationEnabled, value);
            ResetSequentialNavigationState();
            ChangeNavigationCommandsCanExecute();

            if (!IsSequentialNavigationEnabled)
            {
                ChangeNavigationCommandsCanExecute();
            }
            else if (SelectedImage != null)
            {
                _ = PrefetchAdjacentImagesAsync();
                SetImages([]);
            }
        }
    }

    /// <summary>
    /// Gets the error, if any, produced while loading an adjacent image.
    /// </summary>
    public Exception? SequentialNavigationError
    {
        get => _sequentialNavigationError;
        private set => SetProperty(ref _sequentialNavigationError, value);
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
    /// </remarks>
    /// <param name="images">The oriented images to display.</param>
    /// <param name="searchPoint">The point from which the images were searched.</param>
    public void SetImages(IEnumerable<OrientedImage> images, MapPoint? searchPoint = null)
    {
        var newImages = images.ToList();
        _deferMarkerSynchronization = true;
        try
        {
            _images = newImages;
            _readOnlyImages = _images.AsReadOnly();
            _footprints = _images.Select((img) => new OrientedImageFootprint(img)).ToList();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Images)));

            SearchPoint = searchPoint;
        }
        finally
        {
            _deferMarkerSynchronization = false;
            UpdateCameraMarkers();
            SynchronizeMarkers();
            UpdateVisibleFootprints();
            ChangeNavigationCommandsCanExecute();
        }
    }

    private bool CanNavigate(SequenceStep step)
    {
        if (IsSequentialNavigationEnabled)
            return SupportsSequentialNavigation && SelectedImage != null &&
                (step == SequenceStep.Next ? _hasNextImage : _hasPreviousImage);

        if (_images.Count == 0)
            return false;

        int index = SelectedImage == null ? -1 : _images.IndexOf(SelectedImage);
        return step == SequenceStep.Next ? index < _images.Count - 1 : index > 0;
    }

    private void Navigate(SequenceStep step)
    {
        if (!CanNavigate(step))
            return;

        if (IsSequentialNavigationEnabled)
        {
            SelectAdjacentImageIfAvailable(step);
            return;
        }

        int index = SelectedImage == null ? -1 : _images.IndexOf(SelectedImage);
        SelectedImage = _images[index + (step == SequenceStep.Next ? 1 : -1)];
    }

    private void ToggleSequentialNavigation()
    {
        if (IsSequentialNavigationEnabled)
            IsSequentialNavigationEnabled = false;
        else if (SupportsSequentialNavigation && SelectedImage != null)
            IsSequentialNavigationEnabled = true;
    }

    private void SelectAdjacentImageIfAvailable(SequenceStep step)
    {
        if (!IsSequentialNavigationEnabled || !SupportsSequentialNavigation || SelectedImage == null || OrientedImageryLayer == null ||
            !(step == SequenceStep.Next ? _hasNextImage : _hasPreviousImage))
            return;

        var adjacentImage = step == SequenceStep.Next ? _nextSequentialImage : _previousSequentialImage;
        if (adjacentImage != null)
        {
            SelectedImage = adjacentImage;
            SetImages([SelectedImage]);
        }
    }

    private async Task PrefetchAdjacentImagesAsync()
    {
        if (!IsSequentialNavigationEnabled || !SupportsSequentialNavigation || SelectedImage == null || OrientedImageryLayer == null || _isFetchingAdjacentImage)
            return;

        _isFetchingAdjacentImage = true;
        var cancellation = new CancellationTokenSource();
        _adjacentImagesCancellation = cancellation;
        var navigationVersion = _sequentialNavigationVersion;
        SequentialNavigationError = null;
        try
        {
            _adjacentImagesFetchTask = Task.WhenAll(
                PrefetchAdjacentImageAsync(OrientedImageryLayer, SelectedImage, SequenceStep.Next, navigationVersion, cancellation.Token),
                PrefetchAdjacentImageAsync(OrientedImageryLayer, SelectedImage, SequenceStep.Previous, navigationVersion, cancellation.Token));
            await _adjacentImagesFetchTask;
        }
        finally
        {
            if (navigationVersion == _sequentialNavigationVersion)
            {
                _adjacentImagesFetchTask = null;
                _adjacentImagesCancellation = null;
                _isFetchingAdjacentImage = false;
            }
            cancellation.Dispose();
        }
    }

    private async Task PrefetchAdjacentImageAsync(OrientedImageryLayer layer, OrientedImage image, SequenceStep step, int navigationVersion, CancellationToken cancellationToken)
    {
        try
        {
            if (navigationVersion == _sequentialNavigationVersion)
                SetHasAdjacentImage(step, false);

            var adjacentImage = await layer.FetchAdjacentImageAsync(image, step, cancellationToken);
            if (navigationVersion != _sequentialNavigationVersion || cancellationToken.IsCancellationRequested)
                return;

            if (step == SequenceStep.Next)
                _nextSequentialImage = adjacentImage;
            else
                _previousSequentialImage = adjacentImage;

            SetHasAdjacentImage(step, adjacentImage != null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ArcGISException exception) when (IsEndOfSequenceError(exception.Message))
        {
            if (navigationVersion == _sequentialNavigationVersion && !cancellationToken.IsCancellationRequested)
                SetHasAdjacentImage(step, false);
        }
        catch (Exception exception)
        {
            if (navigationVersion == _sequentialNavigationVersion && !cancellationToken.IsCancellationRequested)
            {
                SequentialNavigationError = exception;
                SetHasAdjacentImage(step, false);
            }
        }
    }

    internal static bool IsEndOfSequenceError(string message) =>
        message.IndexOf("No adjacent image query result is available", StringComparison.OrdinalIgnoreCase) >= 0;

    private void SetHasAdjacentImage(SequenceStep step, bool hasAdjacentImage)
    {
        if (step == SequenceStep.Next)
            _hasNextImage = hasAdjacentImage;
        else
            _hasPreviousImage = hasAdjacentImage;

        ChangeNavigationCommandsCanExecute();
    }

    private void ResetSequentialNavigationState()
    {
        _sequentialNavigationVersion++;
        _adjacentImagesCancellation?.Cancel();
        _adjacentImagesCancellation = null;
        _adjacentImagesFetchTask = null;
        _isFetchingAdjacentImage = false;
        _nextSequentialImage = null;
        _previousSequentialImage = null;
        _hasNextImage = false;
        _hasPreviousImage = false;
        SequentialNavigationError = null;
    }

    private void ChangeNavigationCommandsCanExecute()
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

    /// <summary>
    /// Gets or sets a value indicating whether or not to display available footprints.
    /// </summary>
    public bool AreFootprintsEnabled
    {
        get => _isSelectedFootprintVisible;
        set
        {
            _isSelectedFootprintVisible = value;
            if (OrientedImageryLayer != null && SelectedImageFootprint != null)
                UpdateVisibleFootprints();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AreFootprintsEnabled)));
        }
    }

    private void UpdateVisibleFootprints()
    {
        if (OrientedImageryLayer == null) return;

        OrientedImageryLayer.VisibleFootprints.Clear();

        if (!AreFootprintsEnabled)
            return;

        foreach (var ftp in _footprints)
        {
            if (ShowUnselectedFootprints && ftp.OrientedImage != SelectedImage)
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
    // Application markers remain public; toolkit markers are merged internally for rendering.
    private readonly ObservableCollection<OrientedImageMarker> _appMarkers;
    private readonly List<OrientedImageMarker> _managedMarkers;
    private readonly ResettableObservableCollection<OrientedImageMarker> _displayMarkers;
    private readonly GraphicsOverlay _markersOverlay;
    private readonly HashSet<OrientedImageMarker> _overlayMarkerSubscriptions = [];
    private bool _deferMarkerSynchronization;
    private bool _showCameraLocations = true;
    private static readonly string SearchPointMarkerTag = "SearchPointMarker";
    private static readonly string CamerasMarkerTag = "CamerasMarker";

    /// <summary>
    /// Gets the application-owned markers displayed by this view model.
    /// </summary>
    /// <remarks>
    /// Applications may add, remove, and modify entries directly. Toolkit-managed search and camera markers are not
    /// included in this collection.
    /// </remarks>
    public ObservableCollection<OrientedImageMarker> Markers
    {
        get { return _appMarkers; }
    }

    internal ObservableCollection<OrientedImageMarker> DisplayMarkers => _displayMarkers;

    internal GraphicsOverlay MarkersOverlay => _markersOverlay;

    internal OrientedImageTappedEventArgs GetPublicImageTappedEventArgs(OrientedImageTappedEventArgs eventArgs) =>
        eventArgs.Marker is null || Markers.Contains(eventArgs.Marker)
            ? eventArgs
            : new OrientedImageTappedEventArgs(eventArgs.ImagePoint, eventArgs.Image);

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
    /// Gets or sets a value indicating whether camera location markers are shown on the GeoView.
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
    /// Gets the command that clears all application-owned markers.
    /// </summary>
    public ICommand ClearMarkersCommand { get; private set; }

    /// <summary>
    /// Adds a marker at a geographic location. The marker uses <see cref="NewMarkerSymbol"/> unless
    /// overridden using the <paramref name="symbol"/> parameter.
    /// </summary>
    /// <remarks>
    /// Applications may also add markers directly to <see cref="Markers"/>.
    /// </remarks>
    /// <param name="location">The geographic location of the marker.</param>
    /// <param name="symbol">The optional symbol to use for the marker.</param>
    public void AddMarkerLocation(MapPoint location, MarkerSymbol? symbol = null)
    {
        Markers.Add(new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(location), symbol ?? NewMarkerSymbol));
    }

    private void Markers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ((Command)ClearMarkersCommand).ChangeCanExecute();
        SynchronizeMarkers();
    }

    private void SynchronizeMarkers()
    {
        if (_deferMarkerSynchronization)
            return;

        foreach (var marker in _overlayMarkerSubscriptions)
            marker.PropertyChanged -= Marker_PropertyChanged;
        _overlayMarkerSubscriptions.Clear();

        var allMarkers = _managedMarkers.Concat(_appMarkers).ToArray();
        _displayMarkers.ReplaceAll(allMarkers);
        foreach (var marker in allMarkers)
        {
            marker.PropertyChanged += Marker_PropertyChanged;
            _overlayMarkerSubscriptions.Add(marker);
        }

        _markersOverlay.Graphics.Clear();
        AddOverlayMarkers(_managedMarkers, true);
        AddOverlayMarkers(_appMarkers);
    }

    private void Marker_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        System.Windows.Threading.Dispatcher? dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            _ = dispatcher.BeginInvoke(SynchronizeMarkers);
            return;
        }

        SynchronizeMarkers();
    }

    private void AddOverlayMarkers(IEnumerable<OrientedImageMarker> markers, bool showHiddenMarkers = false)
    {
        foreach (var marker in markers)
        {
            if (marker.Position.Location is not MapPoint location)
                continue;

            _markersOverlay.Graphics.Add(new Graphic(location, marker.Symbol)
            {
                IsVisible = showHiddenMarkers || marker.IsVisible
            });
        }
    }

    private void UpdateSearchPointMarker(MapPoint? location)
    {
        var existingMarker = _managedMarkers.FirstOrDefault((marker) => marker.Tag is string tag && tag == SearchPointMarkerTag);

        if (existingMarker != null)
            _managedMarkers.Remove(existingMarker);

        if (location != null)
        {
            _managedMarkers.Add(new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation(location), SearchPointMarkerSymbol) { Tag = SearchPointMarkerTag });
        }
        SynchronizeMarkers();
    }

    private void UpdateCameraMarkers()
    {
        _managedMarkers.RemoveAll(marker => marker.Tag is string tag && tag == CamerasMarkerTag);

        if (ShowCameraLocations)
        {
            foreach (var image in _images.Where(img => img != SelectedImage))
            {
                _managedMarkers.Add(new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation((MapPoint)image.Geometry!), AllCamerasMarkerSymbol)
                {
                    Tag = CamerasMarkerTag,
                    IsVisible = false
                });
            }
        }

        if (SelectedImage != null)
        {
            var newMarker = new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation((MapPoint)SelectedImage.Geometry!), SelectedCameraMarkerSymbol)
            {
                Tag = CamerasMarkerTag,
                IsVisible = false
            };
            _managedMarkers.Add(newMarker);
        }

        SynchronizeMarkers();
    }
#endregion Markers

#region ToolbarItems
    private HashSet<OrientedImageryToolbarCategory> _attachedToolbarCategories = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// The collection of toolbar categories to be displayed on the containing <see cref="OrientedImageryView"/>.
    /// </summary>
    /// <remarks>
    /// Each toolbar item is an <see cref="OrientedImageryToolbarCategory"/> object with a reference to the containing 
    /// <see cref="OrientedImageryViewModel"/>. The list is initially populated with a set of default toolbar items for
    /// the base control. Items are disconnected from the view model when they are removed from the collection.
    /// </remarks>
    public ObservableCollection<OrientedImageryToolbarCategory> ToolbarCategories { get; private set; }

    /// <summary>
    /// Gets the default toolbar categories for a <see cref="OrientedImageryViewModel"/>.
    /// </summary>
    private ObservableCollection<OrientedImageryToolbarCategory> GetDefaultToolbarCategories()
    {
        var symbolOutline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 1);
        var markerSymbolPickerVM = new SelectNewMarkerSymbolVM(new Collection<MarkerSymbol>()
        {
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, System.Drawing.Color.Violet, 12) { Outline =  symbolOutline },
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Yellow, 12) { Outline =  symbolOutline },
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 12) { Outline =  symbolOutline }
        });

        ObservableCollection<OrientedImageryToolbarCategory> categories =
        [
            new OrientedImageryToolbarCategory(OrientedImageryToolbarCategory.ModeCategoryName)
            {
                ToolbarItems = { new ImageSearchModeVM() }
            },
            new OrientedImageryToolbarCategory(OrientedImageryToolbarCategory.OverlaysCategoryName)
            {
                ToolbarItems =
                {
                    new ShowSelectedFootprintVM(),
                    new ShowUnselectedFootprintsVM(),
                    new ShowCameraMarkersVM()
                }
            },
            new OrientedImageryToolbarCategory(OrientedImageryToolbarCategory.MarkersCategoryName)
            {
                ToolbarItems =
                {
                    new AllowAddingMarkersVM(),
                    markerSymbolPickerVM,
                    new ClearMarkersVM()
                }
            },
            new OrientedImageryToolbarCategory(OrientedImageryToolbarCategory.MoreCategoryName)
            {
                ToolbarItems = { new SequentialNavigationVM() }
            }
        ];

#if WPF || WINDOWS_XAML
        for (var i = 1; i < categories.Count; i++)
        {
            categories[i].ToolbarItems.Insert(0, new ToolbarDividerVM());
        }
#endif

        return categories;
    }

    private void ToolbarCategories_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => SynchronizeToolbarCategories();

    private void SynchronizeToolbarCategories()
    {
        var currentItems = new HashSet<OrientedImageryToolbarCategory>(ToolbarCategories, ReferenceEqualityComparer.Instance);
        foreach (var item in _attachedToolbarCategories)
        {
            if (!currentItems.Contains(item))
                item.ViewModel = null;
        }

        foreach (var item in ToolbarCategories)
        {
            item.ViewModel = this;
        }
        _attachedToolbarCategories = currentItems;
    }
#endregion ToolbarItems

#region InteractionStateFlags
    // These are only used by the View, but because of the toolbar setup binding is easiest if they are kept in the ViewModel
    private OrientedImageryViewInteractionState _interactionState;

    /// <summary>
    /// Gets or sets the current interaction state for the Viewport and connected <see cref="GeoView"/>.
    /// </summary>
    public OrientedImageryViewInteractionState InteractionState
    {
        get => _interactionState;
        set => SetProperty(ref _interactionState, value);
    }
#endregion InteractionStateFlags

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

/// <summary>
/// Represents the current interaction state of an <see cref="OrientedImageryView"/>.
/// </summary>
public enum OrientedImageryViewInteractionState
{
    /// <summary>No behavior when the GeoView or viewport is tapped.</summary>
    None,

    /// <summary>Taps on the GeoView will search for all images with a view on the tapped location. No behavior for taps on the viewport.</summary>
    FindImages,

    /// <summary>
    /// Taps on the GeoView will attempt to retrieve a single OrientedImagery feature at the tapped location and display it on the viewport.
    /// No behavior for taps on the viewport.
    /// </summary>
    DisplayImage,

    /// <summary>Taps on both the GeoView and viewport will add reference markers.</summary>
    AddMarkers
}

#endif
