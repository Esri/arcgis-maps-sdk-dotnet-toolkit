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

#if !_XAMARIN_ANDROID_ && !_XAMARIN_IOS_
using System;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
#if XAMARIN_FORMS
using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Internal;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Point = Xamarin.Forms.Point;
#elif NETFX_CORE
using Point = Windows.Foundation.Point;
#else
using Point = System.Windows.Point;
#endif
#if !XAMARIN_FORMS
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls.OverviewMap
{
    internal class OverviewMapController
    {
        private double _scaleFactor;
        private Symbol? _symbol;
        private GeoView? _connectedView;
        private readonly GraphicsOverlay _extentOverlay;
        private readonly GeoView _overview;

        // Flag needed because when the GeoView is first attached, GetCurrentViewpoint returns null.
        // Subsequent ViewpointChanged event happens when IsNavigating is false, so special case is needed to
        // ensure viewpoint is shown before manual interaction.
        private bool _hasSetViewpoint;

        public OverviewMapController(GeoView overview)
        {
            _overview = overview;
            _extentOverlay = new GraphicsOverlay();
            if (_overview.GraphicsOverlays == null)
            {
                _overview.GraphicsOverlays = new GraphicsOverlayCollection();
            }

            _overview.GraphicsOverlays.Add(_extentOverlay);

            // _overview.ViewpointChanged += OnViewpointChanged;
            var listener = new WeakEventListener<GeoView, object?, EventArgs>(_overview);
            listener.OnEventAction = (instance, source, eventArgs) => OnViewpointChanged(source, eventArgs);
            listener.OnDetachAction = (instance, weakEventListener) => instance.ViewpointChanged -= weakEventListener.OnEvent;
            _overview.ViewpointChanged += listener.OnEvent;

            // _overview.NavigationCompleted += OnNavigationCompleted;
            var listener2 = new WeakEventListener<GeoView, object?, EventArgs>(_overview);
            listener2.OnEventAction = (instance, source, eventArgs) => OnNavigationCompleted(source, eventArgs);
            listener2.OnDetachAction = (instance, weakEventListener) => instance.NavigationCompleted -= weakEventListener.OnEvent;
            _overview.NavigationCompleted += listener2.OnEvent;
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
                    _connectedView.ViewpointChanged -= OnViewpointChanged;
                    _connectedView.NavigationCompleted -= OnNavigationCompleted;
                }

                _connectedView = value;
                if (_connectedView != null)
                {
                    _connectedView.ViewpointChanged += OnViewpointChanged;
                    _connectedView.NavigationCompleted += OnNavigationCompleted;
                    UpdateSymbol();
                    ApplyViewpoint(_connectedView, _overview);
                }
            }
        }

        public double ScaleFactor
        {
            get => _scaleFactor;
            set
            {
                if (_scaleFactor != value)
                {
                    _scaleFactor = value;
                    if (_connectedView != null)
                    {
                        ApplyViewpoint(_connectedView, _overview);
                    }
                }
            }
        }

        public Symbol? Symbol
        {
            get => _symbol;
            set
            {
                if (_symbol != value)
                {
                    _symbol = value;
                    UpdateSymbol();
                    UpdateGraphic();
                }
            }
        }

        private void UpdateGraphic()
        {
            if (_connectedView == null || _overview == null)
            {
                return;
            }

            _extentOverlay.Graphics.Clear();

            if (_connectedView is MapView mv && mv.VisibleArea is Polygon area)
            {
                _extentOverlay.Graphics.Add(new Graphic(area));
            }
            else if (_connectedView is SceneView sv && sv.GetCurrentViewpoint(ViewpointType.CenterAndScale)?.TargetGeometry is MapPoint centerPoint)
            {
                _extentOverlay.Graphics.Add(new Graphic(centerPoint));
            }
        }

        private void OnViewpointChanged(object? sender, EventArgs e)
        {
            GeoView? sendingView = sender as GeoView;
            GeoView? receivingView = sendingView == _overview ? _connectedView : _overview;

            // Check for IsNavigating is intended to ignore OnViewpointChanged calls triggered by the call to SetViewpoint.
            if ((sendingView?.IsNavigating ?? false) && receivingView != null)
            {
                ApplyViewpoint(sendingView, receivingView);
                return;
            }

            if (_connectedView != null && !_hasSetViewpoint)
            {
                ApplyViewpoint(_connectedView, _overview);
            }
        }

        private void OnNavigationCompleted(object? sender, EventArgs e)
        {
            GeoView? sendingView = sender as GeoView;
            GeoView? receivingView = sendingView == _overview ? _connectedView : _overview;
            if (receivingView != null && sendingView != null)
            {
                ApplyViewpoint(sendingView, receivingView);
            }
        }

        private void ApplyViewpoint(GeoView sendingView, GeoView receivingView)
        {
            if (sendingView.GetCurrentViewpoint(ViewpointType.CenterAndScale) is Viewpoint existingViewpoint)
            {
                _hasSetViewpoint = true;
                var scaleMultiplier = sendingView == _overview ? 1.0 / ScaleFactor : ScaleFactor;
                Viewpoint newViewpoint = new Viewpoint((MapPoint)existingViewpoint.TargetGeometry, existingViewpoint.TargetScale * scaleMultiplier);
                receivingView.SetViewpoint(newViewpoint);
                UpdateGraphic();
            }
        }

        private void UpdateSymbol()
        {
            if (Symbol != null)
            {
                _extentOverlay.Renderer = new SimpleRenderer(Symbol);
            }
            else if (_connectedView is MapView)
            {
                _extentOverlay.Renderer = new SimpleRenderer(
                    new SimpleFillSymbol(SimpleFillSymbolStyle.Null, System.Drawing.Color.Transparent,
                    new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Red, 1)));
            }
            else if (_connectedView is SceneView)
            {
                _extentOverlay.Renderer = new SimpleRenderer(
                    new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Red, 16));
            }
        }
    }
}
#endif
