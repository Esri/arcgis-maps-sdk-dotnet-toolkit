using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

/// <summary>
/// Displays oriented imagery and provides controls for managing image selection, footprints, and markers.
/// </summary>
public partial class OrientedImageryView : TemplatedView
{
    #region State

    private readonly ObservableCollection<OrientedImageryLayer> _layers = new();
    private GeoView? _connectedGeoView;
    private GeoModel? _geoModel;
    private LayerCollection? _operationalLayers;
    private readonly HashSet<LayerCollection> _nestedLayerCollections = new();
    private readonly HashSet<GroupLayer> _groupLayers = new();
    private OrientedImageryViewModel? _connectedModel;
    private OrientedImageryLayer? _observedLayer;
    private OrientedImageryLayer? _lastZoomedLayer;
    private GeoView? _lastZoomedGeoView;
    private int _operationVersion;
    private int _layerVersion;
    private bool _connected;
    private bool _searching;
    private bool _hasSearchedForImages;
    private Exception? _operationError;
    private OrientedImageViewport? _viewport;
    private int _lookAtVersion;
    private bool HasValidLayer => GeoView != null && SelectedLayer != null && _layers.Contains(SelectedLayer);
    private static readonly BindableProperty ThemeImageBackgroundProperty = BindableProperty.Create(
        "ThemeImageBackground", typeof(Color), typeof(OrientedImageryView), Colors.White,
        propertyChanged: (s, _, _) => ((OrientedImageryView)s).Refresh());

    #endregion

    #region Initialization and public API

    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryView"/> class.
    /// </summary>
    public OrientedImageryView()
    {
        OrientedImageryLayers = new ReadOnlyObservableCollection<OrientedImageryLayer>(_layers);
        ViewModel = new OrientedImageryViewModel();
        this.SetAppThemeColor(ThemeImageBackgroundProperty, Colors.White, Color.FromArgb("#202020"));
        ControlTemplate = DefaultControlTemplate;
        SizeChanged += (_, _) => Refresh();
        Loaded += (_, _) => Refresh();
#if !WINDOWS
        Loaded += PresentationLoaded;
        Unloaded += PresentationUnloaded;
#endif
    }

    /// <summary>
    /// Gets or sets the <see cref="Esri.ArcGISRuntime.Maui.GeoView"/> on which marker graphics are displayed.
    /// </summary>
    /// <remarks>
    /// Setting <see cref="GeoView"/> to <c>null</c> will clear all markers and footprints from the geo view and set
    /// <see cref="OrientedImageryViewModel.OrientedImageryLayer"/> on <see cref="ViewModel"/> to <c>null</c>.
    /// </remarks>
    public GeoView? GeoView
    {
        get => (GeoView?)GetValue(GeoViewProperty);
        set => SetValue(GeoViewProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="GeoView"/> bindable property.
    /// </summary>
    public static readonly BindableProperty GeoViewProperty = BindableProperty.Create(
        nameof(GeoView), typeof(GeoView), typeof(OrientedImageryView), null,
        propertyChanged: (s, _, _) => ((OrientedImageryView)s).Reconnect());

    /// <summary>
    /// Gets or sets the oriented imagery layer to inspect.
    /// </summary>
    /// <remarks>
    /// Set this property explicitly; the control does not select a layer automatically or provide a layer selector.
    /// Selection is retained while <see cref="GeoView"/> is disconnected.
    /// The layer must belong to the connected map or scene. Selecting a valid layer zooms to its extent after loading.
    /// </remarks>
    public OrientedImageryLayer? SelectedLayer
    {
        get => (OrientedImageryLayer?)GetValue(SelectedLayerProperty);
        set => SetValue(SelectedLayerProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="SelectedLayer"/> bindable property.
    /// </summary>
    public static readonly BindableProperty SelectedLayerProperty = BindableProperty.Create(
        nameof(SelectedLayer), typeof(OrientedImageryLayer), typeof(OrientedImageryView), null,
        propertyChanged: (s, _, _) => ((OrientedImageryView)s).ChangeLayer());

    /// <summary>
    /// Gets the oriented imagery layers in the connected map or scene, including nested group layers.
    /// </summary>
    public ReadOnlyObservableCollection<OrientedImageryLayer> OrientedImageryLayers { get; }

    /// <summary>
    /// Gets or sets the view model for the oriented imagery view.
    /// </summary>
    public OrientedImageryViewModel ViewModel
    {
        get => (OrientedImageryViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ViewModel"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ViewModelProperty = BindableProperty.Create(
        nameof(ViewModel), typeof(OrientedImageryViewModel), typeof(OrientedImageryView), null,
        validateValue: (_, value) => value is OrientedImageryViewModel,
        propertyChanged: (s, _, _) =>
        {
            var view = (OrientedImageryView)s;
            view._hasSearchedForImages = false;
            view.Reconnect();
        });

    /// <summary>
    /// Gets or sets the background color shown where the image does not fill the display.
    /// </summary>
    /// <remarks><see cref="System.Drawing.Color.Empty"/> uses the current theme's background color.</remarks>
    public System.Drawing.Color DisplayBackgroundColor
    {
        get => (System.Drawing.Color)GetValue(DisplayBackgroundColorProperty);
        set => SetValue(DisplayBackgroundColorProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="DisplayBackgroundColor"/> bindable property.
    /// </summary>
    public static readonly BindableProperty DisplayBackgroundColorProperty = BindableProperty.Create(
        nameof(DisplayBackgroundColor), typeof(System.Drawing.Color), typeof(OrientedImageryView), System.Drawing.Color.Empty,
        propertyChanged: (s, _, _) => ((OrientedImageryView)s).Refresh());

    /// <summary>
    /// Gets or sets the template selector for toolbar categories and their items.
    /// </summary>
    /// <remarks>Templates apply to <see cref="OrientedImageryViewModel.ToolbarCategories"/> and their <see cref="OrientedImageryToolbarCategory.ToolbarItems"/>.</remarks>
    public DataTemplateSelector? ToolbarItemTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(ToolbarItemTemplateSelectorProperty);
        set => SetValue(ToolbarItemTemplateSelectorProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ToolbarItemTemplateSelector"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ToolbarItemTemplateSelectorProperty = BindableProperty.Create(
        nameof(ToolbarItemTemplateSelector), typeof(DataTemplateSelector), typeof(OrientedImageryView), null,
        propertyChanged: (s, _, _) => ((OrientedImageryView)s).InvalidateToolbar());

    /// <summary>
    /// Identifies the <see cref="ErrorMessage"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ErrorMessageProperty = BindableProperty.Create(
        nameof(ErrorMessage), typeof(string), typeof(OrientedImageryView), null);

    /// <summary>
    /// Gets the current error or status message.
    /// </summary>
    public string? ErrorMessage
    {
        get => (string?)GetValue(ErrorMessageProperty);
        private set => SetValue(ErrorMessageProperty, value);
    }

    /// <summary>
    /// Gets or sets a handler that replaces the default image-tap behavior. Set to <c>null</c> to use the default behavior.
    /// </summary>
    /// <remarks>Toolkit-managed marker hits raise this event with <see cref="OrientedImageTappedEventArgs.Marker"/> set to <c>null</c>.</remarks>
    public Action<object?, OrientedImageTappedEventArgs>? OnImageTappedOverride { get; set; }

    /// <summary>
    /// Gets or sets a handler that replaces the default map or scene tap behavior. Set to <c>null</c> to use the default behavior.
    /// </summary>
    public Action<object?, GeoViewInputEventArgs>? OnGeoViewTappedOverride { get; set; }

    #endregion

    #region Connection lifecycle

    private void CancelOperation()
    {
        // In-flight searches may finish, but their results no longer apply.
        _operationVersion++;
        _searching = false;
        _operationError = null;
    }

    private void Disconnect()
    {
        _lookAtVersion++;
        DetachBackNavigation();
        _connected = false;
        _lastStatusAnnouncement = null;
        _lastNotice = null;
        DismissPanel();
#if ANDROID
        DismissAndroidPopup();
#endif
        _layerVersion++;
        CancelOperation();
        if (_connectedModel != null)
        {
            _connectedModel.PropertyChanged -= ModelChanged;
            _connectedModel.SequenceBoundaryReached -= SequenceBoundaryReached;
            _connectedModel.ToolbarCategories.CollectionChanged -= ToolbarChanged;
            _connectedModel.SelectNextImageCommand.CanExecuteChanged -= NavigationAvailabilityChanged;
            _connectedModel.SelectPreviousImageCommand.CanExecuteChanged -= NavigationAvailabilityChanged;
            _connectedModel.ToggleSequentialNavigationCommand.CanExecuteChanged -= NavigationAvailabilityChanged;
            _connectedModel.ClearMarkersCommand.CanExecuteChanged -= NavigationAvailabilityChanged;
            _connectedGeoView?.GraphicsOverlays?.Remove(_connectedModel.MarkersOverlay);
            _connectedModel.DispatchAction = null;
            _connectedModel.CloseSelectedImagePopupCommand.Execute(null);
            _connectedModel.OrientedImageryLayer = null;
            _connectedModel.Markers.Clear();
        }

        foreach (var category in _toolbarCategories)
            category.ToolbarItems.CollectionChanged -= ToolbarChanged;
        _toolbarCategories.Clear();

        if (_connectedGeoView != null)
        {
            _connectedGeoView.GeoViewTapped -= GeoViewTapped;
            ((INotifyPropertyChanged)_connectedGeoView).PropertyChanged -= GeoViewChanged;
        }

        if (_geoModel != null)
            _geoModel.PropertyChanged -= GeoModelChanged;
        if (_operationalLayers != null)
            _operationalLayers.CollectionChanged -= LayersChanged;
        UnwireGroupLayers();
        if (_observedLayer != null)
            _observedLayer.PropertyChanged -= LayerChanged;
        _connectedGeoView = null;
        _connectedModel = null;
        _geoModel = null;
        _operationalLayers = null;
        _observedLayer = null;
        if (_viewport != null)
        {
            _viewport.Footprint = null;
            _viewport.Markers = null;
        }
    }

    private void Reconnect()
    {
        Disconnect();
        if (GeoView != _lastZoomedGeoView)
        {
            _lastZoomedGeoView = null;
            _lastZoomedLayer = null;
        }
        _connectedModel = ViewModel;
        _connectedModel.DispatchAction = DispatchUpdate;
        _connectedModel.PropertyChanged += ModelChanged;
        _connectedModel.SequenceBoundaryReached += SequenceBoundaryReached;
        _connectedModel.ToolbarCategories.CollectionChanged += ToolbarChanged;
        ObserveToolbarCategories();
        _connectedModel.SelectNextImageCommand.CanExecuteChanged += NavigationAvailabilityChanged;
        _connectedModel.SelectPreviousImageCommand.CanExecuteChanged += NavigationAvailabilityChanged;
        _connectedModel.ToggleSequentialNavigationCommand.CanExecuteChanged += NavigationAvailabilityChanged;
        _connectedModel.ClearMarkersCommand.CanExecuteChanged += NavigationAvailabilityChanged;
        _connectedGeoView = GeoView;
        _connected = _connectedGeoView != null;
        AttachBackNavigation();
        if (_connectedGeoView != null)
        {
            _connectedGeoView.GraphicsOverlays ??= new GraphicsOverlayCollection();
            if (!_connectedGeoView.GraphicsOverlays.Contains(ViewModel.MarkersOverlay))
                _connectedGeoView.GraphicsOverlays.Add(ViewModel.MarkersOverlay);
            _connectedGeoView.GeoViewTapped += GeoViewTapped;
            ((INotifyPropertyChanged)_connectedGeoView).PropertyChanged += GeoViewChanged;
        }

        UpdateLayers();
        ChangeLayer();
    }

    #endregion

    #region Layer discovery

    private void UpdateLayers()
    {
        GeoModel? geoModel = GeoView is MapView map ? map.Map : GeoView is SceneView scene ? scene.Scene : null;
        if (_geoModel != geoModel)
        {
            if (_geoModel != null)
                _geoModel.PropertyChanged -= GeoModelChanged;
            _geoModel = geoModel;
            if (_geoModel != null)
                _geoModel.PropertyChanged += GeoModelChanged;
        }
        var operationalLayers = _geoModel?.OperationalLayers;
        if (_operationalLayers != operationalLayers)
        {
            if (_operationalLayers != null)
                _operationalLayers.CollectionChanged -= LayersChanged;
            _operationalLayers = operationalLayers;
            if (_operationalLayers != null)
                _operationalLayers.CollectionChanged += LayersChanged;
        }
        UnwireGroupLayers();
        var discovered = new List<OrientedImageryLayer>();
        void CollectLayers(IEnumerable<Layer> layers)
        {
            foreach (var layer in layers)
            {
                if (layer is OrientedImageryLayer imagery && !discovered.Contains(imagery))
                    discovered.Add(imagery);
                else if (layer is GroupLayer group && _groupLayers.Add(group))
                {
                    group.PropertyChanged += GroupLayerChanged;
                    if (_nestedLayerCollections.Add(group.Layers))
                        group.Layers.CollectionChanged += LayersChanged;
                    CollectLayers(group.Layers);
                }
            }
        }
        if (_operationalLayers != null)
            CollectLayers(_operationalLayers);
        if (!_layers.SequenceEqual(discovered))
            CancelOperation();
        // Keep collection identity and ordering for applications observing the layer list.
        int index = 0;
        foreach (var layer in discovered)
        {
            if (index >= _layers.Count || _layers[index] != layer)
            {
                int existing = _layers.IndexOf(layer);
                if (existing >= 0)
                    _layers.Move(existing, index);
                else
                    _layers.Insert(index, layer);
            }
            index++;
        }
        while (_layers.Count > index)
            _layers.RemoveAt(_layers.Count - 1);
        if (_connected && SelectedLayer != null && !_layers.Contains(SelectedLayer))
            SelectedLayer = null;
        DismissPanel();
        Refresh();
    }

    private void GeoViewChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is "Map" or "Scene")
            UpdateLayersOnDispatcher();
        else if (e.PropertyName == "DrawStatus")
            DispatchUpdate(() =>
            {
                GeoModel? model = GeoView is MapView map ? map.Map : GeoView is SceneView scene ? scene.Scene : null;
                if (model != _geoModel)
                    UpdateLayers();
            });
    }

    private void GeoModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GeoModel.OperationalLayers))
            UpdateLayersOnDispatcher();
    }

    private void LayersChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateLayersOnDispatcher();

    private void UpdateLayersOnDispatcher()
    {
        if (Dispatcher.IsDispatchRequired)
            Dispatcher.Dispatch(UpdateLayers);
        else
            UpdateLayers();
    }

    private void GroupLayerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GroupLayer.Layers))
            UpdateLayersOnDispatcher();
    }

    private void UnwireGroupLayers()
    {
        foreach (var layers in _nestedLayerCollections)
            layers.CollectionChanged -= LayersChanged;
        _nestedLayerCollections.Clear();
        foreach (var group in _groupLayers)
            group.PropertyChanged -= GroupLayerChanged;
        _groupLayers.Clear();
    }

    private void DispatchUpdate(Action action)
    {
        // Ignore queued updates from a layer that has since been replaced.
        int version = _layerVersion;
        if (Dispatcher.IsDispatchRequired)
            Dispatcher.Dispatch(() => { if (version == _layerVersion) action(); });
        else
            action();
    }

    #endregion

    #region Layer selection and model updates

    private void ChangeLayer()
    {
        _hasSearchedForImages = false;
        if (!_connected)
        {
            Refresh();
            return;
        }
        _layerVersion++;
        if (SelectedLayer != _lastZoomedLayer)
            _lastZoomedLayer = null;
        CancelOperation();
        DismissPanel();
        if (_observedLayer != null)
            _observedLayer.PropertyChanged -= LayerChanged;
        _observedLayer = SelectedLayer;
        if (_observedLayer != null)
            _observedLayer.PropertyChanged += LayerChanged;
        if (ViewModel.OrientedImageryLayer != SelectedLayer)
            ViewModel.OrientedImageryLayer = SelectedLayer;
        if (SelectedLayer != null)
            SelectedLayer.IsVisible = true;
        Refresh();
        if (HasValidLayer)
            _ = LoadSelectedLayerAsync(SelectedLayer, _layerVersion, _operationVersion);
    }

    private async Task LoadSelectedLayerAsync(OrientedImageryLayer layer, int version, int operationVersion)
    {
        try
        {
            await layer.LoadAsync();
            if (version == _layerVersion && _connected && layer == SelectedLayer && GeoView is { } view)
            {
                if ((_lastZoomedLayer != layer || _lastZoomedGeoView != view) &&
                    operationVersion == _operationVersion && layer.FullExtent is { } extent)
                    view.SetViewpoint(new Viewpoint(extent));
                _lastZoomedLayer = layer;
                _lastZoomedGeoView = view;
            }
        }
        catch (Exception ex)
        {
            if (version == _layerVersion && _connected && layer == SelectedLayer)
            {
                _operationError = ex;
                Refresh();
            }
        }
    }

    private void LayerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Layer.Name) or nameof(Layer.LoadStatus))
            DispatchUpdate(Refresh);
    }

    private void ModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(OrientedImageryViewModel.OrientedImageryLayer):
                DispatchUpdate(() =>
                {
                    _hasSearchedForImages = false;
                    SynchronizeSelectedLayer();
                });
                break;
            case nameof(OrientedImageryViewModel.SearchPoint):
            case nameof(OrientedImageryViewModel.SelectedImageFootprint):
                DispatchUpdate(() =>
                {
                    Refresh();
                    _ = LookAtSearchPointAsync();
                });
                break;
            case nameof(OrientedImageryViewModel.InteractionState):
                DispatchUpdate(() =>
                {
                    CancelOperation();
                    Refresh();
                });
                break;
            default:
                DispatchUpdate(Refresh);
                break;
        }
    }

    private async Task LookAtSearchPointAsync()
    {
        int version = ++_lookAtVersion;
        var viewport = _viewport;
        var model = ViewModel;
        if (!_connected || viewport == null || model.SearchPoint is not { } point ||
            model.SelectedImageFootprint?.OrientedImage is not { } image ||
            !OrientedImageViewport.IsPanoramic(image.Type, image.Attributes))
            return;
        try
        {
            await viewport.LookAtAsync(point);
        }
        catch (Exception ex)
        {
            if (version == _lookAtVersion && _connected && viewport == _viewport && model == ViewModel)
            {
                _operationError = ex;
                Refresh();
            }
        }
    }

    private void SynchronizeSelectedLayer()
    {
        if (SelectedLayer != ViewModel.OrientedImageryLayer)
            SelectedLayer = ViewModel.OrientedImageryLayer;
        else
            Refresh();
    }

    private void NavigationAvailabilityChanged(object? sender, EventArgs e) => DispatchUpdate(Refresh);

    private void SequenceBoundaryReached(bool start, bool end)
    {
        if (CanAnnounce && ViewModel.IsSequentialNavigationEnabled && ViewModel.SelectedImage != null &&
            ViewModel.SequentialNavigationError == null && _operationError == null && _viewport?.Error == null)
            SemanticScreenReader.Default.Announce(Text(start && end ? "SequenceStartAndEndReached" :
                start ? "SequenceStartReached" : "SequenceEndReached"));
    }

    #endregion

    #region Map and image interaction

    private async void GeoViewTapped(object? sender, GeoViewInputEventArgs e)
    {
        if (!_connected)
            return;
        if (OnGeoViewTappedOverride != null)
        {
            OnGeoViewTappedOverride(this, e);
            return;
        }

        var geoView = GeoView;
        var layer = SelectedLayer;
        var model = ViewModel;
        if (geoView == null || layer == null || e.Location == null || !_layers.Contains(layer))
            return;
        if (model.InteractionState == OrientedImageryViewInteractionState.AddMarkers)
        {
            model.AddMarkerLocation(e.Location);
            return;
        }
        if (model.InteractionState == OrientedImageryViewInteractionState.None)
            return;

        CancelOperation();
        int version = _operationVersion;
        _searching = true;
        Refresh();
        try
        {
            IReadOnlyList<OrientedImage> images;
            if (model.InteractionState == OrientedImageryViewInteractionState.FindImages)
            {
                images = await layer.SearchImagesAsync(e.Location, new OrientedImageSearchParameters { MaxResults = -1 });
            }
            else
            {
                var identified = await geoView.IdentifyLayerAsync(layer, e.Position, 10, false);
                if (version != _operationVersion)
                    return;
                images = identified.GeoElements.FirstOrDefault() is Feature feature
                    ? new[] { await layer.FetchImageForFeatureAsync(feature) }
                    : [];
            }

            if (version == _operationVersion && layer == SelectedLayer && geoView == GeoView && model == ViewModel)
            {
                bool explore = model.InteractionState == OrientedImageryViewInteractionState.FindImages;
                model.SetImages(explore ? images : [], explore || images.Count == 0 ? e.Location : null);
                model.SelectedImage = images.FirstOrDefault();
                if (explore)
                    model.IsSequentialNavigationEnabled = false;
                _hasSearchedForImages = true;
            }
        }
        catch (OperationCanceledException) when (version != _operationVersion)
        {
        }
        catch (Exception ex)
        {
            if (version == _operationVersion)
                _operationError = ex;
        }
        finally
        {
            if (version == _operationVersion)
            {
                _searching = false;
                Refresh();
            }
        }
    }

    private async void ImageTapped(object? sender, OrientedImageTappedEventArgs e)
    {
        if (!_connected)
            return;
        if (OnImageTappedOverride != null)
        {
            OnImageTappedOverride(this, ViewModel.GetPublicImageTappedEventArgs(e));
            return;
        }
        var model = ViewModel;
        var layer = SelectedLayer;
        int version = _operationVersion;
        if (model.InteractionState != OrientedImageryViewInteractionState.AddMarkers || e.Marker != null || _viewport?.IsInteractive != true)
            return;
        try
        {
            var location = await e.Image.ImageToLocationAsync(e.ImagePoint);
            if (_connected && version == _operationVersion && model == ViewModel && layer == SelectedLayer && e.Image == model.SelectedImage && model.InteractionState == OrientedImageryViewInteractionState.AddMarkers)
                model.AddMarkerLocation(location);
        }
        catch (Exception ex)
        {
            if (_connected && version == _operationVersion && model == ViewModel && e.Image == model.SelectedImage)
            {
                _operationError = ex;
                Refresh();
            }
        }
    }

    #endregion
}
