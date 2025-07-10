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
using Microsoft.Maui.Controls.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class UtilityAssociationsFilterResultsPopupView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static UtilityAssociationsFilterResultsPopupView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            CollectionView cv = new CollectionView() { SelectionMode = SelectionMode.None };
            cv.SetBinding(CollectionView.ItemsSourceProperty, static (UtilityAssociationsFilterResultsPopupView view) => view.AssociationsFilterResult?.GroupResults, source: RelativeBindingSource.TemplatedParent);
            cv.ItemTemplate = new DataTemplate(BuildDefaultItemTemplate);

            INameScope nameScope = new NameScope();
            nameScope.RegisterName("ResultsList", cv);
            return cv;
        }

        private static object BuildDefaultItemTemplate()
        {
            Grid layout = new Grid() { Margin = new Thickness(10, 0, 0, 0) };
            TapGestureRecognizer itemTapGesture = new TapGestureRecognizer();
            itemTapGesture.Tapped += Result_Tapped;
            layout.GestureRecognizers.Add(itemTapGesture);

            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            Label title = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
            title.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationGroupResult result) => result.Name);
            title.SetBinding(VisualElement.IsVisibleProperty, static (UtilityNetworks.UtilityAssociationGroupResult result) => result.Name, converter: Internal.EmptyToFalseConverter.Instance);
            title.Style = PopupViewer.GetPopupViewerTitleStyle();
            layout.Add(title);

            var border = new Border() { StrokeThickness = 0, WidthRequest = 30, HeightRequest = 20, BackgroundColor = Colors.LightGray, Margin = new Thickness(2) };
            border.StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(10) };

            Label count = new Label() { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
            count.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationGroupResult result) => result.AssociationResults.Count);
            count.Style = PopupViewer.GetPopupViewerCaptionStyle();
            border.Content = count;
            Grid.SetColumn(border, 1);
            layout.Add(border);

            Image image = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(5) };
            image.Source = new FontImageSource() { Glyph = ToolkitIcons.ChevronRight, Color = Colors.Gray, FontFamily = ToolkitIcons.FontFamilyName, Size = 18 };
            Grid.SetColumn(image, 2);
            layout.Add(image);

            var divider = new Border() { StrokeThickness = 0, HeightRequest = 1, BackgroundColor = Colors.LightGray, Margin = new Thickness(2) };
            Grid.SetRow(divider, 1);
            Grid.SetColumnSpan(divider, 3);
            layout.Add(divider);

            return layout;
        }

        private static void Result_Tapped(object? sender, EventArgs e)
        {
            var cell = sender as View;
            if (cell?.BindingContext is UtilityNetworks.UtilityAssociationGroupResult result)
            {
                var parent = PopupViewer.GetPopupViewerParent(cell);
                parent?.NavigateToItem(result);
            }
        }
    }
}
#endif