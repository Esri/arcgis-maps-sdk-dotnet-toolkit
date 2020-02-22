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

#if !__IOS__ && !__ANDROID__ && !NETSTANDARD2_0 && !NETFX_CORE
using System.Windows;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.Preview.UI.Controls
{
    /// <summary>
    /// Event argument fired by the <see cref="TableOfContents"/> when right-clicking an item
    /// </summary>
    /// <seealso cref="TableOfContents.TocItemContextMenuOpening"/>
    public class TocItemContextMenuEventArgs : RoutedEventArgs
    {
        internal TocItemContextMenuEventArgs(object source, ContextMenuEventArgs args)
            : base(args.RoutedEvent, source)
        {
        }

        /// <summary>
        /// Gets a reference to the node that was clicked.
        /// </summary>
        public TocItem Item { get; internal set; }

        /// <summary>
        /// Gets a reference to the context menu that will be displayed
        /// </summary>
        public ContextMenu Menu { get; internal set; }

        /// <summary>
        /// Gets a set of menu items to display. If empty, no context menu will be displayed
        /// </summary>
        public ItemCollection MenuItems { get; internal set; }
    }
}
#endif