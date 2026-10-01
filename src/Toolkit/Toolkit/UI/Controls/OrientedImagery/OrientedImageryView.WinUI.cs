#if WINDOWS_XAML

using Esri.ArcGISRuntime.Symbology;
using System.Collections.ObjectModel;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

[TemplatePart(Name = ImageViewportName, Type = typeof(OrientedImageViewport))]
[TemplatePart(Name = ToolbarContainerName, Type = typeof(ItemsControl))]
[TemplatePart(Name = PaginatorName, Type = typeof(OrientedImageryPaginator))]
public partial class OrientedImageryView : Control
{
    private long _isBusyCallbackToken;
    private long _isInteractiveCallbackToken;
    private long _errorCallbackToken;

    internal Windows.UI.Color ThemeDisplayBackgroundColor
    {
        get => (Windows.UI.Color)GetValue(ThemeDisplayBackgroundColorProperty);
        set => SetValue(ThemeDisplayBackgroundColorProperty, value);
    }

    internal static readonly DependencyProperty ThemeDisplayBackgroundColorProperty =
        DependencyProperty.Register(nameof(ThemeDisplayBackgroundColor), typeof(Windows.UI.Color), typeof(OrientedImageryView),
            new PropertyMetadata(default(Windows.UI.Color), (sender, _) =>
            {
                var view = (OrientedImageryView)sender;
                view.UpdateDisplayBackgroundColor(view.DisplayBackgroundColor);
            }));

    internal DefaultWinUIToolbarTemplates? DefaultToolbarTemplates
    {
        get => (DefaultWinUIToolbarTemplates?)GetValue(DefaultToolbarTemplatesProperty);
        set => SetValue(DefaultToolbarTemplatesProperty, value);
    }

    internal static readonly DependencyProperty DefaultToolbarTemplatesProperty =
        DependencyProperty.Register(nameof(DefaultToolbarTemplates), typeof(DefaultWinUIToolbarTemplates), typeof(OrientedImageryView), new PropertyMetadata(null));

    private void WireToolbarWinUI(ItemsControl toolbarContainer)
    {
        if (ToolbarItemTemplateSelector == null)
            ToolbarItemTemplateSelector = CreateDefaultSelector();
        toolbarContainer.ItemTemplateSelector = ToolbarItemTemplateSelector;
    }

    private void UnwireToolbarWinUI(ItemsControl toolbarContainer)
    {
        toolbarContainer.ItemTemplateSelector = null;
    }

    private OrientedImageryViewToolbarSelector CreateDefaultSelector()
    {
        var selector = new OrientedImageryViewToolbarSelector();
        var pairs = selector.SelectionPairs;

        var templates = DefaultToolbarTemplates;
        if (templates != null)
        {
            if (templates.CategoryTemplate is DataTemplate categoryTemplate)
            {
                pairs.Add(new OrientedImageryToolbarCategorySelectorPair { CategoryName = OrientedImageryToolbarCategory.ModeCategoryName, Template = categoryTemplate });
                pairs.Add(new OrientedImageryToolbarCategorySelectorPair { CategoryName = OrientedImageryToolbarCategory.OverlaysCategoryName, Template = categoryTemplate });
                pairs.Add(new OrientedImageryToolbarCategorySelectorPair { CategoryName = OrientedImageryToolbarCategory.MarkersCategoryName, Template = categoryTemplate });
            }
            if (templates.MoreCategoryTemplate is DataTemplate moreCategoryTemplate)
                pairs.Add(new OrientedImageryToolbarCategorySelectorPair { CategoryName = OrientedImageryToolbarCategory.MoreCategoryName, Template = moreCategoryTemplate });
            if (templates.ToolbarDividerVMTemplate is DataTemplate toolbarDividerTemplate)
                pairs.Add(new OrientedImageryToolbarItemSelectorPair { ItemType = typeof(ToolbarDividerVM), Template = toolbarDividerTemplate });
            if (templates.ImageSearchModeVMTemplate is DataTemplate imageSearchModeTemplate)
                pairs.Add(new OrientedImageryToolbarItemSelectorPair { ItemType = typeof(ImageSearchModeVM), Template = imageSearchModeTemplate });
            if (templates.SequentialNavigationVMTemplate is DataTemplate sequentialNavigationTemplate)
                pairs.Add(new OrientedImageryToolbarItemSelectorPair { ItemType = typeof(SequentialNavigationVM), Template = sequentialNavigationTemplate });
            if (templates.ShowSelectedFootprintVMTemplate is DataTemplate selectedFootprintTemplate)
                pairs.Add(new OrientedImageryToolbarItemSelectorPair { ItemType = typeof(ShowSelectedFootprintVM), Template = selectedFootprintTemplate });
            if (templates.ShowUnselectedFootprintsVMTemplate is DataTemplate unselectedFootprintsTemplate)
                pairs.Add(new OrientedImageryToolbarItemSelectorPair { ItemType = typeof(ShowUnselectedFootprintsVM), Template = unselectedFootprintsTemplate });
            if (templates.ShowCameraMarkersVMTemplate is DataTemplate cameraMarkersTemplate)
                pairs.Add(new OrientedImageryToolbarItemSelectorPair { ItemType = typeof(ShowCameraMarkersVM), Template = cameraMarkersTemplate });
            if (templates.AllowAddingMarkersVMTemplate is DataTemplate addingMarkersTemplate)
                pairs.Add(new OrientedImageryToolbarItemSelectorPair { ItemType = typeof(AllowAddingMarkersVM), Template = addingMarkersTemplate });
            if (templates.MarkerPickerVMTemplate is DataTemplate markerPickerTemplate)
                pairs.Add(new OrientedImageryToolbarItemSelectorPair { ItemType = typeof(SelectNewMarkerSymbolVM), Template = markerPickerTemplate });
            if (templates.ClearMarkersVMTemplate is DataTemplate clearMarkersTemplate)
                pairs.Add(new OrientedImageryToolbarItemSelectorPair { ItemType = typeof(ClearMarkersVM), Template = clearMarkersTemplate });
        }

        return selector;
    }

    internal AllowAddingMarkersVM AllowAddingMarkers { get; private set; } = null!;

    private void UnwireViewport_WinUI(OrientedImageViewport? viewport)
    {
        if (viewport != null)
        {
            viewport.UnregisterPropertyChangedCallback(OrientedImageViewport.IsBusyProperty, _isBusyCallbackToken);
            viewport.UnregisterPropertyChangedCallback(OrientedImageViewport.IsInteractiveProperty, _isInteractiveCallbackToken);
            viewport.UnregisterPropertyChangedCallback(OrientedImageViewport.ErrorProperty, _errorCallbackToken);
        }

        UpdateViewportState();
    }

    private void WireViewport_WinUI(OrientedImageViewport? viewport)
    {
        if (viewport != null)
        {
            _isBusyCallbackToken = viewport.RegisterPropertyChangedCallback(OrientedImageViewport.IsBusyProperty, OnViewportStateChanged);
            _isInteractiveCallbackToken = viewport.RegisterPropertyChangedCallback(OrientedImageViewport.IsInteractiveProperty, OnViewportStateChanged);
            _errorCallbackToken = viewport.RegisterPropertyChangedCallback(OrientedImageViewport.ErrorProperty, OnViewportStateChanged);
        }

        UpdateViewportState();
    }

    private void OnViewportStateChanged(DependencyObject sender, DependencyProperty property)
        => UpdateViewportState();

    private void UpdateViewportState()
    {
        IsImageViewportBusy = _viewport?.IsBusy ?? false;
        IsImageViewportInteractive = _viewport?.IsInteractive ?? false;
        ImageViewportError = _viewport?.Error;
        UpdateNavigationVisibility_WinUI();
    }

    private void UpdateNavigationVisibility_WinUI()
    {
        bool hasImage = ViewModel.SelectedImage != null;
        NoImageSelectedVisibility = hasImage ? Visibility.Collapsed : Visibility.Visible;
        ImageNavigationVisibility = hasImage ? Visibility.Visible : Visibility.Collapsed;
        ResultNavigationVisibility = hasImage && !ViewModel.IsSequentialNavigationEnabled ? Visibility.Visible : Visibility.Collapsed;
        SequentialNavigationVisibility = hasImage && ViewModel.IsSequentialNavigationEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Visibility NoImageSelectedVisibility
    {
        get => (Visibility)GetValue(NoImageSelectedVisibilityProperty);
        private set => SetValue(NoImageSelectedVisibilityProperty, value);
    }

    internal static readonly DependencyProperty NoImageSelectedVisibilityProperty =
        DependencyProperty.Register(nameof(NoImageSelectedVisibility), typeof(Visibility), typeof(OrientedImageryView), new PropertyMetadata(Visibility.Visible));

    internal Visibility ImageNavigationVisibility
    {
        get => (Visibility)GetValue(ImageNavigationVisibilityProperty);
        private set => SetValue(ImageNavigationVisibilityProperty, value);
    }

    internal static readonly DependencyProperty ImageNavigationVisibilityProperty =
        DependencyProperty.Register(nameof(ImageNavigationVisibility), typeof(Visibility), typeof(OrientedImageryView), new PropertyMetadata(Visibility.Collapsed));

    internal Visibility ResultNavigationVisibility
    {
        get => (Visibility)GetValue(ResultNavigationVisibilityProperty);
        private set => SetValue(ResultNavigationVisibilityProperty, value);
    }

    internal static readonly DependencyProperty ResultNavigationVisibilityProperty =
        DependencyProperty.Register(nameof(ResultNavigationVisibility), typeof(Visibility), typeof(OrientedImageryView), new PropertyMetadata(Visibility.Collapsed));

    internal Visibility SequentialNavigationVisibility
    {
        get => (Visibility)GetValue(SequentialNavigationVisibilityProperty);
        private set => SetValue(SequentialNavigationVisibilityProperty, value);
    }

    internal static readonly DependencyProperty SequentialNavigationVisibilityProperty =
        DependencyProperty.Register(nameof(SequentialNavigationVisibility), typeof(Visibility), typeof(OrientedImageryView), new PropertyMetadata(Visibility.Collapsed));
        
    internal bool IsImageViewportBusy
    {
        get => (bool)GetValue(IsImageViewportBusyProperty);
        private set => SetValue(IsImageViewportBusyProperty, value);
    }

    internal static readonly DependencyProperty IsImageViewportBusyProperty =
        DependencyProperty.Register(nameof(IsImageViewportBusy), typeof(bool), typeof(OrientedImageryView), new PropertyMetadata(false));

    internal bool IsImageViewportInteractive
    {
        get => (bool)GetValue(IsImageViewportInteractiveProperty);
        private set => SetValue(IsImageViewportInteractiveProperty, value);
    }

    internal static readonly DependencyProperty IsImageViewportInteractiveProperty =
        DependencyProperty.Register(nameof(IsImageViewportInteractive), typeof(bool), typeof(OrientedImageryView), new PropertyMetadata(false));

    internal Exception? ImageViewportError
    {
        get => (Exception?)GetValue(ImageViewportErrorProperty);
        private set => SetValue(ImageViewportErrorProperty, value);
    }

    internal static readonly DependencyProperty ImageViewportErrorProperty =
        DependencyProperty.Register(nameof(ImageViewportError), typeof(Exception), typeof(OrientedImageryView), new PropertyMetadata(null));
}

internal sealed class DefaultWinUIToolbarTemplates
{
    public DataTemplate? CategoryTemplate { get; set; }
    public DataTemplate? MoreCategoryTemplate { get; set; }
    public DataTemplate? ToolbarDividerVMTemplate { get; set; }
    public DataTemplate? ImageSearchModeVMTemplate { get; set; }
    public DataTemplate? SequentialNavigationVMTemplate { get; set; }
    public DataTemplate? ShowSelectedFootprintVMTemplate { get; set; }
    public DataTemplate? ShowUnselectedFootprintsVMTemplate { get; set; }
    public DataTemplate? ShowCameraMarkersVMTemplate { get; set; }
    public DataTemplate? AllowAddingMarkersVMTemplate { get; set; }
    public DataTemplate? MarkerPickerVMTemplate { get; set; }
    public DataTemplate? ClearMarkersVMTemplate { get; set; }
}
#endif
