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

#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    public partial class OfflineMapAreasView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        /// <summary>
        /// Template name of the <see cref="ItemsView"/> items layout view.
        /// </summary>
        public const string ItemsViewName = "ItemsView";

        static OfflineMapAreasView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            CollectionView listView = new CollectionView();
            INameScope nameScope = new NameScope();
            listView.SetBinding(ItemsView.VerticalScrollBarVisibilityProperty, static (OfflineMapAreasView viewer) => viewer.VerticalScrollBarVisibility, source: RelativeBindingSource.TemplatedParent);
            listView.SetBinding(ItemsView.ItemsSourceProperty, static (OfflineMapAreasView viewer) => viewer.ItemTemplate, source: RelativeBindingSource.TemplatedParent);
            NameScope.SetNameScope(listView, nameScope);
            nameScope.RegisterName(ItemsViewName, listView);
            return listView;
        }
    }
}
#endif