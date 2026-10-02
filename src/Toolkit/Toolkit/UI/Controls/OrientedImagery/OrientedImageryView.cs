#if WPF || WINDOWS_XAML

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
#if WPF
using Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;
#elif WINDOWS_XAML
using Paginator = Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImageryPaginator;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

/// <summary>
/// Displays oriented imagery and provides controls for managing image selection, footprints, and markers.
/// </summary>
public partial class OrientedImageryView
{
    private const string ImageDisplayName = "PART_ImageDisplay";
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

        _orientedImageryLayers = new ResettableObservableCollection<OrientedImageryLayer>();
        _readOnlyOrientedImageryLayers = new ReadOnlyObservableCollection<OrientedImageryLayer>(_orientedImageryLayers);

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

        if (_display != null)
            UnwireDisplay(_display);
        if (_toolbarContainer != null)
            UnwireToolbarContainer(_toolbarContainer);
        if (_paginator != null)
            _paginator.SelectedPageIndexChanged -= Paginator_SelectedPageIndexChanged;

        _display = GetTemplateChild(ImageDisplayName) as OrientedImageDisplay;
        _toolbarContainer = GetTemplateChild(ToolbarContainerName) as ItemsControl;
        _paginator = GetTemplateChild(PaginatorName) as Paginator;

        if (_display != null)
            WireDisplay(_display);
        if (_toolbarContainer != null)
            WireToolbarContainer(_toolbarContainer);
        if (_paginator != null)
            _paginator.SelectedPageIndexChanged += Paginator_SelectedPageIndexChanged;

        RewireViewModel();
        UpdatePaginatorSelection();
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
        if (oldValue != null)
        {
            oldValue.PropertyChanged -= ViewModel_PropertyChanged;

            if (GeoView?.GraphicsOverlays != null)
                GeoView.GraphicsOverlays.Remove(oldValue.MarkersOverlay);
        }

        if (newValue == null)
        {
            SetValue(ViewModelProperty, new OrientedImageryViewModel());
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
    }

    private void RewireViewModel()
    {
        if (_display != null)
        {
            _display.AutoUpdateFootprint = ViewModel.AutoUpdateFootprint;
            _display.Markers = ViewModel.DisplayMarkers;
            _display.Footprint = ViewModel.SelectedImageFootprint;
        }
        if (_toolbarContainer != null)
            _toolbarContainer.ItemsSource = ViewModel.ToolbarItems;

        UpdatePaginatorSelection();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(OrientedImageryViewModel.SelectedImageFootprint):
                if (_display != null)
                    _display.Footprint = ViewModel.SelectedImageFootprint;
                break;
            case nameof(OrientedImageryViewModel.SelectedImage):
            case nameof(OrientedImageryViewModel.Images):
            case nameof(OrientedImageryViewModel.IsSequentialNavigationEnabled):
                UpdatePaginatorSelection();
                break;
            case nameof(OrientedImageryViewModel.AutoUpdateFootprint):
                if (_display != null)
                    _display.AutoUpdateFootprint = ViewModel.AutoUpdateFootprint;
                break;
            case nameof(OrientedImageryViewModel.OrientedImageryLayer):
                SelectedLayer = ViewModel.OrientedImageryLayer;
                break;
        }
    }
#endregion ViewModel

#region Display
    private OrientedImageDisplay? _display;
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
    /// <remarks>On WinUI, the default <see cref="System.Drawing.Color.Empty"/> follows the active theme.
    /// Set an explicit color to override it, or restore <see cref="System.Drawing.Color.Empty"/> to follow the theme again.</remarks>
    public System.Drawing.Color DisplayBackgroundColor
    {
        get => (System.Drawing.Color)GetValue(DisplayBackgroundColorProperty);
        set => SetValue(DisplayBackgroundColorProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="DisplayBackgroundColor" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty DisplayBackgroundColorProperty =
#if WINDOWS_XAML
        PropertyHelper.CreateProperty<System.Drawing.Color, OrientedImageryView>(nameof(DisplayBackgroundColor), System.Drawing.Color.Empty, (s, oldValue, newValue) => s.UpdateDisplayBackgroundColor(newValue));
#else
        PropertyHelper.CreateProperty<System.Drawing.Color, OrientedImageryView>(nameof(DisplayBackgroundColor), System.Drawing.Color.White, (s, oldValue, newValue) => s.UpdateDisplayBackgroundColor(newValue));
#endif

    private void WireDisplay(OrientedImageDisplay display)
    {
        display.ImageTapped += Display_ImageTapped;

#if WINDOWS_XAML
        if (_display != null)
            WireDisplay_WinUI(_display);
#endif
    }

    private void UnwireDisplay(OrientedImageDisplay display)
    {
        display.ImageTapped -= Display_ImageTapped;
        display.ClearValue(OrientedImageDisplay.AutoUpdateFootprintProperty);
        display.ClearValue(OrientedImageDisplay.MarkersProperty);
        display.ClearValue(OrientedImageDisplay.FootprintProperty);
        display.ClearValue(OrientedImageDisplay.DisplayBackgroundColorProperty);

#if WINDOWS_XAML
        if (_display != null)
            UnwireDisplay_WinUI(_display);
#endif
    }

    private void UpdateDisplayBackgroundColor(System.Drawing.Color displayBackgroundColor)
    {
#if WINDOWS_XAML
        if (displayBackgroundColor.IsEmpty)
        {
            var color = ThemeDisplayBackgroundBrush?.Color ?? default;
            displayBackgroundColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
#endif
        if (_display != null)
            _display.DisplayBackgroundColor = displayBackgroundColor;
    }

    private void Display_ImageTapped(object? sender, OrientedImageTappedEventArgs e) => _onImageTapped(this, ViewModel.GetPublicImageTappedEventArgs(e));

    private async void OnImageTapped_Default(object? _, OrientedImageTappedEventArgs e)
    {
        // Only adds a marker if sketch mode is enabled and there is not already a marker nearby
        if (!ViewModel.SketchModeEnabled || e.Marker != null)
            return;

        try
        {
            var location = await e.Image.ImageToLocationAsync(e.ImagePoint);
            ViewModel.AddMarkerLocation(location);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error converting image point to location: {ex.Message}");
        }
    }
#endregion Display

#region GeoView
    private GeoModel? _currentGeoModel;
    private LayerCollection? _currentOperationalLayers;
    private ResettableObservableCollection<OrientedImageryLayer> _orientedImageryLayers;
    private ReadOnlyObservableCollection<OrientedImageryLayer> _readOnlyOrientedImageryLayers;
    private Action<object?, GeoViewInputEventArgs>? _onGeoViewTappedOverride;
    private Action<object?, GeoViewInputEventArgs> _onGeoViewTapped;

    /// <summary>
    /// Gets or sets the <see cref="Esri.ArcGISRuntime.UI.Controls.GeoView"/> on which marker graphics are displayed.
    /// </summary>
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
            ((INotifyPropertyChanged)oldGeoView).PropertyChanged -= GeoView_PropertyChanged;
        }

        if (newGeoView != null)
        {
            if (newGeoView.GraphicsOverlays == null)
                newGeoView.GraphicsOverlays = new GraphicsOverlayCollection();
            newGeoView.GraphicsOverlays.Add(ViewModel.MarkersOverlay);
            newGeoView.GeoViewTapped += GeoView_GeoViewTapped;
            ((INotifyPropertyChanged)newGeoView).PropertyChanged += GeoView_PropertyChanged;
        }

        UpdateCurrentGeoModel();
    }

    /// <summary>
    /// The <see cref="OrientedImageryLayer"/> currently selected for inspection. The selected layer must be present
    /// in <see cref="OrientedImageryLayers"/> or the <see cref="OrientedImageryView"/> will not function correctly.
    /// </summary>
    /// <remarks>
    /// Layers will be made visible when assigned as the <see cref="SelectedLayer"/>.
    /// </remarks>
    public OrientedImageryLayer? SelectedLayer
    {
        get => (OrientedImageryLayer?)GetValue(SelectedLayerProperty);
        set => SetValue(SelectedLayerProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="GeoView"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectedLayerProperty =
        PropertyHelper.CreateProperty<OrientedImageryLayer?, OrientedImageryView>(nameof(SelectedLayer), null, (s, oldValue, newValue) => s.UpdateSelectedLayer(oldValue, newValue));

    private void UpdateSelectedLayer(OrientedImageryLayer? oldLayer, OrientedImageryLayer? newLayer)
    {
        if (ViewModel.OrientedImageryLayer != newLayer)
            ViewModel.OrientedImageryLayer = newLayer;

#if WPF
        UpdateErrorMessage();
#endif

        if (GeoView != null && newLayer != null)
        {
            if (newLayer.LoadStatus != LoadStatus.Loaded)
                newLayer.Loaded += NewLayer_Loaded;
            else
                ZoomToLayer();
        }
    }

    private void NewLayer_Loaded(object? sender, EventArgs e)
    {
        this.Dispatch(ZoomToLayer);
        if (sender is ILoadable loadable)
            loadable.Loaded -= NewLayer_Loaded;
    }

    private void ZoomToLayer()
    {
        if (GeoView != null &&
            ViewModel.OrientedImageryLayer?.LoadStatus == LoadStatus.Loaded &&
            ViewModel.OrientedImageryLayer.FullExtent is Envelope extent)
            GeoView.SetViewpoint(new Viewpoint(extent));
    }

    /// <summary>
    /// A read-only collection of <see cref="OrientedImageryLayer"/>s held in the <see cref="GeoModel.OperationalLayers"/>
    /// of the connected <see cref="GeoView"/>'s geo model.
    /// </summary>
    public ReadOnlyObservableCollection<OrientedImageryLayer> OrientedImageryLayers => _readOnlyOrientedImageryLayers;

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

        if (ViewModel.SketchModeEnabled)
        {
            ViewModel.AddMarkerLocation(e.Location);
            return;
        }

        if (ViewModel.ImageSearchEnabled)
        {
            var parameters = new OrientedImageSearchParameters() { MaxResults = -1 };
            var images = await ViewModel.OrientedImageryLayer.SearchImagesAsync(e.Location, parameters) ?? new List<OrientedImage>();
            ViewModel.SetImages(images.ToList(), e.Location);
            ViewModel.SelectedImage = images.Count < 1 ? null : images[0];
        }
        else
        {
            var identifyResult = await GeoView.IdentifyLayerAsync(ViewModel.OrientedImageryLayer, e.Position, 0, false);
            if (identifyResult.GeoElements.Count > 0 && identifyResult.GeoElements[0] is Feature feature)
            {
                var image = await ViewModel.OrientedImageryLayer.FetchImageForFeatureAsync(feature);
                ViewModel.SetImages([image]);
                ViewModel.SelectedImage = image;
            }
        }
    }

    private void GeoView_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Scene" || e.PropertyName == "Map")
        {
            UpdateCurrentGeoModel();
        }
    }

    private void UpdateCurrentGeoModel()
    {
        if (_currentGeoModel != null)
        {
            _currentGeoModel.PropertyChanged -= _currentGeoModel_PropertyChanged;
        }

        if (GeoView is SceneView sceneView)
            _currentGeoModel = sceneView.Scene;
        else if (GeoView is MapView mapView)
            _currentGeoModel = mapView.Map;
        else
            _currentGeoModel = null;

        if (_currentGeoModel != null)
            _currentGeoModel.PropertyChanged += _currentGeoModel_PropertyChanged;

        UpdateCurrentOperationalLayers();
    }

    private void _currentGeoModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GeoModel.OperationalLayers))
        {
            UpdateCurrentOperationalLayers();
        }
    }

    private void UpdateCurrentOperationalLayers()
    {
        if (_currentOperationalLayers != null)
            _currentOperationalLayers.CollectionChanged -= _currentOperationalLayers_CollectionChanged;

        if (GeoView is SceneView sceneView && sceneView.Scene != null)
            _currentOperationalLayers = sceneView.Scene.OperationalLayers;
        else if (GeoView is MapView mapView && mapView.Map != null)
            _currentOperationalLayers = mapView.Map.OperationalLayers;
        else
            _currentOperationalLayers = null;

        if (_currentOperationalLayers != null)
            _currentOperationalLayers.CollectionChanged += _currentOperationalLayers_CollectionChanged;

        ResetOrientedImageryLayers();
    }

    private void _currentOperationalLayers_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => ResetOrientedImageryLayers();

    private void ResetOrientedImageryLayers()
    {
        if (GeoView != null)
        {
            var currentLayers = new List<OrientedImageryLayer>();
            foreach (var layer in _currentOperationalLayers ?? [])
            {
                if (layer is OrientedImageryLayer oiLayer)
                    currentLayers.Add(oiLayer);
            }
            _orientedImageryLayers.ReplaceAll(currentLayers);

            if (_orientedImageryLayers.Count < 1)
                SelectedLayer = null;
        }

#if WPF
        UpdateErrorMessage();
#endif
    }
#endregion GeoView

#region Toolbar
    private ItemsControl? _toolbarContainer;
    private OrientedImageryViewTemplateSelector? _defaultItemTemplateSelector;

    /// <summary>
    /// Gets or sets the <see cref="DataTemplateSelector"/> used by the toolbar <see cref="ItemsControl"/> to display the toolbar items in
    /// <see cref="OrientedImageryViewModel.ToolbarItems"/>.
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
        if (newItemTemplateSelector is not OrientedImageryViewTemplateSelector newSelector)
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
        toolbarContainer.ItemsSource = ViewModel?.ToolbarItems;
#if WINDOWS_XAML
        WireToolbarWinUI(toolbarContainer);
#endif
    }

    private void UnwireToolbarContainer(ItemsControl toolbarContainer)
    {
        toolbarContainer.ClearValue(ItemsControl.ItemTemplateSelectorProperty);
        toolbarContainer.ClearValue(ItemsControl.ItemsSourceProperty);
#if WINDOWS_XAML
        UnwireToolbarWinUI(toolbarContainer);
#endif
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

#if WINDOWS_XAML
    UpdateNavigationVisibility_WinUI();
#endif
    }

    private void Paginator_SelectedPageIndexChanged(Paginator sender, int newPageIndex)
    {
        if (newPageIndex >= 0 && newPageIndex < ViewModel.Images.Count)
            ViewModel.SelectedImage = ViewModel.Images[newPageIndex];
    }
#endregion Pagination

#region Error
    private const string NoGeoViewMessageKey = "OrientedImageryViewNoGeoView";
    private const string NoOrientedImageryLayersKey = "OrientedImageryViewNoOrientedImageryLayers";
    private const string NoLayerSelectedKey = "OrientedImageryViewNoLayerSelected";
    private const string SelectedLayerNotOnGeoViewKey = "OrientedImageryViewSelectedLayerNotOnGeoView";

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
        else if (_orientedImageryLayers.Count < 1)
            ErrorMessage = Properties.Resources.GetString(NoOrientedImageryLayersKey);
        else if (SelectedLayer == null)
            ErrorMessage = Properties.Resources.GetString(NoLayerSelectedKey);
        else if (!_orientedImageryLayers.Contains(SelectedLayer))
            ErrorMessage = Properties.Resources.GetString(SelectedLayerNotOnGeoViewKey);
        else
            ErrorMessage = null;
    }
#endregion Error
}

#endif
