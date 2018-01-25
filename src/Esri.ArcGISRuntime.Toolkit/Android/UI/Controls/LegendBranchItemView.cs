﻿// /*******************************************************************************
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
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class LegendBranchItemView : LegendItemView
    {
        private readonly TextView _textView;
        private readonly ListView _listView;

        internal LegendBranchItemView(Context context) : base(context)
        {
            _textView = new TextView(context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent)
            };
            _textView.SetPadding(10, 0, 0, 0);
            AddView(_textView);

            _listView = new ListView(context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent),
                ScrollingCacheEnabled = false
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
                    _listView.Adapter = new LegendAdapter(Context, new List<LayerContentViewModel>(subLayers), typeof(LegendBranchItemView));
                }
                else
                {
                    (_listView.Adapter as LegendAdapter)?.NotifyDataSetChanged();
                }
            }
            else if (propertyName == nameof(LayerContentViewModel.DisplayLegend))
            {
                Visibility = (layerContent?.DisplayLegend ?? false) ? ViewStates.Visible : ViewStates.Invisible;
            }
            else if (propertyName == nameof(LayerContentViewModel.IsSublayer))
            {
                _textView.Visibility = (layerContent?.IsSublayer ?? false) ? ViewStates.Visible : ViewStates.Invisible;
            }
        }

        internal override void Update(LayerContentViewModel layerContent)
        {
            _textView.Text = layerContent?.LayerContent?.Name;
            if (layerContent?.Sublayers != null)
            {
                _listView.Adapter = new LegendAdapter(Context, new List<LayerContentViewModel>(layerContent.Sublayers), typeof(LegendLeafItemView));
            }
            if (layerContent is INotifyPropertyChanged)
            {
                var inpc = layerContent as INotifyPropertyChanged;
                var listener = new Internal.WeakEventListener<INotifyPropertyChanged, object, PropertyChangedEventArgs>(inpc);
                listener.OnEventAction = (instance, source, eventArgs) =>
                {
                    Refresh(instance as LayerContentViewModel, eventArgs.PropertyName);
                };
                listener.OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent;
                inpc.PropertyChanged += listener.OnEvent;
            }
        }
    }
}