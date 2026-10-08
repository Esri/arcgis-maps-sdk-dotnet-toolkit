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
        _appMarkers = new ObservableCollection<OrientedImageMarker>();
        _managedMarkers = new List<OrientedImageMarker>();
        _displayMarkers = new ResettableObservableCollection<OrientedImageMarker>();
        _appMarkers.CollectionChanged += Markers_CollectionChanged;

        _markersOverlay = new GraphicsOverlay() { Id = "OrientedImageryView_Markers_Overlay" };
        NewMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 15);
        SearchPointMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Red, 12);
        AllCamerasMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.FromArgb(200,0,0,255), 15);
        SelectedCameraMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Yellow, 15);

        _images = new List<OrientedImage>();
        _readOnlyImages = _images.AsReadOnly();
        _markersBeforeSequentialNavigation = new List<OrientedImageMarker>();

        AutoUpdateFootprint = true;
        SelectedFootprintFillColor = System.Drawing.Color.FromArgb(32, System.Drawing.Color.Red);
        SelectedFootprintOutlineColor = System.Drawing.Color.Red;
        UnselectedFootprintFillColor = System.Drawing.Color.FromArgb(16, System.Drawing.Color.Blue);
        UnselectedFootprintOutlineColor = System.Drawing.Color.Blue;

        ToolbarItems = GetDefaultToolbarItems();
        ToolbarItems.CollectionChanged += ToolbarItems_CollectionChanged;
        SynchronizeToolbarItems();

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
            _imageBeforeSequentialNavigation = null;
            _images.Clear();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Images)));
            _footprints.Clear();
            SelectedImage = null;
            _managedMarkers.Clear();
            Markers.Clear();

            _oiLayer = value;
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
    private List<OrientedImageMarker> _markersBeforeSequentialNavigation;
    private OrientedImage? _imageBeforeSequentialNavigation;

    /// <summary>
    /// Gets or sets the currently selected oriented image.
    /// </summary>
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
            UpdateSelectedCameraMarker();

            ResetSequentialNavigationState();
            ChangeNavigationCommandsCanExecute();
            if (IsSequentialNavigationEnabled)
                _ = PrefetchAdjacentImagesAsync();
        }
    }

    /// <summary>
    /// Gets a read-only view of the list of selected and unselected images currently assigned to the control.
    /// </summary>
    public IReadOnlyList<OrientedImage> Images
    {
        get => _readOnlyImages;
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
        private set
        {
            if (_isSequentialNavigationEnabled == value) return;
            SetProperty(ref _isSequentialNavigationEnabled, value);
            ResetSequentialNavigationState();
            ChangeNavigationCommandsCanExecute();

            if (!IsSequentialNavigationEnabled)
            {
                SelectedImage = _imageBeforeSequentialNavigation;
                _imageBeforeSequentialNavigation = null;
                _managedMarkers.AddRange(_markersBeforeSequentialNavigation);
                _markersBeforeSequentialNavigation.Clear();
                SynchronizeMarkers();
                ChangeNavigationCommandsCanExecute();
            }
            else if (SelectedImage != null)
            {
                _ = PrefetchAdjacentImagesAsync();

                _imageBeforeSequentialNavigation = SelectedImage;
                for (int i = 0; i < _managedMarkers.Count; i++)
                {
                    if (_managedMarkers[i].Tag is MarkerTag tag && (tag.Identifier == AllCamerasMarkerTag.Identifier || tag.Identifier == SearchPointMarkerTag.Identifier))
                    {
                        _markersBeforeSequentialNavigation.Add(_managedMarkers[i]);
                        _managedMarkers.RemoveAt(i);
                        i--;
                    }
                }
                SynchronizeMarkers();
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
    /// Assigning results exits sequential navigation and clears the selected image and any previous navigation state.
    /// </remarks>
    /// <param name="images">The oriented images to display.</param>
    /// <param name="searchPoint">The point from which the images were searched.</param>
    public void SetImages(IEnumerable<OrientedImage> images, MapPoint? searchPoint = null)
    {
        var newImages = images.ToList();
        _deferMarkerSynchronization = true;
        try
        {
            IsSequentialNavigationEnabled = false;
            ResetSequentialNavigationState();
            _imageBeforeSequentialNavigation = null;
            SelectedImage = null;
            _images = newImages;
            _readOnlyImages = _images.AsReadOnly();
            _footprints = _images.Select((img) => new OrientedImageFootprint(img)).ToList();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Images)));

            UpdateSearchPointMarker(searchPoint);
            UpdateCameraMarkers();
        }
        finally
        {
            _deferMarkerSynchronization = false;
            SynchronizeMarkers();
        }

        UpdateVisibleFootprints();

        ChangeNavigationCommandsCanExecute();
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
            SelectedImage = adjacentImage;
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

    private bool _allowAddingMarkers;

    /// <summary>
    /// Gets or sets a value indicating whether marker-creation mode is enabled.
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
    // Application markers remain public; toolkit markers are merged internally for rendering.
    private readonly ObservableCollection<OrientedImageMarker> _appMarkers;
    private readonly List<OrientedImageMarker> _managedMarkers;
    private readonly ResettableObservableCollection<OrientedImageMarker> _displayMarkers;
    private readonly GraphicsOverlay _markersOverlay;
    private readonly HashSet<OrientedImageMarker> _overlayMarkerSubscriptions = [];
    private bool _deferMarkerSynchronization;
    private bool _showCameraLocations = true;
    private static readonly MarkerTag SearchPointMarkerTag = new MarkerTag("SearchPointMarker");
    private static readonly MarkerTag SelectedImageMarkerTag = new MarkerTag("SelectedImageMarker", int.MaxValue);
    private static readonly MarkerTag AllCamerasMarkerTag = new MarkerTag("AllSelectedCamerasMarker", -1);

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
    /// <see cref="AllowAddingMarkers"/> does not restrict this method.
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
                IsVisible = showHiddenMarkers || marker.IsVisible,
                ZIndex = marker.Tag is MarkerTag markerTag ? markerTag.ZIndex : 0
            });
        }
    }

    private void UpdateSearchPointMarker(MapPoint? location)
    {
        var existingMarker = _managedMarkers.FirstOrDefault((marker) => marker.Tag is MarkerTag tag && tag.Identifier == SearchPointMarkerTag.Identifier);

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
        _managedMarkers.RemoveAll(marker => marker.Tag is MarkerTag tag && tag.Identifier == AllCamerasMarkerTag.Identifier);

        if (ShowCameraLocations)
        {
            foreach (var image in _images)
            {
                _managedMarkers.Add(new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation((MapPoint)image.Geometry!), AllCamerasMarkerSymbol)
                {
                    Tag = AllCamerasMarkerTag,
                    IsVisible = false
                });
            }
        }
        SynchronizeMarkers();
    }

    private void UpdateSelectedCameraMarker()
    {
        var currentMarker = _managedMarkers.FirstOrDefault(mk => mk.Tag is MarkerTag tag && tag.Identifier == SelectedImageMarkerTag.Identifier);

        if (SelectedImage != null)
        {
            var newMarker = new OrientedImageMarker(OrientedImageMarkerPosition.FromLocation((MapPoint)SelectedImage.Geometry!), SelectedCameraMarkerSymbol)
            {
                Tag = SelectedImageMarkerTag,
                IsVisible = false
            };
            if (currentMarker != null)
                _managedMarkers[_managedMarkers.IndexOf(currentMarker)] = newMarker;
            else
                _managedMarkers.Add(newMarker);
        }
        else if (currentMarker != null)
        {
            _managedMarkers.Remove(currentMarker);
        }
        SynchronizeMarkers();
    }

    private struct MarkerTag(string identifier, int zIndex = 0)
    {
        public string Identifier = identifier;
        public int ZIndex = zIndex;
    }

    private sealed class ResettableObservableCollection<T> : ObservableCollection<T>
    {
        public void ReplaceAll(IEnumerable<T> items)
        {
            Items.Clear();
            foreach (var item in items)
                Items.Add(item);

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
#endregion Markers

#region ToolbarItems
    private HashSet<OrientedImageryToolbarItemBase> _attachedToolbarItems = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// The collection of toolbar items to be displayed on the containing <see cref="OrientedImageryView"/>.
    /// </summary>
    /// <remarks>
    /// Each toolbar items is an <see cref="OrientedImageryToolbarItemBase"/> object with a reference to the containing <see cref="OrientedImageryViewModel"/>.
    /// The list is initially populated with a set of default toolbar items for the base control.
    /// Items are disconnected from this view model when their last occurrence is removed, including when the collection is cleared.
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

        return
        [
            new ShowSelectedFootprintVM(),
            new ShowUnselectedFootprintsVM(),
            new ShowCameraMarkersVM(),
            new AllowAddingMarkersVM(),
            markerSymbolPickerVM,
            new ClearMarkersVM(),
            new SequentialNavigationVM()
        ];
    }

    private void ToolbarItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => SynchronizeToolbarItems();

    private void SynchronizeToolbarItems()
    {
        var currentItems = new HashSet<OrientedImageryToolbarItemBase>(ToolbarItems, ReferenceEqualityComparer.Instance);
        foreach (var item in _attachedToolbarItems)
        {
            if (!currentItems.Contains(item))
                item.ViewModel = null;
        }

        foreach (var item in ToolbarItems)
        {
            item.ViewModel = this;
        }
        _attachedToolbarItems = currentItems;
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
