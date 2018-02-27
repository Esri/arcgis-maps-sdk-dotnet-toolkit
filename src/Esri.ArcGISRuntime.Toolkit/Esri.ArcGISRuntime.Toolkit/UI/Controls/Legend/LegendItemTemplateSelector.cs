// /*******************************************************************************
//  * Copyright 2012-2016 Esri
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

#if !XAMARIN
using System.Linq;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Determines which DataTemplate to use for a given layer content item in a Legend control.
    /// </summary>
    internal class LegendItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LeafItemTemplate { get; set; }
        public DataTemplate BranchItemTemplate { get; set; }

#if NETFX_CORE
            protected override DataTemplate SelectTemplateCore
#else
            public override DataTemplate SelectTemplate
#endif
            (object item, DependencyObject container)
        {
            if ((item as LayerContentViewModel)?.Sublayers?.Any() ?? false)
            {
                return BranchItemTemplate;
            }

            return LeafItemTemplate;
        }
    }
}
#endif