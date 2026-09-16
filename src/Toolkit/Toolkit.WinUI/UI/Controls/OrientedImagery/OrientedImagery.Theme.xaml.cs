namespace Esri.ArcGISRuntime.Toolkit;

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

    public static Visibility VisibleWhenAddingMarkers(Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImageryViewInteractionState state)
        => state == Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImageryViewInteractionState.AddMarkers ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility VisibleWhenFalse(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility VisibleWhenNull(object? value) => value is null ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility VisibleWhenNotNull(object? value) => value is null ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility VisibleWhenLoading(object? selectedImage, bool isInteractive)
        => selectedImage is not null && !isInteractive ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility VisibleWhenBusy(object? selectedImage, bool isBusy)
        => selectedImage is not null && isBusy ? Visibility.Visible : Visibility.Collapsed;

    public static Windows.UI.Color ToWindowsColor(System.Drawing.Color color)
        => Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B);
}
