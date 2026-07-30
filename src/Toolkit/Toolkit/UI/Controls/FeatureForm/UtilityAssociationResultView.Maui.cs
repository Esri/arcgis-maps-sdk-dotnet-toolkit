// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class UtilityAssociationResultView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static UtilityAssociationResultView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            Grid layout = new Grid() { MinimumHeightRequest = 40 };
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            Image icon = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(0,0,4,0) };
            Grid.SetRowSpan(icon, 2);
            layout.Add(icon);

            Label title = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
            title.SetBinding(Label.TextProperty, static (UtilityAssociationResultView result) => result.AssociationResult?.Title, source: RelativeBindingSource.TemplatedParent);
            title.Style = FeatureFormView.GetFeatureFormTitleStyle();
            Grid.SetColumn(title, 1);
            layout.Add(title);

            Label fractionAlong = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
            Grid.SetColumn(fractionAlong, 2);
            Grid.SetRowSpan(fractionAlong, 2);
            layout.Add(fractionAlong);

            Label connectionInfo = new Label() { Style = FeatureFormView.GetFeatureFormCaptionStyle(), IsVisible = false, LineBreakMode = LineBreakMode.TailTruncation, Margin = new Thickness(0,0,2,0) };
            connectionInfo.SetBinding(ToolTipProperties.TextProperty, static (Label label) => label.Text, source: RelativeBindingSource.Self);
            Grid.SetRow(connectionInfo, 1);
            Grid.SetColumn(connectionInfo, 1);
            layout.Add(connectionInfo);

            Button detailsButton = new Button() { Text = "...", VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center, WidthRequest = 32, Padding = 0, BackgroundColor = Colors.Transparent, BorderWidth = 0 };
            detailsButton.SetAppThemeColor(Button.TextColorProperty, Colors.DarkGray, Color.FromRgb(225, 225, 225));
            Grid.SetColumn(detailsButton, 3);
            Grid.SetRowSpan(detailsButton, 2);
            layout.Add(detailsButton);
            // TODO: Set theme-based background once https://github.com/dotnet/maui/issues/26620 is addressed
            // var g = new VisualStateGroup();
            // g.States.Add(new VisualState() { Name = "Normal" });
            // g.States.Add(new VisualState() { Name = "PointerOver" });
            // g.States[1].Setters.Add(new Setter() { Property = Grid.BackgroundColorProperty, Value = Colors.LightGray });
            // VisualStateManager.SetVisualStateGroups(layout, new VisualStateGroupList { g });

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(layout, nameScope);
            nameScope.RegisterName("FractionAlong", fractionAlong);
            nameScope.RegisterName("Icon", icon);
            nameScope.RegisterName("ConnectionInfo", connectionInfo);
            nameScope.RegisterName("DetailsButton", detailsButton);
            return layout;
        }

        private async partial void ShowDetailsFlyout(FeatureFormView parent, object? flyoutTarget)
        {
            string? showOnMap = parent.GeoView is null ? null : Properties.Resources.GetString("FeatureFormUtilityAssociationsShowOnMap");
            string? moreInformation = Properties.Resources.GetString("FeatureFormUtilityAssociationsMoreInformation");
            string? removeAssociation = CanRemoveAssociation(parent) ? Properties.Resources.GetString("FeatureFormUtilityAssociationsRemoveAssociation") : null;

#if WINDOWS
            // On Windows the ActionSheet doesn't look great, so we use the native contextmenu instead.
            if (flyoutTarget is Button button &&
                button.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement nativeButton)
            {
                ShowNativeDetailsFlyout(parent, nativeButton, ResolveNativeToolkitIconsFontFamily(button), showOnMap, moreInformation, removeAssociation);
                return;
            }
#endif

            Page? page = Window?.Page;
            if (page is null)
            {
                return;
            }

            var actions = new List<string>();
            if (showOnMap is not null)
            {
                actions.Add(showOnMap);
            }

            if (moreInformation is not null)
            {
                actions.Add(moreInformation);
            }
            if (removeAssociation is not null)
            {
                actions.Add(removeAssociation);
            }
            
            string cancel = Properties.Resources.GetString("FeatureFormDeleteAssociationConfirmationCancel")!;
            string? action = await page.DisplayActionSheet(AssociationResult?.Title, cancel, null, actions.ToArray());
            if (action == showOnMap)
            {
                ShowAssociationOnMap(parent);
            }
            else if (action == moreInformation)
            {
                NavigateToAssociationDetails(parent);
            }
            else if (action == removeAssociation)
            {
                _ = RemoveAssociation(AssociationResult.Association, parent);
            }
        }

#if WINDOWS
        private void ShowNativeDetailsFlyout(
            FeatureFormView parent,
            Microsoft.UI.Xaml.FrameworkElement target,
            Microsoft.UI.Xaml.Media.FontFamily iconFontFamily,
            string? showOnMap,
            string? moreInformation,
            string? removeAssociation)
        {
            Microsoft.UI.Xaml.Controls.MenuFlyout flyout = new Microsoft.UI.Xaml.Controls.MenuFlyout();

            if (showOnMap is not null)
            {
                flyout.Items.Add(CreateNativeMenuFlyoutItem(
                    showOnMap,
                    ToolkitIcons.CenterMap,
                    iconFontFamily,
                    (s, e) => ShowAssociationOnMap(parent)));
            }

            if (moreInformation is not null)
            {
                flyout.Items.Add(CreateNativeMenuFlyoutItem(
                    moreInformation,
                    ToolkitIcons.MoreInformation,
                    iconFontFamily,
                    (s, e) => NavigateToAssociationDetails(parent)));
            }

            if (removeAssociation is not null)
            {
                Microsoft.UI.Xaml.Media.SolidColorBrush foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                flyout.Items.Add(CreateNativeMenuFlyoutItem(
                    removeAssociation,
                    ToolkitIcons.Delete,
                    iconFontFamily,
                    (s, e) => _ = RemoveAssociation(AssociationResult?.Association, parent),
                    foreground));
            }

            flyout.ShowAt(target);
        }

        private Microsoft.UI.Xaml.Controls.MenuFlyoutItem CreateNativeMenuFlyoutItem(
            string text,
            string glyph,
            Microsoft.UI.Xaml.Media.FontFamily iconFontFamily,
            Microsoft.UI.Xaml.RoutedEventHandler clickHandler,
            Microsoft.UI.Xaml.Media.Brush? foreground = null)
        {
            Microsoft.UI.Xaml.Controls.FontIcon icon = new Microsoft.UI.Xaml.Controls.FontIcon
            {
                Glyph = glyph,
                FontFamily = iconFontFamily
            };

            Microsoft.UI.Xaml.Controls.MenuFlyoutItem item = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem
            {
                Text = text,
                Icon = icon
            };

            if (foreground is not null)
            {
                icon.Foreground = foreground;
                item.Foreground = foreground;
            }

            item.Click += clickHandler;
            return item;
        }

        private static Microsoft.UI.Xaml.Media.FontFamily ResolveNativeToolkitIconsFontFamily(Button button)
        {
            if (button.Handler?.MauiContext?.Services.GetService(typeof(Microsoft.Maui.IFontManager)) is Microsoft.Maui.IFontManager fontManager)
            {
                return fontManager.GetFontFamily(Microsoft.Maui.Font.OfSize(ToolkitIcons.FontFamilyName, 16));
            }

            return new Microsoft.UI.Xaml.Media.FontFamily(ToolkitIcons.FontFamilyName);
        }
#endif
    }
}
#endif
