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

using System.Windows;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Event argument fired by the <see cref="LayerList"/> when right-clicking an item
    /// </summary>
    /// <seealso cref="LayerList.LayerContentContextMenuOpening"/>
    public class LayerContentContextMenuEventArgs : RoutedEventArgs
    {
        internal LayerContentContextMenuEventArgs(object source, ContextMenuEventArgs args)
            : base(args.RoutedEvent, source)
        {
        }

        /// <summary>
        /// Gets the layer content instance that was clicked
        /// </summary>
        public ILayerContent LayerContent { get; internal set; }

        /// <summary>
        /// Gets a reference to the context menu that will be displayed
        /// </summary>
        public ContextMenu Menu { get; internal set; }

        /// <summary>
        /// Gets a set of menu items to display. If empty, no context menu will be displayed
        /// </summary>
        public System.Collections.Generic.IList<MenuItem> MenuItems { get; internal set; }
    }
}