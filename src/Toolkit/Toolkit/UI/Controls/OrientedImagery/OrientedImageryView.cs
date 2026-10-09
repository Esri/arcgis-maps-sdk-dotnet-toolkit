#if WPF || WINDOWS_XAML

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using System.ComponentModel;
using System.Diagnostics;
#if WPF
using Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

/// <summary>
/// Displays oriented imagery and provides controls for managing image selection, footprints, and markers.
/// </summary>
public partial class OrientedImageryView
{
    private const string ImageViewportName = "PART_ImageViewport";
    private const string ToolbarContainerName = "PART_ToolbarContainer";
    private const string PaginatorName = "PART_Paginator";

    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryView"/> class.
    /// </summary>
    public OrientedImageryView() : base()
    {
        ViewModel = new OrientedImageryViewModel();
        _onImageTapped = OnImageTapped_Default;
        _onGeoViewTapped = GeoViewTapped_Default;

#if WPF
        UpdateErrorMessage();
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
#if WPF
        if (_toolbarContainer != null)
            UnwireToolbarContainer(_toolbarContainer);
        if (_paginator != null)
            _paginator.SelectedPageIndexChanged -= Paginator_SelectedPageIndexChanged;
#endif

        _viewport = GetTemplateChild(ImageViewportName) as OrientedImageViewport;
        _toolbarContainer = GetTemplateChild(ToolbarContainerName) as ItemsControl;
#if WPF
        _paginator = GetTemplateChild(PaginatorName) as Paginator;
#endif

        if (_viewport != null)
            WireViewport(_viewport);
#if WPF
        if (_toolbarContainer != null)
            WireToolbarContainer(_toolbarContainer);
        if (_paginator != null)
            _paginator.SelectedPageIndexChanged += Paginator_SelectedPageIndexChanged;
#endif
        RewireViewModel();
#if WPF
        UpdatePaginatorSelection();
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
#if WPF
        UpdateErrorMessage();
        UpdatePaginatorSelection();
#endif

#if WINDOWS_XAML
        // Temporary workaround to avoid doing full toolbar implementation
        MarkerSymbolPicker = ViewModel.ToolbarCategories.Single(c => c.Name == OrientedImageryToolbarCategory.MarkersCategoryName).ToolbarItems.OfType<SelectNewMarkerSymbolVM>().Single();
        CameraMarkers = ViewModel.ToolbarCategories.Single(c => c.Name == OrientedImageryToolbarCategory.OverlaysCategoryName).ToolbarItems.OfType<ShowCameraMarkersVM>().Single();
        AllowAddingMarkers = ViewModel.ToolbarCategories.Single(c => c.Name == OrientedImageryToolbarCategory.MarkersCategoryName).ToolbarItems.OfType<AllowAddingMarkersVM>().Single();
#endif
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
#if WPF
        UpdatePaginatorSelection();
#endif
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
#if WPF
                UpdatePaginatorSelection();
                break;
#endif
            case nameof(OrientedImageryViewModel.AutoUpdateFootprint):
                if (_viewport != null)
                    _viewport.AutoUpdateFootprint = ViewModel.AutoUpdateFootprint;
                break;
            case nameof(OrientedImageryViewModel.OrientedImageryLayer):
                HasSearchedForImages = false;
#if WPF
                UpdateErrorMessage();
#endif
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
        private set => SetValue(HasSearchedForImagesProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="HasSearchedForImages" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty HasSearchedForImagesProperty =
        PropertyHelper.CreateProperty<bool, OrientedImageryView>(nameof(HasSearchedForImages), false);

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
#if WINDOWS_XAML
        WireViewport_WinUI(viewport);
#endif
    }

    private void UnwireViewport(OrientedImageViewport viewport)
    {
        viewport.ImageTapped -= Viewport_ImageTapped;
        viewport.ClearValue(OrientedImageViewport.AutoUpdateFootprintProperty);
        viewport.ClearValue(OrientedImageViewport.MarkersProperty);
        viewport.ClearValue(OrientedImageViewport.FootprintProperty);
        viewport.ClearValue(OrientedImageViewport.DisplayBackgroundColorProperty);
#if WINDOWS_XAML
        UnwireViewport_WinUI(viewport);
#endif
    }

    private void UpdateDisplayBackgroundColor(System.Drawing.Color displayBackgroundColor)
    {
        if (_viewport != null)
            _viewport.DisplayBackgroundColor = displayBackgroundColor;
    }

    private void Viewport_ImageTapped(object? sender, OrientedImageTappedEventArgs e) => _onImageTapped(this, ViewModel.GetPublicImageTappedEventArgs(e));

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
            _viewport.StateChanged += Viewport_StateChanged;
        }
    }

    private void Viewport_StateChanged(object? sender, EventArgs e)
    {
        if (sender is not OrientedImageViewport viewport)
            return;

        if (viewport != _viewport)
        {
            viewport.StateChanged -= Viewport_StateChanged;
        }
        else if (viewport.IsInteractive == true)
        {
            ViewModel.AreFootprintsEnabled = true;
            _viewport.StateChanged -= Viewport_StateChanged;
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

#if WPF
        UpdateErrorMessage();
#endif
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
                ViewModel.SetImages(images.ToList(), e.Location);
                ViewModel.SelectedImage = images.Count < 1 ? null : images[0];
                ViewModel.IsSequentialNavigationEnabled = false;
                HasSearchedForImages = true;
                break;

            case OrientedImageryViewInteractionState.DisplayImage:
                var identifyResult = await GeoView.IdentifyLayerAsync(ViewModel.OrientedImageryLayer, e.Position, 0, false);
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
                HasSearchedForImages = true;
                break;

            default:
                return;
        }
    }
#endregion GeoView

#region Toolbar
    private ItemsControl? _toolbarContainer;

#if WPF
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
#endif
#endregion Toolbar

#if WPF
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

#region Error
    private const string NoGeoViewMessageKey = "OrientedImageryViewNoGeoView";
    private const string NoLayerSelectedKey = "OrientedImageryViewNoLayerSelected";

    /// <summary>
    /// The message shown when the <see cref="OrientedImageryView"/> is in an invalid state.
    /// </summary>
    public string? ErrorMessage
    {
        get => (string?)GetValue(ErrorMessageProperty);
        private set => SetValue(ErrorMessageProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ErrorMessage" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty ErrorMessageProperty =
        PropertyHelper.CreateProperty<string?, OrientedImageryView>(nameof(ErrorMessage), null);

    private void UpdateErrorMessage()
    {
        if (GeoView == null)
            ErrorMessage = Properties.Resources.GetString(NoGeoViewMessageKey);
        else if (ViewModel.OrientedImageryLayer == null)
            ErrorMessage = Properties.Resources.GetString(NoLayerSelectedKey);
        else
            ErrorMessage = null;
    }
#endregion Error
#endif
}

#endif
