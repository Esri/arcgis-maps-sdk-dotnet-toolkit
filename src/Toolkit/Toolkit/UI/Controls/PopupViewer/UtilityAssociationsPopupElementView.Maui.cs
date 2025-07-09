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

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class UtilityAssociationsPopupElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private Label? _titleLabel;
        private CollectionView? _associationsListView;
        private Grid? _noAssociationsGrid;

        static UtilityAssociationsPopupElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            VerticalStackLayout root = new VerticalStackLayout();

            Label title = new Label();
            title.SetBinding(Label.TextProperty, static (UtilityAssociationsPopupElementView view) => view.Element?.Title, source: RelativeBindingSource.TemplatedParent, fallbackValue: Properties.Resources.GetString("PopupViewerUtilityAssociationsDefaultTitle"));
            title.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationsPopupElementView view) => view.Element?.Title, source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance);
            title.Style = PopupViewer.GetPopupViewerTitleStyle();
            root.Add(title);

            Label description = new Label();
            description.SetBinding(Label.TextProperty, static (UtilityAssociationsPopupElementView view) => view.Element?.Description, source: RelativeBindingSource.TemplatedParent);
            description.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationsPopupElementView view) => view.Element?.Description, source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance);
            description.Style = PopupViewer.GetPopupViewerCaptionStyle();
            root.Add(description);

            CollectionView cv = new CollectionView() { SelectionMode = SelectionMode.None };
            cv.SetBinding(CollectionView.ItemsSourceProperty, static (UtilityAssociationsPopupElementView view) => view.Element?.AssociationsFilterResults, source: RelativeBindingSource.TemplatedParent);
            cv.ItemTemplate = new DataTemplate(BuildDefaultItemTemplate);
            root.Add(cv);

            Grid noAssociationsGrid = new Grid() { Margin = new Thickness(10, 0, 0, 0) };
            noAssociationsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            noAssociationsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            noAssociationsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            var divider = new Border() { StrokeThickness = 0, WidthRequest = 1, BackgroundColor = Colors.Red, Margin = new Thickness(2) };
            noAssociationsGrid.Add(divider);

            Image warningImage = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Start, Margin = new Thickness(5) };
            warningImage.Source = new FontImageSource() { Glyph = ToolkitIcons.ExclamationMarkTriangle, Color = Colors.Red, FontFamily = ToolkitIcons.FontFamilyName, Size = 18 };
            Grid.SetColumn(warningImage, 1);
            noAssociationsGrid.Add(warningImage);

            Label warningText = new Label() { Margin = new Thickness(5) };
            warningText.Text = Properties.Resources.GetString("PopupViewerUtilityAssociationsNone");
            Grid.SetColumn(warningText, 2);
            noAssociationsGrid.Add(warningText);

            root.Add(noAssociationsGrid);

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName("Title", title);
            nameScope.RegisterName("AssociationsList", cv);
            nameScope.RegisterName("NoAssociationsGrid", noAssociationsGrid);

            return root;
        }

        private static object BuildDefaultItemTemplate()
        {
            Grid layout = new Grid() { Margin = new Thickness(10, 0, 0, 0) };
            TapGestureRecognizer itemTapGesture = new TapGestureRecognizer();
            itemTapGesture.Tapped += Result_Tapped;
            layout.GestureRecognizers.Add(itemTapGesture);

            var view = new UtilityAssociationsFilterResultsPopupView() { IsExpanded = false };
            view.SetBinding(UtilityAssociationsFilterResultsPopupView.AssociationsFilterResultProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result);
            layout.Add(view);

            Border root = new Border() { StrokeThickness = 0, Content = layout };
            return root;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("Title") is Label label)
            {
                _titleLabel = label;
            }
            if (GetTemplateChild("AssociationsList") is CollectionView listView)
            {
                _associationsListView = listView;
            }
            if (GetTemplateChild("NoAssociationsGrid") is Grid grid)
            {
                _noAssociationsGrid = grid;
            }
            UpdateView();
        }

        private void UpdateView()
        {
            if (_titleLabel is not null)
            {
                _titleLabel.Text = (Element is null || string.IsNullOrEmpty(Element.Title)) ? Properties.Resources.GetString("PopupViewerUtilityAssociationsDefaultTitle") : Element.Title;
            }

            bool hasAssociations = Element?.AssociationsFilterResults.Any(r => r.ResultCount > 0) == true;
            if (_associationsListView is not null)
            {
                _associationsListView.ItemsSource = Element?.AssociationsFilterResults.Where(r => r.ResultCount > 0);
                _associationsListView.IsVisible = hasAssociations;
            }
            if (_noAssociationsGrid is not null)
            {
                _noAssociationsGrid.IsVisible = !hasAssociations;
            }
        }

        private static void Result_Tapped(object? sender, EventArgs e)
        {
            var cell = sender as View;
            if (cell?.BindingContext is UtilityNetworks.UtilityAssociationsFilterResult result)
            {
                var parent = PopupViewer.GetPopupViewerParent(cell);
                parent?.NavigateToItem(result);
            }
        }
    }
}
#endif