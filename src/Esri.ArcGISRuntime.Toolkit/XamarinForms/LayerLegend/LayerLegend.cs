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
    /// The Legend Control that generates a list of Legend Items for a Layer
    /// </summary>
    public class LayerLegend : View
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LayerLegend"/> class
        /// </summary>
        public LayerLegend()
            : this(new UI.Controls.LayerLegend())
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
        public ILayerContent LayerContent
        {
            get { return (ILayerContent)GetValue(LayerContentProperty); }
            set { SetValue(LayerContentProperty, value); }
        }

        private static void OnLayerContentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var layerLegend = bindable as LayerLegend;
            if (layerLegend?.NativeLayerLegend != null)
            {
                layerLegend.NativeLayerLegend.LayerContent = newValue as ILayerContent;
                layerLegend.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="ShowEntireTreeHierarchy"/> bindable property.
        /// </summary>
        public static readonly BindableProperty ShowEntireTreeHierarchyProperty =
            BindableProperty.Create(nameof(ShowEntireTreeHierarchy), typeof(bool), typeof(LayerLegend), true, BindingMode.OneWay, null, OnShowEntireTreeHierarchyPropertyChanged);

        /// <summary>
        /// Gets or sets a value indicating whether the entire <see cref="ILayerContent"/> tree hierarchy should be rendered
        /// </summary>
        /// <seealso cref="ShowEntireTreeHierarchyProperty"/>
        public bool ShowEntireTreeHierarchy
        {
            get { return (bool)GetValue(ShowEntireTreeHierarchyProperty); }
            set { SetValue(ShowEntireTreeHierarchyProperty, value); }
        }

        private static void OnShowEntireTreeHierarchyPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var layerLegend = bindable as LayerLegend;
            if (layerLegend?.NativeLayerLegend != null && newValue is bool)
            {
                layerLegend.NativeLayerLegend.ShowEntireTreeHierarchy = (bool)newValue;
                layerLegend.InvalidateMeasure();
            }
        }
    }
}
