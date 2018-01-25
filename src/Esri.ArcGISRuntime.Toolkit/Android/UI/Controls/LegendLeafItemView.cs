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
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class LegendLeafItemView : LegendItemView
    {
        private readonly LayerLegend _layerLegend;

        public LegendLeafItemView(Context context) : base(context)
        {
            _layerLegend = new LayerLegend(context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent),
                ShowEntireTreeHierarchy = false
            };
            _layerLegend.SetPadding(10, 0, 0, 0);
            AddView(_layerLegend);
            RequestLayout();
        }

        internal override void Update(LayerContentViewModel layerContent)
        {
            _layerLegend.LayerContent = layerContent?.LayerContent;
            if (layerContent is INotifyPropertyChanged)
            {
                var inpc = layerContent as INotifyPropertyChanged;
                var listener = new Internal.WeakEventListener<INotifyPropertyChanged, object, PropertyChangedEventArgs>(inpc);
                listener.OnEventAction = (instance, source, eventArgs) =>
                {
                    if (eventArgs.PropertyName == nameof(LayerContentViewModel.DisplayLegend))
                    {
                        Visibility = ((instance as LayerContentViewModel)?.DisplayLegend ?? false) ? ViewStates.Visible : ViewStates.Invisible;
                    }
                };
                listener.OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent;
                inpc.PropertyChanged += listener.OnEvent;
            }
        }
    }
}