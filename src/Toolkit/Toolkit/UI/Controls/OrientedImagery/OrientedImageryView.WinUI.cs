#if WINDOWS_XAML

using Microsoft.UI.Xaml.Automation.Peers;
using Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

[TemplatePart(Name = ImageViewportName, Type = typeof(OrientedImageViewport))]
[TemplatePart(Name = ToolbarContainerName, Type = typeof(ItemsControl))]
[TemplatePart(Name = PaginatorName, Type = typeof(Paginator))]
[TemplatePart(Name = MainErrorMessagePresenterName, Type = typeof(TextBlock))]
[TemplatePart(Name = ImageDetailsButtonName, Type = typeof(Button))]
[TemplatePart(Name = CloseDetailsButtonName, Type = typeof(Button))]
[TemplatePart(Name = DismissTransientErrorButtonName, Type = typeof(Button))]
public partial class OrientedImageryView : Control
{
#region Accessibility
    private void DismissTransientErrorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { FocusState: not FocusState.Unfocused } button && ReferenceEquals(button, _dismissTransientErrorButton))
            FocusManager.TryMoveFocus(FocusNavigationDirection.Next, new FindNextElementOptions { SearchRoot = XamlRoot.Content });

        TransientErrorMessage = null;
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new OrientedImageryViewAutomationPeer(this);

    private void InitializeAccessibility_WinUI()
    {
        Loaded += (_, _) => RefreshAccessibility_WinUI();
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => RefreshAccessibility_WinUI());
    }

    private void RefreshAccessibility_WinUI()
    {
        if (!LoadedAndVisible())
            return;

        UpdateErrorMessages();
    }

    private sealed partial class OrientedImageryViewAutomationPeer : FrameworkElementAutomationPeer
    {
        public OrientedImageryViewAutomationPeer(OrientedImageryView owner) : base(owner)
        {
        }

        protected override string GetLocalizedControlTypeCore() => Properties.Resources.GetString("OrientedImageryViewAutomationTypeName")!;
    }

    private void UpdateDetailsFocus()
    {
        bool isPopupOpen = ViewModel.ImageDetailsPopup != null;
        // Don't mess with focus if we closed the popup but the close details button was not the original focus source
        if (!isPopupOpen && GetTemplateChild(CloseDetailsButtonName) is Button button &&
            button.FocusState == FocusState.Unfocused)
            return;

        DispatcherQueue.TryEnqueue(() =>
        {
            if ((ViewModel.ImageDetailsPopup != null) == isPopupOpen &&
                GetTemplateChild(isPopupOpen ? CloseDetailsButtonName : ImageDetailsButtonName) is Button { Visibility: Visibility.Visible, IsEnabled: true } button)
            {
                button.Focus(FocusState.Programmatic);
            }
        });
    }
#endregion Accessibility

#region BackgroundColor
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
#endregion BackgroundColor

#region ToolbarTemplates
    internal DefaultWinUIToolbarTemplates? DefaultToolbarTemplates
    {
        get => (DefaultWinUIToolbarTemplates?)GetValue(DefaultToolbarTemplatesProperty);
        set => SetValue(DefaultToolbarTemplatesProperty, value);
    }

    internal static readonly DependencyProperty DefaultToolbarTemplatesProperty =
        DependencyProperty.Register(nameof(DefaultToolbarTemplates), typeof(DefaultWinUIToolbarTemplates), typeof(OrientedImageryView), new PropertyMetadata(null));

    private void WireToolbar_WinUI()
    {
        if (_defaultItemTemplateSelector == null)
            _defaultItemTemplateSelector = CreateDefaultSelector_WinUI();

        if (ToolbarItemTemplateSelector == null)
            ToolbarItemTemplateSelector = _defaultItemTemplateSelector;
        else if (!ReferenceEquals(ToolbarItemTemplateSelector, _defaultItemTemplateSelector))
            ToolbarItemTemplateSelector?.Merge(_defaultItemTemplateSelector);
    }

    private ToolbarSelector CreateDefaultSelector_WinUI()
    {
        var selector = new ToolbarSelector();
        var pairs = selector.SelectionPairs;

        var templates = DefaultToolbarTemplates;
        if (templates != null)
        {
            if (templates.CategoryTemplate is DataTemplate categoryTemplate)
            {
                pairs.Add(new ToolbarCategorySelectorPair { CategoryName = ToolbarCategory.ModeCategoryName, Template = categoryTemplate });
                pairs.Add(new ToolbarCategorySelectorPair { CategoryName = ToolbarCategory.OverlaysCategoryName, Template = categoryTemplate });
                pairs.Add(new ToolbarCategorySelectorPair { CategoryName = ToolbarCategory.MarkersCategoryName, Template = categoryTemplate });
            }
            if (templates.MoreCategoryTemplate is DataTemplate moreCategoryTemplate)
                pairs.Add(new ToolbarCategorySelectorPair { CategoryName = ToolbarCategory.MoreCategoryName, Template = moreCategoryTemplate });
            if (templates.ToolbarDividerVMTemplate is DataTemplate toolbarDividerTemplate)
                pairs.Add(new ToolbarItemSelectorPair { ItemType = typeof(ToolbarDividerVM), Template = toolbarDividerTemplate });
            if (templates.ImageSearchModeVMTemplate is DataTemplate imageSearchModeTemplate)
                pairs.Add(new ToolbarItemSelectorPair { ItemType = typeof(ImageSearchModeVM), Template = imageSearchModeTemplate });
            if (templates.SequentialNavigationVMTemplate is DataTemplate sequentialNavigationTemplate)
                pairs.Add(new ToolbarItemSelectorPair { ItemType = typeof(SequentialNavigationVM), Template = sequentialNavigationTemplate });
            if (templates.ShowSelectedFootprintVMTemplate is DataTemplate selectedFootprintTemplate)
                pairs.Add(new ToolbarItemSelectorPair { ItemType = typeof(ShowSelectedFootprintVM), Template = selectedFootprintTemplate });
            if (templates.ShowUnselectedFootprintsVMTemplate is DataTemplate unselectedFootprintsTemplate)
                pairs.Add(new ToolbarItemSelectorPair { ItemType = typeof(ShowUnselectedFootprintsVM), Template = unselectedFootprintsTemplate });
            if (templates.ShowCameraMarkersVMTemplate is DataTemplate cameraMarkersTemplate)
                pairs.Add(new ToolbarItemSelectorPair { ItemType = typeof(ShowCameraMarkersVM), Template = cameraMarkersTemplate });
            if (templates.AllowAddingMarkersVMTemplate is DataTemplate addingMarkersTemplate)
                pairs.Add(new ToolbarItemSelectorPair { ItemType = typeof(AllowAddingMarkersVM), Template = addingMarkersTemplate });
            if (templates.MarkerPickerVMTemplate is DataTemplate markerPickerTemplate)
                pairs.Add(new ToolbarItemSelectorPair { ItemType = typeof(SelectNewMarkerSymbolVM), Template = markerPickerTemplate });
            if (templates.ClearMarkersVMTemplate is DataTemplate clearMarkersTemplate)
                pairs.Add(new ToolbarItemSelectorPair { ItemType = typeof(ClearMarkersVM), Template = clearMarkersTemplate });
        }

        return selector;
    }
#endregion ToolbarTemplates
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
