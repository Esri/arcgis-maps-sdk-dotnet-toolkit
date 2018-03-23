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
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class LegendTableSource : UITableViewSource, INotifyCollectionChanged
    {
        private readonly IReadOnlyList<LayerContentViewModel> _legends;
        internal static readonly NSString CellId = new NSString(nameof(LegendItemCell));

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public LegendTableSource(IReadOnlyList<LayerContentViewModel> legends)
            : base()
        {
            _legends = legends;
            if (_legends is INotifyCollectionChanged)
            {
                var incc = _legends as INotifyCollectionChanged;
                var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                {
                    OnEventAction = (instance, source, eventArgs) =>
                     {
                         CollectionChanged?.Invoke(this, eventArgs);
                     },
                    OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent
                };
                incc.CollectionChanged += listener.OnEvent;
            }
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            return _legends?.Count ?? 0;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var info = _legends[indexPath.Row];
            var cell = tableView.DequeueReusableCell(CellId, indexPath);
            (cell as LegendItemCell)?.Update(info);
            cell?.SetNeedsUpdateConstraints();
            cell?.UpdateConstraints();
            return cell;
        }
    }
}