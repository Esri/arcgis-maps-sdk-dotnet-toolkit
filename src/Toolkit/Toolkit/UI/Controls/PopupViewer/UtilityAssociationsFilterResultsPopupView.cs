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
using Esri.ArcGISRuntime.UtilityNetworks;

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
    public partial class UtilityAssociationsFilterResultsPopupView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityAssociationsFilterResultsPopupView"/> class.
        /// </summary>
        public UtilityAssociationsFilterResultsPopupView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(UtilityAssociationsFilterResultsPopupView);
#endif
        }

        /// <summary>
        /// Gets or sets the AssociationsFilterResults.
        /// </summary>
        public UtilityAssociationsFilterResult? AssociationsFilterResult
        {
            get => GetValue(AssociationsFilterResultProperty) as UtilityAssociationsFilterResult;
            set => SetValue(AssociationsFilterResultProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AssociationsFilterResult"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty AssociationsFilterResultProperty =
            BindableProperty.Create(nameof(AssociationsFilterResult), typeof(UtilityAssociationsFilterResult), typeof(UtilityAssociationsFilterResultsPopupView));
#else
        public static readonly DependencyProperty AssociationsFilterResultProperty =
            DependencyProperty.Register(nameof(AssociationsFilterResult), typeof(UtilityAssociationsFilterResult), typeof(UtilityAssociationsFilterResultsPopupView), new PropertyMetadata(null, (s, e) => ((UtilityAssociationsFilterResultsPopupView)s).OnAssociationsFilterResultPropertyChanged()));
#endif
    }
}