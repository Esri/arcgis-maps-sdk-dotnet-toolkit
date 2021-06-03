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

using System.Collections.Generic;
using System.Collections.Specialized;
using Android.Content;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class LayerLegendAdapter : BaseAdapter<LegendInfo>
    {
        private readonly IReadOnlyList<LegendInfo> _layerLegends;
        private readonly Context _context;

        internal LayerLegendAdapter(Context context, IReadOnlyList<LegendInfo> layerLegends)
        {
            _context = context;
            _layerLegends = layerLegends;
            if (_layerLegends is INotifyCollectionChanged)
            {
                var incc = _layerLegends as INotifyCollectionChanged;
                var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                {
                    OnEventAction = (instance, source, eventArgs) =>
                    {
                        NotifyDataSetChanged();
                    },
                    OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent,
                };
                incc.CollectionChanged += listener.OnEvent;
            }
        }

        public override LegendInfo this[int position] => _layerLegends[position];

        public override int Count => _layerLegends.Count;

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var layerLegend = _layerLegends[position];
            if (convertView == null)
            {
                convertView = new LayerLegendItemView(_context);
            }

            (convertView as LayerLegendItemView)?.Update(layerLegend);
            return convertView;
        }
    }
}