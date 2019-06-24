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

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class LegendAdapter : BaseAdapter<LayerContentViewModel>
    {
        private readonly IReadOnlyList<LayerContentViewModel> _allLayers;
        private readonly Context _context;

        internal LegendAdapter(Context context, IReadOnlyList<LayerContentViewModel> allLayers)
        {
            _context = context;
            _allLayers = allLayers;
            if (_allLayers is INotifyCollectionChanged)
            {
                var incc = _allLayers as INotifyCollectionChanged;
                var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                {
                    OnEventAction = (instance, source, eventArgs) =>
                    {
                        NotifyDataSetChanged();
                    },
                    OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent
                };
                incc.CollectionChanged += listener.OnEvent;
            }
        }

        public override LayerContentViewModel this[int position] => _allLayers[position];

        public override int Count => _allLayers.Count;

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var legend = _allLayers[position];
            if (convertView == null)
            {
                convertView = new LegendItemView(_context);
            }

            (convertView as LegendItemView)?.Update(legend);
            return convertView;
        }
    }
}