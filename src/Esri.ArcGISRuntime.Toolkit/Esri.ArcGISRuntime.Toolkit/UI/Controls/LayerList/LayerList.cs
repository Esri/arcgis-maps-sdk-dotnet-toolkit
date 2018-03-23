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
using System.ComponentModel;
using System.Linq;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

#if NETFX_CORE
using Windows.UI.Xaml.Controls;
#elif __IOS__
using Control = UIKit.UIView;
#elif __ANDROID__
using Control = Android.Views.ViewGroup;
#else
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The base class for <see cref="Legend"/>
    /// and TableOfContents control is used to display symbology and description for a set of <see cref="Layer"/>s
    /// in a <see cref="Map"/> or <see cref="Scene"/> contained in a <see cref="Esri.ArcGISRuntime.UI.Controls.GeoView"/>.
    /// </summary>
    public partial class LayerList : Control
    {
        private bool _isScaleSet = false;
        private bool _scaleChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerList"/> class.
        /// </summary>
        public LayerList()
#if __ANDROID__
            : base(Android.App.Application.Context)
#endif
        {
            Initialize();
        }

        internal void ScaleChanged()
        {
            _scaleChanged = true;

            // First time map is loaded and scale is established
            if (!_isScaleSet)
            {
                UpdateScaleVisiblity();
            }
        }

        /// <summary>
        /// Gets or sets the geoview that contain the layers whose symbology and description will be displayed.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        public GeoView GeoView
        {
            get => GeoViewImpl;
            set => GeoViewImpl = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the scale of <see cref="GeoView"/> and any scale ranges on the <see cref="Layer"/>s
        /// are used to determine when legend for layer is displayed.
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, legend for layer is displayed only when layer is in visible scale range;
        /// otherwise, <c>false</c>, legend for layer is displayed regardless of its scale range.
        /// </remarks>
        public bool FilterByVisibleScaleRange
        {
            get => FilterByVisibleScaleRangeImpl;
            set => FilterByVisibleScaleRangeImpl = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the order of layers in the <see cref="GeoView"/>, top to bottom, is used.
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, legend for layers is displayed from top to bottom order;
        /// otherwise, <c>false</c>, legend for layers is displayed from bottom to top order.
        /// </remarks>
        public bool ReverseLayerOrder
        {
            get => ReverseLayerOrderImpl;
            set => ReverseLayerOrderImpl = value;
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

            Refresh();
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
                if (scale.HasValue && (_layerContentList?.Any() ?? false))
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

        private void UpdateLegendVisiblity()
        {
            if (_layerContentList != null)
            {
                foreach (var item in _layerContentList)
                {
                    item.UpdateLegendVisiblity(FilterByVisibleScaleRange);
                }
            }
        }

        private void GeoView_LayerViewStateChanged(object sender, LayerViewStateChangedEventArgs e)
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

                Refresh();
            }
            else if (sender is SceneView && e.PropertyName == nameof(SceneView.Scene))
            {
                Refresh();
            }
            else if (e.PropertyName == nameof(MapView.MapScale))
            {
                ScaleChanged();
            }
        }

        private void Map_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Map.AllLayers))
            {
                Refresh();
            }
        }

        private ObservableLayerContentList _layerContentList;

        private void SetLayerContentList(ObservableLayerContentList layerContentList)
        {
            _layerContentList = layerContentList;
        }

        private bool _showLegendInternal;

        /// <summary>
        /// Gets or sets a value indicating whether legend is displayed.
        /// </summary>
        internal bool ShowLegendInternal
        {
            get
            {
                return _showLegendInternal;
            }

            set
            {
                if (_showLegendInternal != value)
                {
                    _showLegendInternal = value;
                    Refresh();
                }
            }
        }
    }
}