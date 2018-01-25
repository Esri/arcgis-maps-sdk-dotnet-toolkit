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
    internal class LegendTrunkItemView : LegendItemView
    {
        private readonly TextView _textView;
        private readonly LayerLegend _layerLegend;
        private readonly ListView _listView;

        internal LegendTrunkItemView(Context context) : base(context)
        {
            _textView = new TextView(context)
            {
                TextAlignment = TextAlignment.Center,
                LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent)
            };
            _textView.SetTypeface(null, TypefaceStyle.Bold);
            AddView(_textView);

            _layerLegend = new LayerLegend(context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent),
                ShowEntireTreeHierarchy = false
            };
            _layerLegend.SetPadding(10, 0, 0, 0);
            AddView(_layerLegend);

            _listView = new ListView(context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent),
            };
            AddView(_listView);
            RequestLayout();
        }

        internal override void Update(LayerContentViewModel layerContent)
        {           
            _textView.Text = layerContent?.LayerContent?.Name;
            _layerLegend.LayerContent = layerContent?.LayerContent;
            if (layerContent?.Sublayers != null)
            {
                _listView.Adapter = new LegendAdapter(Context, new List<LayerContentViewModel>(layerContent.Sublayers), typeof(LegendBranchItemView));
            }
            if (layerContent is INotifyPropertyChanged)
            {
                var inpc = layerContent as INotifyPropertyChanged;
                var listener = new Internal.WeakEventListener<INotifyPropertyChanged, object, PropertyChangedEventArgs>(inpc);
                listener.OnEventAction = (instance, source, eventArgs) =>
                {
                    if (eventArgs.PropertyName == nameof(LayerContentViewModel.Sublayers))
                    {
                        var subLayers = (instance as LayerContentViewModel)?.Sublayers;
                        if (subLayers == null)
                        {
                            return;
                        }                    
                        if (_listView.Adapter == null)
                        {
                            _listView.Adapter = new LegendAdapter(Context, new List<LayerContentViewModel>(subLayers), typeof(LegendBranchItemView));
                        }
                        else
                        {
                            (_listView.Adapter as LegendAdapter)?.NotifyDataSetChanged();
                        }
                    }
                    else if (eventArgs.PropertyName == nameof(LayerContentViewModel.DisplayLegend))
                    {
                        Visibility = ((instance as LayerContentViewModel)?.DisplayLegend ?? false) ? ViewStates.Visible : ViewStates.Invisible;
                    }
                    else if (eventArgs.PropertyName == nameof(LayerContentViewModel.IsSublayer))
                    {
                        _textView.Visibility = ((instance as LayerContentViewModel)?.IsSublayer ?? false) ? ViewStates.Visible : ViewStates.Invisible;
                    }
                };
                listener.OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent;
                inpc.PropertyChanged += listener.OnEvent;
            }
        }
    }
}