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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
#if XAMARIN_FORMS
using Esri.ArcGISRuntime.Xamarin.Forms;
#else
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Core;
#endif
#endif

#if XAMARIN_FORMS
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
#if NETFX_CORE
    [Windows.UI.Xaml.Data.Bindable]
#endif
    internal class LegendDataSource : LayerContentDataSource<LegendEntry>
    {
        private readonly ConcurrentDictionary<ILayerContent, Task<IReadOnlyList<LegendInfo>>> _legendInfoTasks = new ConcurrentDictionary<ILayerContent, Task<IReadOnlyList<LegendInfo>>>();

        private bool _filterByVisibleScaleRange = true;
        private bool _filterHiddenLayers = true;
        private bool _reverseLayerOrder = true;
        private bool _filterEmptyLayers = false;

        public LegendDataSource(Legend owner)
            : base(owner)
        {
        }

        // Hides any layer contents that are out of scale range
        public bool FilterByVisibleScaleRange
        {
            get => _filterByVisibleScaleRange;
            set
            {
                if (_filterByVisibleScaleRange != value)
                {
                    _filterByVisibleScaleRange = value;
                    MarkCollectionDirty(false);
                }
            }
        }

        // Hides any layer contents where the IsVisible property is false.
        public bool FilterHiddenLayers
        {
            get => _filterHiddenLayers;
            set
            {
                if (_filterHiddenLayers != value)
                {
                    _filterHiddenLayers = value;
                    MarkCollectionDirty(false);
                }
            }
        }

        // Reverses the layer order to be top-to-bottom
        public bool ReverseLayerOrder
        {
            get => _reverseLayerOrder;
            set
            {
                if (_reverseLayerOrder != value)
                {
                    _reverseLayerOrder = value;
                    MarkCollectionDirty(false);
                }
            }
        }

        // Set to true to not show layer contents that have no legends to display
        public bool FilterEmptyLayers
        {
            get => _filterEmptyLayers;
            set
            {
                if (_filterEmptyLayers != value)
                {
                    _filterEmptyLayers = value;
                    MarkCollectionDirty(false);
                }
            }
        }

#if NETFX_CORE && !XAMARIN_FORMS
        private long _propertyChangedCallbackToken = 0;
#endif

        protected override void OnGeoViewChanged(GeoView oldGeoview, GeoView newGeoview)
        {
            base.OnGeoViewChanged(oldGeoview, newGeoview);
            _currentScale = (newGeoview as MapView)?.MapScale ?? double.NaN;
        }

#if !XAMARIN_FORMS
        private List<LegendEntry> GenerateDesignData()
        {
            var items = new List<LegendEntry>();
            if (GeoView != null)
            {
                items.Add(new LegendEntry(new ArcGISTiledLayer() { Name = "Layer 1" }));
                items.Add(new LegendEntry(new DesigntimeSublayer("Sublayer A")));
                items.Add(new LegendEntry(new DesignLegendInfo("Symbol 1", new Symbology.SimpleMarkerSymbol() { Color = System.Drawing.Color.Red })));
                items.Add(new LegendEntry(new DesignLegendInfo("Symbol 2", new Symbology.SimpleMarkerSymbol() { Color = System.Drawing.Color.Green })));
                items.Add(new LegendEntry(new DesignLegendInfo("Symbol 3", new Symbology.SimpleMarkerSymbol() { Color = System.Drawing.Color.Blue })));
                items.Add(new LegendEntry(new DesigntimeSublayer("Sublayer B")));
                items.Add(new LegendEntry(new DesignLegendInfo("Small", new Symbology.SimpleMarkerSymbol() { Size = 5 })));
                items.Add(new LegendEntry(new DesignLegendInfo("Medium", new Symbology.SimpleMarkerSymbol() { Size = 10 })));
                items.Add(new LegendEntry(new DesignLegendInfo("Large", new Symbology.SimpleMarkerSymbol() { Size = 15 })));
                if (!FilterEmptyLayers)
                {
                    items.Add(new LegendEntry(new ArcGISTiledLayer() { Name = "Layer 2" }));
                    items.Add(new LegendEntry(new ArcGISTiledLayer() { Name = "Layer 3" }));
                }
            }

            return items;
        }

        private class DesigntimeSublayer : ILayerContent
        {
            internal DesigntimeSublayer(string name)
            {
                Name = name;
            }

            public bool CanChangeVisibility => true;

            public bool IsVisible { get; set; }

            public string Name { get; }

            public bool ShowInLegend { get; set; }

            public IReadOnlyList<ILayerContent> SublayerContents { get; }

            public Task<IReadOnlyList<LegendInfo>> GetLegendInfosAsync() => throw new NotImplementedException();

            public bool IsVisibleAtScale(double scale) => true;
        }
#endif

        protected override void OnDocumentReset()
        {
            _currentScale = double.NaN;
            if (GeoView is MapView mv)
            {
                _currentScale = mv.MapScale;
            }

            _legendInfoTasks.Clear();
            base.OnDocumentReset();
        }

        protected override void OnLayerPropertyChanged(ILayerContent layer, string propertyName)
        {
            if (!layer.ShowInLegend && propertyName != nameof(layer.ShowInLegend))
            {
                return;
            }
            else if ((propertyName == nameof(layer.IsVisible) && _filterHiddenLayers) || propertyName == nameof(layer.ShowInLegend))
            {
                MarkCollectionDirty(false);
            }
            else if (propertyName == nameof(FeatureLayer.Renderer))
            {
                MarkCollectionDirty(false);
            }

            base.OnLayerPropertyChanged(layer, propertyName);
        }

        protected override List<LegendEntry> OnRebuildCollection()
        {
#if !XAMARIN_FORMS
            if (DesignTime.IsDesignMode)
            {
                return GenerateDesignData();
            }
#endif
            IEnumerable<Layer> layers = null;

            if (GeoView is MapView mv)
            {
                layers = mv.Map?.OperationalLayers;
            }
            else if (GeoView is SceneView sv)
            {
                layers = sv.Scene?.OperationalLayers;
            }

            return BuildLegendList(layers, _reverseLayerOrder) ?? new List<LegendEntry>();
        }

        protected override void OnLayerViewStateChanged(Layer layer, LayerViewStateChangedEventArgs layerViewState) => MarkCollectionDirty();

        protected override void OnGeoViewPropertyChanged(GeoView geoView, string propertyName)
        {
            base.OnGeoViewPropertyChanged(geoView, propertyName);
            if (propertyName == nameof(MapView.MapScale) && geoView is MapView mv)
            {
                _currentScale = mv.MapScale;
                MarkCollectionDirty();
            }
        }

        private double _currentScale = double.NaN;

        private List<LegendEntry> BuildLegendList(IEnumerable<ILayerContent> layers, bool reverse)
        {
            if (layers == null)
            {
                return null;
            }

            if (layers is ICollection ic && ic.IsSynchronized)
            {
                lock (ic.SyncRoot)
                {
                    return BuildLegendListLocked(layers, reverse);
                }
            }

            return BuildLegendListLocked(layers, reverse);
        }

        private List<LegendEntry> BuildLegendListLocked(IEnumerable<ILayerContent> layers, bool reverse)
        {
            List<LegendEntry> data = new List<LegendEntry>();
            foreach (var layerContent in reverse ? layers.Reverse() : layers)
            {
                if (!layerContent.ShowInLegend || (!layerContent.IsVisible && _filterHiddenLayers))
                {
                    continue;
                }

                if (layerContent is Layer l)
                {
                    var state = GeoView.GetLayerViewState(l);
                    if (state != null &&
                        ((state.Status == LayerViewStatus.NotVisible && _filterHiddenLayers && !(l is GroupLayer)) ||
                        (state.Status == LayerViewStatus.OutOfScale && _filterByVisibleScaleRange)))
                    {
                        continue;
                    }
                }
                else if (layerContent is ILayerContent ilc)
                {
                    if (!ilc.IsVisible && _filterHiddenLayers && !(layerContent is WmsSublayer))
                    {
                        continue;
                    }
                    else if (_filterByVisibleScaleRange && !double.IsNaN(_currentScale) && _currentScale > 0 && !ilc.IsVisibleAtScale(_currentScale))
                    {
                        continue;
                    }
                }

                IReadOnlyList<LegendInfo> legendInfos = null;
                if (!(layerContent is Layer) || (((Layer)layerContent).LoadStatus == LoadStatus.Loaded && !(layerContent is GroupLayer)))
                {
                    // Generate the legend infos
                    // For layers, we'll wait with entering here, until the GeoView decides to load them before generating the legend
                    if (!_legendInfoTasks.ContainsKey(layerContent))
                    {
                        var task = LoadLegend(layerContent);
                        _legendInfoTasks[layerContent] = task;
                    }
                    else
                    {
                        var task = _legendInfoTasks[layerContent];
                        if (task.Status == TaskStatus.RanToCompletion)
                        {
                            legendInfos = _legendInfoTasks[layerContent].Result;
                        }
                    }
                }

                // Only add the entry if it has a name
                if (!string.IsNullOrEmpty(layerContent.Name))
                {
                    if (!_filterEmptyLayers || legendInfos?.Count > 0)
                    {
                        data.Add(new LegendEntry(layerContent));
                    }
                }

                if (legendInfos != null)
                {
                    data.AddRange(legendInfos.Select(s => new LegendEntry(s)));
                }

                if (layerContent.SublayerContents != null)
                {
                    var sublayers = layerContent.SublayerContents;

                    // This might seem counter-intuitive, but sublayers are already top-to-bottom, as opposed to the layer collection...
                    bool reverseSublayers = !_reverseLayerOrder;

                    if (layerContent is GroupLayer || layerContent is FeatureCollectionLayer)
                    {
                        // These layers have the sublayer content in the opposite order of other services
                        reverseSublayers = !_reverseLayerOrder;
                    }

                    data.AddRange(BuildLegendList(layerContent.SublayerContents, reverseSublayers));
                }
            }

            return data;
        }

        private async Task<IReadOnlyList<LegendInfo>> LoadLegend(ILayerContent layer)
        {
            var result = await layer.GetLegendInfosAsync().ConfigureAwait(false);
            if (result.Count > 0)
            {
                MarkCollectionDirty();
            }

            return result;
        }
    }
}
