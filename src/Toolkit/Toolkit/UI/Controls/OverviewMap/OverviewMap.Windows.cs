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

#if !XAMARIN
using System;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
#else
using System.Windows;
using System.Windows.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls.OverviewMap
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
                Symbol = Symbol,
                ScaleFactor = ScaleFactor,
                AttachedView = GeoView,
            };

            BorderThickness = new Thickness(1);
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
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
        /// Gets or sets the symbol used to draw the <see cref="GeoView"/>'s extent.
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
        /// Identifies the <see cref="ScaleFactor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScaleFactorProperty =
            DependencyProperty.Register(nameof(ScaleFactor), typeof(double), typeof(OverviewMap), new PropertyMetadata(25.0, OnScaleFactorPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Symbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SymbolProperty =
            DependencyProperty.Register(nameof(Symbol), typeof(Symbol), typeof(OverviewMap), new PropertyMetadata(null, OnSymbolPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(OverviewMap), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((OverviewMap)d)._controller.AttachedView = e.NewValue as GeoView;

        private static void OnSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((OverviewMap)d)._controller.Symbol = e.NewValue as Symbol;

        private static void OnScaleFactorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((OverviewMap)d)._controller.ScaleFactor = (double)e.NewValue;
    }
}

#endif
