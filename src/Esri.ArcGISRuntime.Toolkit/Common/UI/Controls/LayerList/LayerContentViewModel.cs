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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class LayerContentViewModel : INotifyPropertyChanged
    {
        private WeakReference<GeoView> _view;
        private bool _generateLegend;
        private SynchronizationContext _context;

        public LayerContentViewModel(ILayerContent layerContent, WeakReference<GeoView> view, Symbology.Symbol symbol = null, bool generateLegend = true)
        {
            _context = SynchronizationContext.Current ?? new SynchronizationContext();
            LayerContent = layerContent;
            _view = view;
            Symbol = symbol;
            _generateLegend = generateLegend;
            if (LayerContent is INotifyPropertyChanged)
            {
                (LayerContent as INotifyPropertyChanged).PropertyChanged += LayerContentViewModel_PropertyChanged;
            }

            if (LayerContent.SublayerContents is INotifyCollectionChanged)
            {
                var incc = LayerContent.SublayerContents as INotifyCollectionChanged;
                var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc);
                listener.OnEventAction = (instance, source, eventArgs) => { LayerContentViewModel_CollectionChanged(source, eventArgs); };
                listener.OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent;
                incc.CollectionChanged += listener.OnEvent;
            }

            if (layerContent is Esri.ArcGISRuntime.ILoadable)
            {
                var l = layerContent as Esri.ArcGISRuntime.ILoadable;
                UpdateLoadingStatus();
            }

            if (LayerContent is Layer)
            {
                Layer layer = LayerContent as Layer;
                GeoView gview;
                if (layer.LoadStatus != LoadStatus.NotLoaded && _view.TryGetTarget(out gview))
                {
                    try
                    {
                        var viewState = gview.GetLayerViewState(layer);
                        UpdateLayerViewState(viewState);
                    }
                    catch (ArgumentException)
                    {
                    }
                }
                else
                {
                    UpdateLayerViewState(null);
                }
            }
        }

        public Symbology.Symbol Symbol { get; }

        private void OnLayerContentLoaded(object sender, EventArgs e)
        {
            _isSublayersInitialized = false;
            _context.Post((o) =>
               {
                   PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sublayers)));
               }, null);
        }

        private async void BuildSublayerList()
        {
            if (LayerContent is ILoadable)
            {
                var loadable = (ILoadable)LayerContent;
                if (loadable.LoadStatus != LoadStatus.Loaded)
                {
                    loadable.Loaded += OnLayerContentLoaded;
                }
            }

            if (LayerContent.SublayerContents != null && LayerContent.SublayerContents.Count > 0)
            {
                _sublayers = new List<LayerContentViewModel>(LayerContent.SublayerContents.ToArray().Where(t => t.ShowInLegend).Select(t => new LayerContentViewModel(t, _view, null, _generateLegend))).ToArray();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sublayers)));
            }
            else
            {
                _sublayers = null;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sublayers)));
                if (_generateLegend)
                {
                    IReadOnlyList<LegendInfo> legend;
                    try
                    {
                        legend = await LayerContent.GetLegendInfosAsync();
                    }
                    catch
                    {
                        return;
                    }

                    if (legend != null && legend.Count > 0)
                    {
                        _sublayers = new List<LayerContentViewModel>(legend.Select(l => new LayerContentViewModel(new LegendContentInfo(l, LayerContent.IsVisibleAtScale), _view, l.Symbol, _generateLegend)));
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sublayers)));
                    }
                }
            }

            if (_sublayers != null && !double.IsNaN(_currentScale))
            {
                foreach (var item in _sublayers)
                {
                    item.UpdateScaleVisibility(_currentScale, IsInScaleRange);
                }
            }
        }

        private bool _filterByVisibleScaleRange = true;

        internal void UpdateLegendVisiblity(bool filterByVisibleScaleRange)
        {
            if (_filterByVisibleScaleRange == filterByVisibleScaleRange)
            {
                return;
            }

            _filterByVisibleScaleRange = filterByVisibleScaleRange;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayLegend)));
            if (Sublayers != null)
            {
                foreach (var item in Sublayers)
                {
                    item.UpdateLegendVisiblity(filterByVisibleScaleRange);
                }
            }
        }

        private double _currentScale = double.NaN;

        internal void UpdateScaleVisibility(double scale, bool isParentVisible)
        {
            if (double.IsNaN(scale))
            {
                return;
            }

            IsInScaleRange = isParentVisible && LayerContent.IsVisibleAtScale(scale);
            _currentScale = scale;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInScaleRange)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayLegend)));
            if (Sublayers != null)
            {
                foreach (var item in Sublayers)
                {
                    item.UpdateScaleVisibility(scale, IsInScaleRange);
                }
            }
        }

        private class LegendContentInfo : ILayerContent
        {
            private Func<double, bool> _visibleAtScaleCalculation;

            public LegendContentInfo(LegendInfo legend, Func<double, bool> visibleAtScaleCalculation)
            {
                Name = legend.Name;
                _visibleAtScaleCalculation = visibleAtScaleCalculation;
            }

            public bool CanChangeVisibility { get; }

            public bool IsVisible { get; set; } = true;

            public string Name { get; }

            public bool ShowInLegend { get; set; }

            public IReadOnlyList<ILayerContent> SublayerContents { get; }

            public Task<IReadOnlyList<LegendInfo>> GetLegendInfosAsync()
            {
                return Task.FromResult<IReadOnlyList<LegendInfo>>(null);
            }

            public bool IsVisibleAtScale(double scale)
            {
                return _visibleAtScaleCalculation(scale);
            }
        }

        private void LayerContentViewModel_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _isSublayersInitialized = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sublayers)));
        }

        private Viewpoint GetExtent()
        {
            if (LayerContent is Layer)
            {
                var ext = ((Layer)LayerContent).FullExtent;
                if (ext != null && !ext.IsEmpty)
                {
                    return new Viewpoint(ext);
                }
            }

            return null;
        }

        private void LayerContentViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ILayerContent.SublayerContents))
            {
                _isSublayersInitialized = false;
                if (LayerContent?.SublayerContents is INotifyCollectionChanged)
                {
                    var incc = LayerContent.SublayerContents as INotifyCollectionChanged;
                    var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc);
                    listener.OnEventAction = (instance, source, eventArgs) => { LayerContentViewModel_CollectionChanged(source, eventArgs); };
                    listener.OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent;
                    incc.CollectionChanged += listener.OnEvent;
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sublayers)));
            }
            else if (e.PropertyName == nameof(ILoadable.LoadStatus))
            {
                UpdateLoadingStatus();
            }
        }

        public ILayerContent LayerContent { get; }

        private bool _isSublayersInitialized = false;
        private IEnumerable<LayerContentViewModel> _sublayers;

        public IEnumerable<LayerContentViewModel> Sublayers
        {
            get
            {
                // Lazy initalization of sublayers
                if (!_isSublayersInitialized)
                {
                    _isSublayersInitialized = true;
                    BuildSublayerList();
                }

                return _sublayers;
            }
        }

        internal void UpdateLayerViewState(LayerViewState state)
        {
            UpdateLoadingStatus();
            UpdateIsInScaleRange(state != null && state.Status != LayerViewStatus.OutOfScale);
            UpdateIsActive(state != null && state.Status == LayerViewStatus.Active);
        }

        private void UpdateIsActive(bool isActive)
        {
            if (IsActive != isActive)
            {
                IsActive = isActive;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
            }
        }

        private void UpdateLoadingStatus()
        {
            bool isLoading = false;
            if (LayerContent is ILoadable)
            {
                isLoading = (LayerContent as ILoadable).LoadStatus == LoadStatus.Loading;
                UpdateError((LayerContent as ILoadable).LoadError?.Message);
                UpdateIsActive(false);
            }

            if (IsLoading != isLoading)
            {
                IsLoading = isLoading;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
            }
        }

        private void UpdateError(string error)
        {
            var hasError = !string.IsNullOrEmpty(error);
            if (HasError != hasError)
            {
                HasError = hasError;
                if (hasError)
                {
                    Error = error;
                    UpdateIsActive(false);
                }
                else
                {
                    Error = null;
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasError)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Error)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayLegend)));
            }
        }

        private void UpdateIsInScaleRange(bool isInRange)
        {
            if (IsInScaleRange != isInRange)
            {
                IsInScaleRange = isInRange;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInScaleRange)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayLegend)));
            }
        }

        public bool IsInScaleRange { get; private set; } = true;

        public bool HasError { get; private set; }

        public bool DisplayLegend => !HasError && (!_filterByVisibleScaleRange || IsInScaleRange);

        public bool IsSublayer => LayerContent is ArcGISSublayer;

        public string Error { get; private set; }

        public bool IsLoading { get; private set; }

        public bool IsActive { get; private set; } = true;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}