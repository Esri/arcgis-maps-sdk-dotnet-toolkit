using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;

namespace Esri.ArcGISRuntime.Toolkit;

internal sealed partial class OrientedImageryResources : ResourceDictionary
{
    public OrientedImageryResources()
    {
        InitializeComponent();
    }

    private void ImageSearchModeOption_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: OrientedImageryViewInteractionState state } element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            while (parent is not null && parent is not ComboBoxItem)
                parent = VisualTreeHelper.GetParent(parent);

            if (parent is ComboBoxItem item)
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(item, ImageSearchModeLabel(state));
        }
    }

    /// <summary>
    /// Inherits the OrientedImageryViewToolbarSelector from its parent ItemsControl.
    /// </summary>
    private void ToolbarCategory_Loaded(object sender, RoutedEventArgs e)
    {
        var items = (ItemsControl)sender;
        var parent = VisualTreeHelper.GetParent(items);
        while (parent is not null && parent is not ItemsControl)
            parent = VisualTreeHelper.GetParent(parent);

        if (parent is ItemsControl toolbar && toolbar.ItemTemplateSelector is ToolbarSelector)
        {
            items.ItemTemplateSelector = toolbar.ItemTemplateSelector;
            var token = toolbar.RegisterPropertyChangedCallback(ItemsControl.ItemTemplateSelectorProperty,
                (s, p) => items.ItemTemplateSelector = toolbar.ItemTemplateSelector);
            items.Unloaded += OnUnloaded;

            void OnUnloaded(object sender, RoutedEventArgs e)
            {
                toolbar.UnregisterPropertyChangedCallback(ItemsControl.ItemTemplateSelectorProperty, token);
                items.Unloaded -= OnUnloaded;
            }
        }
    }

    public static Visibility VisibleWhen(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static bool IsGreaterThanOne(int imageCount) => imageCount > 1;

    public static string? NoImageMessage(bool hasSearched)
        => Properties.Resources.GetString(hasSearched ? "OrientedImageryViewNoImagesFound" : "OrientedImageryViewNoImageToDisplay");

    public static Visibility VisibleWhenImageSelectionDisabled(OrientedImageryViewInteractionState state)
        => VisibleWhen(state is not (OrientedImageryViewInteractionState.FindImages or OrientedImageryViewInteractionState.DisplayImage));

    public static Visibility VisibleWhenFindingImages(OrientedImageryViewInteractionState state)
        => state == OrientedImageryViewInteractionState.FindImages ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility VisibleWhenDisplayingImage(OrientedImageryViewInteractionState state)
        => state == OrientedImageryViewInteractionState.DisplayImage ? Visibility.Visible : Visibility.Collapsed;

    public static string? ImageSearchModeLabel(OrientedImageryViewInteractionState state)
        => Properties.Resources.GetString(state == OrientedImageryViewInteractionState.FindImages
            ? "OrientedImageryViewImageSearchModeFind" : "OrientedImageryViewImageSearchModeDisplay");

    public static Visibility VisibleWhenFalse(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

}

#region Converters
internal sealed partial class ImageSearchModeLabelConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
        => OrientedImageryResources.ImageSearchModeLabel((OrientedImageryViewInteractionState)value);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

internal sealed partial class NotNullToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language) => value is not null;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

internal sealed partial class ImageSearchModeIconTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DisplayImageTemplate { get; set; }

    public DataTemplate? FindImagesTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
        => (item is OrientedImageryViewInteractionState.FindImages ? FindImagesTemplate : DisplayImageTemplate)
            ?? base.SelectTemplateCore(item);

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        => SelectTemplateCore(item);
}
#endregion Converters

#region PseudoConverters
/// <summary>
/// The attached properties in this class are a workaround for a WinUI x:Bind behavior/bug where values aren't updated properly when set to null.
/// If this wasn't the case we would be using all static methods as defined in the <see cref="OrientedImageryResources"/> code-behind as seen earlier in this file.
/// For now we'll need to wait until the following PR is a.) shipped in a released version, and b.) that version becomes our minimum: https://github.com/microsoft/microsoft-ui-xaml/pull/11732.
/// The other workaround would have been to use IValueConverters, but they didn't seem to work within the ControlTemplate and would always crash at runtime.
/// </summary>
internal static class NullVisibility
{
    public static readonly DependencyProperty WhenNotNullProperty = DependencyProperty.RegisterAttached(
        "WhenNotNull", typeof(object), typeof(NullVisibility),
        new PropertyMetadata(null, (sender, args) => ((UIElement)sender).Visibility = args.NewValue is not null ? Visibility.Visible : Visibility.Collapsed));

    public static object? GetWhenNotNull(DependencyObject element) => element.GetValue(WhenNotNullProperty);

    public static void SetWhenNotNull(DependencyObject element, object? value)
    {
        element.SetValue(WhenNotNullProperty, value);
        // An initial null value does not trigger the property-changed callback.
        ((UIElement)element).Visibility = value is not null ? Visibility.Visible : Visibility.Collapsed;
    }

    public static readonly DependencyProperty WhenNullProperty = DependencyProperty.RegisterAttached(
        "WhenNull", typeof(object), typeof(NullVisibility),
        new PropertyMetadata(null, (sender, args) => ((UIElement)sender).Visibility = args.NewValue is null ? Visibility.Visible : Visibility.Collapsed));

    public static object? GetWhenNull(DependencyObject element) => element.GetValue(WhenNullProperty);

    public static void SetWhenNull(DependencyObject element, object? value)
    {
        element.SetValue(WhenNullProperty, value);
        ((UIElement)element).Visibility = value is null ? Visibility.Visible : Visibility.Collapsed;
    }

    public static readonly DependencyProperty WhenTrueProperty = DependencyProperty.RegisterAttached(
        "WhenTrue", typeof(bool), typeof(NullVisibility),
        new PropertyMetadata(false, (sender, args) => ((UIElement)sender).Visibility = (bool)args.NewValue ? Visibility.Visible : Visibility.Collapsed));

    public static bool GetWhenTrue(DependencyObject element) => (bool)element.GetValue(WhenTrueProperty);

    public static void SetWhenTrue(DependencyObject element, bool value)
    {
        element.SetValue(WhenTrueProperty, value);
        ((UIElement)element).Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }
}
#endregion PseudoConverters

#region ImageSearchModeComboBox
// By default comboboxes don't trigger an update when clicking on an already-selected dropdown item. In our case we do want it to trigger
// so we can set the actual ViewModel.InteractionState if it got changed by another control in the meantime
internal static class ImageSearchModeSelectionBehavior
{
    private static readonly PointerEventHandler PointerPressedHandler = Item_PointerPressed;
    private static readonly KeyEventHandler KeyEventHandler = Item_KeyDown;

    public static readonly DependencyProperty RepeatSelectionProperty = DependencyProperty.RegisterAttached(
        "RepeatSelection", typeof(bool), typeof(ImageSearchModeSelectionBehavior), new PropertyMetadata(false, OnRepeatSelectionChanged));

    public static bool GetRepeatSelection(DependencyObject element) => (bool)element.GetValue(RepeatSelectionProperty);

    public static void SetRepeatSelection(DependencyObject element, bool value) => element.SetValue(RepeatSelectionProperty, value);

    private static void OnRepeatSelectionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is ComboBoxItem item)
        {
            if ((bool)e.NewValue)
            {
                item.AddHandler(UIElement.PointerPressedEvent, PointerPressedHandler, true);
                item.AddHandler(UIElement.KeyDownEvent, KeyEventHandler, true);
            }
            else
            {
                item.RemoveHandler(UIElement.PointerPressedEvent, PointerPressedHandler);
                item.RemoveHandler(UIElement.KeyDownEvent, KeyEventHandler);
            }
        }
    }

    private static void Item_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.GetCurrentPoint((UIElement)sender).Properties.IsLeftButtonPressed)
            RepeatSelection(sender);
    }

    private static void Item_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is Windows.System.VirtualKey.Enter or Windows.System.VirtualKey.Space)
            RepeatSelection(sender);
    }

    private static void RepeatSelection(object sender)
    {
        if (sender is ComboBoxItem { IsSelected: true, Content: OrientedImageryViewInteractionState } item &&
            ItemsControl.ItemsControlFromItemContainer(item) is ComboBox { DataContext: ImageSearchModeVM viewModel })
        {
            viewModel.IsImageSelectionEnabled = true;
        }
    }
}
#endregion ImageSearchModeComboBox
