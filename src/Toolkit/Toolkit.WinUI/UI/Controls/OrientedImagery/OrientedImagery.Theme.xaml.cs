namespace Esri.ArcGISRuntime.Toolkit;

internal sealed partial class OrientedImageryResources : ResourceDictionary
{
    public OrientedImageryResources()
    {
        InitializeComponent();
    }

    public static Visibility VisibleWhen(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

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
