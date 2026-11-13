#if WPF

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using System.ComponentModel;
using System.Windows.Automation;
using Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

/// <summary>
/// Displays oriented imagery and provides controls for managing image selection, footprints, and markers.
/// </summary>
public partial class OrientedImageryViewer
{
    private const string ImageViewportName = "PART_ImageViewport";
    private const string ToolbarContainerName = "PART_ToolbarContainer";
    private const string PaginatorName = "PART_Paginator";
    private const string MainErrorMessagePresenterName = "PART_MainErrorMessagePresenter";
    private const string DismissTransientErrorButtonName = "PART_DismissTransientErrorButton";

    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryViewer"/> class.
    /// </summary>
    public OrientedImageryViewer() : base()
    {
        ViewModel = new OrientedImageryViewerModel();

        UpdateErrorMessages();

#if WPF
        Loaded += (_, _) => UpdateErrorMessages();
        IsVisibleChanged += (_, _) => UpdateErrorMessages();
#endif

#if MAUI
        // MAUI layout containers are not tab stops by default, so no IsTabStop is needed here.
        ControlTemplate = DefaultControlTemplate;
#else
        DefaultStyleKey = typeof(OrientedImageryViewer);
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

        if (_viewport != null)
            UnwireViewport(_viewport);
        if (_toolbarContainer != null)
            UnwireToolbarContainer(_toolbarContainer);
        if (_paginator != null)
            _paginator.SelectedPageIndexChanged -= Paginator_SelectedPageIndexChanged;

        _viewport = GetTemplateChild(ImageViewportName) as OrientedImageViewport;
        _toolbarContainer = GetTemplateChild(ToolbarContainerName) as ItemsControl;
        _paginator = GetTemplateChild(PaginatorName) as OrientedImagery.Paginator;
        _mainErrorMessagePresenter = GetTemplateChild(MainErrorMessagePresenterName) as TextBlock;
        RewireDismissTransientErrorButton();

        if (_viewport != null)
            WireViewport(_viewport);
        if (_toolbarContainer != null)
            WireToolbarContainer(_toolbarContainer);
        if (_paginator != null)
            _paginator.SelectedPageIndexChanged += Paginator_SelectedPageIndexChanged;

        RewireViewModel();
        UpdatePaginatorSelection();
        UpdateErrorMessages();
#if WPF
        UpdateDetailsFocus();
#endif
    }

#region ViewModel
    private WeakEventListener<OrientedImageryViewer, OrientedImageryViewerModel, object?, PropertyChangedEventArgs>? _viewModelPropertyChangedListener;
    private WeakEventListener<OrientedImageryViewer, OrientedImageryViewerModel, object?, SequenceBoundaryReachedArgs>? _viewModelSequenceBoundaryListener;

    /// <summary>
    /// Gets or sets the view model for the oriented imagery view.
    /// </summary>
    public OrientedImageryViewerModel ViewModel
    {
        get => (OrientedImageryViewerModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ViewModel" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty ViewModelProperty =
        PropertyHelper.CreateProperty<OrientedImageryViewerModel?, OrientedImageryViewer>(nameof(ViewModel), null, (s, oldValue, newValue) => s.OnViewModelChanged(oldValue, newValue));

    private void OnViewModelChanged(OrientedImageryViewerModel? oldValue, OrientedImageryViewerModel? newValue)
    {
        HasSearchedForImages = false;
        if (oldValue != null)
        {
            if (GeoView?.GraphicsOverlays != null)
                GeoView.GraphicsOverlays.Remove(oldValue.MarkersOverlay);
        }

        _viewModelPropertyChangedListener?.Detach();
        _viewModelPropertyChangedListener = null;
        _viewModelSequenceBoundaryListener?.Detach();
        _viewModelSequenceBoundaryListener = null;

        CancelGeoViewTappedHandler();

        if (newValue == null)
        {
            ViewModel = new OrientedImageryViewerModel();
            return;
        }

        _viewModelPropertyChangedListener = new(this, newValue)
        {
            OnEventAction = static (instance, source, eventArgs) => instance.ViewModel_PropertyChanged(source, eventArgs),
            OnDetachAction = static (instance, source, weakListener) => source.PropertyChanged -= weakListener.OnEvent
        };
        newValue.PropertyChanged += _viewModelPropertyChangedListener.OnEvent;

        _viewModelSequenceBoundaryListener = new(this, newValue)
        {
            OnEventAction = static (instance, source, eventArgs) => instance.ViewModel_SequenceBoundaryReached(source, eventArgs),
            OnDetachAction = static (instance, source, weakListener) => source.SequenceBoundaryReached -= weakListener.OnEvent
        };
        newValue.SequenceBoundaryReached += _viewModelSequenceBoundaryListener.OnEvent;

        if (GeoView != null)
        {
            if (GeoView.GraphicsOverlays == null)
                GeoView.GraphicsOverlays = new GraphicsOverlayCollection();
            GeoView.GraphicsOverlays.Add(newValue.MarkersOverlay);
        }

        RewireViewModel();
        UpdatePaginatorSelection();
        UpdateErrorMessages();
    }

    private void RewireViewModel()
    {
        if (_viewport != null)
        {
            _viewport.AutoUpdateFootprint = ViewModel.AutoUpdateFootprint;
            _viewport.Markers = ViewModel.DisplayMarkers;
            _viewport.Footprint = ViewModel.SelectedImageFootprint;
        }
        if (_toolbarContainer != null)
            _toolbarContainer.ItemsSource = ViewModel.ToolbarCategories;
        UpdatePaginatorSelection();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(OrientedImageryViewerModel.SearchPoint):
            case nameof(OrientedImageryViewerModel.SelectedImage):
                UpdateViewportImage();
                goto case nameof(OrientedImageryViewerModel.Images);
            case nameof(OrientedImageryViewerModel.Images):
                UpdatePaginatorSelection();
                UpdateImageStateMessages();
                TransientErrorMessage = null;
                break;
            case nameof(OrientedImageryViewerModel.InteractionState):
                UpdateImageStateMessages();
                break;
            case nameof(OrientedImageryViewerModel.AutoUpdateFootprint):
                if (_viewport != null)
                    _viewport.AutoUpdateFootprint = ViewModel.AutoUpdateFootprint;
                break;
            case nameof(OrientedImageryViewerModel.OrientedImageryLayer):
                CancelGeoViewTappedHandler();
                HasSearchedForImages = false;
                TransientErrorMessage = null;
                UpdateErrorMessages();
                break;
            case nameof(OrientedImageryViewerModel.SequentialNavigationError):
                if (ViewModel.SequentialNavigationError?.Message != null)
                    TransientErrorMessage = ViewModel.SequentialNavigationError?.Message;
                break;
#if WPF
            case nameof(OrientedImageryViewerModel.ImageDetailsPopup):
                UpdateDetailsFocus();
                break;
#endif
        }
    }

    private void ViewModel_SequenceBoundaryReached(object? sender, SequenceBoundaryReachedArgs e)
    {
        if (!ReferenceEquals(sender, ViewModel) || !IsLoaded || !IsVisible || ErrorMessage != null)
            return;

        string? resourceKey = (e.HasReachedStart, e.HasReachedEnd) switch
        {
            (true, true) => "OrientedImageryViewerSequenceStartAndEndReached",
            (true, false) => "OrientedImageryViewerSequenceStartReached",
            (false, true) => "OrientedImageryViewerSequenceEndReached",
            _ => null,
        };

        if (resourceKey != null && Properties.Resources.GetString(resourceKey) is string announcement)
        {
            ScreenReaderHelpers.RaiseNotificationAnnouncement(
                this,
                announcement,
                "OrientedImageryViewerSequenceBoundary");
        }
    }
#endregion ViewModel

#region Display
    /// <summary>
    /// Gets a value indicating whether an image selection search has completed after a map tap.
    /// </summary>
    internal bool HasSearchedForImages
    {
        get => (bool)GetValue(HasSearchedForImagesProperty);
        private set => SetValue(HasSearchedForImagesPropertyKey, value);
    }

    private static readonly DependencyPropertyKey HasSearchedForImagesPropertyKey =
        PropertyHelper.CreateReadOnlyProperty<bool, OrientedImageryViewer>(nameof(HasSearchedForImages));

    /// <summary>
    /// Identifies the <see cref="HasSearchedForImages" /> dependency property.
    /// </summary>
    internal static readonly DependencyProperty HasSearchedForImagesProperty = PropertyHelper.GetProperty(HasSearchedForImagesPropertyKey);

    private OrientedImageViewport? _viewport;

    /// <summary>
    /// Raised when a tap is registered on the displayed image.
    /// </summary>
    /// <remarks>
    /// Set <see cref="OrientedImageryViewerImageTappedArgs.Handled"/> to <c>true</c> before awaiting asynchronous work to override the default tap behavior.
    /// Internally-managed marker hits (such as on the search point marker) are not registered on this event.
    /// </remarks>
    public event EventHandler<OrientedImageryViewerImageTappedArgs>? ImageTapped;

    /// <summary>
    /// Gets or sets the background color shown where the image does not fill the display.
    /// </summary>
    public System.Drawing.Color DisplayBackgroundColor
    {
        get => (System.Drawing.Color)GetValue(DisplayBackgroundColorProperty);
        set => SetValue(DisplayBackgroundColorProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="DisplayBackgroundColor" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty DisplayBackgroundColorProperty =
        PropertyHelper.CreateProperty<System.Drawing.Color, OrientedImageryViewer>(nameof(DisplayBackgroundColor), System.Drawing.Color.White, (s, oldValue, newValue) => s.UpdateDisplayBackgroundColor(newValue));

    private void WireViewport(OrientedImageViewport viewport)
    {
        viewport.ImageTapped += Viewport_ImageTapped;
        viewport.StateChanged += Viewport_StateChanged;
    }

    private void UnwireViewport(OrientedImageViewport viewport)
    {
        viewport.ImageTapped -= Viewport_ImageTapped;
        viewport.StateChanged -= Viewport_StateChanged;
        viewport.ClearValue(OrientedImageViewport.AutoUpdateFootprintProperty);
        viewport.ClearValue(OrientedImageViewport.MarkersProperty);
        viewport.ClearValue(OrientedImageViewport.FootprintProperty);
        viewport.ClearValue(OrientedImageViewport.DisplayBackgroundColorProperty);
    }

    private void UpdateDisplayBackgroundColor(System.Drawing.Color displayBackgroundColor)
    {
        if (_viewport != null)
            _viewport.DisplayBackgroundColor = displayBackgroundColor;
    }

    private void Viewport_StateChanged(object? sender, EventArgs e) => UpdateErrorMessages();

    private async void Viewport_ImageTapped(object? _, OrientedImageTappedEventArgs e)
    {
        var eventArgs = new OrientedImageryViewerImageTappedArgs(ViewModel.GetPublicImageTappedEventArgs(e));
        ImageTapped?.Invoke(this, eventArgs);

        // Only adds a marker if sketch mode is enabled and there is not already a marker nearby
        if (eventArgs.Handled || ViewModel.InteractionState != OrientedImageryViewerInteractionState.AddMarkers || e.Marker != null)
            return;

        TransientErrorMessage = null;

        try
        {
            throw new Exception("test");
            var location = await e.Image.ImageToLocationAsync(e.ImagePoint);
            ViewModel.AddMarkerLocation(location);
        }
        catch (Exception ex)
        {
            TransientErrorMessage = ex.Message;
        }
    }

    private void UpdateViewportImage()
    {
        if (_viewport == null || ViewModel == null)
            return;

        _imageChangedSincePreviousLoadAnnouncement = true;

        _viewport.Footprint = ViewModel.SelectedImageFootprint;
        if (_viewport.Footprint != null)
        {
            var image = _viewport.Footprint.OrientedImage;
            if (ViewModel.SearchPoint is MapPoint searchPoint &&
                (OrientedImageViewport.IsPanoramic(image.Type, image.Attributes) ||
                image.Type == OrientedImageType.Terrestrial360Video ||
                image.Type == OrientedImageType.Aerial360Video))
                _viewport.LookAtAsync(searchPoint);

            if (!_viewport.IsInteractive)
            {
                ViewModel.IsShowSelectedFootprintEnabled = false;
                _viewport.StateChanged += Viewport_StateChanged_UpdateFootprint;
            }
        }

        if (ViewModel.SelectedImage == null)
            UpdateNoImagesErrorMessage();
    }

    private void Viewport_StateChanged_UpdateFootprint(object? sender, EventArgs e)
    {
        if (sender is not OrientedImageViewport viewport)
            return;

        if (viewport != _viewport)
        {
            viewport.StateChanged -= Viewport_StateChanged_UpdateFootprint;
        }
        else if (viewport.IsInteractive == true)
        {
            ViewModel.IsShowSelectedFootprintEnabled = true;
            _viewport.StateChanged -= Viewport_StateChanged_UpdateFootprint;
        }
    }
#endregion Display

#region GeoView
    private CancellationTokenSource? _geoViewTappedCTS;

    /// <summary>
    /// Raised when a tap is registered on the connected <see cref="GeoView"/>.
    /// </summary>
    /// <remarks>
    /// Set <see cref="OrientedImageryViewerGeoViewTappedArgs.Handled"/> to <c>true</c> before awaiting asynchronous work to override the default tap behavior.
    /// </remarks>
    public event EventHandler<OrientedImageryViewerGeoViewTappedArgs>? GeoViewTapped;

    /// <summary>
    /// Gets or sets the <see cref="Esri.ArcGISRuntime.UI.Controls.GeoView"/> on which marker graphics are displayed.
    /// </summary>
    /// <remarks>
    /// Setting <see cref="GeoView"/> to <c>null</c> will clear all markers and footprints from the geo view and set
    /// <see cref="OrientedImageryViewerModel.OrientedImageryLayer"/> to <c>null</c>.
    /// </remarks>
    public GeoView? GeoView
    {
        get => (GeoView?)GetValue(GeoViewProperty);
        set => SetValue(GeoViewProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="GeoView"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty GeoViewProperty =
        PropertyHelper.CreateProperty<GeoView?, OrientedImageryViewer>(nameof(GeoView), null, (s, oldValue, newValue) => s.UpdateGeoView(oldValue, newValue));

    private void UpdateGeoView(GeoView? oldGeoView, GeoView? newGeoView)
    {
        if (oldGeoView == newGeoView) return;

        if (oldGeoView != null)
        {
            oldGeoView.GraphicsOverlays?.Remove(ViewModel.MarkersOverlay);
            oldGeoView.GeoViewTapped -= GeoView_GeoViewTapped;
        }

        CancelGeoViewTappedHandler();

        if (newGeoView != null)
        {
            if (newGeoView.GraphicsOverlays == null)
                newGeoView.GraphicsOverlays = new GraphicsOverlayCollection();
            newGeoView.GraphicsOverlays.Add(ViewModel.MarkersOverlay);
            newGeoView.GeoViewTapped += GeoView_GeoViewTapped;
        }
        else if (ViewModel != null)
        {
            ViewModel.OrientedImageryLayer = null;
        }

        UpdateErrorMessages();
    }

    private async void GeoView_GeoViewTapped(object? _, GeoViewInputEventArgs e)
    {
        var eventArgs = new OrientedImageryViewerGeoViewTappedArgs(e);
        GeoViewTapped?.Invoke(this, eventArgs);

        if (eventArgs.Handled || e.Location == null || GeoView == null || ViewModel.OrientedImageryLayer == null)
            return;

        TransientErrorMessage = null;

        using var currentCTS = new CancellationTokenSource();
        if (_geoViewTappedCTS == null)
        {
            _geoViewTappedCTS = currentCTS;
        }
        else
        {
            var oldCTS = _geoViewTappedCTS;
            _geoViewTappedCTS = currentCTS;
            oldCTS.Cancel();
        }

        var token = currentCTS.Token;
        try
        {
            switch (ViewModel.InteractionState)
            {
                case OrientedImageryViewerInteractionState.AddMarkers:
                    ViewModel.AddMarkerLocation(e.Location);
                    break;

                case OrientedImageryViewerInteractionState.FindImages:
                    var parameters = new OrientedImageSearchParameters() { MaxResults = -1 };
                    var images = await ViewModel.OrientedImageryLayer.SearchImagesAsync(e.Location, parameters, token) ?? new List<OrientedImage>();
                    if (token.IsCancellationRequested) return;

                    _noImagesAnnouncementCanRepeat = true;
                    HasSearchedForImages = true;

                    ViewModel.SetImages(images.ToList(), e.Location);
                    ViewModel.SelectedImage = images.Count < 1 ? null : images[0];
                    ViewModel.IsSequentialNavigationEnabled = false;
                    break;

                case OrientedImageryViewerInteractionState.DisplayImage:
                    var identifyResult = await GeoView.IdentifyLayerAsync(ViewModel.OrientedImageryLayer, e.Position, 0, false, token);
                    if (token.IsCancellationRequested) return;

                    if (identifyResult.GeoElements.Count > 0 && identifyResult.GeoElements[0] is Feature feature)
                    {
                        var image = await ViewModel.OrientedImageryLayer.FetchImageForFeatureAsync(feature, token);
                        if (token.IsCancellationRequested) return;

                        _noImagesAnnouncementCanRepeat = true;
                        HasSearchedForImages = true;
                        ViewModel.SetImages([image]);
                        ViewModel.SelectedImage = image;
                    }
                    else
                    {
                        _noImagesAnnouncementCanRepeat = true;
                        HasSearchedForImages = true;
                        ViewModel.SetImages([], e.Location);
                        ViewModel.SelectedImage = null;
                    }
                    _noImagesAnnouncementCanRepeat = true;
                    break;

                default:
                    return;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // A newer tap or a layer change canceled this search.
        }
        catch (Exception ex)
        {
            TransientErrorMessage = ex.Message;
        }
        finally
        {
            if (_geoViewTappedCTS == currentCTS)
                _geoViewTappedCTS = null;
        }
    }

    /// Only applies to the default handler
    private void CancelGeoViewTappedHandler()
    {
        if (_geoViewTappedCTS != null)
        {
            _geoViewTappedCTS.Cancel();
            _geoViewTappedCTS = null;
        }
    }
#endregion GeoView

#region Toolbar
    private ItemsControl? _toolbarContainer;
    private ToolbarSelector? _defaultItemTemplateSelector;

    /// <summary>
    /// Gets or sets the <see cref="ToolbarSelector"/> used by the toolbar <see cref="ItemsControl"/> to display the toolbar categories and
    /// items in <see cref="OrientedImageryViewerModel.ToolbarCategories"/>.
    /// </summary>
    public ToolbarSelector? ToolbarItemTemplateSelector
    {
        get => (ToolbarSelector)GetValue(ToolbarItemTemplateSelectorProperty);
        set => SetValue(ToolbarItemTemplateSelectorProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ToolbarItemTemplateSelector" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty ToolbarItemTemplateSelectorProperty =
        PropertyHelper.CreateProperty<ToolbarSelector?, OrientedImageryViewer>(nameof(ToolbarItemTemplateSelector), null, (s, oldValue, newValue) => s.OnItemTemplateSelectorChanged(oldValue, newValue));

    private void OnItemTemplateSelectorChanged(ToolbarSelector? oldSelector, ToolbarSelector? newSelector)
    {
        // Save and in the future merge the default selector so the default styles are always available unless overridden.
        if (ReadLocalValue(ToolbarItemTemplateSelectorProperty) == DependencyProperty.UnsetValue)
        {
            _defaultItemTemplateSelector = newSelector;
        }
        else if (_defaultItemTemplateSelector is not null)
        {
            newSelector?.Merge(_defaultItemTemplateSelector);
        }

        if (_toolbarContainer != null)
            _toolbarContainer.ItemTemplateSelector = newSelector;
    }

    private void WireToolbarContainer(ItemsControl toolbarContainer)
    {
        toolbarContainer.ItemTemplateSelector = ToolbarItemTemplateSelector;
        toolbarContainer.Items.Clear();
    }

    private void UnwireToolbarContainer(ItemsControl toolbarContainer)
    {
        toolbarContainer.ClearValue(ItemsControl.ItemTemplateSelectorProperty);
        toolbarContainer.ClearValue(ItemsControl.ItemsSourceProperty);
    }
#endregion Toolbar

#region Pagination
    private OrientedImagery.Paginator? _paginator;

    private void UpdatePaginatorSelection()
    {
        if (_paginator == null)
            return;
        var images = ViewModel.Images;
        var index = -1;
        for (var i = 0; i < images.Count; i++)
        {
            if (ReferenceEquals(images[i], ViewModel.SelectedImage))
                index = i;
        }
        _paginator.TotalPages = images.Count;
        _paginator.SelectedPageIndex = index;
    }

    private void Paginator_SelectedPageIndexChanged(OrientedImagery.Paginator sender, int newPageIndex)
    {
        if (newPageIndex >= 0 && newPageIndex < ViewModel.Images.Count)
            ViewModel.SelectedImage = ViewModel.Images[newPageIndex];
    }
#endregion Pagination

#region Errors
    private const string NoGeoViewMessageKey = "OrientedImageryViewerNoGeoView";
    private const string NoLayerSelectedKey = "OrientedImageryViewerNoLayerSelected";
    private const string NoImagesNoneKey = "OrientedImageryViewerImageSelectionNoImagesNone";
    private const string NoImagesFindKey = "OrientedImageryViewerImageSelectionNoImagesFind";
    private const string NoImagesDisplayKey = "OrientedImageryViewerImageSelectionNoImagesDisplay";
    private const string NoImagesForLocationKey = "OrientedImageryViewerNoImagesFound";
    private const string ImageLoadingKey = "OrientedImageryViewerImageLoading";
    private const string ImageLoadedKey = "OrientedImageryViewerImageLoaded";
    private const string OrientedImageryViewerAnnounceTransientErrorKey = "OrientedImageryViewerAnnounceTransientError";
    private const string ImageStateActivityId = "OrientedImageryViewerImageState";
    private bool _noImagesAnnouncementCanRepeat;
    private string? _lastNoImagesAnnouncement;
    private string? _lastImageErrorAnnouncement;
    private bool _lastLoadingState = false;
    private bool _imageChangedSincePreviousLoadAnnouncement = true;
    private TextBlock? _mainErrorMessagePresenter;
    private Button? _dismissTransientErrorButton;

    /// <summary>
    /// The message shown when the <see cref="OrientedImageryViewer"/> is in a state that prevents all use of the control.
    /// This property does not cover image load errors or user prompts when no image is selected.
    /// </summary>
    internal string? ErrorMessage
    {
        get => (string?)GetValue(ErrorMessageProperty);
        private set => SetValue(ErrorMessagePropertyKey, value);
    }

    private static readonly DependencyPropertyKey ErrorMessagePropertyKey =
        PropertyHelper.CreateReadOnlyProperty<string?, OrientedImageryViewer>(nameof(ErrorMessage));

    /// <summary>
    /// Identifies the <see cref="ErrorMessage" /> dependency property.
    /// </summary>
    internal static readonly DependencyProperty ErrorMessageProperty = PropertyHelper.GetProperty(ErrorMessagePropertyKey);

    /// <summary>
    /// Holds temporary error messages that do not prevent normal control usage.
    /// </summary>
    internal string? TransientErrorMessage
    {
        get => (string?)GetValue(TransientErrorMessageProperty);
        private set => SetValue(TransientErrorMessagePropertyKey, value);
    }

    private static readonly DependencyPropertyKey TransientErrorMessagePropertyKey =
        PropertyHelper.CreateReadOnlyProperty<string?, OrientedImageryViewer>(nameof(TransientErrorMessage), null, OnTransientErrorMessageChanged);

    /// <summary>
    /// Identifies the <see cref="TransientErrorMessage" /> dependency property.
    /// </summary>
    internal static readonly DependencyProperty TransientErrorMessageProperty = PropertyHelper.GetProperty(TransientErrorMessagePropertyKey);

    private static void OnTransientErrorMessageChanged(object? sender, string? _, string? newValue)
    {
        if (sender is OrientedImageryViewer oiView && newValue != null &&
            Properties.Resources.GetString(OrientedImageryViewerAnnounceTransientErrorKey) is string announcement)
        {
            oiView.RaiseImageStateNotification(announcement);
        }
    }

    /// <summary>
    /// Updates error messages and announces them if needed.
    /// </summary>
    private void UpdateErrorMessages()
    {
        if (UpdateMainErrorMessage())
        {
            _noImagesAnnouncementCanRepeat = true;
            return;
        }
        UpdateImageStateMessages();
    }

    private bool UpdateMainErrorMessage()
    {
        var lastMessage = ErrorMessage;

        if (GeoView == null)
            ErrorMessage = Properties.Resources.GetString(NoGeoViewMessageKey);
        else if (ViewModel.OrientedImageryLayer == null)
            ErrorMessage = Properties.Resources.GetString(NoLayerSelectedKey);
        else
            ErrorMessage = null;

        if (ErrorMessage == null || ErrorMessage == lastMessage)
            return false;

        ScreenReaderHelpers.RaiseLiveRegionChanged(_mainErrorMessagePresenter);
        return true;
    }

    private void UpdateImageStateMessages()
    {
        if (UpdateNoImagesErrorMessage()) return;
        if (UpdateImageErrorAnnouncement()) return;
        UpdateImageLoadingAnnouncement();
    }

    private bool UpdateNoImagesErrorMessage()
    {
        bool repeatAnnouncement = _noImagesAnnouncementCanRepeat;
        _noImagesAnnouncementCanRepeat = false;

        string? announcement = GetNoImagesAnnouncement();
        if (announcement == null)
        {
            _lastNoImagesAnnouncement = null;
            return false;
        }

        if (!repeatAnnouncement && announcement == _lastNoImagesAnnouncement)
            return false;

        _lastNoImagesAnnouncement = announcement;
        RaiseImageStateNotification(announcement);

        return true;
    }

    private bool UpdateImageErrorAnnouncement()
    {
        string? announcement = _viewport?.Error?.Message;
        if (!IsLoaded || !IsVisible || ErrorMessage != null || string.IsNullOrWhiteSpace(announcement))
        {
            _lastImageErrorAnnouncement = null;
            return false;
        }

        if (announcement == _lastImageErrorAnnouncement)
            return false;

        _lastImageErrorAnnouncement = announcement;
        RaiseImageStateNotification(announcement);
        return true;
    }

    private void UpdateImageLoadingAnnouncement()
    {
        if (_viewport?.Footprint == null)
            return;

        bool isLoading = _viewport.IsImageLoading;
        bool imageHasChanged = _imageChangedSincePreviousLoadAnnouncement;
        _imageChangedSincePreviousLoadAnnouncement = false;
        if (isLoading == _lastLoadingState && !imageHasChanged)
            return;

        _lastLoadingState = isLoading;
        if (Properties.Resources.GetString(isLoading ? ImageLoadingKey : ImageLoadedKey) is string announcement)
            RaiseImageStateNotification(announcement);
    }

    private string? GetNoImagesAnnouncement()
    {
        if (!IsLoaded || !IsVisible || ErrorMessage != null || _viewport?.IsImageLoading == true || ViewModel == null || ViewModel.SelectedImage != null || ViewModel.Images.Count > 0)
            return null;

        string resourceKey;
        if (HasSearchedForImages)
            resourceKey = NoImagesForLocationKey;
        else if (ViewModel.InteractionState == OrientedImageryViewerInteractionState.FindImages)
            resourceKey = NoImagesFindKey;
        else if (ViewModel.InteractionState == OrientedImageryViewerInteractionState.DisplayImage)
            resourceKey = NoImagesDisplayKey;
        else
            resourceKey = NoImagesNoneKey;

        return Properties.Resources.GetString(resourceKey);
    }

    private void RaiseImageStateNotification(string announcement)
    {
#if WPF || WINUI
        ScreenReaderHelpers.RaiseNotificationAnnouncement(
            this,
            announcement,
            ImageStateActivityId,
            AutomationNotificationProcessing.MostRecent);
#endif
    }

    private void RewireDismissTransientErrorButton()
    {
        if (_dismissTransientErrorButton != null)
            _dismissTransientErrorButton.Click -= DismissTransientErrorButton_Click;

        _dismissTransientErrorButton = GetTemplateChild(DismissTransientErrorButtonName) as Button;

        if (_dismissTransientErrorButton != null)
            _dismissTransientErrorButton.Click += DismissTransientErrorButton_Click;
    }
#endregion Errors
}

/// <summary>
/// Provides event data and an option to intercept default behavior for taps on the viewport of an <see cref="OrientedImageryViewer"/>.
/// </summary>
public sealed class OrientedImageryViewerImageTappedArgs : OrientedImageTappedEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryViewerImageTappedArgs"/> class.
    /// </summary>
    /// <param name="orientedImageTappedEventArgs">The event arguments supplied by the original <see cref="OrientedImageViewport.ImageTapped"/> event.</param>
    public OrientedImageryViewerImageTappedArgs(OrientedImageTappedEventArgs orientedImageTappedEventArgs) :
        base(orientedImageTappedEventArgs.ImagePoint, orientedImageTappedEventArgs.Image, orientedImageTappedEventArgs.Marker)
    {
        Handled = false;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the default <see cref="OrientedImageryViewer"/> image tap logic should be skipped for the current tap event.
    /// </summary>
    public bool Handled { get; set; }
}

/// <summary>
/// Provides event data and an option to intercept default behavior for taps on the <see cref="GeoView"/> connected to an <see cref="OrientedImageryViewer"/>.
/// </summary>
public sealed class OrientedImageryViewerGeoViewTappedArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryViewerGeoViewTappedArgs"/> class.
    /// </summary>
    /// <param name="geoViewInputEventArgs">The event arguments supplied by the original <see cref="GeoView.GeoViewTapped"/> event.</param>
    public OrientedImageryViewerGeoViewTappedArgs(GeoViewInputEventArgs geoViewInputEventArgs)
    {
        GeoViewInputEventArgs = geoViewInputEventArgs;
    }

    /// <summary>
    /// Gets the event arguments supplied by the original <see cref="GeoView.GeoViewTapped"/> event.
    /// </summary>
    public GeoViewInputEventArgs GeoViewInputEventArgs { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the default <see cref="OrientedImageryViewer"/> GeoView tap logic should be skipped for the current tap event.
    /// </summary>
    public bool Handled { get; set; }
}

#endif