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

#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Custom ListView implementation enables disabling listview selection for disabled items.
    /// </summary>
    internal class UWPCustomListView : ListView
    {
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (item is BasemapGalleryItem bmgi)
            {
                Binding isenabledBinding = new Binding
                {
                    Path = new PropertyPath(nameof(bmgi.IsValid)),
                    Mode = BindingMode.OneWay,
                    Source = bmgi,
                };

                (element as ListViewItem).SetBinding(IsEnabledProperty, isenabledBinding);
                (element as ListViewItem).SetBinding(IsHitTestVisibleProperty, isenabledBinding);
            }
        }
    }
}
#endif
