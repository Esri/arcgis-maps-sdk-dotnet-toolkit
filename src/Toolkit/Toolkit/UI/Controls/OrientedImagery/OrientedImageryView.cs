#if WPF

using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

/// <summary>
/// Displays oriented imagery and provides controls for managing image selection, footprints, and markers.
/// </summary>
public partial class OrientedImageryView
{
    private const string ImageDisplayName = "PART_ImageDisplay";
    private const string ToolbarContainerName = "PART_ToolbarContainer";

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

        _display = GetTemplateChild(ImageDisplayName) as OrientedImageDisplay;
        _toolbarContainer = GetTemplateChild(ToolbarContainerName) as ItemsControl;

        if (_display != null)
            WireDisplay(_display);
        if (_toolbarContainer != null)
            WireToolbarContainer(_toolbarContainer);
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

        if (_display != null)
            WireDisplayToViewModel(_display);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(OrientedImageryViewModel.SelectedImageFootprint):
                if (_display != null)
                    _display.Footprint = ViewModel.SelectedImageFootprint;
                break;
            case nameof(OrientedImageryViewModel.AutoUpdateFootprint):
                if (_display != null)
                    _display.AutoUpdateFootprint = ViewModel.AutoUpdateFootprint;
                break;
            case nameof(OrientedImageryViewModel.Markers):
                if (_display != null)
                    _display.Markers = ViewModel.Markers;
                break;
        }
    }
#endregion ViewModel

#region Display
    private OrientedImageDisplay? _display;

    /// <summary>
    /// Occurs whenever a user taps on the image display.
    /// </summary>
    public event EventHandler<OrientedImageDisplay.ImageClickedEventArgs>? ImageTapped;

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
        display.ImageClicked += Display_ImageClicked;
        display.AutoUpdateFootprint = ViewModel.AutoUpdateFootprint;
        WireDisplayToViewModel(display);
    }

    private void WireDisplayToViewModel(OrientedImageDisplay display)
    {
        display.Markers = ViewModel.Markers;
        display.Footprint = ViewModel.SelectedImageFootprint;
        display.DisplayBackgroundColor = DisplayBackgroundColor;
    }

    private void UnwireDisplay(OrientedImageDisplay display)
    {
        display.ImageClicked -= Display_ImageClicked;
        display.ClearValue(OrientedImageDisplay.AutoUpdateFootprintProperty);
        display.ClearValue(OrientedImageDisplay.MarkersProperty);
        display.ClearValue(OrientedImageDisplay.FootprintProperty);
        display.ClearValue(OrientedImageDisplay.DisplayBackgroundColorProperty);
    }

    private async void Display_ImageClicked(object? sender, OrientedImageDisplay.ImageClickedEventArgs e) => ImageTapped?.Invoke(this, e);

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
        toolbarContainer.ItemsSource = ViewModel?.ToolbarItems;
    }

    private void UnwireToolbarContainer(ItemsControl toolbarContainer)
    {
        toolbarContainer.ClearValue(ItemsControl.ItemTemplateSelectorProperty);
        toolbarContainer.ClearValue(ItemsControl.ItemsSourceProperty);
    }
#endregion Toolbar
}

#endif