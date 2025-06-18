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

using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UtilityNetworks;

#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui;
using TextBlock = Microsoft.Maui.Controls.Label;
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
    /// Supporting control for the <see cref="FeatureFormView"/> control,
    /// used for rendering a <see cref="UtilityAssociationGroupResult"/>.
    /// </summary>
    public partial class UtilityAssociationGroupResultView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityAssociationGroupResultView"/> class.
        /// </summary>
        public UtilityAssociationGroupResultView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(UtilityAssociationGroupResultView);
#endif
        }

        /// <summary>
        /// Gets or sets the AssociationsFilterResults.
        /// </summary>
        public UtilityAssociationGroupResult? GroupResult
        {
            get => GetValue(GroupResultProperty) as UtilityAssociationGroupResult;
            set => SetValue(GroupResultProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="GroupResult"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty GroupResultProperty =
            BindableProperty.Create(nameof(GroupResult), typeof(UtilityAssociationGroupResult), typeof(UtilityAssociationGroupResultView), null);
#else
        public static readonly DependencyProperty GroupResultProperty =
            DependencyProperty.Register(nameof(GroupResult), typeof(UtilityAssociationGroupResult), typeof(UtilityAssociationGroupResultView), null);
#endif

    }
}