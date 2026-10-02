#if WINDOWS_XAML

using Esri.ArcGISRuntime.Symbology;
using System.Collections.ObjectModel;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

[TemplatePart(Name = ImageDisplayName, Type = typeof(OrientedImageDisplay))]
[TemplatePart(Name = ToolbarContainerName, Type = typeof(ItemsControl))]
[TemplatePart(Name = PaginatorName, Type = typeof(OrientedImageryPaginator))]
public partial class OrientedImageryView : ItemsControl
{
    private long _isBusyCallbackToken;
    private long _isInteractiveCallbackToken;
    private long _errorCallbackToken;

    private long _backgroundColorCallbackToken;

    internal Microsoft.UI.Xaml.Media.SolidColorBrush? ThemeDisplayBackgroundBrush
    {
        get => (Microsoft.UI.Xaml.Media.SolidColorBrush?)GetValue(ThemeDisplayBackgroundBrushProperty);
        set => SetValue(ThemeDisplayBackgroundBrushProperty, value);
    }

    internal static readonly DependencyProperty ThemeDisplayBackgroundBrushProperty =
        DependencyProperty.Register(nameof(ThemeDisplayBackgroundBrush), typeof(Microsoft.UI.Xaml.Media.SolidColorBrush), typeof(OrientedImageryView),
            new PropertyMetadata(null, (sender, args) => ((OrientedImageryView)sender).OnThemeDisplayBackgroundBrushChanged(
                args.OldValue as Microsoft.UI.Xaml.Media.SolidColorBrush, args.NewValue as Microsoft.UI.Xaml.Media.SolidColorBrush)));

    private void OnThemeDisplayBackgroundBrushChanged(Microsoft.UI.Xaml.Media.SolidColorBrush? oldBrush, Microsoft.UI.Xaml.Media.SolidColorBrush? newBrush)
    {
        oldBrush?.UnregisterPropertyChangedCallback(Microsoft.UI.Xaml.Media.SolidColorBrush.ColorProperty, _backgroundColorCallbackToken);
        if (newBrush != null)
        {
            // Theme resources can update a shared brush in place; do not let that brush retain the view.
            var weakView = new WeakReference<OrientedImageryView>(this);
            _backgroundColorCallbackToken = newBrush.RegisterPropertyChangedCallback(Microsoft.UI.Xaml.Media.SolidColorBrush.ColorProperty, (_, _) =>
            {
                if (weakView.TryGetTarget(out var view))
                    view.UpdateDisplayBackgroundColor(view.DisplayBackgroundColor);
            });
        }

        UpdateDisplayBackgroundColor(DisplayBackgroundColor);
    }

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

    private OrientedImageryViewTemplateSelector CreateDefaultSelector()
    {
        var selector = new OrientedImageryViewTemplateSelector();

        var templates = DefaultToolbarTemplates;
        if (templates != null)
        {
            selector.TypeTemplatePairs.Add(new() { Type = typeof(ShowSelectedFootprintVM), Template = templates.ShowSelectedFootprintVMTemplate });
            selector.TypeTemplatePairs.Add(new() { Type = typeof(ShowUnselectedFootprintsVM), Template = templates.ShowUnselectedFootprintsVMTemplate });
            selector.TypeTemplatePairs.Add(new() { Type = typeof(ShowCameraMarkersVM), Template = templates.ShowCameraMarkersVMTemplate });
            selector.TypeTemplatePairs.Add(new() { Type = typeof(AllowAddingMarkersVM), Template = templates.AllowAddingMarkersVMTemplate });
            selector.TypeTemplatePairs.Add(new() { Type = typeof(SelectNewMarkerSymbolVM), Template = templates.MarkerPickerVMTemplate });
            selector.TypeTemplatePairs.Add(new() { Type = typeof(ClearMarkersVM), Template = templates.ClearMarkersVMTemplate });
            selector.TypeTemplatePairs.Add(new() { Type = typeof(SequentialNavigationVM), Template = templates.SequentialNavigationVMTemplate });
        }

        return selector;
    }

    private void UpdateDisplayStateSubscriptions(OrientedImageDisplay? oldDisplay, OrientedImageDisplay? newDisplay)
    {
        if (oldDisplay != null)
        {
            oldDisplay.UnregisterPropertyChangedCallback(OrientedImageDisplay.IsBusyProperty, _isBusyCallbackToken);
            oldDisplay.UnregisterPropertyChangedCallback(OrientedImageDisplay.IsInteractiveProperty, _isInteractiveCallbackToken);
            oldDisplay.UnregisterPropertyChangedCallback(OrientedImageDisplay.ErrorProperty, _errorCallbackToken);
        }

        if (newDisplay != null)
        {
            _isBusyCallbackToken = newDisplay.RegisterPropertyChangedCallback(OrientedImageDisplay.IsBusyProperty, OnDisplayStateChanged);
            _isInteractiveCallbackToken = newDisplay.RegisterPropertyChangedCallback(OrientedImageDisplay.IsInteractiveProperty, OnDisplayStateChanged);
            _errorCallbackToken = newDisplay.RegisterPropertyChangedCallback(OrientedImageDisplay.ErrorProperty, OnDisplayStateChanged);
        }

        UpdateDisplayState();
    }

    private void OnDisplayStateChanged(DependencyObject sender, DependencyProperty property)
        => UpdateDisplayState();

    private void UpdateDisplayState()
    {
        IsImageDisplayBusy = _display?.IsBusy ?? false;
        IsImageDisplayInteractive = _display?.IsInteractive ?? false;
        // Publish non-null template values so clearing an error also clears the compiled binding's target.
        var error = _display?.Error;
        ImageDisplayErrorMessage = error?.Message ?? string.Empty;
        ImageDisplayErrorVisibility = error is null ? Visibility.Collapsed : Visibility.Visible;
        UpdateNavigationVisibility();
    }

    private void UpdateNavigationVisibility()
    {
        bool hasImage = ViewModel.SelectedImage != null;
        NoImageSelectedVisibility = hasImage ? Visibility.Collapsed : Visibility.Visible;
        ImageNavigationVisibility = hasImage ? Visibility.Visible : Visibility.Collapsed;
        ResultNavigationVisibility = hasImage && !ViewModel.IsSequentialNavigation ? Visibility.Visible : Visibility.Collapsed;
        SequentialNavigationVisibility = hasImage && ViewModel.IsSequentialNavigation ? Visibility.Visible : Visibility.Collapsed;
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

    internal bool IsImageDisplayBusy
    {
        get => (bool)GetValue(IsImageDisplayBusyProperty);
        private set => SetValue(IsImageDisplayBusyProperty, value);
    }

    internal static readonly DependencyProperty IsImageDisplayBusyProperty =
        DependencyProperty.Register(nameof(IsImageDisplayBusy), typeof(bool), typeof(OrientedImageryView), new PropertyMetadata(false));

    internal bool IsImageDisplayInteractive
    {
        get => (bool)GetValue(IsImageDisplayInteractiveProperty);
        private set => SetValue(IsImageDisplayInteractiveProperty, value);
    }

    internal static readonly DependencyProperty IsImageDisplayInteractiveProperty =
        DependencyProperty.Register(nameof(IsImageDisplayInteractive), typeof(bool), typeof(OrientedImageryView), new PropertyMetadata(false));

    internal string ImageDisplayErrorMessage
    {
        get => (string)GetValue(ImageDisplayErrorMessageProperty);
        private set => SetValue(ImageDisplayErrorMessageProperty, value);
    }

    internal static readonly DependencyProperty ImageDisplayErrorMessageProperty =
        DependencyProperty.Register(nameof(ImageDisplayErrorMessage), typeof(string), typeof(OrientedImageryView), new PropertyMetadata(string.Empty));

    internal Visibility ImageDisplayErrorVisibility
    {
        get => (Visibility)GetValue(ImageDisplayErrorVisibilityProperty);
        private set => SetValue(ImageDisplayErrorVisibilityProperty, value);
    }

    internal static readonly DependencyProperty ImageDisplayErrorVisibilityProperty =
        DependencyProperty.Register(nameof(ImageDisplayErrorVisibility), typeof(Visibility), typeof(OrientedImageryView), new PropertyMetadata(Visibility.Collapsed));

}

internal sealed class DefaultWinUIToolbarTemplates
{
    public DataTemplate? ShowSelectedFootprintVMTemplate { get; set; }
    public DataTemplate? ShowUnselectedFootprintsVMTemplate { get; set; }
    public DataTemplate? ShowCameraMarkersVMTemplate { get; set; }
    public DataTemplate? AllowAddingMarkersVMTemplate { get; set; }
    public DataTemplate? MarkerPickerVMTemplate { get; set; }
    public DataTemplate? ClearMarkersVMTemplate { get; set; }
    public DataTemplate? SequentialNavigationVMTemplate { get; set; }
}
#endif
