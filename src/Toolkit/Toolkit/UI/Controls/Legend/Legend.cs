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
    public partial class Legend
    {
        private readonly LegendDataSource _datasource = new LegendDataSource(null);

#if !__ANDROID__
        public Legend()
        {
            Initialize();
        }
#endif

#if XAMARIN
        private GeoView _geoView;
#endif

        public GeoView GeoView
        {
#if XAMARIN
            get { return _geoView; }
            set
            { 
                if(_geoView != value) 
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
    }
}