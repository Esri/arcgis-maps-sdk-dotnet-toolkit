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

    private GeoView? _connectedGeoView;
    private OrientedImageryViewModel? _connectedModel;
    private OrientedImageryLayer? _observedLayer;
    private int _operationVersion;
    private int _layerVersion;
    private bool _connected;
    private bool _searching;
    private Exception? _operationError;
    private OrientedImageViewport? _viewport;
    private int _lookAtVersion;
    private bool HasLayer => GeoView != null && ViewModel.OrientedImageryLayer != null;
    private static readonly BindableProperty ThemeImageBackgroundProperty = BindableProperty.Create(
        "ThemeImageBackground", typeof(Color), typeof(OrientedImageryView), Colors.White,
        propertyChanged: (s, _, _) => ((OrientedImageryView)s).Refresh());

    #endregion

    #region Initialization and public API

    /// <summary>
    /// Raised when a tap is registered on the displayed image.
    /// </summary>
    /// <remarks>
    /// Set <see cref="OrientedImageryViewImageTappedArgs.Handled"/> to <c>true</c> before awaiting asynchronous work to override the default tap behavior.
    /// Toolkit-managed marker hits expose <see cref="OrientedImageTappedEventArgs.Marker"/> as <c>null</c>.
    /// </remarks>
    public event EventHandler<OrientedImageryViewImageTappedArgs>? ImageTapped;

    /// <summary>
    /// Raised when a tap is registered on the connected <see cref="GeoView"/>.
    /// </summary>
    /// <remarks>
    /// Set <see cref="OrientedImageryViewGeoViewTappedArgs.Handled"/> to <c>true</c> before awaiting asynchronous work to override the default tap behavior.
    /// </remarks>
    public event EventHandler<OrientedImageryViewGeoViewTappedArgs>? GeoViewTapped;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryView"/> class.
    /// </summary>
    public OrientedImageryView()
    {
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
    /// Setting <see cref="GeoView"/> to <c>null</c> detaches the marker overlay and viewer presentation
    /// and sets <see cref="OrientedImageryViewModel.OrientedImageryLayer"/> on <see cref="ViewModel"/> to <c>null</c>,
    /// clearing its images, footprints, and markers.
    /// The application manages layer loading, map or scene membership, visibility, and viewpoint.
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
        propertyChanged: (s, _, newValue) =>
        {
            var view = (OrientedImageryView)s;
            view.Reconnect();
            if (newValue == null)
                view.ViewModel.OrientedImageryLayer = null;
        });

    /// <summary>
    /// Gets or sets the view model for the oriented imagery view.
    /// </summary>
    /// <remarks>
    /// Set <see cref="OrientedImageryViewModel.OrientedImageryLayer"/> on the view model to select a layer.
    /// </remarks>
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
            view.HasSearchedForImages = false;
            view.Reconnect();
        });

    /// <summary>
    /// Gets a value indicating whether an image selection search has completed after a map tap.
    /// </summary>
    public bool HasSearchedForImages
    {
        get => (bool)GetValue(HasSearchedForImagesProperty);
        private set => SetValue(HasSearchedForImagesProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="HasSearchedForImages"/> bindable property.
    /// </summary>
    public static readonly BindableProperty HasSearchedForImagesProperty = BindableProperty.Create(
        nameof(HasSearchedForImages), typeof(bool), typeof(OrientedImageryView), false);

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
    /// Gets the temporary error message that does not prevent normal control usage.
    /// </summary>
    public string? TransientErrorMessage
    {
        get => (string?)GetValue(TransientErrorMessageProperty);
        private set => SetValue(TransientErrorMessagePropertyKey, value);
    }

    private static readonly BindablePropertyKey TransientErrorMessagePropertyKey = BindableProperty.CreateReadOnly(
        nameof(TransientErrorMessage), typeof(string), typeof(OrientedImageryView), null,
        propertyChanged: (s, _, _) => ((OrientedImageryView)s).Refresh());

    /// <summary>
    /// Identifies the <see cref="TransientErrorMessage"/> bindable property.
    /// </summary>
    public static readonly BindableProperty TransientErrorMessageProperty = TransientErrorMessagePropertyKey.BindableProperty;

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
            _connectedModel.CloseImageDetailsPopupCommand.Execute(null);
        }

        foreach (var category in _toolbarCategories)
            category.ToolbarItems.CollectionChanged -= ToolbarChanged;
        _toolbarCategories.Clear();

        if (_connectedGeoView != null)
        {
            _connectedGeoView.GeoViewTapped -= GeoView_GeoViewTapped;
            ((INotifyPropertyChanged)_connectedGeoView).PropertyChanged -= GeoViewChanged;
        }

        if (_observedLayer != null)
            _observedLayer.PropertyChanged -= LayerChanged;
        _connectedGeoView = null;
        _connectedModel = null;
        _observedLayer = null;
        if (_viewport != null)
        {
            _viewport.Footprint = null;
            _viewport.Markers = null;
        }
        TransientErrorMessage = null;
    }

    private void Reconnect()
    {
        Disconnect();
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
            _connectedGeoView.GeoViewTapped += GeoView_GeoViewTapped;
            ((INotifyPropertyChanged)_connectedGeoView).PropertyChanged += GeoViewChanged;
        }

        ChangeLayer();
    }

    private void GeoViewChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Results from the previous map or scene must not update the current viewer.
        if (e.PropertyName is nameof(MapView.Map) or nameof(SceneView.Scene))
            DispatchUpdate(ChangeLayer);
    }

    #endregion

    #region Layer selection and model updates

    private void DispatchUpdate(Action action)
    {
        // Ignore queued updates from a layer that has since been replaced.
        int version = _layerVersion;
        if (Dispatcher.IsDispatchRequired)
            Dispatcher.Dispatch(() => { if (version == _layerVersion) action(); });
        else
            action();
    }

    private void ChangeLayer()
    {
        _lookAtVersion++;
        _layerVersion++;
        CancelOperation();
        DismissPanel();
        TransientErrorMessage = null;
        if (_observedLayer != null)
            _observedLayer.PropertyChanged -= LayerChanged;
        _observedLayer = ViewModel.OrientedImageryLayer;
        if (_observedLayer != null)
            _observedLayer.PropertyChanged += LayerChanged;
        Refresh();
    }

    private void LayerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Layer.Name) or nameof(Layer.LoadStatus) or nameof(Layer.LoadError))
            DispatchUpdate(Refresh);
    }

    private void ModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(OrientedImageryViewModel.OrientedImageryLayer):
                DispatchUpdate(() =>
                {
                    HasSearchedForImages = false;
                    ChangeLayer();
                });
                break;
            case nameof(OrientedImageryViewModel.SearchPoint):
            case nameof(OrientedImageryViewModel.SelectedImage):
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
            case nameof(OrientedImageryViewModel.SequentialNavigationError):
                var message = ViewModel.SequentialNavigationError?.Message;
                DispatchUpdate(() =>
                {
                    if (message != null)
                        TransientErrorMessage = message;
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

    private void NavigationAvailabilityChanged(object? sender, EventArgs e) => DispatchUpdate(Refresh);

    private void SequenceBoundaryReached(object? sender, SequenceBoundaryReachedArgs e)
    {
        if (ReferenceEquals(sender, ViewModel) && CanAnnounce && ViewModel.IsSequentialNavigationEnabled && ViewModel.SelectedImage != null &&
            ViewModel.SequentialNavigationError == null && _operationError == null && _viewport?.Error == null)
            SemanticScreenReader.Default.Announce(Text(e.HasReachedStart && e.HasReachedEnd ? "SequenceStartAndEndReached" :
                e.HasReachedStart ? "SequenceStartReached" : "SequenceEndReached"));
    }

    #endregion

    #region Map and image interaction

    private async void GeoView_GeoViewTapped(object? sender, GeoViewInputEventArgs e)
    {
        if (!_connected)
            return;
        int layerVersion = _layerVersion;
        var eventArgs = new OrientedImageryViewGeoViewTappedArgs(e);
        GeoViewTapped?.Invoke(this, eventArgs);
        if (eventArgs.Handled || !_connected || layerVersion != _layerVersion)
            return;

        var geoView = GeoView;
        var model = ViewModel;
        var layer = model.OrientedImageryLayer;
        if (geoView == null || layer == null || e.Location == null)
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

            if (version == _operationVersion && layer == model.OrientedImageryLayer && geoView == GeoView && model == ViewModel)
            {
                bool explore = model.InteractionState == OrientedImageryViewInteractionState.FindImages;
                model.SetImages(images, explore || images.Count == 0 ? e.Location : null);
                model.SelectedImage = images.FirstOrDefault();
                if (explore)
                    model.IsSequentialNavigationEnabled = false;
                HasSearchedForImages = true;
            }
        }
        catch (OperationCanceledException) when (version != _operationVersion)
        {
        }
        catch (Exception ex)
        {
            if (version == _operationVersion)
                TransientErrorMessage = ex.Message;
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

    private async void Viewport_ImageTapped(object? sender, OrientedImageTappedEventArgs e)
    {
        if (!_connected)
            return;
        int layerVersion = _layerVersion;
        var eventArgs = new OrientedImageryViewImageTappedArgs(ViewModel.GetPublicImageTappedEventArgs(e));
        ImageTapped?.Invoke(this, eventArgs);
        if (eventArgs.Handled || !_connected || layerVersion != _layerVersion)
            return;
        var model = ViewModel;
        var layer = model.OrientedImageryLayer;
        int version = _operationVersion;
        if (model.InteractionState != OrientedImageryViewInteractionState.AddMarkers || e.Marker != null || _viewport?.IsInteractive != true)
            return;
        try
        {
            var location = await e.Image.ImageToLocationAsync(e.ImagePoint);
            if (_connected && version == _operationVersion && model == ViewModel && layer == model.OrientedImageryLayer && e.Image == model.SelectedImage && model.InteractionState == OrientedImageryViewInteractionState.AddMarkers)
                model.AddMarkerLocation(location);
        }
        catch (Exception ex)
        {
            if (_connected && version == _operationVersion && model == ViewModel && e.Image == model.SelectedImage)
            {
                TransientErrorMessage = ex.Message;
            }
        }
    }

    #endregion
}
