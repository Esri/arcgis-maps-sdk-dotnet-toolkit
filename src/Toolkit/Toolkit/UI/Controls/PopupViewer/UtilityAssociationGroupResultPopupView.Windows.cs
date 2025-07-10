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

#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.UtilityNetworks;
using Popup = Esri.ArcGISRuntime.Mapping.Popups.Popup;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class UtilityAssociationGroupResultPopupView : Control
    {
#if WINDOWS_XAML
        private FontIcon? _expandIcon;
#elif WPF
        private TextBlock? _expandTextBlock;
        private Button? _clearButton;
#endif
        private ListView? _resultsListView;
        private Grid? _showAllGrid;
        private TextBox? _searchTextBox;

        /// <inheritdoc />
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {

            base.OnApplyTemplate();

#if WINDOWS_XAML
            if (_expandIcon is not null)
            {
                _expandIcon.Tapped -= ExpandTextBlock_Tapped;
            }
#elif WPF
            if (_expandTextBlock is not null)
            {
                _expandTextBlock.MouseLeftButtonUp -= ExpandTextBlock_MouseLeftButtonUp;
            }
#endif

            if (_resultsListView is not null)
            {
#if WINDOWS_XAML
                _resultsListView.ItemClick -= ResultsListView_ItemClick;
#elif WPF
                _resultsListView.SelectionChanged -= ResultsListView_SelectionChanged;
#endif
            }

            if (_showAllGrid is not null)
            {
#if WINDOWS_XAML
                _showAllGrid.Tapped -= ShowAllGrid_Tapped;
#elif WPF
                _showAllGrid.MouseLeftButtonUp -= ShowAllGrid_MouseLeftButtonUp;
#endif
            }

            if (_searchTextBox is not null)
            {
                _searchTextBox.TextChanged -= SearchTextBox_TextChanged;
            }
#if WPF
            if (_clearButton is not null)
            {
                _clearButton.Click -= ClearButton_Click;
            }
#endif

#if WINDOWS_XAML
            if (GetTemplateChild("ExpandIcon") is FontIcon icon)
            {
                _expandIcon = icon;
                _expandIcon.Tapped += ExpandTextBlock_Tapped;
            }
#elif WPF
            if (GetTemplateChild("ExpandIcon") is TextBlock textBlock)
            {
                _expandTextBlock = textBlock;
                _expandTextBlock.MouseLeftButtonUp += ExpandTextBlock_MouseLeftButtonUp;
            }
#endif

            if (GetTemplateChild("ResultsList") is ListView listView)
            {
                _resultsListView = listView;
                _resultsListView.ItemsSource = IsSearchable ? GroupResult?.AssociationResults : GroupResult?.AssociationResults?.Take(DisplayCount);
#if WINDOWS_XAML
                _resultsListView.ItemClick += ResultsListView_ItemClick;
#elif WPF
                _resultsListView.SelectionChanged += ResultsListView_SelectionChanged;
#endif
            }

            if (GetTemplateChild("ShowAll") is Grid grid)
            {
                _showAllGrid = grid;
                _showAllGrid.Visibility = IsSearchable ? Visibility.Collapsed : GroupResult?.AssociationResults?.Count > DisplayCount ? Visibility.Visible : Visibility.Collapsed;
#if WINDOWS_XAML
                _showAllGrid.Tapped += ShowAllGrid_Tapped;
#elif WPF
                _showAllGrid.MouseLeftButtonUp += ShowAllGrid_MouseLeftButtonUp;
#endif
            }

            if (GetTemplateChild("SearchText") is TextBox textBox)
            {
                _searchTextBox = textBox;
                _searchTextBox.TextChanged += SearchTextBox_TextChanged;
            }

#if WPF
            if (GetTemplateChild("ClearSearch") is Button button)
            {
                _clearButton = button;
                _clearButton.Click += ClearButton_Click;
            }
#endif
        }

        private void UpdateView()
        {
            if (_resultsListView is not null)
            {
                _resultsListView.ItemsSource = IsSearchable ? GroupResult?.AssociationResults : GroupResult?.AssociationResults?.Take(DisplayCount);
            }

            if (_showAllGrid is not null)
            {
                _showAllGrid.Visibility = !IsSearchable && GroupResult?.AssociationResults?.Count > DisplayCount ? Visibility.Visible : Visibility.Collapsed;
            }
        }

#if WPF
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (_searchTextBox is not null)
            {
                _searchTextBox.Text = string.Empty;
            }
        }
#endif

#if WINDOWS_XAML
        private void ExpandTextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            bool isExpanded = true;
            if (_expandIcon is not null)
            {
                _expandIcon.Glyph = _expandIcon.Glyph == ToolkitIcons.ChevronDown ? ToolkitIcons.ChevronRight : ToolkitIcons.ChevronDown;
                isExpanded = _expandIcon.Glyph == ToolkitIcons.ChevronDown;
            }
#elif WPF
        private void ExpandTextBlock_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            bool isExpanded = true;
            if (_expandTextBlock is not null)
            {
                _expandTextBlock.Text = _expandTextBlock.Text == ToolkitIcons.ChevronDown ? ToolkitIcons.ChevronRight : ToolkitIcons.ChevronDown;
                isExpanded = _expandTextBlock.Text == ToolkitIcons.ChevronDown;
            }
#endif
            if (_resultsListView is not null)
            {
                _resultsListView.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
            }
            if (_showAllGrid is not null)
            {
                _showAllGrid.Visibility = isExpanded && GroupResult?.AssociationResults?.Count > DisplayCount ? Visibility.Visible : Visibility.Collapsed;
            }
        }

#if WINDOWS_XAML
        private void ShowAllGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
#elif WPF
        private void ShowAllGrid_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
#endif
            if (GroupResult != null)
            {
                var parent = UI.Controls.PopupViewer.GetPopupViewerParent(this);
                parent?.NavigateToItem(GroupResult);
            }
        }

        private void SearchTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            var searchText = (sender as TextBox)?.Text.Trim();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                if (_resultsListView is not null)
                {
                    _resultsListView.ItemsSource = GroupResult?.AssociationResults?.Where(r => r.Title?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
                }
#if WPF
                if (_clearButton is not null)
                {
                    _clearButton.Visibility = Visibility.Visible;
                }
#endif
            }
            else
            {
                if (_resultsListView is not null)
                {
                    _resultsListView.ItemsSource = GroupResult?.AssociationResults;
                }
#if WPF
                if (_clearButton is not null)
                {
                    _clearButton.Visibility = Visibility.Collapsed;
                }
#endif
            }
        }

#if WPF
        private void ResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ((ListView)sender).SelectedItem as UtilityAssociationResult;
            ((ListView)sender).SelectedItem = null; // Clear selection
#elif WINDOWS_XAML
        private void ResultsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as UtilityAssociationResult;
#endif
            if (item is null)
            {
                return;
            }
            var parent = UI.Controls.PopupViewer.GetPopupViewerParent(this);
            parent?.NavigateToItem(Popup.FromGeoElement(item.AssociatedFeature));
        }

        private void OnGroupResultPropertyChanged()
        {
            UpdateView();
        }

        /// <summary>
        /// Gets or sets the template for UtilityAssociationsFilterResult items.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(UtilityAssociationGroupResultPopupView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the display count from <see cref="Mapping.Popups.UtilityAssociationsPopupElement.DisplayCount"/>.
        /// </summary>
        public int DisplayCount
        {
            get => (int)GetValue(DisplayCountProperty);
            set => SetValue(DisplayCountProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DisplayCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayCountProperty =
            DependencyProperty.Register(nameof(DisplayCount), typeof(int), typeof(UtilityAssociationGroupResultPopupView), new PropertyMetadata(1));

        /// <summary>
        /// Gets or sets a value indicating whether results in this group is searchable.
        /// </summary>
        public bool IsSearchable
        {
            get => (bool)GetValue(IsSearchableProperty);
            set => SetValue(IsSearchableProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsSearchable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSearchableProperty =
            DependencyProperty.Register(nameof(IsSearchable), typeof(bool), typeof(UtilityAssociationGroupResultPopupView), new PropertyMetadata(false));
    }
}
#endif