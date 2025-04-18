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

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    [TemplatePart(Name = "PART_Placeholder", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_SearchBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_NoResultsLabel", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_FilteredListView", Type = typeof(ListView))]
    [TemplatePart(Name = "PART_UnfilteredListView", Type = typeof(ListView))]
    internal partial class FilteringListView : ListView
    {
        private TextBlock? _placeholder;
        private TextBox? _searchBox;
        private TextBlock? _noResultsLabel;
        private ListView? _filteredListView;
        private ListView? _unfilteredListView;

        private bool _waitFlag;

        private List<object>? _filteredList;
        private List<object>? _unfilteredList;
        private object? _lastSetItemsSource;

        public FilteringListView()
        {
            DefaultStyleKey = typeof(FilteringListView);

            SelectionChanged += FilteringListView_SelectionChanged;
            var propertyChangedCallbackToken = RegisterPropertyChangedCallback(ListView.ItemsSourceProperty, HandleDPChange);
        }

        private void HandleDPChange(DependencyObject o, DependencyProperty dp) => (o as FilteringListView)?.UpdateForCurrentState();

        private void FilteringListView_SelectionChanged(object? sender, SelectionChangedEventArgs e) => UpdateForCurrentState();

        private void UpdateForCurrentState()
        {
            _noResultsLabel?.SetValue(TextBlock.TextProperty, NoResultsMessage);
            _noResultsLabel?.SetValue(VisibilityProperty, Visibility.Collapsed);

            // Only update the unfiltered list when the ItemsSource changes
            if (ItemsSource != _lastSetItemsSource)
            {
                if (ItemsSource is System.Collections.IEnumerable enumerable)
                {
                    _unfilteredList = enumerable.OfType<object>().ToList();
                }
                else
                {
                    _unfilteredList = new List<object>();
                }

                _lastSetItemsSource = ItemsSource;
            }

            // Only update the unfiltered list view when its item source is out of date
            if (_unfilteredListView != null && _unfilteredListView.ItemsSource != _unfilteredList)
            {
                _unfilteredListView?.SetValue(ItemsSourceProperty, _unfilteredList);
            }

            // Update filtered list and both views' visibilities
            if (_searchBox?.Text?.ToLower() is string searchString && !string.IsNullOrWhiteSpace(searchString))
            {
                _filteredList = _unfilteredList?.Where(m => AsFilterString(m).Contains(searchString)).ToList();
                _filteredListView?.SetValue(ItemsSourceProperty, _filteredList);
                _filteredListView?.SetValue(VisibilityProperty, Visibility.Visible);
                _unfilteredListView?.SetValue(VisibilityProperty, Visibility.Collapsed);

                if (_filteredList != null && !_filteredList.Any())
                {
                    _filteredListView?.SetValue(VisibilityProperty, Visibility.Collapsed);
                    _noResultsLabel?.SetValue(VisibilityProperty, Visibility.Visible);
                }
            }
            else
            {
                _unfilteredListView?.SetValue(VisibilityProperty, Visibility.Visible);
                _filteredListView?.SetValue(VisibilityProperty, Visibility.Collapsed);

                if (_unfilteredList != null && !_unfilteredList.Any())
                {
                    _unfilteredListView?.SetValue(VisibilityProperty, Visibility.Collapsed);
                    _noResultsLabel?.SetValue(VisibilityProperty, Visibility.Visible);
                }
            }

            // Update selection
            if (_unfilteredListView is not null && _unfilteredList?.FirstOrDefault(item => item == SelectedItem) is object item)
            {
                _unfilteredListView.SetValue(SelectedItemProperty, item);
                _unfilteredListView.ScrollIntoView(item);
            }

            if (_filteredListView is not null && _filteredList?.FirstOrDefault(item => item == SelectedItem) is object filteredItem)
            {
                _filteredListView.SetValue(SelectedItemProperty, filteredItem);
                _filteredListView.ScrollIntoView(filteredItem);
            }
        }

        public string? Placeholder
        {
            get => GetValue(PlaceholderProperty) as string;
            set => SetValue(PlaceholderProperty, value);
        }

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(FilteringListView), null);

        public string? NoResultsMessage
        {
            get => GetValue(NoResultsMessageProperty) as string;
            set => SetValue(NoResultsMessageProperty, value);
        }

        public static readonly DependencyProperty NoResultsMessageProperty =
            DependencyProperty.Register(nameof(NoResultsMessage), typeof(string), typeof(FilteringListView), null);

        private async void HandleTextChanged()
        {
            if (string.IsNullOrWhiteSpace(_searchBox?.Text))
            {
                _placeholder?.SetValue(VisibilityProperty, Visibility.Visible);
            }
            else
            {
                _placeholder?.SetValue(VisibilityProperty, Visibility.Collapsed);
            }

            if (_waitFlag)
            {
                return;
            }

            _waitFlag = true;
            await Task.Delay(500);
            _waitFlag = false;

            UpdateForCurrentState();
        }

        protected override void OnApplyTemplate()
        {
            if (_filteredListView != null)
            {
                _filteredListView.ItemClick -= InnerListView_ItemClick;
                _filteredListView = null;
            }

            if (_unfilteredListView != null)
            {
                _unfilteredListView.ItemClick -= InnerListView_ItemClick;
                _unfilteredListView = null;
            }

            if (_searchBox != null)
            {
                _searchBox.TextChanged -= SearchBox_TextChanged;
            }

            base.OnApplyTemplate();

            _placeholder = GetTemplateChild("PART_Placeholder") as TextBlock;
            _noResultsLabel = GetTemplateChild("PART_NoResultsLabel") as TextBlock;
            _searchBox = GetTemplateChild("PART_SearchBox") as TextBox;
            _filteredListView = GetTemplateChild("PART_FilteredListView") as ListView;
            _unfilteredListView = GetTemplateChild("PART_UnfilteredListView") as ListView;

            _placeholder?.SetValue(TextBlock.TextProperty, Placeholder);

            if (_filteredListView != null)
            {
                _filteredListView.ItemClick += InnerListView_ItemClick;
                _filteredListView.ItemTemplate = ItemTemplate;
                _filteredListView.ItemContainerStyle = ItemContainerStyle;
            }

            if (_unfilteredListView != null)
            {
                _unfilteredListView.ItemClick += InnerListView_ItemClick;
                _unfilteredListView.ItemTemplate = ItemTemplate;
                _unfilteredListView.ItemContainerStyle = ItemContainerStyle;
            }

            if (_searchBox != null)
            {
                _searchBox.TextChanged += SearchBox_TextChanged;
            }

            UpdateForCurrentState();
        }

        private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e) => HandleTextChanged();

        private void InnerListView_ItemClick(object? sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is object newobject)
            {
                SelectedItem = newobject;
                SelectionChanged2?.Invoke(this, new SelectionChangedEventArgs(new List<object>(), new List<object>() { newobject }));
            }

            UpdateForCurrentState();
        }

        public event SelectionChangedEventHandler? SelectionChanged2;

        private static string AsFilterString(object input)
        {
            if (input is string inputString)
            {
                return inputString;
            }
            else if (input is FloorFacility facilityInput)
            {
                return facilityInput.Name.ToLower();
            }
            else if (input is FloorSite siteInput)
            {
                return siteInput.Name.ToLower();
            }
            else
            {
                return input?.ToString() ?? string.Empty;
            }
        }
    }
}
