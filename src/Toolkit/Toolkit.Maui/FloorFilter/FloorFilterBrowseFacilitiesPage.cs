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

using Esri.ArcGISRuntime.Mapping.Floor;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using ListView = Microsoft.Maui.Controls.ListView;
using SearchBar = Microsoft.Maui.Controls.SearchBar;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

internal class FloorFilterBrowseFacilitiesPage : ContentPage
{
    private Label _browseLabel;
    private Label _noResultLabel;
    private Button _closeButton;
    private Button? _backButton;
    private SearchBar _searchBar;
    private CollectionView _unfilteredListView;
    private CollectionView _filteredListView;
    private bool _isAllSites;
    private IList<FloorFacility>? _itemsSource;

    private FloorFilter? _ff;

    internal FloorFilterBrowseFacilitiesPage(FloorFilter ff, bool isAllSites, bool shouldShowBack)
    {
        On<iOS>().SetUseSafeArea(true);

        _ff = ff;
        this.SetAppThemeColor(ContentPage.BackgroundColorProperty, Color.FromArgb("#fff"), Color.FromArgb("#353535"));
        _isAllSites = isAllSites;

        Grid parentGrid = new Grid();
        parentGrid.SetAppThemeColor(Grid.BackgroundColorProperty, Color.FromArgb("#fff"), Color.FromArgb("#353535"));
        parentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Browse label, close button
        parentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Search bar
        parentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star }); // list views

        parentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Back button
        parentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star }); // Browse label
        parentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Close button
        const string vsmDictionaryString =
$@"<ResourceDictionary xmlns=""http://schemas.microsoft.com/dotnet/2021/maui"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"">
<Style TargetType=""Grid"">
                        <Setter Property=""VisualStateManager.VisualStateGroups"">
                            <VisualStateGroupList>
                                <VisualStateGroup x:Name=""CommonStates"">
                                    <VisualState x:Name=""Normal"">
                                        <VisualState.Setters>
                                            <Setter Property=""BackgroundColor""
                                                    Value=""{{AppThemeBinding Light=#fff,Dark=#353535}}"" />
                                        </VisualState.Setters>
                                    </VisualState>
                                    <VisualState x:Name=""Selected"">
                                        <VisualState.Setters>
                                            <Setter Property=""BackgroundColor""
                                                    Value=""{{AppThemeBinding Light=#e2f1fb,Dark=#009af2}}"" />
                                        </VisualState.Setters>
                                    </VisualState>
                                </VisualStateGroup>
                            </VisualStateGroupList>
                        </Setter>
                    </Style>
                    <Style TargetType=""Label"">
                        <Setter Property=""TextColor"" Value=""{{AppThemeBinding Light=#6e6e6e,Dark=#fff}}"" />
                    </Style>
        </ResourceDictionary>";
        var vsmDictionary = new ResourceDictionary().LoadFromXaml(vsmDictionaryString);
        parentGrid.Resources.MergedDictionaries.Add(vsmDictionary);

        _browseLabel = new Label { Text = ff.BrowseFacilitiesLabel };
        _browseLabel.VerticalTextAlignment = TextAlignment.Center;
        _browseLabel.Margin = new Thickness(8, 2);
        _browseLabel.FontAttributes = FontAttributes.Bold;
        _browseLabel.FontSize = 16;
        _browseLabel.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#6e6e6e"), Color.FromArgb("#fff"));
        Grid.SetRow(_browseLabel, 0);
        Grid.SetColumn(_browseLabel, 1);

        if (shouldShowBack)
        {
            _backButton = new Button { Text = IconFont.ChevronLeft };
            _backButton.FontFamily = "calcite-ui-icons-24";
            _backButton.WidthRequest = 32;
            _backButton.HeightRequest = 32;
            _backButton.CornerRadius = 16;
            _backButton.SetAppThemeColor(Button.BackgroundColorProperty, Color.FromArgb("#f3f3f3"), Color.FromArgb("#2b2b2b"));
            _backButton.Padding = new Thickness(0);
            _backButton.Margin = new Thickness(8);
            _backButton.SetAppThemeColor(Button.TextColorProperty, Color.FromRgb(0, 122, 194), Color.FromRgb(0, 154, 242));
            _backButton.Clicked += HandleBack_Clicked;
        }

        _closeButton = new Button { Text = IconFont.X };
        _closeButton.FontFamily = "calcite-ui-icons-24";
        _closeButton.WidthRequest = 32;
        _closeButton.HeightRequest = 32;
        _closeButton.CornerRadius = 16;
        _closeButton.SetAppThemeColor(Button.BackgroundColorProperty, Color.FromArgb("#f3f3f3"), Color.FromArgb("#2b2b2b"));
        _closeButton.Padding = new Thickness(0);
        _closeButton.Margin = new Thickness(8);
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
        Grid.SetColumnSpan(_noResultLabel, 3);

        Grid.SetRow(_closeButton, 0);
        Grid.SetColumn(_closeButton, 2);

        _searchBar = new SearchBar { Placeholder = ff.SearchPlaceholder };
        _searchBar.SetAppThemeColor(SearchBar.BackgroundColorProperty, Color.FromArgb("#F8F8F8"), Color.FromArgb("#353535"));
        Grid.SetRow(_searchBar, 1);
        Grid.SetColumnSpan(_searchBar, 3);

        _unfilteredListView = new CollectionView { SelectionMode = SelectionMode.Single };
        Grid.SetRow(_unfilteredListView, 2);
        Grid.SetColumnSpan(_unfilteredListView, 3);

        _filteredListView = new CollectionView { IsVisible = false, SelectionMode = SelectionMode.Single };

        Grid.SetRow(_filteredListView, 2);
        Grid.SetColumnSpan(_filteredListView, 3);

        if (isAllSites)
        {
            _filteredListView.ItemTemplate = ff.DifferentiatingFacilityDataTemplate;
            _unfilteredListView.ItemTemplate = ff.DifferentiatingFacilityDataTemplate;
            _unfilteredListView.ItemsSource = ff.AllFacilities;
            _unfilteredListView.ItemsSource = _itemsSource = ff.AllFacilities;
            _unfilteredListView.SelectedItem = ff.SelectedFacility;
        }
        else
        {
            _browseLabel.Text = ff.SelectedSite?.Name ?? ff.BrowseLabel;
            _filteredListView.ItemTemplate = ff.FacilityDataTemplate;
            _unfilteredListView.ItemTemplate = ff.FacilityDataTemplate;
            _unfilteredListView.ItemsSource = _itemsSource = ff.SelectedSite?.Facilities?.ToList();
            if (ff.SelectedFacility != null && (ff.SelectedSite?.Facilities?.Contains(ff.SelectedFacility) ?? false))
            {
                _unfilteredListView.SelectedItem = ff.SelectedFacility;
            }
        }

        _noResultLabel.IsVisible = !(_itemsSource?.Any() ?? false);
        _unfilteredListView.IsVisible = !_noResultLabel.IsVisible;

        parentGrid.Children.Add(_browseLabel);
        parentGrid.Children.Add(_closeButton);
        parentGrid.Children.Add(_searchBar);
        parentGrid.Children.Add(_noResultLabel);
        parentGrid.Children.Add(_unfilteredListView);
        parentGrid.Children.Add(_filteredListView);

        if (shouldShowBack)
        {
            parentGrid.Children.Add(_backButton);
        }

        _closeButton.Clicked += HandleClose_Clicked;
        _searchBar.TextChanged += HandleSearchText_Changed;
        _filteredListView.SelectionChanged += HandleItem_Tapped;
        _unfilteredListView.SelectionChanged += HandleItem_Tapped;

        Content = parentGrid;
    }

    private void HandleBack_Clicked(object? sender, EventArgs e) => _ff?.GoBack();

    private void HandleItem_Tapped(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is FloorFacility newFacility && _ff != null)
        {
            if (newFacility.Site != null)
            {
                _ff.SelectedSite = newFacility.Site;
            }

            _ff.SelectedFacility = newFacility;
        }

        _ff?.CloseBrowsing();
    }

    private void HandleClose_Clicked(object? sender, EventArgs e) => _ff?.CloseBrowsing();

    private void HandleSearchText_Changed(object? sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_searchBar.Text))
        {
            var simplified = _searchBar.Text.ToLower();
            IList<FloorFacility>? facilitiesList = _isAllSites ? _ff?.AllFacilities : _ff?.SelectedSite?.Facilities?.ToList();
            var results = facilitiesList?.Where(facility => facility.Name.ToLower().Contains(simplified));

            _filteredListView.ItemsSource = results;
            if (results != null)
            {
                if (_ff?.SelectedFacility != null && results.Contains(_ff.SelectedFacility))
                {
                    _filteredListView.SelectedItem = _ff.SelectedFacility;
                }

                _unfilteredListView.IsVisible = false;
                _filteredListView.IsVisible = true;
            }
            _noResultLabel.IsVisible = !(results?.Any() ?? false);
        }
        else
        {
            _filteredListView.ItemsSource = null;
            _filteredListView.IsVisible = false;
            _unfilteredListView.IsVisible = true;
            _noResultLabel.IsVisible = !(_itemsSource?.Any() ?? false);
        }
    }
}