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
using Android.Util;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Toolkit.Internal;
using static Android.Widget.AbsListView;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class LayerList
    {
        private ListView _listView;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerList"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        public LayerList(Context context) : base(context) { Initialize(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerList"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        /// <param name="attr">The attributes of the AXML element declaring the view</param>
        public LayerList(Context context, IAttributeSet attr) : base(context, attr) { Initialize(); }

        internal void Initialize()
        {
            _listView = new ListView(Context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent),
                ScrollingCacheEnabled = false,
                PersistentDrawingCache = PersistentDrawingCaches.NoCache,
            };

            AddView(_listView);
        }

        private void Refresh()
        {
            if (_listView == null)
            {
                return;
            }

            if ((GeoView as MapView)?.Map == null && (GeoView as SceneView)?.Scene == null)
            {
                _listView.Adapter = null;
                return;
            }

            ObservableLayerContentList layers = null;
            if (GeoView is MapView)
            {
                layers = new ObservableLayerContentList(GeoView as MapView, ShowLegendInternal)
                {
                    ReverseOrder = !ReverseLayerOrder,
                };
            }
            else if (GeoView is SceneView)
            {
                layers = new ObservableLayerContentList(GeoView as SceneView, ShowLegendInternal)
                {
                    ReverseOrder = !ReverseLayerOrder,
                };
            }

            if (layers == null)
            {
                _listView.Adapter = null;
                return;
            }
            foreach (var l in layers)
            {
                if (!(l.LayerContent is Layer))
                    continue;
                var layer = l.LayerContent as Layer;
                if (layer.LoadStatus == LoadStatus.Loaded)
                {
                    l.UpdateLayerViewState(GeoView.GetLayerViewState(layer));
                }
            }

            ScaleChanged();
            SetLayerContentList(layers);
            _listView.Adapter = new LegendAdapter(Context, layers);
            _listView.SetHeightBasedOnChildren();
        }
        
        /// <inheritdoc />
        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            // Initialize dimensions of root layout
            MeasureChild(_listView, widthMeasureSpec, MeasureSpec.MakeMeasureSpec(MeasureSpec.GetSize(heightMeasureSpec), MeasureSpecMode.AtMost));

            // Calculate the ideal width and height for the view
            var desiredWidth = PaddingLeft + PaddingRight + _listView.MeasuredWidth;
            var desiredHeight = PaddingTop + PaddingBottom + _listView.MeasuredHeight;

            // Get the width and height of the view given any width and height constraints indicated by the width and height spec values
            var width = ResolveSize(desiredWidth, widthMeasureSpec);
            var height = ResolveSize(desiredHeight, heightMeasureSpec);

            SetMeasuredDimension(width, height);
        }

        /// <inheritdoc />
        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            // Forward layout call to the root layout
            _listView.Layout(PaddingLeft, PaddingTop, _listView.MeasuredWidth + PaddingLeft, _listView.MeasuredHeight + PaddingBottom);
        }
    }
}