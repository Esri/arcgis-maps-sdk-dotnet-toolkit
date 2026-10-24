using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit;

internal sealed partial class ImageSearchModeLabelConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
        => OrientedImageryResources.ImageSearchModeLabel((OrientedImageryViewInteractionState)value);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

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

internal sealed partial class OrientedImageryResources : ResourceDictionary
{
    public OrientedImageryResources()
    {
        InitializeComponent();
    }

    private void ToolbarCategory_Loaded(object sender, RoutedEventArgs e)
    {
        var items = (ItemsControl)sender;
        var parent = VisualTreeHelper.GetParent(items);
        while (parent is not null && parent is not ItemsControl)
            parent = VisualTreeHelper.GetParent(parent);

        if (parent is ItemsControl toolbar)
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

    public static Visibility VisibleWhenFindingImages(OrientedImageryViewInteractionState state)
        => state == OrientedImageryViewInteractionState.FindImages ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility VisibleWhenDisplayingImage(OrientedImageryViewInteractionState state)
        => state == OrientedImageryViewInteractionState.DisplayImage ? Visibility.Visible : Visibility.Collapsed;

    public static string? ImageSearchModeLabel(OrientedImageryViewInteractionState state)
        => Properties.Resources.GetString(state == OrientedImageryViewInteractionState.FindImages
            ? "OrientedImageryViewImageSearchModeFind" : "OrientedImageryViewImageSearchModeDisplay");

    public static Visibility VisibleWhenAddingMarkers(Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImageryViewInteractionState state)
        => state == Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImageryViewInteractionState.AddMarkers ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility VisibleWhenFalse(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility VisibleWhenNotNull(object? value) => value is null ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility VisibleWhenLoading(object? selectedImage, bool isInteractive)
        => selectedImage is not null && !isInteractive ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility VisibleWhenBusy(object? selectedImage, bool isBusy)
        => selectedImage is not null && isBusy ? Visibility.Visible : Visibility.Collapsed;

    public static string? SequentialNavigationToolTip(bool isSequential)
        => Properties.Resources.GetString(isSequential ? "OrientedImageryViewReturnToResults" : "OrientedImageryViewEnterSequentialNavigation");
}
