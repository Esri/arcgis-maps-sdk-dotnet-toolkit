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

#if !__IOS__ && !__ANDROID__ && !NETSTANDARD2_0 && !NETFX_CORE

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
#if XAMARIN_FORMS
using Esri.ArcGISRuntime.Xamarin.Forms;
#else
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Core;
using Windows.UI.Xaml;
#endif
#endif

namespace Esri.ArcGISRuntime.Toolkit.Preview.UI
{
#if NETFX_CORE
    [Windows.UI.Xaml.Data.Bindable]
#endif
    internal class TocDataSource : LayerContentDataSource<TocItem>
    {
        public TocDataSource(DependencyObject owner)
            : base(owner)
        {
            _showLegend = true;
        }

        private bool _showLegend;

        public bool ShowLegend
        {
            get => _showLegend;
            set
            {
                if (_showLegend != value)
                {
                    _showLegend = value;
                    if (Items != null)
                    {
                        foreach (var item in Items)
                        {
                            item.ShowLegend = value;
                        }
                    }
                }
            }
        }

        protected override void OnDocumentPropertyChanged(object sender, string propertyName)
        {
            if (propertyName == nameof(Map.Basemap))
            {
                MarkCollectionDirty(false);
            }

            base.OnDocumentPropertyChanged(sender, propertyName);
        }

        protected override void OnLayerPropertyChanged(ILayerContent layer, string propertyName)
        {
            base.OnLayerPropertyChanged(layer, propertyName);
            if (propertyName == nameof(FeatureLayer.Renderer) && ShowLegend)
            {
                MarkCollectionDirty(false);
            }
        }

        protected override List<TocItem> OnRebuildCollection()
        {
            return BuildTocList() ?? new List<TocItem>();
        }

        private List<TocItem> BuildTocList()
        {
            IEnumerable<Layer> layers = null;
            Basemap basemap = null;

            if (GeoView is MapView mv)
            {
                layers = mv.Map?.OperationalLayers;
                basemap = mv.Map?.Basemap;
            }
            else if (GeoView is SceneView sv)
            {
                layers = sv.Scene?.OperationalLayers;
                basemap = sv.Scene?.Basemap;
            }

            var result = new List<TocItem>();
            if (layers != null)
            {
                result.AddRange(layers.Reverse().Select(l => new TocItem(l, _showLegend, 0, null) { IsExpanded = true }));
            }

            if (basemap != null)
            {
                result.Add(new TocItem(basemap, false, 0, null));
            }

            return result;
        }
    }
}

#endif