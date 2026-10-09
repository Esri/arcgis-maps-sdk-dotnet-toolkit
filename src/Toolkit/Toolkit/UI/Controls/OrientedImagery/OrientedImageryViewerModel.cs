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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Popup = Esri.ArcGISRuntime.Mapping.Popups.Popup;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

#if WPF
using Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;
<<<<<<< HEAD
#elif MAUI
using Esri.ArcGISRuntime.Toolkit.Maui.OrientedImagery;
#endif
#if WPF || MAUI
||||||| parent of 91d1aa915 (Initial winui implementation - non-extensible toolbar)

=======

#if WPF || WINDOWS_XAML
>>>>>>> 91d1aa915 (Initial winui implementation - non-extensible toolbar)
/// <summary>
/// Manages oriented imagery selection, footprints, and map markers for an oriented imagery view.
/// </summary>
public class OrientedImageryViewerModel : INotifyPropertyChanged
{
#if MAUI
    internal Action<Action>? DispatchAction { get; set; }
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryViewerModel"/> class.
    /// </summary>
    public OrientedImageryViewerModel() : base()
    {
        _appMarkers = new ObservableCollection<OrientedImageMarker>();
        _managedMarkers = new List<OrientedImageMarker>();
        _displayMarkers = new ResettableObservableCollection<OrientedImageMarker>();
        _appMarkers.CollectionChanged += Markers_CollectionChanged;

        var symbolOutline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.White, 1);
        _markersOverlay = new GraphicsOverlay() { Id = "OrientedImageryViewer_Markers_Overlay" };
        NewMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 12);
        SearchPointMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Red, 15) { Outline = symbolOutline };
        AllCamerasMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.FromArgb(200,0,0,255), 15) { Outline = symbolOutline };
        SelectedCameraMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 15) { Outline = symbolOutline };
        NoImageMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.LightGray, 12)
        {
            Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 1)
        };

        _images = new List<OrientedImage>();
        _readOnlyImages = _images.AsReadOnly();

        AutoUpdateFootprint = true;
        SelectedFootprintFillColor = System.Drawing.Color.FromArgb(128, System.Drawing.Color.Red);
        SelectedFootprintOutlineColor = System.Drawing.Color.Red;
        UnselectedFootprintFillColor = System.Drawing.Color.FromArgb(64, System.Drawing.Color.Blue);
        UnselectedFootprintOutlineColor = System.Drawing.Color.Blue;

        _interactionState = OrientedImageryViewerInteractionState.FindImages;

        ToolbarCategories = GetDefaultToolbarCategories();
        ToolbarCategories.CollectionChanged += ToolbarCategories_CollectionChanged;
        SynchronizeToolbarCategories();

        SelectNextImageCommand = new Command(() => Navigate(SequenceStep.Next));
        SelectPreviousImageCommand = new Command(() => Navigate(SequenceStep.Previous));
        ClearMarkersCommand = new Command(
            execute: () => Markers.Clear(),
            canExecute: () => Markers.Count > 0);
        OpenImageDetailsPopupCommand = new Command(
            execute: SetImageDetailsPopup,
            canExecute: () => SelectedImage != null );
        CloseImageDetailsPopupCommand = new Command(
            execute: () => ImageDetailsPopup = null,
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
            InteractionState = OrientedImageryViewerInteractionState.FindImages;
            SearchPoint = null;

            SetProperty(ref _oiLayer, value);
            if (_oiLayer != null)
            {
                _oiLayer.PropertyChanged += OrientedImageryLayer_PropertyChanged;
                _oiLayerSceneProperties = _oiLayer.SceneProperties;
                _oiLayerSceneProperties.PropertyChanged += OrientedImageryLayer_SceneProperties_PropertyChanged;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SupportsSequentialNavigation)));
            UpdateNavigationState();
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
            UpdateNavigationState();
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
    private Popup? _imageDetailsPopup;
    private bool _isSequentialNavigationEnabled;
    private bool _isFetchingAdjacentImage;
    private Task? _adjacentImagesFetchTask;
    private OrientedImage? _nextSequentialImage;
    private OrientedImage? _previousSequentialImage;
    private CancellationTokenSource? _adjacentImagesCancellation;
    private bool _hasNextImage;
    private bool _hasPreviousImage;
    private bool _canNavigateNext;
    private bool _canNavigatePrevious;
    private int _sequentialNavigationVersion;
    private Exception? _sequentialNavigationError;
    private MapPoint? _searchPoint;

    /// <summary>
    /// Raised when the start and/or end of a sequence has been reached when in sequential navigation mode.
    /// </summary>
    internal event EventHandler<SequenceBoundaryReachedArgs>? SequenceBoundaryReached;

    /// <summary>
    /// Gets or sets the currently selected oriented image.
    /// </summary>
    [NotNullIfNotNull(nameof(SelectedImageFootprint))]
    public OrientedImage? SelectedImage
    {
        get => _selectedImage;
        set
        {
            if (value == _selectedImage) return;

            // Reset the current selected footprint to it's default extent so we don't leave a trail of updated footprints
            if (SelectedImageFootprint != null && SelectedImage != null)
            {
                var i = _footprints.IndexOf(SelectedImageFootprint);
                if (i > -1)
                {
                    _footprints[i] = new OrientedImageFootprint(SelectedImage);
                }
            }

            if (value != null)
            {
                var footprint = _footprints.FirstOrDefault((fpt) => fpt.OrientedImage == value);
                _selectedImageFootprint = footprint ?? new OrientedImageFootprint(value);
            }
            else
            {
                _selectedImageFootprint = null;
            }

            // Only set property (and raise property changed event) after updating _selectedImageFootprint
            SetProperty(ref _selectedImage, value);

            UpdateVisibleFootprints();
            UpdateCameraMarkers();
            ImageDetailsPopup = null;

            ResetSequentialNavigationState();
            UpdateNavigationState();
            if (IsSequentialNavigationEnabled)
                _ = PrefetchAdjacentImagesAsync();

            ((Command)OpenImageDetailsPopupCommand).ChangeCanExecute();
        }
    }

    /// <summary>
    /// Gets a fields-only popup created from the selected <see cref="OrientedImage"/>, or <see langword="null"/> when the popup is closed.
    /// </summary>
    public Popup? ImageDetailsPopup
    {
        get => _imageDetailsPopup;
        private set => SetProperty(ref _imageDetailsPopup, value);
    }

    /// <summary>
    /// Gets the command that opens the popup for the currently selected image.
    /// </summary>
    public ICommand OpenImageDetailsPopupCommand { get; }

    /// <summary>
    /// Gets the command that closes the selected image popup.
    /// </summary>
    public ICommand CloseImageDetailsPopupCommand { get; }

    private void SetImageDetailsPopup()
    {
        if (SelectedImage is null)
        {
            ImageDetailsPopup = null;
            return;
        }

        var graphic = new Graphic(SelectedImage.Geometry);
        foreach (var attribute in SelectedImage.Attributes)
        {
            graphic.Attributes[attribute.Key] = attribute.Value;
        }

        ImageDetailsPopup = Popup.FromGeoElement(graphic);
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
            if (!(SupportsSequentialNavigation && SelectedImage != null))
            {
                SetProperty(ref _isSequentialNavigationEnabled, false);
                return;
            }
            if (_isSequentialNavigationEnabled == value) return;

            SetProperty(ref _isSequentialNavigationEnabled, value);
            ResetSequentialNavigationState();
            UpdateNavigationState();

            if (IsSequentialNavigationEnabled && SelectedImage != null)
            {
                _ = PrefetchAdjacentImagesAsync();
                SetImages([SelectedImage]);
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether navigation to the next image is available.
    /// </summary>
    public bool CanNavigateNext
    {
        get => _canNavigateNext;
        private set => SetProperty(ref _canNavigateNext, value);
    }

    /// <summary>
    /// Gets a value indicating whether navigation to the previous image is available.
    /// </summary>
    public bool CanNavigatePrevious
    {
        get => _canNavigatePrevious;
        private set => SetProperty(ref _canNavigatePrevious, value);
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
    /// This command is always enabled but does nothing when <see cref="CanNavigateNext"/> is <c>false</c>.
    /// </remarks>
    public ICommand SelectNextImageCommand { get; private set; }

    /// <summary>
    /// Selects the previous image in the list of images.
    /// </summary>
    /// <remarks>
    /// This command will not select an image if the currently selected image is either <c>null</c> or not in the list.
    /// This command is always enabled but does nothing when <see cref="CanNavigatePrevious"/> is <c>false</c>.
    /// </remarks>
    public ICommand SelectPreviousImageCommand { get; private set; }

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
            UpdateNavigationState();
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
        if (AtSequentialNavigationBoundary(step))
        {
            // Raise sequenceboundaryreached again if already at boundary so view can raise a new narrator notification
            SequenceBoundaryReached?.Invoke(this, new SequenceBoundaryReachedArgs(!_hasPreviousImage, !_hasNextImage));
            return;
        }

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
            _isFetchingAdjacentImage || AtSequentialNavigationBoundary(step))
            return;

        var adjacentImage = step == SequenceStep.Next ? _nextSequentialImage : _previousSequentialImage;
        if (adjacentImage != null)
        {
            SelectedImage = adjacentImage;
            SetImages([SelectedImage]);
        }
    }

    private bool AtSequentialNavigationBoundary(SequenceStep step)
    {
        return !_isFetchingAdjacentImage && IsSequentialNavigationEnabled && !(step == SequenceStep.Next ? _hasNextImage : _hasPreviousImage);
    }

    private async Task PrefetchAdjacentImagesAsync()
    {
        if (!IsSequentialNavigationEnabled || !SupportsSequentialNavigation || SelectedImage == null || OrientedImageryLayer == null || _isFetchingAdjacentImage)
            return;

        _isFetchingAdjacentImage = true;
        UpdateNavigationState();
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
                UpdateNavigationState();
            }
            cancellation.Dispose();
        }

        // Unavailable directions are only confirmed boundaries after both current requests finish.
        if (navigationVersion == _sequentialNavigationVersion && IsSequentialNavigationEnabled &&
            SequentialNavigationError == null && (!_hasPreviousImage || !_hasNextImage))
            SequenceBoundaryReached?.Invoke(this, new SequenceBoundaryReachedArgs(!_hasPreviousImage, !_hasNextImage));
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
        catch (ArcGISException exception) when (exception.ErrorCode == 20)
        {
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

    private void SetHasAdjacentImage(SequenceStep step, bool hasAdjacentImage)
    {
        if (step == SequenceStep.Next)
            _hasNextImage = hasAdjacentImage;
        else
            _hasPreviousImage = hasAdjacentImage;

        UpdateNavigationState();
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

    private void UpdateNavigationState()
    {
        CanNavigateNext = CanNavigate(SequenceStep.Next);
        CanNavigatePrevious = CanNavigate(SequenceStep.Previous);
    }
#endregion Images

#region Footprints
    private List<OrientedImageFootprint> _footprints = new();

    private bool _showSelectedFootprint = true;

    /// <summary>
    /// Gets the footprint of the current <see cref="SelectedImage"/> if it exists.
    /// </summary>
    /// <remarks>
    /// The <see cref="PropertyChanged"/> event is never raised for this property. Listen to <see cref="SelectedImage"/> instead.
    /// </remarks>
    [NotNullIfNotNull(nameof(SelectedImage))]
    public OrientedImageFootprint? SelectedImageFootprint
    {
        get => _selectedImageFootprint;
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
    /// Internal flag so the OrientedImageryViewer can temporarily disable showing the current footprint without
    /// touching <see cref="ShowSelectedFootprint"/>. While <c>true</c> does not render the selected footprint.
    /// </summary>
    internal bool IsShowSelectedFootprintEnabled
    {
        get => _isShowSelectedFootprintEnabled;
        set
        {
            if (_isShowSelectedFootprintEnabled == value) { return; }
            SetProperty(ref _isShowSelectedFootprintEnabled, value);
            UpdateVisibleFootprints();
        }
    }

    private bool _isShowSelectedFootprintEnabled = true;

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

    private void UpdateVisibleFootprints()
    {
        if (OrientedImageryLayer == null) return;

        OrientedImageryLayer.VisibleFootprints.Clear();

        foreach (var ftp in _footprints)
        {
            if (ShowUnselectedFootprints && ftp.OrientedImage != SelectedImage)
            {
                ftp.FillColor = UnselectedFootprintFillColor;
                ftp.OutlineColor = UnselectedFootprintOutlineColor;
                OrientedImageryLayer.VisibleFootprints.Add(ftp);
            }
        }

        if (IsShowSelectedFootprintEnabled && ShowSelectedFootprint && SelectedImageFootprint != null)
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
    public Symbol NewMarkerSymbol { get; set; }

    /// <summary>
    /// Gets or sets the symbol to use for the search point marker.
    /// </summary>
    public Symbol SearchPointMarkerSymbol { get; set; }

    /// <summary>
    /// Gets or sets the symbol to use when a location with no images is tapped while in <see cref="OrientedImageryViewerInteractionState.DisplayImage"/>.
    /// </summary>
    public Symbol NoImageMarkerSymbol { get; set; }

    /// <summary>
    /// Gets or sets the symbol to use for the current camera marker.
    /// </summary>
    public Symbol SelectedCameraMarkerSymbol { get; set; }

    /// <summary>
    /// Gets or sets the symbol to use for all camera markers.
    /// </summary>
    public Symbol AllCamerasMarkerSymbol { get; set; }

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
    public void AddMarkerLocation(MapPoint location, Symbol? symbol = null)
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
#if WPF
        System.Windows.Threading.Dispatcher? dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            _ = dispatcher.BeginInvoke(SynchronizeMarkers);
            return;
        }
<<<<<<< HEAD
#elif MAUI
        if (DispatchAction != null)
        {
            DispatchAction(SynchronizeMarkers);
            return;
        }
||||||| parent of 91d1aa915 (Initial winui implementation - non-extensible toolbar)
=======
>>>>>>> 91d1aa915 (Initial winui implementation - non-extensible toolbar)
#endif

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
            _managedMarkers.Add(new OrientedImageMarker(
                OrientedImageMarkerPosition.FromLocation(location),
                InteractionState != OrientedImageryViewerInteractionState.DisplayImage ? SearchPointMarkerSymbol : NoImageMarkerSymbol
                ) { Tag = SearchPointMarkerTag });
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
    private HashSet<ToolbarCategory> _attachedToolbarCategories = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// The collection of toolbar categories to be displayed on the containing <see cref="OrientedImageryViewer"/>.
    /// </summary>
    /// <remarks>
    /// Each toolbar item is an <see cref="ToolbarCategory"/> object with a reference to the containing
    /// <see cref="OrientedImageryViewerModel"/>. The list is initially populated with a set of default toolbar items for
    /// the base control. Items are disconnected from the view model when they are removed from the collection.
    /// </remarks>
    public ObservableCollection<ToolbarCategory> ToolbarCategories { get; private set; }

    /// <summary>
    /// Gets the default toolbar categories for a <see cref="OrientedImageryViewerModel"/>.
    /// </summary>
    private ObservableCollection<ToolbarCategory> GetDefaultToolbarCategories()
    {
        var symbolOutline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 1);
        var markerSymbolPickerVM = new SelectNewMarkerSymbolVM(new Collection<LabeledSymbol>()
        {
            new LabeledSymbol(
                new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, System.Drawing.Color.Violet, 12) { Outline =  symbolOutline },
                Properties.Resources.GetString("OrientedImageryViewerMarkerSquare") ?? "Square marker"
            ),
            new LabeledSymbol(
                new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Yellow, 12) { Outline =  symbolOutline },
                Properties.Resources.GetString("OrientedImageryViewerMarkerTriangle") ?? "Triangle marker"
            ),
            new LabeledSymbol(
                new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 12) { Outline =  symbolOutline },
                Properties.Resources.GetString("OrientedImageryViewerMarkerDiamond") ?? "Diamond marker"
            )
        });

        ObservableCollection<ToolbarCategory> categories =
        [
            new ToolbarCategory(ToolbarCategory.ModeCategoryName)
            {
                ToolbarItems = { new ImageSearchModeVM() }
            },
            new ToolbarCategory(ToolbarCategory.OverlaysCategoryName)
            {
                ToolbarItems =
                {
                    new ShowSelectedFootprintVM(),
                    new ShowUnselectedFootprintsVM(),
                    new ShowCameraMarkersVM()
                }
            },
            new ToolbarCategory(ToolbarCategory.MarkersCategoryName)
            {
                ToolbarItems =
                {
                    new AllowAddingMarkersVM(),
                    markerSymbolPickerVM,
                    new ClearMarkersVM()
                }
            },
            new ToolbarCategory(ToolbarCategory.MoreCategoryName)
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
        var currentItems = new HashSet<ToolbarCategory>(ToolbarCategories, ReferenceEqualityComparer.Instance);
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
    private OrientedImageryViewerInteractionState _interactionState;

    /// <summary>
    /// Gets or sets the current interaction state for the Viewport and connected <see cref="GeoView"/>.
    /// </summary>
    public OrientedImageryViewerInteractionState InteractionState
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
/// Represents the current interaction state of an <see cref="OrientedImageryViewer"/>.
/// </summary>
public enum OrientedImageryViewerInteractionState
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

/// <summary>
/// Args passed by <see cref="OrientedImageryViewerModel.SequenceBoundaryReached"/>.
/// </summary>
internal readonly struct SequenceBoundaryReachedArgs(bool hasReachedStart, bool hasReachedEnd)
{
    /// <summary>
    /// Whether or not the start of the sequence has been reached.
    /// </summary>
    public readonly bool HasReachedStart = hasReachedStart;

    /// <summary>
    /// Whether or not the end of the sequence has been reached.
    /// </summary>
    public readonly bool HasReachedEnd = hasReachedEnd;
}

#endif
