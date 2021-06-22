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
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI.Controls.OverviewMap;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// Defines a small "overview" (or "inset") map displaying a representation of the attached <see cref="GeoView"/>'s current viewpoint.
    /// </summary>
    public class OverviewMap : MapView
    {
        private readonly OverviewMapController _controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="OverviewMap"/> class.
        /// </summary>
        public OverviewMap()
        {
            IsAttributionTextVisible = false;
            Map = new Map(BasemapStyle.ArcGISTopographic);

            _controller = new OverviewMapController(this)
            {
                ScaleFactor = ScaleFactor,
                AttachedView = GeoView,
            };
        }

        /// <summary>
        /// Gets or sets the amount to scale the overview map's viewpoint compared to the attached <see cref="GeoView"/>.
        /// </summary>
        /// <remarks>
        /// The default is 25.
        /// </remarks>
        public double ScaleFactor
        {
            get { return (double)GetValue(ScaleFactorProperty); }
            set { SetValue(ScaleFactorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the symbol used to draw a representation of the <see cref="GeoView"/>'s current viewpoint.
        /// </summary>
        /// <remarks>
        /// The default is an empty fill symbol with a 1 point red outline or a red crosshair for scenes.
        /// </remarks>
        public Symbol Symbol
        {
            get { return (Symbol)GetValue(SymbolProperty); }
            set { SetValue(SymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Symbol"/> bindable property.
        /// </summary>
        public static readonly BindableProperty SymbolProperty =
            BindableProperty.Create(nameof(Symbol), typeof(Symbol), typeof(OverviewMap), null, propertyChanged: OnExtentSymbolPropertyChanged);

        /// <summary>
        /// Gets or sets the geoview whose extent is to be displayed.
        /// </summary>
        /// <remarks>
        /// Note that by default interaction with <see cref="OverviewMap"/> will navigate the attached GeoView.
        /// </remarks>
        public GeoView GeoView
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ScaleFactor"/> bindable property.
        /// </summary>
        public static readonly BindableProperty ScaleFactorProperty =
            BindableProperty.Create(nameof(ScaleFactor), typeof(double), typeof(OverviewMap), 25.0, propertyChanged: OnScaleFactorPropertyChanged);

        /// <summary>
        /// Identifies the <see cref="GeoView"/> bindable property.
        /// </summary>
        public static readonly BindableProperty GeoViewProperty =
            BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(OverviewMap), null, BindingMode.OneWay, null, propertyChanged: OnGeoViewPropertyChanged);

        private static void OnGeoViewPropertyChanged(BindableObject sender, object oldValue, object newValue)
            => ((OverviewMap)sender)._controller.AttachedView = newValue as GeoView;

        private static void OnExtentSymbolPropertyChanged(BindableObject sender, object oldValue, object newValue)
            => ((OverviewMap)sender)._controller.Symbol = newValue as FillSymbol;

        private static void OnScaleFactorPropertyChanged(BindableObject sender, object oldValue, object newValue)
            => ((OverviewMap)sender)._controller.ScaleFactor = (double)newValue;
    }
}
