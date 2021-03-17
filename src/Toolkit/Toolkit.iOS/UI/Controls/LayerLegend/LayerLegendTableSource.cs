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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Esri.ArcGISRuntime.Mapping;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class LayerLegendTableSource : UITableViewSource, INotifyCollectionChanged
    {
        private readonly IReadOnlyList<LegendInfo> _layerLegends;
        internal static readonly NSString CellId = new NSString(nameof(LayerLegendItemCell));

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public LayerLegendTableSource(IReadOnlyList<LegendInfo> layerLegends)
            : base()
        {
            _layerLegends = layerLegends;
            if (_layerLegends is INotifyCollectionChanged)
            {
                var incc = _layerLegends as INotifyCollectionChanged;
                var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                {
                    OnEventAction = (instance, source, eventArgs) =>
                     {
                         CollectionChanged?.Invoke(this, eventArgs);
                     },
                    OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent,
                };
                incc.CollectionChanged += listener.OnEvent;
            }
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            return _layerLegends?.Count ?? 0;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var info = _layerLegends[indexPath.Row];
            var cell = tableView.DequeueReusableCell(CellId, indexPath);
            (cell as LayerLegendItemCell)?.Update(info);
            cell?.SetNeedsUpdateConstraints();
            cell?.UpdateConstraints();
            return cell;
        }
    }
}