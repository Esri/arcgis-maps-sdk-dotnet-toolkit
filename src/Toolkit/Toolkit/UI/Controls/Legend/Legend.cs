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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#elif __IOS__
using Control = UIKit.UIView;
#elif __ANDROID__
using Control = Android.Views.ViewGroup;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The Legend control is used to display symbology and description for a set of <see cref="Layer"/>s
    /// in a <see cref="Map"/> or <see cref="Scene"/> contained in a <see cref="GeoView"/>.
    /// </summary>
    public partial class Legend
    {
        private readonly LegendDataSource _datasource;

#if !__ANDROID__
        /// <summary>
        /// Initializes a new instance of the <see cref="Legend"/> class.
        /// </summary>
        public Legend()
        {
            _datasource = new LegendDataSource(this);
            Initialize();
        }
#endif

#if XAMARIN
        private GeoView _geoView;
#endif

        /// <summary>
        /// Gets or sets the geoview that contain the layers whose symbology and description will be displayed.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        public GeoView GeoView
        {
#if XAMARIN
            get => _geoView;
            set
            {
                if (_geoView != value)
                {
                    var oldView = _geoView;
                    _geoView = value;
                    _datasource.SetGeoView(value);
                }
            }
#else
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
#endif
        }

        /// <summary>
        /// Gets or sets a value indicating whether the scale of <see cref="GeoView"/> and any scale ranges on the <see cref="Layer"/>s
        /// are used to determine when legend for layer is displayed.
        /// </summary>
        /// <value>
        /// If <c>true</c>, legend for layer is displayed only when layer is in visible scale range;
        /// otherwise, <c>false</c>, legend for layer is displayed regardless of its scale range.
        /// </value>
        public bool FilterByVisibleScaleRange
        {
#if XAMARIN
            get => _datasource.FilterByVisibleScaleRange;
            set => _datasource.FilterByVisibleScaleRange = value;
#else
            get { return (bool)GetValue(FilterByVisibleScaleRangeProperty); }
            set { SetValue(FilterByVisibleScaleRangeProperty, value); }
#endif
        }

        /// <summary>
        /// Gets or sets a value indicating whether the visibility of a <see cref="Layer"/>
        /// is used to determine when the legend for the layer is displayed.
        /// </summary>
        /// <value>
        /// If <c>true</c>, legend for the layer and sublayers is displayed only when the layer's <see cref="ILayerContent.IsVisible"/> property is true.
        /// </value>
        public bool FilterHiddenLayers
        {
#if XAMARIN
            get => _datasource.FilterHiddenLayers;
            set => _datasource.FilterHiddenLayers = value;
#else
            get { return (bool)GetValue(FilterHiddenLayersProperty); }
            set { SetValue(FilterHiddenLayersProperty, value); }
#endif
        }

        /// <summary>
        /// Gets or sets a value indicating whether the order of layers in the <see cref="GeoView"/> are displayed top to bottom.
        /// </summary>
        /// <value>
        /// If <c>true</c>, legend for layers is displayed from top to bottom order;
        /// otherwise, <c>false</c>, legend for layers is displayed from bottom to top order.
        /// </value>
        public bool ReverseLayerOrder
        {
#if XAMARIN
            get => _datasource.ReverseLayerOrder;
            set => _datasource.ReverseLayerOrder = value;
#else
            get { return (bool)GetValue(ReverseLayerOrderProperty); }
            set { SetValue(ReverseLayerOrderProperty, value); }
#endif
        }
    }
}