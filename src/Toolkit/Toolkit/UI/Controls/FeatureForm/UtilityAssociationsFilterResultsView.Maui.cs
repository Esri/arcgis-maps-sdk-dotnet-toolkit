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

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class UtilityAssociationsFilterResultsView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static UtilityAssociationsFilterResultsView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var border = new Border() { StrokeThickness = 1, Margin = new Thickness(0, 4), VerticalOptions = LayoutOptions.Start };
            border.SetAppThemeColor(Border.StrokeProperty, Colors.Black, Colors.White);
            CollectionView cv = new CollectionView() { SelectionMode = SelectionMode.None };
            cv.SetBinding(CollectionView.ItemsSourceProperty, static (UtilityAssociationsFilterResultsView view) => view.AssociationsFilterResult?.GroupResults, source: RelativeBindingSource.TemplatedParent);
            cv.ItemTemplate = new DataTemplate(BuildDefaultItemTemplate);
            border.Content = cv;

            Button addAssociationButton = new Button()
            {
                Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsAddAssociation"),
                ImageSource = new FontImageSource()
                {
                    Glyph = ToolkitIcons.Plus,
                    Color = Color.FromArgb("#007AC2"),
                    FontFamily = ToolkitIcons.FontFamilyName,
                    Size = 16,
                },
                ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 6),
                BackgroundColor = Colors.Transparent,
                BorderWidth = 0,
                Padding = new Thickness(6, 4),
                TextColor = Color.FromArgb("#007AC2"),
                IsVisible = false,
#if __IOS__ || MACCATALYST
                HorizontalOptions = LayoutOptions.Start,
#else
                HorizontalOptions = LayoutOptions.Center,
#endif
            };
            Grid.SetRow(addAssociationButton, 1);

            root.Add(border);
            root.Add(addAssociationButton);

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName("ResultsList", cv);
            nameScope.RegisterName("AddAssociationButton", addAssociationButton);
            return root;
        }

        private static object BuildDefaultItemTemplate()
        {
            Grid layout = new Grid() { Padding = new Thickness(8, 0, 8, 0), MinimumHeightRequest = 40 };
            TapGestureRecognizer itemTapGesture = new TapGestureRecognizer();
            itemTapGesture.Tapped += Result_Tapped;
            layout.GestureRecognizers.Add(itemTapGesture);
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            Label title = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
            title.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationGroupResult result) => result.Name);
            title.Style = FeatureFormView.GetFeatureFormTitleStyle();
            layout.Add(title);

            Label count = new Label() { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End };
            count.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationGroupResult result) => result.AssociationResults.Count);
            count.Style = FeatureFormView.GetFeatureFormCaptionStyle();
            Grid.SetColumn(count, 1);
            layout.Add(count);

            Image image = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center };
            image.Source = new FontImageSource() { Glyph = ToolkitIcons.ChevronRight, Color = Colors.Gray, FontFamily = ToolkitIcons.FontFamilyName, Size = 18 };
            Grid.SetColumn(image, 2);
            layout.Add(image);

            return layout;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateAddAssociationButton();
        }

        private static void Result_Tapped(object? sender, EventArgs e)
        {
            var cell = sender as View;
            if (cell?.BindingContext is UtilityNetworks.UtilityAssociationGroupResult result)
            {
                var parent = FeatureFormView.GetFeatureFormViewParent(cell);
                parent?.NavigateToItem(result, FeatureFormView.GetParent<UtilityAssociationsFilterResultsView>(cell)?.AssociationsFilterResult); 
            }
        }

        private async partial void ShowAddAssociationMenu(object? flyoutTarget)
        {
            string fromNetworkDataSource = Properties.Resources.GetString("FeatureFormUtilityAssociationsSelectFromNetworkDataSource")!;

#if WINDOWS
            if (flyoutTarget is Button button &&
                button.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement nativeButton)
            {
                Microsoft.UI.Xaml.Controls.MenuFlyout flyout = new Microsoft.UI.Xaml.Controls.MenuFlyout();
                // Add the On Map option here when map-based association selection is implemented.
                // if (CanSelectAssociationOnMap())
                // {
                //     flyout.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutItem() { Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsSelectOnMap") });
                // }
                var fromNetworkDataSourceItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem() { Text = fromNetworkDataSource };
                fromNetworkDataSourceItem.Click += (_, _) => SelectFromNetworkDataSource();
                flyout.Items.Add(fromNetworkDataSourceItem);
                if (flyout.Items.Count == 1)
                {
                    // If there's only one entry, no reason to ask users to pick from a menu, just go straight to the action.
                    SelectFromNetworkDataSource();
                    return;
                }
                flyout.ShowAt(nativeButton);
            }
#else
            // Add the On Map option here when map-based association selection is implemented.
            // var onMap = CanSelectAssociationOnMap()
            //     ? Properties.Resources.GetString("FeatureFormUtilityAssociationsSelectOnMap")
            //     : null;
            var actions = new List<string>() { fromNetworkDataSource };
            if (actions.Count == 1)
            {
                // If there's only one entry, no reason to ask users to pick from a menu, just go straight to the action.
                SelectFromNetworkDataSource();
                return;
            }

            Page? page = Window?.Page;
            if (page is null)
            {
                return;
            }
            string? title = Properties.Resources.GetString("FeatureFormUtilityAssociationsChooseAddMethod");
            string cancel = Properties.Resources.GetString("FeatureFormDeleteAssociationConfirmationCancel")!;
            var selectedAction = await page.DisplayActionSheetAsync(title, cancel, null, actions.ToArray());
            if (selectedAction == fromNetworkDataSource)
            {
                SelectFromNetworkDataSource();
            }
#endif
        }
    }
}
#endif