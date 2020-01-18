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

#if __ANDROID__
using System.Collections.Generic;
using System.Collections.Specialized;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The Legend control is used to display symbology and description for a set of <see cref="Layer"/>s
    /// in a <see cref="Map"/> or <see cref="Scene"/> contained in a <see cref="GeoView"/>.
    /// </summary>
    [Register("Esri.ArcGISRuntime.Toolkit.UI.Controls.egend")]
    public partial class Legend : Android.Widget.ListView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Legend"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        public Legend(Context context)
            : base(context)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Legend"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        /// <param name="attr">The attributes of the AXML element declaring the view</param>
        public Legend(Context context, IAttributeSet attr)
            : base(context, attr)
        {
            Initialize();
        }

        private void Initialize()
        {
            Adapter = new LegendAdapter(Context, _datasource);
        }

      /*  protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            // TODO
        }*/



        internal class LegendAdapter : BaseAdapter<object>
        {
            private readonly IList<object> _layers;
            private readonly Context _context;

            internal LegendAdapter(Context context, IList<object> layers)
            {
                _context = context;
                _layers = layers;
                if (_layers is INotifyCollectionChanged)
                {
                    var incc = _layers as INotifyCollectionChanged;
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

            public override object this[int position] => _layers[position];

            public override int Count => _layers.Count;

            public override long GetItemId(int position)
            {
                return position;
            }

            public override View GetView(int position, View convertView, ViewGroup parent)
            {
                var layerLegend = _layers[position];
                if (convertView == null)
                {
                    convertView = new Android.Widget.TextView(_context);
                }

                var tv = (convertView as TextView);
                if (tv != null)
                {
                    if (layerLegend is Layer l)
                    {
                        tv.Text = l.Name;
                        tv.SetTextSize(ComplexUnitType.Dip, 20);
                    }
                    else if(layerLegend is ILayerContent il)
                    {
                        tv.Text = il.Name;
                        tv.SetTextSize(ComplexUnitType.Dip, 14);
                    }
                    else if (layerLegend is LegendInfo li)
                    {
                        tv.Text = li.Name;
                        tv.SetTextSize(ComplexUnitType.Dip, 12);
                    }
                }

                return convertView;
            }
        }
    }
}
#endif
