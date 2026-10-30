#if WPF

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;
using Esri.ArcGISRuntime.UI;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Automation;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

/// <summary>
/// Displays oriented imagery and provides controls for managing image selection, footprints, and markers.
/// </summary>
public partial class OrientedImageryView
{
    private const string ImageViewportName = "PART_ImageViewport";
    private const string ToolbarContainerName = "PART_ToolbarContainer";
    private const string PaginatorName = "PART_Paginator";
    private const string ErrorMessagePresenterName = "PART_ErrorMessagePresenter";

    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryView"/> class.
    /// </summary>
    public OrientedImageryView() : base()
    {
        ViewModel = new OrientedImageryViewModel();
        _onImageTapped = OnImageTapped_Default;
        _onGeoViewTapped = GeoViewTapped_Default;

        UpdateErrorMessages();

#if WPF
        Loaded += (_, _) => UpdateErrorMessages();
        IsVisibleChanged += (_, _) => UpdateErrorMessages();
#endif

#if MAUI
        // MAUI layout containers are not tab stops by default, so no IsTabStop is needed here.
        ControlTemplate = DefaultControlTemplate;
#else
        DefaultStyleKey = typeof(OrientedImageryView);
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
        _paginator = GetTemplateChild(PaginatorName) as Paginator;
        _errorMessagePresenter = GetTemplateChild(ErrorMessagePresenterName) as TextBlock;

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
        FocusCloseDetailsButton();
#endif
    }

#region ViewModel
    /// <summary>
    /// Gets or sets the view model for the oriented imagery view.
    /// </summary>
    public OrientedImageryViewModel ViewModel
    {
        get => (OrientedImageryViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ViewModel" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty ViewModelProperty =
        PropertyHelper.CreateProperty<OrientedImageryViewModel?, OrientedImageryView>(nameof(ViewModel), null, (s, oldValue, newValue) => s.OnViewModelChanged(oldValue, newValue));

    private void OnViewModelChanged(OrientedImageryViewModel? oldValue, OrientedImageryViewModel? newValue)
    {
        HasSearchedForImages = false;
        if (oldValue != null)
        {
            oldValue.PropertyChanged -= ViewModel_PropertyChanged;

            if (GeoView?.GraphicsOverlays != null)
                GeoView.GraphicsOverlays.Remove(oldValue.MarkersOverlay);
        }

        if (newValue == null)
        {
            ViewModel = new OrientedImageryViewModel();
            return;
        }

        newValue.PropertyChanged += ViewModel_PropertyChanged;

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
            case nameof(OrientedImageryViewModel.SearchPoint):
            case nameof(OrientedImageryViewModel.SelectedImageFootprint):
                UpdateViewportImage();
                goto case nameof(OrientedImageryViewModel.Images);
            case nameof(OrientedImageryViewModel.Images):
                UpdatePaginatorSelection();
                UpdateImageStateMessages();
                break;
            case nameof(OrientedImageryViewModel.InteractionState):
                UpdateImageStateMessages();
                break;
            case nameof(OrientedImageryViewModel.AutoUpdateFootprint):
                if (_viewport != null)
                    _viewport.AutoUpdateFootprint = ViewModel.AutoUpdateFootprint;
                break;
            case nameof(OrientedImageryViewModel.OrientedImageryLayer):
                HasSearchedForImages = false;
                UpdateErrorMessages();
                break;
#if WPF
            case nameof(OrientedImageryViewModel.SelectedImagePopup):
                FocusCloseDetailsButton();
                break;
#endif
        }
    }
#endregion ViewModel

#region Display
    /// <summary>
    /// Gets a value indicating whether an image selection search has completed after a map tap.
    /// </summary>
    public bool HasSearchedForImages
    {
        get => (bool)GetValue(HasSearchedForImagesProperty);
        private set => SetValue(HasSearchedForImagesPropertyKey, value);
    }

    private static readonly DependencyPropertyKey HasSearchedForImagesPropertyKey =
        PropertyHelper.CreateReadOnlyProperty<bool, OrientedImageryView>(nameof(HasSearchedForImages));

    /// <summary>
    /// Identifies the <see cref="HasSearchedForImages" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty HasSearchedForImagesProperty = PropertyHelper.GetProperty(HasSearchedForImagesPropertyKey);

    private OrientedImageViewport? _viewport;
    private Action<object?, OrientedImageTappedEventArgs>? _onImageTappedOverride;
    private Action<object?, OrientedImageTappedEventArgs> _onImageTapped;

    /// <summary>
    /// Set this function to override the default event handler when the image is tapped. A <c>null</c> value means the default behavior is active.
    /// </summary>
    /// <remarks>Toolkit-managed marker hits raise this event with <see cref="OrientedImageTappedEventArgs.Marker"/> set to <c>null</c>.</remarks>
    public Action<object?, OrientedImageTappedEventArgs>? OnImageTappedOverride
    {
        get => _onImageTappedOverride;

        set
        {
            if (value == null)
                _onImageTapped = OnImageTapped_Default;
            else
                _onImageTapped = value;

            _onImageTappedOverride = value;
        }
    }

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
        PropertyHelper.CreateProperty<System.Drawing.Color, OrientedImageryView>(nameof(DisplayBackgroundColor), System.Drawing.Color.White, (s, oldValue, newValue) => s.UpdateDisplayBackgroundColor(newValue));

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

    private void Viewport_ImageTapped(object? sender, OrientedImageTappedEventArgs e) => _onImageTapped(this, ViewModel.GetPublicImageTappedEventArgs(e));

    private void Viewport_StateChanged(object? sender, EventArgs e) => UpdateErrorMessages();

    private async void OnImageTapped_Default(object? _, OrientedImageTappedEventArgs e)
    {
        // Only adds a marker if sketch mode is enabled and there is not already a marker nearby
        if (ViewModel.InteractionState != OrientedImageryViewInteractionState.AddMarkers || e.Marker != null)
            return;

        try
        {
            var location = await e.Image.ImageToLocationAsync(e.ImagePoint);
            ViewModel.AddMarkerLocation(location);
        }
        catch (ArcGISRuntimeException ex)
        {
            Debug.WriteLine($"Error converting image point to location: {ex.Message}");
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

            ViewModel.AreFootprintsEnabled = false;
            _viewport.StateChanged += Viewport_StateChanged_UpdateFootprint;
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
            ViewModel.AreFootprintsEnabled = true;
            _viewport.StateChanged -= Viewport_StateChanged_UpdateFootprint;
        }
    }
#endregion Display

#region GeoView
    private Action<object?, GeoViewInputEventArgs>? _onGeoViewTappedOverride;
    private Action<object?, GeoViewInputEventArgs> _onGeoViewTapped;

    /// <summary>
    /// Gets or sets the <see cref="Esri.ArcGISRuntime.UI.Controls.GeoView"/> on which marker graphics are displayed.
    /// </summary>
    /// <remarks>
    /// Setting <see cref="GeoView"/> to <c>null</c> will clear all markers and footprints from the geo view and set
    /// <see cref="OrientedImageryViewModel.OrientedImageryLayer"/>  on <see cref="ViewModel"/> to <c>null</c>.
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
        PropertyHelper.CreateProperty<GeoView?, OrientedImageryView>(nameof(GeoView), null, (s, oldValue, newValue) => s.UpdateGeoView(oldValue, newValue));

    private void UpdateGeoView(GeoView? oldGeoView, GeoView? newGeoView)
    {
        if (oldGeoView == newGeoView) return;

        if (oldGeoView != null)
        {
            oldGeoView.GraphicsOverlays?.Remove(ViewModel.MarkersOverlay);
            oldGeoView.GeoViewTapped -= GeoView_GeoViewTapped;
        }

        if (newGeoView != null)
        {
            if (newGeoView.GraphicsOverlays == null)
                newGeoView.GraphicsOverlays = new GraphicsOverlayCollection();
            newGeoView.GraphicsOverlays.Add(ViewModel.MarkersOverlay);
            newGeoView.GeoViewTapped += GeoView_GeoViewTapped;
        }

        UpdateErrorMessages();
    }

    /// <summary>
    /// Set this function to override the default event handler when the connected <see cref="GeoView"/> is tapped. A <c>null</c> value means the default behavior is active.
    /// </summary>
    public Action<object?, GeoViewInputEventArgs>? OnGeoViewTappedOverride
    {
        get => _onGeoViewTappedOverride;

        set
        {
            if (value == null)
                _onGeoViewTapped = GeoViewTapped_Default;
            else
                _onGeoViewTapped = value;

            _onGeoViewTappedOverride = value;
        }
    }

    private async void GeoView_GeoViewTapped(object? sender, GeoViewInputEventArgs e) => _onGeoViewTapped(this, e);

    private async void GeoViewTapped_Default(object? _, GeoViewInputEventArgs e)
    {
        if (e.Location == null || GeoView == null || ViewModel.OrientedImageryLayer == null)
            return;

        switch (ViewModel.InteractionState)
        {
            case OrientedImageryViewInteractionState.AddMarkers:
                ViewModel.AddMarkerLocation(e.Location);
                break;

            case OrientedImageryViewInteractionState.FindImages:
                var parameters = new OrientedImageSearchParameters() { MaxResults = -1 };
                var images = await ViewModel.OrientedImageryLayer.SearchImagesAsync(e.Location, parameters) ?? new List<OrientedImage>();

                _noImagesAnnouncementCanRepeat = true;
                HasSearchedForImages = true;

                ViewModel.SetImages(images.ToList(), e.Location);
                ViewModel.SelectedImage = images.Count < 1 ? null : images[0];
                ViewModel.IsSequentialNavigationEnabled = false;
                break;

            case OrientedImageryViewInteractionState.DisplayImage:
                var identifyResult = await GeoView.IdentifyLayerAsync(ViewModel.OrientedImageryLayer, e.Position, 0, false);

                _noImagesAnnouncementCanRepeat = true;
                HasSearchedForImages = true;

                if (identifyResult.GeoElements.Count > 0 && identifyResult.GeoElements[0] is Feature feature)
                {
                    var image = await ViewModel.OrientedImageryLayer.FetchImageForFeatureAsync(feature);
                    ViewModel.SetImages([image]);
                    ViewModel.SelectedImage = image;
                }
                else
                {
                    ViewModel.SetImages([], e.Location);
                    ViewModel.SelectedImage = null;
                }
                _noImagesAnnouncementCanRepeat = true;
                break;

            default:
                return;
        }
    }
#endregion GeoView

#region Toolbar
    private ItemsControl? _toolbarContainer;
    private OrientedImageryViewToolbarSelector? _defaultItemTemplateSelector;

    /// <summary>
    /// Gets or sets the <see cref="DataTemplateSelector"/> used by the toolbar <see cref="ItemsControl"/> to display the toolbar categories and
    /// items in <see cref="OrientedImageryViewModel.ToolbarCategories"/>.
    /// </summary>
    public DataTemplateSelector? ToolbarItemTemplateSelector
    {
        get => (DataTemplateSelector)GetValue(ToolbarItemTemplateSelectorProperty);
        set => SetValue(ToolbarItemTemplateSelectorProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ToolbarItemTemplateSelector" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty ToolbarItemTemplateSelectorProperty =
        PropertyHelper.CreateProperty<DataTemplateSelector?, OrientedImageryView>(nameof(ToolbarItemTemplateSelector), null, (s, oldValue, newValue) => s.OnItemTemplateSelectorChanged(oldValue, newValue));

    private void OnItemTemplateSelectorChanged(DataTemplateSelector? oldItemTemplateSelector, DataTemplateSelector? newItemTemplateSelector)
    {
        if (newItemTemplateSelector is not OrientedImageryViewToolbarSelector newSelector)
        {
            return;
        }

        // Save and in the future merge the default selector so the default styles are always available unless overridden.
        if (ReadLocalValue(ToolbarItemTemplateSelectorProperty) == DependencyProperty.UnsetValue)
        {
            _defaultItemTemplateSelector = newSelector;
        }
        else if (_defaultItemTemplateSelector is not null)
        {
            newSelector.Merge(_defaultItemTemplateSelector);
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
    private Paginator? _paginator;

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

    private void Paginator_SelectedPageIndexChanged(Paginator sender, int newPageIndex)
    {
        if (newPageIndex >= 0 && newPageIndex < ViewModel.Images.Count)
            ViewModel.SelectedImage = ViewModel.Images[newPageIndex];
    }
#endregion Pagination

#region Errors
    private const string NoGeoViewMessageKey = "OrientedImageryViewNoGeoView";
    private const string NoLayerSelectedKey = "OrientedImageryViewNoLayerSelected";
    private const string NoImagesNoneKey = "OrientedImageryViewImageSelectionNoImagesNone";
    private const string NoImagesFindKey = "OrientedImageryViewImageSelectionNoImagesFind";
    private const string NoImagesDisplayKey = "OrientedImageryViewImageSelectionNoImagesDisplay";
    private const string NoImagesForLocationKey = "OrientedImageryViewNoImagesFound";
    private const string ImageLoadingKey = "OrientedImageryViewImageLoading";
    private const string ImageLoadedKey = "OrientedImageryViewImageLoaded";
    private const string ImageStateActivityId = "OrientedImageryViewImageState";
    private bool _noImagesAnnouncementCanRepeat;
    private string? _lastNoImagesAnnouncement;
    private string? _lastImageErrorAnnouncement;
    private bool _lastLoadingState = false;
    private bool _imageChangedSincePreviousLoadAnnouncement = true;
    private TextBlock? _errorMessagePresenter;

    /// <summary>
    /// The message shown when the <see cref="OrientedImageryView"/> is in an invalid state.
    /// </summary>
    public string? ErrorMessage
    {
        get => (string?)GetValue(ErrorMessageProperty);
        private set => SetValue(ErrorMessagePropertyKey, value);
    }

    private static readonly DependencyPropertyKey ErrorMessagePropertyKey =
        PropertyHelper.CreateReadOnlyProperty<string?, OrientedImageryView>(nameof(ErrorMessage));

    /// <summary>
    /// Identifies the <see cref="ErrorMessage" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty ErrorMessageProperty = PropertyHelper.GetProperty(ErrorMessagePropertyKey);

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

        ScreenReaderHelpers.RaiseLiveRegionChanged(_errorMessagePresenter);
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

        bool isLoading = _viewport.IsBusy;
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
        if (!IsLoaded || !IsVisible || ErrorMessage != null || _viewport?.IsBusy == true || ViewModel == null || ViewModel.SelectedImage != null || ViewModel.Images.Count > 0)
            return null;

        string resourceKey;
        if (HasSearchedForImages)
            resourceKey = NoImagesForLocationKey;
        else if (ViewModel.InteractionState == OrientedImageryViewInteractionState.FindImages)
            resourceKey = NoImagesFindKey;
        else if (ViewModel.InteractionState == OrientedImageryViewInteractionState.DisplayImage)
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
#endregion Errors
}

#endif