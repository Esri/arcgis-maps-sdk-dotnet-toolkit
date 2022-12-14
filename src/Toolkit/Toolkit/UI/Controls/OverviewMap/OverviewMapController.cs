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
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if MAUI
using Esri.ArcGISRuntime.Maui;
#endif
#if !MAUI
using Esri.ArcGISRuntime.UI.Controls;
#endif
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    internal class OverviewMapController
    {
        private double _scaleFactor;
        private Symbol? _areaSymbol;
        private Symbol? _pointSymbol;
        private GeoView? _connectedView;
        private readonly WeakEventListener<OverviewMapController, GeoView, object?, EventArgs>? _viewpointListener;
        private readonly WeakEventListener<OverviewMapController, GeoView, object?, EventArgs>? _navigationListener;
        private readonly GraphicsOverlay _extentOverlay;
        private readonly Graphic _extentGraphic;
        private readonly MapView _insetView;

        // Flag needed because when the GeoView is first attached, GetCurrentViewpoint returns null.
        // Subsequent ViewpointChanged event happens when IsNavigating is false, so special case is needed to
        // ensure viewpoint is shown before manual interaction.
        private bool _hasSetViewpoint;

        public OverviewMapController(MapView overview)
        {
            _insetView = overview;
            _extentOverlay = new GraphicsOverlay();
            if (_insetView.GraphicsOverlays == null)
            {
                _insetView.GraphicsOverlays = new GraphicsOverlayCollection();
            }

            _insetView.GraphicsOverlays.Add(_extentOverlay);

            _extentGraphic = new Graphic();
            _extentOverlay.Graphics.Add(_extentGraphic);

            // _overview.ViewpointChanged += OnViewpointChanged
            _viewpointListener = new WeakEventListener<OverviewMapController, GeoView, object?, EventArgs>(this, _insetView)
            {
                OnEventAction = static (instance, source, eventArgs) => instance.OnInsetViewpointChanged(source, eventArgs),
                OnDetachAction = static (instance, source, weakEventListener) => source.ViewpointChanged -= weakEventListener.OnEvent,
            };
            _insetView.ViewpointChanged += _viewpointListener.OnEvent;

            // _overview.NavigationCompleted += OnNavigationCompleted;
            _navigationListener = new WeakEventListener<OverviewMapController, GeoView, object?, EventArgs>(this, _insetView)
            {
                OnEventAction = static (instance, source, eventArgs) => instance.OnInsetNavigationCompleted(source, eventArgs),
                OnDetachAction = static (instance, source, weakEventListener) => source.NavigationCompleted -= weakEventListener.OnEvent,
            };
            _insetView.NavigationCompleted += _navigationListener.OnEvent;
        }

        public GeoView? AttachedView
        {
            get => _connectedView;
            set
            {
                if (value == _connectedView)
                {
                    return;
                }

                if (_connectedView != null)
                {
                    _connectedView.ViewpointChanged -= OnConnectedViewViewpointChanged;
                    _connectedView.NavigationCompleted -= OnConnectedViewNavigationCompleted;
                }

                _hasSetViewpoint = false;
                _connectedView = value;
                if (_connectedView != null)
                {
                    _connectedView.ViewpointChanged += OnConnectedViewViewpointChanged;
                    _connectedView.NavigationCompleted += OnConnectedViewNavigationCompleted;
                    UpdateSymbol();
                    ApplyViewpoint(_connectedView, _insetView);
                    UpdateGraphic();
                }
            }
        }

        public double ScaleFactor
        {
            get => _scaleFactor;
            set
            {
                if (_scaleFactor != value && value >= 1)
                {
                    _scaleFactor = value;
                    if (_connectedView != null)
                    {
                        ApplyViewpoint(_connectedView, _insetView);
                        UpdateGraphic();
                    }
                }
            }
        }

        public Symbol? AreaSymbol
        {
            get => _areaSymbol;
            set
            {
                if (_areaSymbol != value)
                {
                    _areaSymbol = value;
                    UpdateSymbol();
                    UpdateGraphic();
                }
            }
        }

        public Symbol? PointSymbol
        {
            get => _pointSymbol;
            set
            {
                if (_pointSymbol != value)
                {
                    _pointSymbol = value;
                    UpdateSymbol();
                    UpdateGraphic();
                }
            }
        }

        private void UpdateGraphic()
        {
            if (_connectedView == null || _insetView == null)
            {
                return;
            }

            if (_connectedView is MapView mv && mv.VisibleArea is Polygon area)
            {
                _extentGraphic.Geometry = area;
            }
            else if (_connectedView is SceneView sv && sv.GetCurrentViewpoint(ViewpointType.CenterAndScale)?.TargetGeometry is MapPoint centerPoint)
            {
                _extentGraphic.Geometry = centerPoint;
            }
        }

        private void OnInsetViewpointChanged(object? sender, EventArgs e)
        {
            UpdateGraphic();
            if (_connectedView != null && !_hasSetViewpoint)
            {
                ApplyViewpoint(_connectedView, _insetView);
                UpdateGraphic();
            }
        }

        private void OnConnectedViewViewpointChanged(object? sender, EventArgs e)
        {
            UpdateGraphic();
            if (_connectedView != null && !_hasSetViewpoint)
            {
                ApplyViewpoint(_connectedView, _insetView);
                UpdateGraphic();
            }
        }

        private void OnInsetNavigationCompleted(object? sender, EventArgs e)
        {
            if (_connectedView != null)
            {
                ApplyViewpoint(_insetView, _connectedView);
                UpdateGraphic();
            }
        }

        private void OnConnectedViewNavigationCompleted(object? sender, EventArgs e)
        {
            if (_connectedView != null)
            {
                ApplyViewpoint(_connectedView, _insetView);
                UpdateGraphic();
            }
        }

        internal void ApplyViewpoint(GeoView sendingView, GeoView receivingView)
        {
            if (sendingView.GetCurrentViewpoint(ViewpointType.CenterAndScale) is Viewpoint existingViewpoint)
            {
                _hasSetViewpoint = true;
                double scaleMultiplier = sendingView == _insetView ? 1.0 / ScaleFactor : ScaleFactor;
                Viewpoint newViewpoint = new Viewpoint((MapPoint)existingViewpoint.TargetGeometry, existingViewpoint.TargetScale * scaleMultiplier);
                try
                {
                    receivingView.SetViewpoint(newViewpoint);
                }
                catch (Exception)
                {
                    // Ignore
                }
            }
        }

        private void UpdateSymbol()
        {
            if (_connectedView is MapView)
            {
                _extentOverlay.Renderer = new SimpleRenderer(AreaSymbol);
            }
            else if (_connectedView is SceneView)
            {
                _extentOverlay.Renderer = new SimpleRenderer(PointSymbol);
            }
        }

        public void Dispose()
        {
            AttachedView = null;
            _navigationListener?.Detach();
            _viewpointListener?.Detach();
        }
    }
}
