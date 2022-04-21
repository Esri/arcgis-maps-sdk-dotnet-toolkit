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

using System;
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISRuntime.Mapping.Floor;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using ListView = Xamarin.Forms.ListView;
using SearchBar = Xamarin.Forms.SearchBar;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    internal class FloorFilterBrowseSitesPage : ContentPage
    {
        private Label _browseLabel;
        private Label _noResultLabel;
        private Button _closeButton;
        private SearchBar _searchBar;
        private ListView _unfilteredListView;
        private ListView _filteredListView;
        private Button _allSitesButton;
        private IList<FloorSite>? _itemsSource;

        private FloorFilter? _ff;

        public FloorFilterBrowseSitesPage(FloorFilter ff)
        {
            On<iOS>().SetUseSafeArea(true);
            _ff = ff;

            BackgroundColor = Color.White;

            Grid parentGrid = new Grid();
            parentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Browse label, close button
            parentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Search bar
            parentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star }); // list views
            parentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // "all" button

            parentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star }); // Browse label
            parentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Close button

            _browseLabel = new Label
            {
                Text = ff.BrowseSitesLabel,
                VerticalTextAlignment = TextAlignment.Center,
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                Margin = new Thickness(8, 2),
            };

            Grid.SetRow(_browseLabel, 0);

            _closeButton = new Button
            {
                Text = IconFont.X,
                FontFamily = "calcite-ui-icons-24",
                WidthRequest = 32,
                HeightRequest = 32,
                CornerRadius = 16,
                BackgroundColor = Color.FromHex("#f3f3f3"),
                Padding = new Thickness(0),
                Margin = new Thickness(8, 2),
                TextColor = Color.Accent,
            };

            _noResultLabel = new Label
            {
                Text = ff.NoResultsMessage,
                TextColor = Color.Accent,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(16),
            };

            Grid.SetRow(_noResultLabel, 2);
            Grid.SetColumnSpan(_noResultLabel, 2);

            Grid.SetRow(_closeButton, 0);
            Grid.SetColumn(_closeButton, 1);

            _searchBar = new SearchBar { Placeholder = ff.SearchPlaceholder, Margin = new Thickness(0) };
            Grid.SetRow(_searchBar, 1);
            Grid.SetColumnSpan(_searchBar, 2);

            _itemsSource = ff.AllSites;
            _noResultLabel.IsVisible = !(_itemsSource?.Any() ?? false);
            _unfilteredListView = new ListView
            {
                ItemsSource = _itemsSource,
                ItemTemplate = ff.SiteDataTemplate,
                SelectedItem = _ff.SelectedSite,
                IsVisible = !_noResultLabel.IsVisible,
            };

            Grid.SetRow(_unfilteredListView, 2);
            Grid.SetColumnSpan(_unfilteredListView, 2);

            _filteredListView = new ListView { ItemTemplate = ff.SiteDataTemplate, IsVisible = false };
            Grid.SetRow(_filteredListView, 2);
            Grid.SetColumnSpan(_filteredListView, 2);

            _allSitesButton = new Button
            {
                Text = ff.AllFacilitiesLabel,
                IsVisible = (ff.AllSites?.Count() ?? 0) > 1,
                TextColor = Color.White,
                BackgroundColor = Color.Accent,
                CornerRadius = 4,
                Margin = 8,
            };
            Grid.SetRow(_allSitesButton, 3);
            Grid.SetColumnSpan(_allSitesButton, 2);

            parentGrid.Children.Add(_browseLabel);
            parentGrid.Children.Add(_closeButton);
            parentGrid.Children.Add(_searchBar);
            parentGrid.Children.Add(_noResultLabel);
            parentGrid.Children.Add(_unfilteredListView);
            parentGrid.Children.Add(_filteredListView);
            parentGrid.Children.Add(_allSitesButton);

            _closeButton.Clicked += HandleClose_Clicked;
            _searchBar.TextChanged += HandleSearchText_Changed;
            _allSitesButton.Clicked += HandleAllSites_Clicked;
            _filteredListView.ItemTapped += HandleListItem_Tapped;
            _unfilteredListView.ItemTapped += HandleListItem_Tapped;

            Content = parentGrid;
        }

        private void HandleListItem_Tapped(object sender, ItemTappedEventArgs e)
        {
            if (_ff == null)
            {
                return;
            }

            _ff.SelectedSite = e.Item as FloorSite;
            if ((_ff.SelectedSite?.Facilities?.Count() ?? 0) > 1)
            {
                _ff.NavigateForward(new FloorFilterBrowseFacilitiesPage(_ff, false, true));
            }
            else
            {
                _ff.CloseBrowsing();
            }
        }

        private void HandleAllSites_Clicked(object sender, EventArgs e) => _ff?.NavigateForward(new FloorFilterBrowseFacilitiesPage(_ff, true, true));

        private void HandleSearchText_Changed(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_searchBar.Text))
            {
                var simplified = _searchBar.Text.ToLower();
                var results = _ff?.AllSites?.Where(site => site.Name.ToLower().Contains(simplified));

                _filteredListView.ItemsSource = results;

                if (_ff?.SelectedSite != null && results.Contains(_ff.SelectedSite))
                {
                    _filteredListView.SelectedItem = _ff.SelectedSite;
                }

                _unfilteredListView.IsVisible = false;
                _filteredListView.IsVisible = results.Any();
                _noResultLabel.IsVisible = !(results?.Any() ?? false);
            }
            else
            {
                _filteredListView.ItemsSource = null;
                _filteredListView.IsVisible = false;
                _unfilteredListView.IsVisible = _itemsSource.Any();
                _noResultLabel.IsVisible = !_itemsSource.Any();
            }
        }

        private void HandleClose_Clicked(object sender, EventArgs e) => _ff?.CloseBrowsing();
    }
}
