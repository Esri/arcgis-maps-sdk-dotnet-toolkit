#if ANDROID || __IOS__
namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView
{
    #region Mode buttons

    private void AddMobileModeButtons(VerticalStackLayout content, ImageSearchModeVM mode, Func<bool> enabled)
    {
        var buttons = new Grid { ColumnSpacing = 1 };
#if ANDROID
        buttons.ColumnSpacing = 2;
        buttons.Margin = new Thickness(0, 8);
#endif
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
#if ANDROID
            icon.WidthRequest = icon.HeightRequest = 18;
            var iconSurface = new Border
            {
                Content = icon, WidthRequest = 24, HeightRequest = 24,
                StrokeThickness = 0, BackgroundColor = Colors.White,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                VerticalOptions = LayoutOptions.Center, InputTransparent = true,
            };
#endif
            var label = new Label
            {
                Text = title, VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.WordWrap,
            };
#if __IOS__
            button.MinimumHeightRequest = 52;
            button.CornerRadius = 26;
            label.FontSize = 17;
            label.FontAttributes = FontAttributes.Bold;
#endif
            // Keep the caption separate so large text can wrap above the native button.
            var caption = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 6, Padding = new Thickness(8, 4),
                VerticalOptions = LayoutOptions.Center, InputTransparent = true,
            };
#if ANDROID
            caption.Add(iconSurface);
#else
            caption.Add(icon);
#endif
            caption.Add(label, 1);
            segment.Add(caption);
            SemanticProperties.SetDescription(button, title);
            button.Clicked += (_, _) => action.Execute();
            int index = buttons.ColumnDefinitions.Count;
            buttons.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
#if ANDROID
            button.BackgroundColor = Colors.Transparent;
            var shape = new Microsoft.Maui.Controls.Shapes.RoundRectangle();
            var surface = new Border { Content = segment, StrokeThickness = 0, StrokeShape = shape };
            buttons.Add(surface, index);
#else
            buttons.Add(segment, index);
#endif
            _panelUpdates.Add(() =>
            {
                bool selected = action.Checked?.Invoke() == true;
                bool dark = Application.Current?.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark;
                button.IsEnabled = action.Enabled();
                caption.Opacity = button.IsEnabled ? 1 : 0.45;
#if ANDROID
                double leading = selected || index == 0 ? 24 : 8;
                double trailing = selected || index == mode.ImageSearchModes.Count - 1 ? 24 : 8;
                bool rtl = FlowDirection == FlowDirection.RightToLeft;
                double left = rtl ? trailing : leading;
                double right = rtl ? leading : trailing;
                shape.CornerRadius = new CornerRadius(left, right, left, right);
                surface.BackgroundColor = Color.FromArgb(selected ? (dark ? "#CCC2DC" : "#5D5C72") : (dark ? "#4A4458" : "#E2E0F9"));
                label.TextColor = Color.FromArgb(selected ? (dark ? "#332D41" : "#FFFFFF") : (dark ? "#E8DEF8" : "#454451"));
                if (icon is Microsoft.Maui.Controls.Shapes.Path path)
                    path.Fill = new SolidColorBrush(Color.FromArgb("#777680"));
#else
                button.BackgroundColor = selected ? Color.FromArgb(dark ? "#48484A" : "#E5E5EA") : Colors.Transparent;
                label.TextColor = selected ? IosAccent : (dark ? Colors.White : Colors.Black);
                if (icon is Microsoft.Maui.Controls.Shapes.Path path)
                    path.Fill = new SolidColorBrush(Color.FromArgb(dark ? "#AEAEB2" : "#636366"));
#endif
                SemanticProperties.SetHint(button, Text(selected ? "Selected" : "NotSelected"));
            });
        }
#if ANDROID
        content.Add(buttons);
#else
        var outline = new Border
        {
            Content = buttons, Margin = new Thickness(0, 8), Padding = 2,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 28 },
            StrokeThickness = 0.5,
        };
        buttons.ColumnSpacing = 0;
        outline.SetAppThemeColor(Border.StrokeProperty, Color.FromArgb("#E5E5EA"), Color.FromArgb("#545458"));
        outline.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#80FFFFFF"), Color.FromArgb("#802C2C2E"));
        content.Add(outline);
#endif
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
#if __IOS__
            StyleIosSheetHeaderButton(previous);
#else
            SetSheetIconColor(previous.ImageSource);
#endif
            previous.Clicked += (_, _) => back();
            header.Add(previous);
        }
#if __IOS__
        header.ColumnDefinitions[0].Width = header.ColumnDefinitions[2].Width = new GridLength(44);
        heading.HorizontalTextAlignment = TextAlignment.Center;
        heading.VerticalTextAlignment = TextAlignment.Center;
        heading.LineBreakMode = LineBreakMode.WordWrap;
        if (back == null)
#endif
        {
            var close = ToolButton("ClosePanel", "", "OrientedImageryPanelClose");
            close.Text = "X";
            close.MinimumWidthRequest = 44;
            close.BorderWidth = 0;
            close.Clicked += (_, _) => dismiss();
#if ANDROID
            close.Text = null;
            close.ImageSource = new FontImageSource { Glyph = ToolkitIcons.X, FontFamily = ToolkitIcons.FontFamilyName, Size = 18 };
            SetSheetIconColor(close.ImageSource);
            header.Add(close, 2);
#else
            close.Text = "\u00D7";
            close.FontSize = 22;
            StyleIosSheetHeaderButton(close);
            header.Add(close);
#endif
        }
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
        surface.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#F0ECF4"), Color.FromArgb("#211F26"));
#else
        View surface = layout;
#endif
        return new ContentView
        {
            Content = surface, AutomationId = automationId, BackgroundColor = Colors.Transparent,
        };
    }

#if __IOS__
    private static void StyleIosSheetHeaderButton(Button button)
    {
        button.FontAutoScalingEnabled = false;
        button.Padding = 0;
        button.HeightRequest = 44;
        button.VerticalOptions = LayoutOptions.Center;
        button.CornerRadius = 22;
        button.BorderWidth = 0.5;
        button.SetAppThemeColor(Button.TextColorProperty, Colors.Black, Colors.White);
        button.SetAppThemeColor(Button.BorderColorProperty, Color.FromArgb("#D1D1D6"), Color.FromArgb("#636366"));
        button.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#B3FFFFFF"), Color.FromArgb("#8048484A"));
        if (button.ImageSource is FontImageSource icon)
            icon.SetAppThemeColor(FontImageSource.ColorProperty, Colors.Black, Colors.White);
    }
#endif

    #endregion
}
#endif
