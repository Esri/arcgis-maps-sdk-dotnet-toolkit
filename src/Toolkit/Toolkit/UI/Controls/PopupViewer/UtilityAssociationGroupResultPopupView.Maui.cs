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
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class UtilityAssociationGroupResultPopupView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        private Image? _expandImage;
        private CollectionView? _resultsListView;
        private Grid? _showAllGrid;
        private Entry? _searchEntry;

        static UtilityAssociationGroupResultPopupView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            Grid layout = new Grid();
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));

            Image icon = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(5) };
            icon.Source = new FontImageSource() { Glyph = ToolkitIcons.ChevronDown, FontFamily = ToolkitIcons.FontFamilyName, Size = 18 };
            icon.SetBinding(IsVisibleProperty, static (UtilityAssociationGroupResultPopupView view) => view.IsSearchable, source: RelativeBindingSource.TemplatedParent, converter: InvertBoolConverter.Instance);
            layout.Add(icon);

            TapGestureRecognizer iconTapGesture = new TapGestureRecognizer();
            icon.GestureRecognizers.Add(iconTapGesture);

            Label title = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
            title.SetBinding(Label.TextProperty, static (UtilityAssociationGroupResultPopupView view) => view.GroupResult?.Name, source: RelativeBindingSource.TemplatedParent);
            title.SetBinding(IsVisibleProperty, static (UtilityAssociationGroupResultPopupView view) => view.IsSearchable, source: RelativeBindingSource.TemplatedParent, converter: InvertBoolConverter.Instance);
            title.Style = PopupViewer.GetPopupViewerTitleStyle();
            layout.Add(title);

            var border = new Border() { StrokeThickness = 0, WidthRequest = 30, HeightRequest = 20, BackgroundColor = Colors.LightGray, Margin = new Thickness(2) };
            border.StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(10) };
            border.SetBinding(IsVisibleProperty, static (UtilityAssociationGroupResultPopupView view) => view.IsSearchable, source: RelativeBindingSource.TemplatedParent, converter: InvertBoolConverter.Instance);

            Label count = new Label() { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
            count.SetBinding(Label.TextProperty, static (UtilityAssociationGroupResultPopupView view) => view.GroupResult?.AssociationResults.Count, source: RelativeBindingSource.TemplatedParent);
            count.Style = PopupViewer.GetPopupViewerCaptionStyle();
            border.Content = count;
            Grid.SetColumn(border, 2);
            layout.Add(border);

            var divider = new Border() { StrokeThickness = 0, HeightRequest = 1, BackgroundColor = Colors.LightGray, Margin = new Thickness(2) };
            divider.SetBinding(IsVisibleProperty, static (UtilityAssociationGroupResultPopupView view) => view.IsSearchable, source: RelativeBindingSource.TemplatedParent, converter: InvertBoolConverter.Instance);
            Grid.SetRow(divider, 1);
            Grid.SetColumnSpan(divider, 3);
            layout.Add(divider);

            Grid searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            searchGrid.SetBinding(IsVisibleProperty, static (UtilityAssociationGroupResultPopupView view) => view.IsSearchable, source: RelativeBindingSource.TemplatedParent);
            Grid.SetColumnSpan(searchGrid, 3);
            layout.Add(searchGrid);

            Grid innerGrid = new Grid();
            innerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            innerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            Image searchIcon = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(5) };
            searchIcon.Source = new FontImageSource() { Glyph = ToolkitIcons.Search, FontFamily = ToolkitIcons.FontFamilyName, Size = 18 };
            innerGrid.Add(icon);

            Entry searchText = new Entry() { Placeholder = Properties.Resources.GetString("PopupViewerUtilityAssociationsFilterByTitle"), VerticalOptions = LayoutOptions.Center, Margin = new Thickness(2) };
            innerGrid.Add(searchText);
            Grid.SetColumn(searchText, 1);
            innerGrid.Add(searchText);

            Border searchBorder = new Border() { StrokeThickness = 1 };
            searchBorder.Content = innerGrid;
            searchGrid.Add(searchBorder);

            CollectionView cv = new CollectionView() { SelectionMode = SelectionMode.None };
            cv.SetBinding(CollectionView.ItemsSourceProperty, static (UtilityAssociationGroupResultPopupView view) => view.GroupResult?.AssociationResults, source: RelativeBindingSource.TemplatedParent);
            cv.ItemTemplate = new DataTemplate(BuildDefaultItemTemplate);
            Grid.SetRow(cv, 3);
            Grid.SetColumn(cv, 1);
            Grid.SetColumnSpan(cv, 2);
            layout.Add(cv);

            Grid showAll = new Grid();
            showAll.SetBinding(Grid.IsVisibleProperty, static (UtilityAssociationGroupResultPopupView view) => view.IsSearchable, source: RelativeBindingSource.TemplatedParent, converter: InvertBoolConverter.Instance);
            TapGestureRecognizer showAllTapGesture = new TapGestureRecognizer();
            showAllTapGesture.Tapped += ShowAll_Tapped;
            showAll.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            showAll.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            showAll.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            showAll.RowDefinitions.Add(new RowDefinition(new GridLength(0.25, GridUnitType.Star)));
            showAll.RowDefinitions.Add(new RowDefinition(new GridLength(0.25, GridUnitType.Star)));
            showAll.RowDefinitions.Add(new RowDefinition(new GridLength(0.25, GridUnitType.Star)));

            Label showAllLabel = new Label() { VerticalTextAlignment = TextAlignment.End, FontSize = 12 };
            showAllLabel.Text = Properties.Resources.GetString("PopupViewerUtilityAssociationsShowAll");
            showAll.Add(showAllLabel);

            HorizontalStackLayout stack = new HorizontalStackLayout() { VerticalOptions = LayoutOptions.Center };
            Grid.SetRow(stack, 1);
            layout.Add(stack);

            Label totalLabel = new Label() { VerticalTextAlignment = TextAlignment.Start, Margin = new Thickness(0, 0, 2, 0) };
            totalLabel.Text = Properties.Resources.GetString("PopupViewerUtilityAssociationsTotal");
            stack.Add(totalLabel);

            Label totalCount = new Label() { VerticalTextAlignment = TextAlignment.Start, Margin = new Thickness(0, 0, 2, 0) };
            totalCount.SetBinding(Label.TextProperty, static (UtilityAssociationGroupResultPopupView view) => view.GroupResult?.AssociationResults.Count, source: RelativeBindingSource.TemplatedParent);
            totalCount.Style = PopupViewer.GetPopupViewerCaptionStyle();
            stack.Add(totalCount);

            Image listIcon = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(2) };
            listIcon.Source = new FontImageSource() { Glyph = ToolkitIcons.List, FontFamily = ToolkitIcons.FontFamilyName, Size = 18 };
            Grid.SetColumn(listIcon, 1);
            Grid.SetRowSpan(listIcon, 2);
            showAll.Add(listIcon);

            var showAllDivider = new Border() { StrokeThickness = 0, HeightRequest = 1, BackgroundColor = Colors.LightGray, Margin = new Thickness(2) };
            Grid.SetRow(showAllDivider, 2);
            Grid.SetColumnSpan(showAllDivider, 3);
            showAll.Add(showAllDivider);

            INameScope nameScope = new NameScope();
            nameScope.RegisterName("ExpandIcon", icon);
            nameScope.RegisterName("ShowAll", showAll);
            nameScope.RegisterName("Search", searchGrid);
            nameScope.RegisterName("SearchText", searchText);
            nameScope.RegisterName("ResultsList", cv);

            return layout;
        }

        private static object BuildDefaultItemTemplate()
        {
            Grid layout = new Grid();
            TapGestureRecognizer itemTapGesture = new TapGestureRecognizer();
            itemTapGesture.Tapped += ResultsListView_Tapped;
            layout.GestureRecognizers.Add(itemTapGesture);

            var view = new UtilityAssociationResultPopupView();
            view.SetBinding(UtilityAssociationResultPopupView.AssociationResultProperty, static (UtilityNetworks.UtilityAssociationResult result) => result);
            layout.Add(view);
            return layout;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_expandImage is not null && _expandImage.GestureRecognizers.First() is TapGestureRecognizer tapGesture)
            {
                tapGesture.Tapped -= ExpandIcon_Tapped;
            }

            if (_searchEntry is not null)
            {
                _searchEntry.TextChanged -= SearchText_TextChanged;
            }

            if (GetTemplateChild("ExpandIcon") is Image image && image.GestureRecognizers.First() is TapGestureRecognizer imageTapGesture)
            {
                _expandImage = image;
                imageTapGesture.Tapped += ExpandIcon_Tapped;
            }

            if (GetTemplateChild("ResultsList") is CollectionView listView)
            {
                _resultsListView = listView;
            }

            if (GetTemplateChild("ShowAll") is Grid grid)
            {
                _showAllGrid = grid;
            }

            if (GetTemplateChild("SearchText") is Entry entry)
            {
                _searchEntry = entry;
                _searchEntry.TextChanged += SearchText_TextChanged;
            }
        }

        private void UpdateView()
        {
            if (_resultsListView is not null)
            {
                _resultsListView.ItemsSource = IsSearchable ? GroupResult?.AssociationResults : GroupResult?.AssociationResults?.Take(DisplayCount);
            }

            if (_showAllGrid is not null)
            {
                _showAllGrid.IsVisible = !IsSearchable && GroupResult?.AssociationResults?.Count > DisplayCount;
            }
        }

        private void ExpandIcon_Tapped(object? sender, EventArgs e)
        {
            bool isExpanded = true;
            if (sender is Image expandIcon && expandIcon.Source is FontImageSource iconSource)
            {
                iconSource.Glyph = iconSource.Glyph == ToolkitIcons.ChevronDown ? ToolkitIcons.ChevronRight : ToolkitIcons.ChevronDown;
                isExpanded = iconSource.Glyph == ToolkitIcons.ChevronDown;
            }
            if (_resultsListView is not null)
            {
                _resultsListView.IsVisible = isExpanded;
            }
            if (_showAllGrid is not null)
            {
                _showAllGrid.IsVisible = isExpanded && GroupResult?.AssociationResults?.Count > DisplayCount;
            }
        }

        private static void ShowAll_Tapped(object? sender, EventArgs e)
        {
            if (sender is Grid showAllGrid && showAllGrid.BindingContext is UtilityNetworks.UtilityAssociationGroupResult groupResult)
            {
                var parent = PopupViewer.GetPopupViewerParent(showAllGrid);
                parent?.NavigateToItem(groupResult);
            }
        }

        private void SearchText_TextChanged(object? sender, TextChangedEventArgs e)
        {
            var searchText = (sender as Entry)?.Text.Trim();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                if (_resultsListView is not null)
                {
                    _resultsListView.ItemsSource = GroupResult?.AssociationResults?.Where(r => r.Title?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
                }
            }
            else
            {
                if (_resultsListView is not null)
                {
                    _resultsListView.ItemsSource = GroupResult?.AssociationResults;
                }
            }
        }

        private static void ResultsListView_Tapped(object? sender, EventArgs e)
        {
            var cell = sender as View;
            if (cell?.BindingContext is UtilityNetworks.UtilityAssociationResult result)
            {
                var parent = PopupViewer.GetPopupViewerParent(cell);
                parent?.NavigateToItem(Popup.FromGeoElement(result.AssociatedFeature));
            }
        }
    }
}
#endif