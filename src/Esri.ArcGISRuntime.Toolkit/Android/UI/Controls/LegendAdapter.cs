// /*******************************************************************************
//  * Copyright 2017 Esri
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

using Android.Content;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class LegendAdapter : BaseAdapter<LayerContentViewModel>
    {
        private readonly IReadOnlyList<LayerContentViewModel> _allLayers;
        private readonly Context _context;
        private readonly Type _legendItemView;
        private List<LayerContentViewModel> _activeLayers = new List<LayerContentViewModel>();

        internal LegendAdapter(Context context, IReadOnlyList<LayerContentViewModel> allLayers, Type legendItemView)
        {
            _context = context;
            _allLayers = allLayers;
            _legendItemView = legendItemView;
            if (_allLayers is INotifyCollectionChanged)
            {
                var incc = _allLayers as INotifyCollectionChanged;
                var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc);
                listener.OnEventAction = (instance, source, eventArgs) => { NotifyDataSetChanged(); };
                listener.OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent;
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
            var view = convertView as LegendItemView;
            if (view == null)
            {
                var legend = _allLayers[position];

                switch (_legendItemView?.Name)
                {
                    case nameof(LegendTrunkItemView):
                        {
                            view = new LegendTrunkItemView(_context);
                            break;
                        }
                    case nameof(LegendBranchItemView):
                        {
                            view = new LegendBranchItemView(_context);
                            break;
                        }
                    case nameof(LegendLeafItemView):
                        {
                            view = new LegendLeafItemView(_context);
                            break;
                        }
                }

                view?.Update(legend);
            }
            return view;
        }
    }
}