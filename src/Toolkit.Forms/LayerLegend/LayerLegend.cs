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

using Esri.ArcGISRuntime.Mapping;
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// The Legend Control that generates a list of Legend Items for a Layer.
    /// </summary>
    [System.Obsolete("Deprecated in favor of Legend control")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class LayerLegend : View
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LayerLegend"/> class.
        /// </summary>
        public LayerLegend()
#if __ANDROID__
            : this(new UI.Controls.LayerLegend(global::Android.App.Application.Context))
#else
            : this(new UI.Controls.LayerLegend())
#endif
        {
        }

        internal LayerLegend(UI.Controls.LayerLegend nativeLayerLegend)
        {
            NativeLayerLegend = nativeLayerLegend;

#if NETFX_CORE
            nativeLayerLegend.SizeChanged += (o, e) => InvalidateMeasure();
#endif
        }

        internal UI.Controls.LayerLegend NativeLayerLegend { get; }

        /// <summary>
        /// Identifies the <see cref="LayerContent"/> bindable property.
        /// </summary>
        public static readonly BindableProperty LayerContentProperty =
            BindableProperty.Create(nameof(LayerContent), typeof(ILayerContent), typeof(LayerLegend), null, BindingMode.OneWay, null, OnLayerContentPropertyChanged);

        /// <summary>
        /// Gets or sets the layer to display the legend for.
        /// </summary>
        /// <seealso cref="LayerContentProperty"/>
        public ILayerContent? LayerContent
        {
            get { return (ILayerContent)GetValue(LayerContentProperty); }
            set { SetValue(LayerContentProperty, value); }
        }

        private static void OnLayerContentPropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
        {
            var layerLegend = bindable as LayerLegend;
            if (layerLegend?.NativeLayerLegend != null)
            {
                layerLegend.NativeLayerLegend.LayerContent = newValue as ILayerContent;
                layerLegend.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="IncludeSublayers"/> bindable property.
        /// </summary>
        public static readonly BindableProperty IncludeSublayersProperty =
            BindableProperty.Create(nameof(IncludeSublayers), typeof(bool), typeof(LayerLegend), true, BindingMode.OneWay, null, OnIncludeSublayersPropertyChanged);

        /// <summary>
        /// Gets or sets a value indicating whether the entire <see cref="ILayerContent"/> tree hierarchy should be rendered.
        /// </summary>
        /// <seealso cref="IncludeSublayersProperty"/>
        public bool IncludeSublayers
        {
            get { return (bool)GetValue(IncludeSublayersProperty); }
            set { SetValue(IncludeSublayersProperty, value); }
        }

        private static void OnIncludeSublayersPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var layerLegend = bindable as LayerLegend;
            if (layerLegend?.NativeLayerLegend != null && newValue is bool)
            {
                layerLegend.NativeLayerLegend.IncludeSublayers = (bool)newValue;
                layerLegend.InvalidateMeasure();
            }
        }
    }
}
