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

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class UtilityAssociationsPopupElementView : Control
    {
        private TextBlock? _titleTextBlock;
        private ListView? _associationsListView;
        private Grid? _noAssociationsGrid;
        /// <inheritdoc />
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            if (_associationsListView is not null)
            {
#if WINDOWS_XAML
                _associationsListView.ItemClick -= AssociationsListView_ItemClick;
#elif WPF
                _associationsListView.SelectionChanged -= AssociationsListView_SelectionChanged;
#endif
            }
            if (GetTemplateChild("Title") is TextBlock textBlock)
            {
                _titleTextBlock = textBlock;
            }
            if (GetTemplateChild("AssociationsList") is ListView listView)
            {
                _associationsListView = listView;
#if WINDOWS_XAML
                _associationsListView.ItemClick += AssociationsListView_ItemClick;
#elif WPF
                _associationsListView.SelectionChanged += AssociationsListView_SelectionChanged;
#endif
            }
            if (GetTemplateChild("NoAssociationsGrid") is Grid grid)
            {
                _noAssociationsGrid = grid;
            }
            UpdateView();
        }
#if WPF
        private void AssociationsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ((ListView)sender).SelectedItem;
            ((ListView)sender).SelectedItem = null; // Clear selection
#elif WINDOWS_XAML
        private void AssociationsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as UtilityNetworks.UtilityAssociationsFilterResult;
#endif
            if (item is null)
            {
                return;
            }
            var parent = UI.Controls.PopupViewer.GetPopupViewerParent(this);
#if WINDOWS_XAML
            (sender as ListView)?.PrepareConnectedAnimation("NavigationSubViewForwardAnimation", item, "ResultView");
#endif
            parent?.NavigateToItem(item);
        }

        private void UpdateView()
        {
            if (_titleTextBlock is not null)
            {
                _titleTextBlock.Text = (Element is null || string.IsNullOrEmpty(Element.Title)) ? Properties.Resources.GetString("PopupViewerUtilityAssociationsDefaultTitle") : Element.Title;
            }

            bool hasAssociations = Element?.AssociationsFilterResults.Any(r => r.ResultCount > 0) == true;
            if (_associationsListView is not null)
            {
                _associationsListView.ItemsSource = Element?.AssociationsFilterResults.Where(r => r.ResultCount > 0);
                _associationsListView.Visibility = hasAssociations ? Visibility.Visible : Visibility.Collapsed;
            }
            if (_noAssociationsGrid is not null)
            {
                _noAssociationsGrid.Visibility = hasAssociations ? Visibility.Collapsed : Visibility.Visible;
            }
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
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(UtilityAssociationsPopupElementView), new PropertyMetadata(null));
    }
}
#endif