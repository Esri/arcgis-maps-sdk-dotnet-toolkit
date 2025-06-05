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
    public partial class UtilityAssociationGroupResultView : Control
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
                //_resultsListView.Loaded -= ResultsListView_Loaded;
#endif
            }
            if (GetTemplateChild("ResultsList") is ListView listView)
            {
                _resultsListView = listView;
#if WINDOWS_XAML
                _resultsListView.ItemClick += ResultsListView_ItemClick;
                //_resultsListView.Loaded += ResultsListView_Loaded;
#elif WPF
                _resultsListView.SelectionChanged += AssociationsListView_SelectionChanged;
#endif
            }
        }

#if WINDOWS_XAML
        //private void ResultsListView_Loaded(object sender, RoutedEventArgs e)
        //{
            
        //    ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("Item");
        //    if (animation != null && GroupResult is not null)
        //    {
        //        _ = ((ListView)sender).TryStartConnectedAnimationAsync(animation, GroupResult, "ItemName");
        //    }
        //}
#endif
#if WPF
        private void AssociationsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            var parent = UI.Controls.FeatureFormView.GetFeatureFormViewParent(this);
            var title = new Mapping.Popups.Popup(item.AssociatedFeature, null).Title;
            var featureForm = new FeatureForm(item.AssociatedFeature);
#if WINDOWS_XAML
            _resultsListView?.PrepareConnectedAnimation("NavigationSubViewForwardAnimation", item, "ResultView");
#endif
            parent?.NavigateToItem(featureForm); 
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
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(UtilityAssociationGroupResultView), new PropertyMetadata(null));
    }
}
#endif