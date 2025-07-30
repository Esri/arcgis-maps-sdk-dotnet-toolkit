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

#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui;
#else
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Supporting control for the <see cref="PopupViewer"/> control,
    /// used for rendering a <see cref="UtilityAssociationsPopupElement"/> and picking the correct template for each Input type.
    /// </summary>
    public partial class UtilityAssociationsPopupElementView
    {
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

#if WINDOWS_XAML
        internal PopupViewer? PopupViewer => PopupViewer.GetPopupViewerParent(this);
#else
        /// <summary>
        /// Gets or sets the PopupViewer that the <see cref="PopupViewer"/> belongs to.
        /// </summary>
        public PopupViewer PopupViewer
        {
            get { return (PopupViewer)GetValue(PopupViewerProperty); }
            set { SetValue(PopupViewerProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PopupViewer"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty PopupViewerProperty =
            BindableProperty.Create(nameof(PopupViewer), typeof(PopupViewer), typeof(UtilityAssociationsPopupElementView), null);
#else
        public static readonly DependencyProperty PopupViewerProperty =
            DependencyProperty.Register(nameof(PopupViewer), typeof(PopupViewer), typeof(UtilityAssociationsPopupElementView), new PropertyMetadata(null));
#endif
#endif

        /// <summary>
        /// Gets or sets the <see cref="UtilityAssociationsPopupElement"/>.
        /// </summary>
        public UtilityAssociationsPopupElement? Element
        {
            get => GetValue(ElementProperty) as UtilityAssociationsPopupElement;
            set => SetValue(ElementProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty ElementProperty =
            BindableProperty.Create(nameof(Element), typeof(UtilityAssociationsPopupElement), typeof(UtilityAssociationsPopupElementView), null, propertyChanged: (s, oldValue, newValue) => ((UtilityAssociationsPopupElementView)s).OnElementPropertyChanged());
#else
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(UtilityAssociationsPopupElement), typeof(UtilityAssociationsPopupElementView), new PropertyMetadata(null, (s, e) => ((UtilityAssociationsPopupElementView)s).OnElementPropertyChanged()));
#endif

        private void OnElementPropertyChanged()
        {
            RefreshAssociations();
        }

        private async void RefreshAssociations()
        {
            if (Element is not null)
            {
                try
                {
                    await Element.FetchAssociationsFilterResultsAsync();
                }
                catch { }
            }
            UpdateView();
        }
    }
}