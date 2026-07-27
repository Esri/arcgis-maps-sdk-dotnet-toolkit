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
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.UtilityNetworks;
#if WINUI
using Microsoft.UI.Xaml.Media.Animation;
#elif WINDOWS_UWP
using Windows.UI.Xaml.Media.Animation;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class UtilityAssociationsFilterResultsView : Control
    {
        private ListView? _resultsListView;
        /// <inheritdoc />
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            if (_resultsListView is not null)
            {
#if WINDOWS_XAML
                _resultsListView.ItemClick -= ResultsListView_ItemClick;
#endif
            }
            if (GetTemplateChild("ResultsList") is ListView listView)
            {
                _resultsListView = listView;
#if WINDOWS_XAML
                _resultsListView.ItemClick += ResultsListView_ItemClick;
#elif WPF
                _resultsListView.SelectionChanged += AssociationsListView_SelectionChanged;
#endif
            }
            UpdateAddAssociationButton();
        }

#if WPF
        private void AssociationsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ((ListView)sender).SelectedItem;
            ((ListView)sender).SelectedItem = null; // Clear selection
#elif WINDOWS_XAML
        private void ResultsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as UtilityAssociationGroupResult;
#endif
            if (item is null)
            {
                return;
            }
            var parent = UI.Controls.FeatureFormView.GetFeatureFormViewParent(this);
#if WINDOWS_XAML
            _resultsListView?.PrepareConnectedAnimation("NavigationSubViewForwardAnimation", item, "Title");
#endif
            parent?.NavigateToItem(item, this.DataContext); 
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
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(UtilityAssociationsFilterResultsView), new PropertyMetadata(null));

        private partial void ShowAddAssociationMenu(object? flyoutTarget)
        {
            string fromNetworkDataSource = Properties.Resources.GetString("FeatureFormUtilityAssociationsSelectFromNetworkDataSource")!;

#if WPF
            ContextMenu contextMenu = new ContextMenu
            {
                PlacementTarget = flyoutTarget as UIElement ?? this,
            };
            // Add the On Map option here when map-based association selection is implemented.
            // if (CanSelectAssociationOnMap())
            // {
            //     contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.GetString("FeatureFormUtilityAssociationsSelectOnMap") });
            // }
            var fromNetworkDataSourceItem = new MenuItem() { Header = fromNetworkDataSource };
            fromNetworkDataSourceItem.Click += (_, _) => SelectFromNetworkDataSource();
            contextMenu.Items.Add(fromNetworkDataSourceItem);
            if (contextMenu.Items.Count == 1)
            {
                // If there's only one entry, no reason to ask users to pick from a menu, just go straight to the action.
                SelectFromNetworkDataSource();
                return;
            }
            contextMenu.IsOpen = true;
#elif WINDOWS_XAML
            MenuFlyout contextMenu = new MenuFlyout();
            // Add the On Map option here when map-based association selection is implemented.
            // if (CanSelectAssociationOnMap())
            // {
            //     contextMenu.Items.Add(new MenuFlyoutItem() { Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsSelectOnMap") });
            // }
            var fromNetworkDataSourceItem = new MenuFlyoutItem() { Text = fromNetworkDataSource };
            fromNetworkDataSourceItem.Click += (_, _) => SelectFromNetworkDataSource();
            contextMenu.Items.Add(fromNetworkDataSourceItem);
            if (contextMenu.Items.Count == 1)
            {
                // If there's only one entry, no reason to ask users to pick from a menu, just go straight to the action.
                SelectFromNetworkDataSource();
                return;
            }
            contextMenu.ShowAt(flyoutTarget as FrameworkElement ?? this);
#endif
        }
    }
}
#endif