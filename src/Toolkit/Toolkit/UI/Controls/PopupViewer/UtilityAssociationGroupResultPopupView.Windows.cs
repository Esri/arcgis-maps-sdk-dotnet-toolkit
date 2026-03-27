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
    [TemplatePart(Name = "ResultsList", Type = typeof(ListView))]
    public partial class UtilityAssociationGroupResultPopupView : Control
    {
        private Button? _expandButton;
#if WPF
        private Button? _clearButton;
#endif
        private ListView? _resultsListView;
        private Button? _showAllButton;
        private TextBox? _searchTextBox;

        /// <inheritdoc />
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {

            base.OnApplyTemplate();

            if (_expandButton is not null)
            {
                _expandButton.Click -= ExpandButton_Click;
            }

            if (_resultsListView is not null)
            {
#if WINDOWS_XAML
                _resultsListView.ItemClick -= ResultsListView_ItemClick;
#elif WPF
                _resultsListView.SelectionChanged -= ResultsListView_SelectionChanged;
#endif
            }

            if (_showAllButton is not null)
            {
                _showAllButton.Click -= ShowAllButton_Click;
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

            if (GetTemplateChild("ExpandIcon") is Button expandButton)
            {
                _expandButton = expandButton;
                _expandButton.Click += ExpandButton_Click;
            }

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

            if (GetTemplateChild("ShowAll") is Button showAllButton)
            {
                _showAllButton = showAllButton;
                _showAllButton.Visibility = IsSearchable ? Visibility.Collapsed : GroupResult?.AssociationResults?.Count > DisplayCount ? Visibility.Visible : Visibility.Collapsed;
                _showAllButton.Click += ShowAllButton_Click;
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
                _clearButton.Visibility = Visibility.Collapsed;
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

            if (_showAllButton is not null)
            {
                _showAllButton.Visibility = !IsSearchable && GroupResult?.AssociationResults?.Count > DisplayCount ? Visibility.Visible : Visibility.Collapsed;
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

        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            bool isExpanded = true;
            if (_expandButton is not null)
            {
#if WINDOWS_XAML
                if(_expandButton.Content is FontIcon fontIcon)
                {
                    fontIcon.Glyph = fontIcon.Glyph == ToolkitIcons.ChevronDown ? ToolkitIcons.ChevronRight : ToolkitIcons.ChevronDown;
                    isExpanded = fontIcon.Glyph == ToolkitIcons.ChevronDown;
                }
#else
                if (_expandButton.Content is TextBlock iconTextBlock)
                {
                    iconTextBlock.Text = iconTextBlock.Text == ToolkitIcons.ChevronDown ? ToolkitIcons.ChevronRight : ToolkitIcons.ChevronDown;
                    isExpanded = iconTextBlock.Text == ToolkitIcons.ChevronDown;
                }
#endif
            }
            if (_resultsListView is not null)
            {
                _resultsListView.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
            }
            if (_showAllButton is not null)
            {
                _showAllButton.Visibility = isExpanded && GroupResult?.AssociationResults?.Count > DisplayCount ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ShowAllButton_Click(object sender, RoutedEventArgs e)
        {
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
#if WINDOWS_XAML
            _resultsListView?.PrepareConnectedAnimation("NavigationSubViewForwardAnimation", item, "ResultView");
#endif
            parent?.NavigateToItem(Popup.FromGeoElement(item.AssociatedFeature));
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