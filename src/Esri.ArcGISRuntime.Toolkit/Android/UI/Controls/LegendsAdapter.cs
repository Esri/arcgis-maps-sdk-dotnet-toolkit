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
using System.Collections.Generic;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class LegendsAdapter : BaseAdapter<LayerLegendInfo>
    {
        private IList<LayerLegendInfo> _legends;
        private Context _context;

        internal LegendsAdapter(Context context, IList<LayerLegendInfo> legends)
        {
            _context = context;
            _legends = legends;
        }

        public override LayerLegendInfo this[int position] => _legends[position];

        public override int Count => _legends.Count;

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView;
            if(view == null)
            {
                var rowView = view as LegendView ?? new LegendView(_context);
                var legend = _legends[position];
                rowView.Update(legend);
                return rowView;
            }
            return view;
        }
    }
}