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
    private readonly ObservableCollection<OrientedImageryLayer> _layers = new();
    private readonly ObservableCollection<OrientedImageryToolbarItemBase> _toolbarItems = new();
    private readonly HashSet<OrientedImageryToolbarCategory> _toolbarCategories = new();
    private bool _imageSearchEnabled = true;
    internal IReadOnlyList<OrientedImageryToolbarItemBase> ToolbarItems => _toolbarItems;
    internal bool ImageSearchEnabled => ViewModel.InteractionState switch
    {
        OrientedImageryViewInteractionState.ExploreImages => true,
        OrientedImageryViewInteractionState.ExamineImage => false,
        _ => _imageSearchEnabled,
    };
    internal bool IsAddingMarkers => ViewModel.InteractionState == OrientedImageryViewInteractionState.AddMarkers;
    private GeoView? _connectedGeoView;
    private GeoModel? _geoModel;
    private LayerCollection? _operationalLayers;
    private OrientedImageryViewModel? _connectedModel;
    private OrientedImageryLayer? _observedLayer;
    private OrientedImageryLayer? _lastZoomedLayer;
    private GeoView? _lastZoomedGeoView;
    private int _operationVersion;
    private int _layerVersion;
    private bool _connected;
    private bool _searching;
    private Exception? _operationError;
    private OrientedImageViewport? _viewport;
    private static readonly BindableProperty ThemeImageBackgroundProperty = BindableProperty.Create(
        "ThemeImageBackground", typeof(Color), typeof(OrientedImageryView), Colors.White,
        propertyChanged: (s, _, _) => ((OrientedImageryView)s).Refresh());

    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryView"/> class.
    /// </summary>
    public OrientedImageryView()
    {
        OrientedImageryLayers = new ReadOnlyObservableCollection<OrientedImageryLayer>(_layers);
        ViewModel = new OrientedImageryViewModel();
        this.SetAppThemeColor(ThemeImageBackgroundProperty, Colors.White, Color.FromArgb("#202020"));
        ControlTemplate = DefaultControlTemplate;
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
    /// The <see cref="OrientedImageryLayer"/> currently selected for inspection.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   When a new layer is selected, the geo view will zoom to the layer's extent.
    ///   </para>
    ///   <para>
    ///   The selected layer must be present on the <see cref="GeoView"/> otherwise the <see cref="OrientedImageryView"/> will not function and an error message will be displayed.
    ///   </para>
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
    /// A read-only collection of <see cref="OrientedImageryLayer"/>s held in the <see cref="GeoModel.OperationalLayers"/>
    /// of the connected <see cref="GeoView"/>'s geo model.
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
        propertyChanged: (s, _, _) => ((OrientedImageryView)s).Reconnect());

    /// <summary>
    /// Gets or sets the background color shown where the image does not fill the display.
    /// </summary>
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
    /// Gets or sets the <see cref="DataTemplateSelector"/> used to display the toolbar items in
    /// <see cref="OrientedImageryToolbarCategory.ToolbarItems"/> across <see cref="OrientedImageryViewModel.ToolbarCategories"/>.
    /// </summary>
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
        propertyChanged: (s, _, _) => ((OrientedImageryView)s).Refresh());

    /// <summary>
    /// Identifies the <see cref="ErrorMessage"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ErrorMessageProperty = BindableProperty.Create(
        nameof(ErrorMessage), typeof(string), typeof(OrientedImageryView), null);

    /// <summary>
    /// The message shown when the <see cref="OrientedImageryView"/> is in an invalid state.
    /// </summary>
    public string? ErrorMessage
    {
        get => (string?)GetValue(ErrorMessageProperty);
        private set => SetValue(ErrorMessageProperty, value);
    }

    /// <summary>
    /// Set this function to override the default event handler when the image is tapped. A <c>null</c> value means the default behavior is active.
    /// </summary>
    /// <remarks>Toolkit-managed marker hits raise this event with <see cref="OrientedImageTappedEventArgs.Marker"/> set to <c>null</c>.</remarks>
    public Action<object?, OrientedImageTappedEventArgs>? OnImageTappedOverride { get; set; }

    /// <summary>
    /// Set this function to override the default event handler when the connected <see cref="GeoView"/> is tapped. A <c>null</c> value means the default behavior is active.
    /// </summary>
    public Action<object?, GeoViewInputEventArgs>? OnGeoViewTappedOverride { get; set; }

    private void CancelOperation()
    {
        _operationVersion++;
        _searching = false;
        _operationError = null;
    }

    private void Disconnect()
    {
        DetachBackNavigation();
        _connected = false;
        DismissPanel();
        _layerVersion++;
        CancelOperation();
        if (_connectedModel != null)
        {
            _connectedModel.PropertyChanged -= ModelChanged;
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
        if (_observedLayer != null)
            _observedLayer.PropertyChanged -= LayerChanged;
        _connectedGeoView = null;
        _connectedModel = null;
        _geoModel = null;
        _operationalLayers = null;
        _observedLayer = null;
        SelectedLayer = null;
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
        _connectedModel.ToolbarCategories.CollectionChanged += ToolbarChanged;
        SynchronizeToolbarItems();
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

    private void UpdateLayers()
    {
        CancelOperation();
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
        int index = 0;
        if (_operationalLayers != null)
        {
            foreach (var layer in _operationalLayers.OfType<OrientedImageryLayer>())
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
        }
        while (_layers.Count > index)
            _layers.RemoveAt(_layers.Count - 1);
        if (SelectedLayer != null && !_layers.Contains(SelectedLayer))
            SelectedLayer = null;
        DismissPanel();
        Refresh();
    }

    private void GeoViewChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is "Map" or "Scene")
            DispatchUpdate(UpdateLayers);
    }

    private void GeoModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GeoModel.OperationalLayers))
            DispatchUpdate(UpdateLayers);
    }

    private void LayersChanged(object? sender, NotifyCollectionChangedEventArgs e) => DispatchUpdate(UpdateLayers);

    private void ToolbarChanged(object? sender, NotifyCollectionChangedEventArgs e) => DispatchUpdate(() =>
    {
        SynchronizeToolbarItems();
        Refresh();
    });

    private void SynchronizeToolbarItems()
    {
        foreach (var category in _toolbarCategories)
            category.ToolbarItems.CollectionChanged -= ToolbarChanged;
        _toolbarCategories.Clear();
        foreach (var category in ViewModel.ToolbarCategories)
        {
            if (_toolbarCategories.Add(category))
                category.ToolbarItems.CollectionChanged += ToolbarChanged;
        }

        var items = ViewModel.ToolbarCategories.SelectMany(category => category.ToolbarItems).ToArray();
        if (_toolbarItems.SequenceEqual(items))
            return;
        _toolbarItems.Clear();
        foreach (var item in items)
            _toolbarItems.Add(item);
    }

    internal void SetAddingMarkers(bool enabled)
    {
        _imageSearchEnabled = ImageSearchEnabled;
        ViewModel.InteractionState = enabled ? OrientedImageryViewInteractionState.AddMarkers :
            _imageSearchEnabled ? OrientedImageryViewInteractionState.ExploreImages : OrientedImageryViewInteractionState.ExamineImage;
    }

    private void DispatchUpdate(Action action)
    {
        int version = _layerVersion;
        if (Dispatcher.IsDispatchRequired)
            Dispatcher.Dispatch(() => { if (version == _layerVersion) action(); });
        else
            action();
    }

    private void ChangeLayer()
    {
        if (!_connected && SelectedLayer != null)
        {
            SelectedLayer = null;
            return;
        }
        _layerVersion++;
        if (SelectedLayer != _lastZoomedLayer)
            _lastZoomedLayer = null;
        CancelOperation();
        DismissPanel();
        if (_observedLayer != null)
            _observedLayer.PropertyChanged -= LayerChanged;
        _observedLayer = _connected ? SelectedLayer : null;
        if (_observedLayer != null)
            _observedLayer.PropertyChanged += LayerChanged;
        if (ViewModel.OrientedImageryLayer != SelectedLayer)
            ViewModel.OrientedImageryLayer = SelectedLayer;
        if (SelectedLayer != null)
            SelectedLayer.IsVisible = true;
        Refresh();
        if (_connected && SelectedLayer != null && _layers.Contains(SelectedLayer))
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
        if (e.PropertyName == nameof(OrientedImageryViewModel.OrientedImageryLayer))
            DispatchUpdate(SynchronizeSelectedLayer);
        else
            DispatchUpdate(Refresh);
    }

    private void SynchronizeSelectedLayer()
    {
        if (SelectedLayer != ViewModel.OrientedImageryLayer)
            SelectedLayer = ViewModel.OrientedImageryLayer;
        else
            Refresh();
    }

    private void NavigationAvailabilityChanged(object? sender, EventArgs e) => DispatchUpdate(Refresh);

    private bool HasTool<T>() where T : OrientedImageryToolbarItemBase => _toolbarItems.Any(item => item is T);

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
        if (!_connected || geoView == null || layer == null || e.Location == null || !_layers.Contains(layer))
            return;
        if (model.InteractionState == OrientedImageryViewInteractionState.AddMarkers)
        {
            model.AddMarkerLocation(e.Location);
            return;
        }
        if (model.IsSequentialNavigationEnabled || model.InteractionState == OrientedImageryViewInteractionState.None)
            return;

        CancelOperation();
        int version = _operationVersion;
        _searching = true;
        Refresh();
        try
        {
            IReadOnlyList<OrientedImage> images;
            if (model.InteractionState == OrientedImageryViewInteractionState.ExploreImages)
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
                    : Array.Empty<OrientedImage>();
            }

            if (version == _operationVersion && layer == SelectedLayer && geoView == GeoView && model == ViewModel)
            {
                model.SetImages(images, model.InteractionState == OrientedImageryViewInteractionState.ExploreImages ? e.Location : null);
                model.SelectedImage = images.FirstOrDefault();
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

}
