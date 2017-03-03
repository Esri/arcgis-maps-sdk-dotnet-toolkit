using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.UI;
using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime;
using System.Collections.Specialized;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class LayerContentViewModel : INotifyPropertyChanged
    {
        private DelegateCommand _reloadCommand;
        private DelegateCommand _zoomToCommand;
        private WeakReference<GeoView> _view;
        private bool _generateLegend;

        public LayerContentViewModel(ILayerContent layerContent, WeakReference<GeoView> view, Symbology.Symbol symbol = null, bool generateLegend = true)
        {
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
                var l = (layerContent as Esri.ArcGISRuntime.ILoadable);
                ReloadCommand = _reloadCommand = new DelegateCommand(
                    (s) => { l.RetryLoadAsync(); UpdateLoadingStatus(); }, (s) => { return l.LoadStatus == Esri.ArcGISRuntime.LoadStatus.FailedToLoad; });
                UpdateLoadingStatus();
            }
            else
            {
                ReloadCommand = _reloadCommand = new DelegateCommand((s) => { }, (s) => { return false; });
            }

            if (LayerContent is Layer)
            {
                Layer layer = (LayerContent as Layer);
                GeoView gview;
                if (layer.LoadStatus != LoadStatus.NotLoaded && _view.TryGetTarget(out gview))
                    UpdateLayerViewState(gview.GetLayerViewState(layer));
                else
                    UpdateLayerViewState(null);
            }

            ZoomToCommand = _zoomToCommand = new DelegateCommand((s) =>
            {
                GeoView gview;
                bool hasView = _view.TryGetTarget(out gview);
                if (hasView)
                {
                    var vp = GetExtent();
                    if (vp != null)
                        gview.SetViewpointAsync(vp);
                }
            }, (s) =>
            {
                GeoView gview;
                return _view.TryGetTarget(out gview) && GetExtent() != null;
            });
        }
        public Symbology.Symbol Symbol { get; }

        private void OnLayerContentLoaded(object sender, EventArgs e)
        {
            _isSublayersInitialized = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sublayers)));
        }

        private async void BuildSublayerList()
        {
            if (LayerContent is ILoadable)
            {
                var loadable = (ILoadable)LayerContent;
                if (loadable.LoadStatus != LoadStatus.Loaded)
                {
                    loadable.Loaded += OnLayerContentLoaded;
                    return;
                }
            }
            if (LayerContent.SublayerContents != null && LayerContent.SublayerContents.Count > 0)
            {
                _sublayers = new List<LayerContentViewModel>(LayerContent.SublayerContents.Where(t => t.ShowInLegend).Select(t => new LayerContentViewModel(t, _view, null, _generateLegend))).ToArray();
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

        private double _currentScale = double.NaN;

        internal void UpdateScaleVisibility(double scale, bool isParentVisible)
        {
            IsInScaleRange = isParentVisible && LayerContent.IsVisibleAtScale(scale);
            _currentScale = scale;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInScaleRange)));
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
            Func<double, bool> _visibleAtScaleCalculation;
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
                if (ext != null) return new Viewpoint(ext);
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
                if(!_isSublayersInitialized) //Lazy initalization of sublayers
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
            if (state != null && state.Status == LayerViewStatus.Error && LayerContent is ILoadable)
            {
                var l = LayerContent as Esri.ArcGISRuntime.ILoadable;
                if (l.LoadError != null)
                {
                    _reloadCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private void UpdateIsActive(bool isActive)
        {
            if (IsActive != isActive)
            {
                IsActive = isActive;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
                _zoomToCommand?.RaiseCanExecuteChanged();
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
                    Error = null;
                _reloadCommand?.RaiseCanExecuteChanged();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasError)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Error)));
            }
        }

        private void UpdateIsInScaleRange(bool isInRange)
        {
            if (IsInScaleRange != isInRange)
            {
                IsInScaleRange = isInRange;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInScaleRange)));
            }
        }

        public bool IsInScaleRange { get; private set; } = true;

        public bool HasError { get; private set; }

        public string Error { get; private set; }

        public bool IsLoading { get; private set; }

        public bool IsActive { get; private set; } = true;

        public System.Windows.Input.ICommand ReloadCommand { get; }

        public System.Windows.Input.ICommand ZoomToCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        private class DelegateCommand : System.Windows.Input.ICommand
        {
            Action<object> _execute;
            Func<object, bool> _canExecute;
            public DelegateCommand(Action execute) : this((o) => { execute(); }, null)
            {
            }
            public DelegateCommand(Action<object> execute, Func<object, bool> canExecute)
            {
                _execute = execute;
                _canExecute = canExecute;
            }
            public event EventHandler CanExecuteChanged;

            internal void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }

            public bool CanExecute(object parameter)
            {
                return _canExecute == null || _canExecute(parameter);
            }

            public void Execute(object parameter)
            {
                _execute(parameter);
            }
        }
    }
}
