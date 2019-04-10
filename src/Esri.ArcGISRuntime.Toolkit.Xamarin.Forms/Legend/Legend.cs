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
using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Internal;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// The Legend control is used to display symbology and description for a set of <see cref="Layer"/>s
    /// in a <see cref="Map"/> or <see cref="Scene"/> contained in a <see cref="GeoView"/>.
    /// </summary>
    public class Legend : View
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Legend"/> class
        /// </summary>
        public Legend()
            : this(new Esri.ArcGISRuntime.Toolkit.UI.Controls.Legend())
        {
        }

        internal Legend(Esri.ArcGISRuntime.Toolkit.UI.Controls.Legend nativeLegend)
        {
            NativeLegend = nativeLegend;

#if NETFX_CORE
            nativeLegend.SizeChanged += (o, e) => InvalidateMeasure();
#endif
        }

        internal Esri.ArcGISRuntime.Toolkit.UI.Controls.Legend NativeLegend { get; }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> bindable property.
        /// </summary>
        public static readonly BindableProperty GeoViewProperty =
            BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(Legend), null, BindingMode.OneWay, null, OnGeoViewPropertyChanged);

        /// <summary>
        /// Gets or sets the geoview that contain the layers whose symbology and description will be displayed.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        /// <seealso cref="GeoViewProperty"/>
        public GeoView GeoView
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        private static void OnGeoViewPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var legend = bindable as Legend;
            if (legend?.NativeLegend != null)
            {
#if !NETSTANDARD2_0
                legend.NativeLegend.GeoView = (newValue as GeoView)?.GetNativeGeoView();
#endif
                legend.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="FilterByVisibleScaleRange"/> bindable property.
        /// </summary>
        public static readonly BindableProperty FilterByVisibleScaleRangeProperty =
            BindableProperty.Create(nameof(FilterByVisibleScaleRange), typeof(bool), typeof(Legend), false, BindingMode.OneWay, null, OnFilterByVisibleScaleRangePropertyChanged);

        /// <summary>
        /// Gets or sets a value indicating whether the scale of <see cref="GeoView"/> and any scale ranges on the <see cref="Layer"/>s
        /// are used to determine when legend for layer is displayed.
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, legend for layer is displayed only when layer is in visible scale range;
        /// otherwise, <c>false</c>, legend for layer is displayed regardless of its scale range.
        /// </remarks>
        /// <seealso cref="FilterByVisibleScaleRangeProperty"/>
        public bool FilterByVisibleScaleRange
        {
            get { return (bool)GetValue(FilterByVisibleScaleRangeProperty); }
            set { SetValue(FilterByVisibleScaleRangeProperty, value); }
        }

        private static void OnFilterByVisibleScaleRangePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var legend = bindable as Legend;
            if (legend?.NativeLegend != null && newValue is bool)
            {
                legend.NativeLegend.FilterByVisibleScaleRange = (bool)newValue;
                legend.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="ReverseLayerOrder"/> bindable property.
        /// </summary>
        public static readonly BindableProperty ReverseLayerOrderProperty =
            BindableProperty.Create(nameof(ReverseLayerOrder), typeof(bool), typeof(Legend), false, BindingMode.OneWay, null, OnReverseLayerOrderPropertyChanged);

        /// <summary>
        /// Gets or sets a value indicating whether the order of layers in the <see cref="GeoView"/>, top to bottom, is used.
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, legend for layers is displayed from top to bottom order;
        /// otherwise, <c>false</c>, legend for layers is displayed from bottom to top order.
        /// </remarks>
        /// <seealso cref="ReverseLayerOrderProperty"/>
        public bool ReverseLayerOrder
        {
            get { return (bool)GetValue(ReverseLayerOrderProperty); }
            set { SetValue(ReverseLayerOrderProperty, value); }
        }

        private static void OnReverseLayerOrderPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var legend = bindable as Legend;
            if (legend?.NativeLegend != null && newValue is bool)
            {
                legend.NativeLegend.ReverseLayerOrder = !(bool)newValue;
                legend.InvalidateMeasure();
            }
        }
    }
}
