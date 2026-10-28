#if ANDROID || (__IOS__ && !MACCATALYST)
namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    #region Mode segments

    private void AddMobileModeSegments(VerticalStackLayout content, ImageSearchModeVM mode, Func<bool> enabled)
    {
        var segments = new Grid { ColumnSpacing = 1 };
        foreach (var state in mode.ImageSearchModes)
        {
            bool search = state == OrientedImageryViewInteractionState.FindImages;
            string title = Text(search ? "ImageSearchModeFind" : "ImageSearchModeDisplay");
            var action = GuardAction(new MenuAction(title, () => ToggleImageMode(mode, state),
                Icon: () => ImageModeIcon(search),
                Checked: () => ViewModel.InteractionState == state, Enabled: enabled), dismiss: false);
            var button = new Button
            {
                CornerRadius = 0, BorderWidth = 0,
                MinimumHeightRequest = 48, Padding = 0,
            };
            var segment = new Grid();
            segment.Add(button);
            var icon = action.Icon();
            icon.WidthRequest = icon.HeightRequest = 24;
            var check = new Label { Text = "\u2713", VerticalOptions = LayoutOptions.Center };
            var label = new Label
            {
                Text = title, VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.WordWrap,
            };
#if __IOS__
            button.MinimumHeightRequest = 52;
            label.FontSize = check.FontSize = 17;
#endif
            // Keep the caption separate so large text can wrap above the native button.
            var caption = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 6, Padding = new Thickness(8, 4),
                VerticalOptions = LayoutOptions.Center, InputTransparent = true,
            };
            caption.Add(check);
            caption.Add(icon, 1);
            caption.Add(label, 2);
            segment.Add(caption);
            SemanticProperties.SetDescription(button, title);
            button.Clicked += (_, _) => action.Execute();
            segments.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            segments.Add(segment, segments.ColumnDefinitions.Count - 1);
            _panelUpdates.Add(() =>
            {
                bool selected = action.Checked?.Invoke() == true;
                bool dark = Application.Current?.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark;
                check.IsVisible = selected;
                button.IsEnabled = action.Enabled();
                caption.Opacity = button.IsEnabled ? 1 : 0.45;
#if ANDROID
                button.BackgroundColor = Color.FromArgb(selected ? (dark ? "#4F378B" : "#EADDFF") : (dark ? "#211F26" : "#FFFBFE"));
                label.TextColor = check.TextColor = Color.FromArgb(dark ? "#E6E1E5" : "#1D192B");
#else
                button.BackgroundColor = selected ? IosAccent : Color.FromArgb(dark ? "#2C2C2E" : "#FFFFFF");
                label.TextColor = check.TextColor = selected || dark ? Colors.White : Colors.Black;
                if (icon is Microsoft.Maui.Controls.Shapes.Path path)
                    path.Fill = new SolidColorBrush(selected ? Colors.White : IosAccent);
#endif
                SemanticProperties.SetHint(button, Text(selected ? "Selected" : "NotSelected"));
            });
        }
        var outline = new Border
        {
            Content = segments, Margin = new Thickness(0, 8),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
            StrokeThickness = 1,
        };
#if ANDROID
        outline.SetAppThemeColor(Border.StrokeProperty, Color.FromArgb("#79747E"), Color.FromArgb("#938F99"));
        segments.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#79747E"), Color.FromArgb("#938F99"));
#else
        outline.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 };
        outline.SetAppThemeColor(Border.StrokeProperty, Color.FromArgb("#C6C6C8"), Color.FromArgb("#545458"));
        segments.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#C6C6C8"), Color.FromArgb("#545458"));
#endif
        content.Add(outline);
    }

    #endregion

    #region Shared sheet header and content

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
        // UIKit supplies the grabber; leave room for it above the MAUI header.
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

    #endregion
}
#endif
