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
using Android.Graphics;
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
    [Register("Esri.ArcGISRuntime.Toolkit.UI.Controls.Legend")]
    public partial class Legend : Android.Widget.ListView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Legend"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        public Legend(Context context)
            : base(context)
        {
            _datasource = new LegendDataSource(this);
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Legend"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        /// <param name="attr">The attributes of the AXML element declaring the view.</param>
        public Legend(Context context, IAttributeSet attr)
            : base(context, attr)
        {
            _datasource = new LegendDataSource(this);
            Initialize();
        }

        private void Initialize()
        {
            Adapter = new LegendAdapter(Context, _datasource);
            DividerHeight = 0;
        }

        internal class LegendAdapter : BaseAdapter<object>
        {
            private readonly IList<LegendEntry> _layers;
            private readonly Context _context;

            internal LegendAdapter(Context context, IList<LegendEntry> layers)
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
                        OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent,
                    };
                    incc.CollectionChanged += listener.OnEvent;
                }
            }

            public override object this[int position] => _layers[position];

            public override int Count => _layers.Count;

            public override long GetItemId(int position) => position;

            public override View GetView(int position, View convertView, ViewGroup parent)
            {
                var layerLegend = _layers[position];
                if (convertView == null)
                {
                    convertView = new LegendItemView(_context);
                }

                var tv = convertView as LegendItemView;
                if (tv != null)
                {
                    tv.Update(layerLegend);
                }

                return convertView;
            }

            private class LegendItemView : LinearLayout
            {
                private readonly TextView _textView;
                private readonly SymbolDisplay _symbol;

                internal LegendItemView(Context context)
                    : base(context)
                {
                    Orientation = Orientation.Horizontal;
                    LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
                    SetGravity(GravityFlags.Top);

                    _textView = new TextView(context)
                    {
                        LayoutParameters = new LinearLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent) { Gravity = GravityFlags.CenterVertical | GravityFlags.Left },
                    };
                    var maxSize = (int)(Resources.DisplayMetrics.Density * 40);
                    _symbol = new SymbolDisplay(context)
                    {
                        LayoutParameters = new LinearLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent) { Gravity = GravityFlags.Center, Width = maxSize },
                    };
                    _symbol.SetMaxHeight(maxSize);
                    _symbol.SetMaxWidth(maxSize);
                    AddView(_symbol);
                    AddView(_textView);
                    RequestLayout();
                }

                internal void Update(object item)
                {
                    if (item is LegendEntry entry)
                    {
                        if (entry.Content is Layer layer)
                        {
                            _textView.Text = layer.Name;
                            _textView.SetTextSize(ComplexUnitType.Dip, 18);
                            _textView.SetPadding(0, 0, 0, 0);
                            _symbol.Visibility = ViewStates.Gone;
                            _symbol.Symbol = null;
                        }
                        else if (entry.Content is ILayerContent layerContent)
                        {
                            _textView.Text = layerContent.Name;
                            _textView.SetTextSize(ComplexUnitType.Dip, 14);
                            _textView.SetPadding(0, 0, 0, 0);
                            _symbol.Visibility = ViewStates.Gone;
                            _symbol.Symbol = null;
                        }
                        else if (entry.Content is LegendInfo legendInfo)
                        {
                            _textView.Text = legendInfo.Name;
                            _textView.SetTextSize(ComplexUnitType.Dip, 12);
                            _textView.SetPadding(5, 0, 0, 0);
                            _symbol.Visibility = ViewStates.Visible;
                            _symbol.Symbol = legendInfo.Symbol;
                        }
                    }
                }
            }
        }
    }
}
#endif
