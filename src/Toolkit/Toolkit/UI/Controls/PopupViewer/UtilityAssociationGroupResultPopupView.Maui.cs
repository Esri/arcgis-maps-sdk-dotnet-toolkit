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
using Microsoft.Maui.Controls.Internals;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class UtilityAssociationGroupResultPopupView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        private CollectionView? _resultsListView;
        private Entry? _searchEntry;

        static UtilityAssociationGroupResultPopupView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));

            Grid searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Grid.SetColumnSpan(searchGrid, 2);

            Image searchIcon = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(5) };
            searchIcon.Source = new FontImageSource() { Glyph = ToolkitIcons.Search, Color = Colors.Gray, FontFamily = ToolkitIcons.FontFamilyName, Size = 18 };
            searchGrid.Add(searchIcon);

            Entry searchText = new Entry() { Placeholder = Properties.Resources.GetString("PopupViewerUtilityAssociationsFilterByTitle"), VerticalOptions = LayoutOptions.Center, Margin = new Thickness(2) };
            Grid.SetColumn(searchText, 1);
            searchGrid.Add(searchText);

            Border searchBorder = new Border() { StrokeThickness = 1 };
            searchBorder.Content = searchGrid;
            layout.Add(searchBorder);

            CollectionView cv = new CollectionView() { SelectionMode = SelectionMode.None };
            cv.SetBinding(CollectionView.ItemsSourceProperty, static (UtilityAssociationGroupResultPopupView view) => view.GroupResult?.AssociationResults, source: RelativeBindingSource.TemplatedParent);
            cv.ItemTemplate = new DataTemplate(BuildDefaultItemTemplate);
            Grid.SetRow(cv, 1);
            layout.Add(cv);

            INameScope nameScope = new NameScope();
            nameScope.RegisterName("SearchText", searchText);
            nameScope.RegisterName("ResultsList", cv);

            return layout;
        }

        private static object BuildDefaultItemTemplate()
        {
            Grid layout = new Grid() { Margin = new Thickness(10, 0, 0, 0) };
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
            return;

            // TODO: Investigate why view stops rendering with this code.
            if (_searchEntry is not null)
            {
                _searchEntry.TextChanged -= SearchText_TextChanged;
            }

            if (GetTemplateChild("ResultsList") is CollectionView listView)
            {
                _resultsListView = listView;
            }

            if (GetTemplateChild("SearchText") is Entry entry)
            {
                _searchEntry = entry;
                _searchEntry.TextChanged += SearchText_TextChanged;
            }
            UpdateView();
        }

        private void UpdateView()
        {
            if (_resultsListView is not null)
            {
                _resultsListView.ItemsSource = GroupResult?.AssociationResults;
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