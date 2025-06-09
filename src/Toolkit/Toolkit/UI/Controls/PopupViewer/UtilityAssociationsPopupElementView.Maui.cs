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
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.PopupViewer"/> control,
    /// used for rendering a <see cref="Esri.ArcGISRuntime.Mapping.Popups.UtilityAssociationsPopupElement"/>.
    /// </summary>
    public partial class UtilityAssociationsPopupElementView : ContentView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        /// <summary>
        /// Name of the carousel control in the template.
        /// </summary>
        public const string CarouselName = "Carousel";

        static UtilityAssociationsPopupElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            VerticalStackLayout root = new VerticalStackLayout();
            Label title = new Label();
            title.SetBinding(Label.TextProperty, static (UtilityAssociationsPopupElementView view) => view.Element?.Title, source: RelativeBindingSource.TemplatedParent, fallbackValue: Properties.Resources.GetString("UtilityAssociationsPopupElementViewDefaultTitle"));
            title.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationsPopupElementView view) => view.Element?.Title, source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance);
            title.Style = PopupViewer.GetPopupViewerTitleStyle();
            root.Add(title);
            Label description = new Label();
            description.SetBinding(Label.TextProperty, static (UtilityAssociationsPopupElementView view) => view.Element?.Description, source: RelativeBindingSource.TemplatedParent);
            description.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationsPopupElementView view) => view.Element?.Description, source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance);
            description.Style = PopupViewer.GetPopupViewerCaptionStyle();
            root.Add(description);
            root.Add(new Border() { StrokeThickness = 0, HeightRequest = 1, BackgroundColor = Colors.Gray, Margin = new Thickness(0, 5) });

            Grid resultGrid = new Grid();

            Grid noAssociationsGrid = new Grid() { Padding = new Thickness(10, 0, 0, 0) };
            noAssociationsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            noAssociationsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            noAssociationsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            noAssociationsGrid.SetBinding(VisualElement.IsVisibleProperty, static (Mapping.Popups.UtilityAssociationsPopupElement result) => result.AssociationsFilterResults, converter: Internal.EmptyToTrueConverter.Instance);

            Rectangle warningRectangle = new Rectangle() { WidthRequest = 1, Fill = Brush.Red, Margin = 5 };
            noAssociationsGrid.Add(warningRectangle);
            Image warningImage = new Image() { WidthRequest = 18, HeightRequest = 18 };
            warningImage.Source = new FontImageSource() { Glyph = ((char)0xE0EE).ToString(), Color = Colors.Red, FontFamily = "calcite-ui-icons-24", Size = 18 };
            Grid.SetColumn(warningImage, 1);
            noAssociationsGrid.Add(warningImage);
            Label warningText = new Label();
            warningText.SetBinding(Label.TextProperty, Properties.Resources.GetString("UtilityAssociationsPopupElementViewNoAssociationsFound"));
            Grid.SetColumn(warningText, 2);
            noAssociationsGrid.Add(warningText);

            resultGrid.Add(noAssociationsGrid);

            CollectionView cv = new CollectionView() { SelectionMode = SelectionMode.None };
            cv.SetBinding(CollectionView.ItemsSourceProperty, static (UtilityAssociationsPopupElementView view) => view.Element?.AssociationsFilterResults, source: RelativeBindingSource.TemplatedParent);
            cv.SetBinding(VisualElement.IsVisibleProperty, static (Mapping.Popups.UtilityAssociationsPopupElement result) => result.AssociationsFilterResults, converter: Internal.EmptyToFalseConverter.Instance);
            cv.ItemTemplate = new DataTemplate(BuildDefaultItemTemplate);

            resultGrid.Add(cv);

            root.Add(resultGrid);

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName("AssociationsList", cv);
            return root;
        }

        private static object BuildDefaultItemTemplate()
        {
            Grid layout = new Grid() { Padding = new Thickness(10, 0, 0, 0) };
            TapGestureRecognizer itemTapGesture = new TapGestureRecognizer();
            itemTapGesture.Tapped += Result_Tapped;
            layout.GestureRecognizers.Add(itemTapGesture);
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.SetBinding(VisualElement.IsVisibleProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result.ResultCount, converter: Internal.EmptyToFalseConverter.Instance);

            Label title = new Label();
            title.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result.Filter?.Title);
            title.SetBinding(VisualElement.IsVisibleProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result.Filter?.Title, converter: Internal.EmptyToFalseConverter.Instance);
            title.Style = PopupViewer.GetPopupViewerTitleStyle();
            layout.Add(title);
            Label description = new Label();
            description.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result.Filter?.Description);
            description.SetBinding(VisualElement.IsVisibleProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result.Filter?.Description, converter: Internal.EmptyToFalseConverter.Instance);
            description.Style = PopupViewer.GetPopupViewerCaptionStyle();

            Grid.SetRow(description, 1);
            layout.Add(description);
            Image image = new Image() { WidthRequest = 18, HeightRequest = 18 };
            image.Source = new FontImageSource() { Glyph = ((char)0xE078).ToString(), Color = Colors.Gray, FontFamily = "calcite-ui-icons-24", Size = 18 };
            Grid.SetColumn(image, 1);
            Grid.SetRowSpan(image, 2);
            layout.Add(image);

            Border root = new Border() { StrokeThickness = 0, Content = layout };
            return root;
        }


        private PopupViewer? GetPopupViewerParent()
        {
            var parent = this.Parent;
            while (parent is not null && parent is not PopupViewer popup)
            {
                parent = parent.Parent;
            }
            return parent as PopupViewer;
        }

        private static void Result_Tapped(object? sender, EventArgs e)
        {
            var cell = sender as View;
            Element? parent = cell?.Parent;
            while (parent is View && parent is not UtilityAssociationsPopupElementView)
            {
                parent = parent.Parent;
            }
            if (parent is UtilityAssociationsPopupElementView a && cell?.BindingContext is UtilityNetworks.UtilityAssociationsFilterResult result)
            {
                a.GetPopupViewerParent()?.NavigateToItem(result);
            }
        }
    }
}
#endif