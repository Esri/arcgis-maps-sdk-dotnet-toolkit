#if WPF || WINDOWS_XAML || MAUI
#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

internal static class OrientedImageryPagination
{
    internal static (int Start, int Count) GetRange(int totalPages, int selectedIndex, double availableWidth, double pageWidth)
    {
        if (totalPages <= 0 || availableWidth <= 0 || pageWidth <= 0)
            return (0, 0);

        int count = (int)Math.Min(totalPages, Math.Max(1, Math.Floor(availableWidth / pageWidth)));
        if (count < totalPages && count % 2 == 0)
            count--;
        int current = Math.Clamp(selectedIndex, 0, totalPages - 1);
        return (Math.Clamp(current - count / 2, 0, totalPages - count), count);
    }
}
#endif
