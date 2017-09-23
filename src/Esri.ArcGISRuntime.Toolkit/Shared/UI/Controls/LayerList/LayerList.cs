// /*******************************************************************************
//  * Copyright 2012-2016 Esri
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
using System.ComponentModel;
using System.Linq;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The base class for <see cref="Legend"/>
    /// and TableOfContents control is used to display symbology and description for a set of <see cref="Layer"/>s
    /// in a <see cref="Map"/> or <see cref="Scene"/> contained in a <see cref="GeoView"/>.
    /// </summary>
    [TemplatePart(Name ="List", Type = typeof(ItemsControl))]
    public class LayerList : Control
    {
        private bool _isScaleSet = false;
        private bool _scaleChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerList"/> class.
        /// </summary>
        internal LayerList()
        {
        }

        /// <inheritdoc/>
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            RebuildList();
        }

        /// <summary>
        /// Gets or sets the geoview that contain the layers whose symbology and description will be displayed.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        public GeoView GeoView
        {
            get { return (GeoView)GetValue(GeoviewProperty); }
            set { SetValue(GeoviewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoviewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(LayerList), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contents = (LayerList)d;
            contents.OnViewChanged(e.OldValue as GeoView, e.NewValue as GeoView);
        }

        private void OnViewChanged(GeoView oldView, GeoView newView)
        {
            if (oldView != null)
            {
                (oldView as INotifyPropertyChanged).PropertyChanged -= GeoView_PropertyChanged;
                oldView.LayerViewStateChanged -= GeoView_LayerViewStateChanged;
            }

            if (newView != null)
            {
                (newView as INotifyPropertyChanged).PropertyChanged += GeoView_PropertyChanged;
                newView.LayerViewStateChanged += GeoView_LayerViewStateChanged;
                newView.ViewpointChanged += GeoView_ViewpointChanged;
                _scaleChanged = true;
                _isScaleSet = false;
            }

            RebuildList();
        }

        private void GeoView_ViewpointChanged(object sender, EventArgs e)
        {
            UpdateScaleVisiblity();
        }

        private void UpdateScaleVisiblity()
        {
            if (_scaleChanged)
            {
                var scale = GeoView.GetCurrentViewpoint(ViewpointType.CenterAndScale)?.TargetScale ??
                    (GeoView as MapView)?.MapScale;
                if (scale.HasValue && _layerContentList != null)
                {
                    _isScaleSet = true;
                    foreach (var item in _layerContentList)
                    {
                        item.UpdateScaleVisibility(scale.Value, true);
                    }

                    _scaleChanged = false;
                }
            }
        }

        private void GeoView_LayerViewStateChanged(object sender, Esri.ArcGISRuntime.Mapping.LayerViewStateChangedEventArgs e)
        {
            if (_layerContentList != null)
            {
                var l = _layerContentList.FirstOrDefault(t => t.LayerContent == e.Layer);
                if (l != null)
                {
                    l.UpdateLayerViewState(e.LayerViewState);
                }
            }
        }

        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is MapView && e.PropertyName == nameof(MapView.Map))
            {
                MapView mv = (MapView)sender;
                if (mv.Map != null)
                {
                    var incc = mv.Map as INotifyPropertyChanged;
                    var listener = new Internal.WeakEventListener<INotifyPropertyChanged, object, PropertyChangedEventArgs>(incc);
                    listener.OnEventAction = (instance, source, eventArgs) => { Map_PropertyChanged(source, eventArgs); };
                    listener.OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent;
                    incc.PropertyChanged += listener.OnEvent;
                }

                RebuildList();
            }
            else if (sender is SceneView && e.PropertyName == nameof(SceneView.Scene))
            {
                RebuildList();
            }
            else if (e.PropertyName == nameof(MapView.MapScale))
            {
                _scaleChanged = true;

                // First time map is loaded and scale is established
                if (!_isScaleSet)
                {
                    UpdateScaleVisiblity();
                }
            }
        }

        private void Map_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Map.AllLayers))
            {
                RebuildList();
            }
        }

        private static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register("Document", typeof(object), typeof(LayerList), new PropertyMetadata(null, OnDocumentPropertyChanged));

        private static void OnDocumentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LayerList)d).RebuildList();
        }

        private ObservableLayerContentList _layerContentList;

        /// <summary>
        /// Gets a value indicating whether legend is displayed.
        /// </summary>
        protected virtual bool ShowLegendInternal => true;

        /// <summary>
        /// Gets a value indicating whether to use scale to determine when to display legend.
        /// </summary>
        protected virtual bool RespectScaleRangeInternal => true;

        /// <summary>
        /// Generates layer list for a set of <see cref="Layer"/>s in a <see cref="Map"/> or <see cref="Scene"/>
        /// contained in a <see cref="GeoView"/>
        /// </summary>
        protected void RebuildList()
        {
            var list = GetTemplateChild("List") as ItemsControl;
            if (list != null)
            {
                if (GeoView != null)
                {
                    ObservableLayerContentList layers = null;
                    if (GeoView is MapView)
                    {
                        var map = (GeoView as MapView).Map;
                        if (map != null)
                        {
                            layers = new ObservableLayerContentList(GeoView as MapView, ShowLegendInternal, RespectScaleRangeInternal) { ReverseOrder = !ReverseLayerOrder };
                        }

                        Binding b = new Binding();
                        b.Source = GeoView;
                        b.Path = new PropertyPath(nameof(MapView.Map));
                        b.Mode = BindingMode.OneWay;
                        SetBinding(DocumentProperty, b);
                    }
                    else if (GeoView is SceneView)
                    {
                        var scene = (GeoView as SceneView).Scene;
                        if (scene != null)
                        {
                            layers = new ObservableLayerContentList(GeoView as SceneView, ShowLegendInternal, RespectScaleRangeInternal) { ReverseOrder = !ReverseLayerOrder };
                        }

                        Binding b = new Binding();
                        b.Source = GeoView;
                        b.Path = new PropertyPath(nameof(SceneView.Scene));
                        b.Mode = BindingMode.OneWay;
                        SetBinding(DocumentProperty, b);
                    }

                    if (layers != null && GeoView != null)
                    {
                        foreach (var l in layers)
                        {
                            var layer = l.LayerContent as Layer;
                            if (layer.LoadStatus == LoadStatus.Loaded)
                            {
                                l.UpdateLayerViewState(GeoView.GetLayerViewState(layer));
                            }
                        }

                        _scaleChanged = true;
                        UpdateScaleVisiblity();
                    }

                    list.ItemsSource = _layerContentList = layers;
                }
                else
                {
                    list.ItemsSource = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the item template for each layer content entry
        /// </summary>
        public DataTemplate LayerItemTemplate
        {
            get { return (DataTemplate)GetValue(LayerItemTemplateProperty); }
            set { SetValue(LayerItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LayerItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LayerItemTemplateProperty =
            DependencyProperty.Register(nameof(LayerItemTemplate), typeof(DataTemplate), typeof(LayerList), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets a value indicating whether the order of layers in the <see cref="GeoView"/>, top to bottom, is used.
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, legend for layers is displayed from top to bottom order;
        /// otherwise, <c>false</c>, legend for layers is displayed from bottom to top order.
        /// </remarks>
        public bool ReverseLayerOrder
        {
            get { return (bool)GetValue(ReverseLayerOrderProperty); }
            set { SetValue(ReverseLayerOrderProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ReverseLayerOrder"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ReverseLayerOrderProperty =
            DependencyProperty.Register(nameof(ReverseLayerOrder), typeof(bool), typeof(LayerList), new PropertyMetadata(false, OnReverseLayerOrderPropertyChanged));

        private static void OnReverseLayerOrderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LayerList)d)._layerContentList.ReverseOrder = !(bool)e.NewValue;
        }
    }
}
#endif