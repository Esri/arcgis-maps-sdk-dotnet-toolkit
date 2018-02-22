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
using Android.Graphics;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class LegendItemView : LinearLayout
    {
        private readonly TextView _textView;
        private readonly LayerLegend _layerLegend;
        private readonly ListView _listView;

        internal LegendItemView(Context context) : base(context)
        {
            Orientation = Orientation.Vertical;
            LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
            SetGravity(GravityFlags.Top);

            _textView = new TextView(context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent)
            };
            AddView(_textView);

            _layerLegend = new LayerLegend(context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent),
                ShowEntireTreeHierarchy = false
            };
            AddView(_layerLegend);

            _listView = new ListView(context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent),
                ScrollingCacheEnabled = false,
                PersistentDrawingCache = PersistentDrawingCaches.NoCache,
            };
            AddView(_listView);
            RequestLayout();
        }

        private void Refresh(LayerContentViewModel layerContent, string propertyName)
        {
            if (propertyName == nameof(LayerContentViewModel.Sublayers))
            {
                var subLayers = layerContent?.Sublayers;
                if (subLayers == null)
                {
                    return;
                }
                if (_listView.Adapter == null)
                {
                    _listView.Adapter = new LegendAdapter(Context, new List<LayerContentViewModel>(subLayers));
                    _listView.SetHeightBasedOnChildren();
                }
                else
                {
                    (_listView.Adapter as LegendAdapter)?.NotifyDataSetChanged();
                }
            }
            else if (propertyName == nameof(LayerContentViewModel.DisplayLegend))
            {
                Visibility = (layerContent?.DisplayLegend ?? false) ? ViewStates.Visible : ViewStates.Gone;
            }
        }

        internal void Update(LayerContentViewModel layerContent)
        {
            if(layerContent == null)
            {
                return;
            }

            if (!layerContent.IsSublayer)
            {
                _textView.SetTypeface(null, TypefaceStyle.Bold);
            }
            else
            {
                _textView.SetPadding(10, 0, 0, 0);
                _layerLegend.SetPadding(10, 0, 0, 0);
                _listView.SetPadding(10, 0, 0, 0);
            }

            _textView.Text = layerContent.LayerContent?.Name;
            _layerLegend.LayerContent = layerContent.LayerContent;
            if (layerContent.Sublayers != null)
            {
                _listView.Adapter = new LegendAdapter(Context, new List<LayerContentViewModel>(layerContent.Sublayers));
                _listView.SetHeightBasedOnChildren();
            }
            if (layerContent is INotifyPropertyChanged)
            {
                var inpc = layerContent as INotifyPropertyChanged;
                var listener = new Internal.WeakEventListener<INotifyPropertyChanged, object, PropertyChangedEventArgs>(inpc)
                {
                    OnEventAction = (instance, source, eventArgs) =>
                    {
                        Refresh(instance as LayerContentViewModel, eventArgs.PropertyName);
                    },
                    OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent
                };
                inpc.PropertyChanged += listener.OnEvent;
            }
        }
    }
}