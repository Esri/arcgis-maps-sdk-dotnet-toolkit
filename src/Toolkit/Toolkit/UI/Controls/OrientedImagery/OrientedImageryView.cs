#if WPF

using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;
using Esri.ArcGISRuntime.UI;

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
        RefreshFromViewModel();
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
            oldValue.SetSelectedImageReady(false);
            oldValue.PropertyChanged -= ViewModel_PropertyChanged;

            if (GeoView?.GraphicsOverlays != null)
                GeoView.GraphicsOverlays.Remove(oldValue.MarkersOverlay);
        }

        if (newValue == null)
        {
            SetCurrentValue(ViewModelProperty, new OrientedImageryViewModel());
            return;
        }

        newValue.PropertyChanged += ViewModel_PropertyChanged;

        if (GeoView != null)
        {
            if (GeoView.GraphicsOverlays == null)
                GeoView.GraphicsOverlays = new GraphicsOverlayCollection();
            GeoView.GraphicsOverlays.Add(newValue.MarkersOverlay);
        }

        RefreshFromViewModel();
    }

    private void RefreshFromViewModel()
    {
        if (_display != null)
        {
            _display.AutoUpdateFootprint = ViewModel.AutoUpdateFootprint;
            _display.Markers = ViewModel.DisplayMarkers;
            _display.Footprint = ViewModel.SelectedImageFootprint;
            UpdateDisplayBackgroundColor(DisplayBackgroundColor);
            UpdateSelectedImageReady();
        }
        if (_toolbarContainer != null)
            _toolbarContainer.ItemsSource = ViewModel.ToolbarItems;
        UpdatePaginatorSelection();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(OrientedImageryViewModel.SelectedImageFootprint):
                if (_display != null)
                {
                    _display.Footprint = ViewModel.SelectedImageFootprint;
                    UpdateSelectedImageReady();
                }
                break;
            case nameof(OrientedImageryViewModel.SelectedImage):
            case nameof(OrientedImageryViewModel.Images):
                UpdatePaginatorSelection();
                break;
            case nameof(OrientedImageryViewModel.AutoUpdateFootprint):
                if (_display != null)
                    _display.AutoUpdateFootprint = ViewModel.AutoUpdateFootprint;
                break;
        }
    }
#endregion ViewModel

#region Display
    private OrientedImageDisplay? _display;

    /// <summary>
    /// Occurs whenever a user taps on the image display.
    /// </summary>
    /// <remarks>Toolkit-managed marker hits raise this event with <c>Marker</c> set to <c>null</c>.</remarks>
    public event EventHandler<OrientedImageTappedEventArgs>? ImageTapped;

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

    private void WireDisplay(OrientedImageDisplay display)
    {
        display.ImageTapped += Display_ImageTapped;
        display.StateChanged += Display_StateChanged;
    }

    private void UnwireDisplay(OrientedImageDisplay display)
    {
        display.ImageTapped -= Display_ImageTapped;
        display.StateChanged -= Display_StateChanged;
        ViewModel.SetSelectedImageReady(false);
        display.ClearValue(OrientedImageDisplay.AutoUpdateFootprintProperty);
        display.ClearValue(OrientedImageDisplay.MarkersProperty);
        display.ClearValue(OrientedImageDisplay.FootprintProperty);
        display.ClearValue(OrientedImageDisplay.DisplayBackgroundColorProperty);
    }

    private void Display_StateChanged(object? sender, EventArgs e) => UpdateSelectedImageReady();

    private void UpdateSelectedImageReady() => ViewModel.SetSelectedImageReady(
        _display?.IsInteractive == true && ViewModel.SelectedImage != null && _display.Footprint?.OrientedImage == ViewModel.SelectedImage);

    private void Display_ImageTapped(object? sender, OrientedImageTappedEventArgs e) =>
        ImageTapped?.Invoke(this, ViewModel.GetPublicImageTappedEventArgs(e));

    private void UpdateDisplayBackgroundColor(System.Drawing.Color displayBackgroundColor)
    {
        if (_display != null)
            _display.DisplayBackgroundColor = displayBackgroundColor;
    }
#endregion Display

#region GeoView
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
        }

        if (newGeoView != null)
        {
            if (newGeoView.GraphicsOverlays == null)
                newGeoView.GraphicsOverlays = new GraphicsOverlayCollection();
            newGeoView.GraphicsOverlays.Add(ViewModel.MarkersOverlay);
        }
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
        _paginator.SetCurrentValue(Paginator.CurrentPageNumberProperty, index);
    }

    private void Paginator_SelectedPageIndexChanged(Paginator sender, int newPageIndex)
    {
        if (newPageIndex >= 0 && newPageIndex < ViewModel.Images.Count)
            ViewModel.SelectedImage = ViewModel.Images[newPageIndex];
    }
#endregion
}

#endif