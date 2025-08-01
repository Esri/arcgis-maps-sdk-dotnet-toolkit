﻿// /*******************************************************************************
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

using Esri.ArcGISRuntime.Mapping.Floor;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using ListView = Microsoft.Maui.Controls.ListView;
using SearchBar = Microsoft.Maui.Controls.SearchBar;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

internal class FloorFilterBrowseSitesPage : ContentPage
{
    private Label _browseLabel;
    private Label _noResultLabel;
    private Button _closeButton;
    private SearchBar _searchBar;
    private CollectionView _unfilteredListView;
    private CollectionView _filteredListView;
    private Button _allSitesButton;
    private IList<FloorSite>? _itemsSource;

    private FloorFilter? _ff;

    public FloorFilterBrowseSitesPage(FloorFilter ff)
    {
        On<Microsoft.Maui.Controls.PlatformConfiguration.iOS>().SetUseSafeArea(true);
        _ff = ff;

        this.SetAppThemeColor(ContentPage.BackgroundColorProperty, Color.FromArgb("#fff"), Color.FromArgb("#353535"));

        Grid parentGrid = new Grid();
        parentGrid.SetAppThemeColor(Grid.BackgroundColorProperty, Color.FromArgb("#fff"), Color.FromArgb("#353535"));
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
        _browseLabel.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#6e6e6e"), Color.FromArgb("#fff"));

        Grid.SetRow(_browseLabel, 0);

        _closeButton = new Button
        {
            Text = ToolkitIcons.X,
            FontFamily = ToolkitIcons.FontFamilyName,
            WidthRequest = 32,
            HeightRequest = 32,
            CornerRadius = 16,
            Padding = new Thickness(0),
            Margin = new Thickness(8),
        };
        _closeButton.SetAppThemeColor(Button.BackgroundColorProperty, Color.FromArgb("#f3f3f3"), Color.FromArgb("#2b2b2b"));
        _closeButton.SetAppThemeColor(Button.TextColorProperty, Color.FromRgb(0, 122, 194), Color.FromRgb(0, 154, 242));

        _noResultLabel = new Label
        {
            Text = ff.NoResultsMessage,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(16),
        };

        _noResultLabel.SetAppThemeColor(Label.TextColorProperty, Color.FromRgb(0, 122, 194), Color.FromRgb(0, 154, 242));

        Grid.SetRow(_noResultLabel, 2);
        Grid.SetColumnSpan(_noResultLabel, 2);

        Grid.SetRow(_closeButton, 0);
        Grid.SetColumn(_closeButton, 1);

        _searchBar = new SearchBar { Placeholder = ff.SearchPlaceholder, Margin = new Thickness(0) };
        _searchBar.SetAppThemeColor(SearchBar.BackgroundColorProperty, Color.FromArgb("#F8F8F8"), Color.FromArgb("#353535"));
        Grid.SetRow(_searchBar, 1);
        Grid.SetColumnSpan(_searchBar, 2);

        _itemsSource = ff.AllSites;
        _noResultLabel.IsVisible = !(_itemsSource?.Any() == true);
        _unfilteredListView = new CollectionView
        {
            ItemsSource = _itemsSource,
            ItemTemplate = ff.SiteDataTemplate,
            //SelectedItem = _ff.SelectedSite, // Prevents navigation to that site
            IsVisible = !_noResultLabel.IsVisible,
            SelectionMode = SelectionMode.Single
        };

        Grid.SetRow(_unfilteredListView, 2);
        Grid.SetColumnSpan(_unfilteredListView, 2);

        _filteredListView = new CollectionView { ItemTemplate = ff.SiteDataTemplate, IsVisible = false, SelectionMode = SelectionMode.Single };
        Grid.SetRow(_filteredListView, 2);
        Grid.SetColumnSpan(_filteredListView, 2);

        _allSitesButton = new Button
        {
            Text = ff.AllFacilitiesLabel,
            IsVisible = (ff.AllSites?.Count() ?? 0) > 1,
            TextColor = Color.FromRgb(255, 255, 255),
            CornerRadius = 4,
            Margin = 8,
        };
        _allSitesButton.SetAppThemeColor(Button.BackgroundColorProperty, Color.FromRgb(0, 122, 194), Color.FromRgb(0, 154, 242));
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
        _filteredListView.SelectionChanged += HandleListItem_Tapped;
        _unfilteredListView.SelectionChanged += HandleListItem_Tapped;

        Content = parentGrid;
    }

    private void HandleListItem_Tapped(object? sender, SelectionChangedEventArgs e)
    {
        if (_ff == null || !e.CurrentSelection.Any())
        {
            return;
        }

        _ff.SelectedSite = e.CurrentSelection.FirstOrDefault() as FloorSite;
        if ((_ff.SelectedSite?.Facilities?.Count() ?? 0) > 1)
        {
            _ff.NavigateForward(new FloorFilterBrowseFacilitiesPage(_ff, false, true));
        }
        else
        {
            _ff.CloseBrowsing();
        }
        _unfilteredListView.SelectedItem = null;
        _filteredListView.SelectedItem = null;
    }

    private void HandleAllSites_Clicked(object? sender, EventArgs e) => _ff?.NavigateForward(new FloorFilterBrowseFacilitiesPage(_ff, true, true));

    private void HandleSearchText_Changed(object? sender, TextChangedEventArgs e)
    {
        var searchText = _searchBar.Text;
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var results = _ff?.AllSites?.Where(site =>
                site.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase));

            bool hasResults = results?.Any() ?? false;

            _filteredListView.ItemsSource = results;
            _filteredListView.SelectedItem = null;

            _unfilteredListView.IsVisible = false;
            _filteredListView.IsVisible = hasResults;
            _noResultLabel.IsVisible = !hasResults;
        }
        else
        {
            bool hasItems = _itemsSource?.Any() ?? false;

            _filteredListView.ItemsSource = null;
            _filteredListView.IsVisible = false;
            _unfilteredListView.IsVisible = hasItems;
            _noResultLabel.IsVisible = !hasItems;
        }
    }

    private void HandleClose_Clicked(object? sender, EventArgs e) => _ff?.CloseBrowsing();
}
