#if ANDROID || (__IOS__ && !MACCATALYST)
namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    private static ContentView CreateMobileSheetContent(Label heading, View body, Action dismiss, Action? back, out Grid layout, string automationId)
    {
        heading.FontSize = 22;
        heading.Margin = 0;
        SemanticProperties.SetHeadingLevel(heading, SemanticHeadingLevel.Level1);
        var header = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Padding = new Thickness(12, 4), ColumnSpacing = 4,
        };
        header.Add(heading, 1);
        if (back != null)
        {
            var previous = ToolButton("MobileBackToMarkers", ToolkitIcons.ChevronLeft, "OrientedImageryBackToMarkers");
            previous.Text = null;
            previous.MinimumWidthRequest = 44;
            previous.BorderWidth = 0;
            previous.Clicked += (_, _) => back();
            header.Add(previous);
        }
        var close = ToolButton("ClosePanel", "", "OrientedImageryPanelClose");
        close.Text = "X";
        close.MinimumWidthRequest = 44;
        close.BorderWidth = 0;
        close.Clicked += (_, _) => dismiss();
        header.Add(close, 2);
        layout = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
        };
#if ANDROID
        var handle = new Border
        {
            WidthRequest = 32, HeightRequest = 4, StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 2 },
            Margin = new Thickness(0, 10, 0, 6), HorizontalOptions = LayoutOptions.Center,
            InputTransparent = true,
        };
        handle.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#79747E"), Color.FromArgb("#938F99"));
        layout.Add(handle);
#else
        close.Text = "\u00D7";
        close.FontAutoScalingEnabled = false;
        close.FontSize = 22;
        close.TextColor = IosAccent;
        header.Padding = new Thickness(16, 8);
        layout.Padding = new Thickness(0, 20, 0, 0);
#endif
        layout.Add(header, 0, 1);
        layout.Add(body, 0, 2);
#if ANDROID
        View surface = new Border
        {
            Content = layout, StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(28, 28, 0, 0) },
        };
        surface.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#FFFBFE"), Color.FromArgb("#211F26"));
#else
        View surface = layout;
#endif
        return new ContentView
        {
            Content = surface, AutomationId = automationId, BackgroundColor = Colors.Transparent,
        };
    }
}
#endif
