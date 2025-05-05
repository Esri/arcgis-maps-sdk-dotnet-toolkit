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

using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UtilityNetworks;

#if WPF
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xaml;
#elif WINDOWS_XAML
using Windows.Foundation;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    public partial class UtilityAssociationsPopupElementView
    {
        private ListView? _associationsListView;

        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityAssociationsPopupElementView"/> class.
        /// </summary>
        public UtilityAssociationsPopupElementView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(UtilityAssociationsPopupElementView);
#endif
        }

        /// <inheritdoc />
#if WINDOWS_XAML || MAUI
        protected override void OnApplyTemplate()
#elif WPF
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            if (_associationsListView is not null)
            {
#if WINDOWS_XAML
                _associationsListView.ItemClick -= AssociationsListView_ItemClick;
#endif
            }
            if (GetTemplateChild("AssociationsList") is ListView listView)
            {
                _associationsListView = listView;
#if WINDOWS_XAML
                _associationsListView.ItemClick += AssociationsListView_ItemClick;
#endif
            }
        }

#if WINDOWS_XAML
        private void AssociationsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as UtilityAssociationsFilterResult;
            if (item is null)
            {
                return;
            }
            var parent = UI.Controls.PopupViewer.GetPopupViewerParent(this);
            parent?.NavigateToItem(item);
        }
#endif

        /// <summary>
        /// Gets or sets the MediaPopupElement.
        /// </summary>
        public UtilityAssociationsPopupElement? Element
        {
            get { return GetValue(ElementProperty) as UtilityAssociationsPopupElement; }
            set { SetValue(ElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ElementProperty =
            PropertyHelper.CreateProperty<UtilityAssociationsPopupElement, UtilityAssociationsPopupElementView>(nameof(Element), null, (s, oldValue, newValue) => s.OnElementPropertyChanged());

        private async void OnElementPropertyChanged()
        {
            if(Element is not null)
            {
                if(Element.AssociationsFilterResults.Count == 0)
                {
                    await Element.FetchAssociationsFilterResultsAsync();
#if !MAUI
                    if (GetTemplateChild("AssociationsList") is ItemsControl itemsView)
                    {
                        itemsView.ItemsSource = Element.AssociationsFilterResults; // Refresh the collection
                    }
#endif
                }
            }
        }
    }
}