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

#if WPF
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using System.Diagnostics;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [TemplatePart(Name = FeatureFormContentScrollViewerName, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = ItemsViewName, Type = typeof(ItemsControl))]
    public partial class FeatureFormView : Control
    {
        private const string ItemsViewName = "ItemsView";
        private const string FeatureFormContentScrollViewerName = "FeatureFormContentScrollViewer";

        private static readonly DependencyPropertyKey IsValidPropertyKey
          = DependencyProperty.RegisterReadOnly(nameof(IsValid), typeof(bool), typeof(FeatureFormView), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsValid"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty IsValidProperty = IsValidPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether this form has any validation errors.
        /// </summary>
        /// <seealso cref="FeatureForm.ValidationErrors"/>
        public bool IsValid
        {
            get { return (bool)GetValue(IsValidProperty); }
            private set { SetValue(IsValidPropertyKey, value); }
        }
    }
}
#endif