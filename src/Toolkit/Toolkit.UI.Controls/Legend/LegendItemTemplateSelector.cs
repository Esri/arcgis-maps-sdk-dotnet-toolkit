﻿// /*******************************************************************************
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
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Determines which DataTemplate to use for a given layer content item in a Legend control.
    /// </summary>
    internal partial class LegendItemTemplateSelector : DataTemplateSelector
    {
        private Legend _owner;

        public LegendItemTemplateSelector(Legend owner)
        {
            _owner = owner;
        }

#if WINDOWS_XAML
        protected override DataTemplate SelectTemplateCore(object item)
#else
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
#endif
        {
            if (item is LegendEntry entry)
            {
                if (entry.Content is Layer)
                {
                    return LayerTemplate;
                }

                if (entry.Content is ILayerContent)
                {
                    return SublayerTemplate;
                }

                if (entry.Content is LegendInfo || entry.Content is DesignLegendInfo)
                {
                    return LegendInfoTemplate;
                }
            }

#if WINDOWS_XAML
            return base.SelectTemplateCore(item);
#else
            return base.SelectTemplate(item, container);
#endif
        }

        public DataTemplate LayerTemplate => _owner.LayerItemTemplate;

        public DataTemplate SublayerTemplate => _owner.SublayerItemTemplate;

        public DataTemplate LegendInfoTemplate => _owner.LegendInfoItemTemplate;
    }
}
#endif