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

#nullable enable
using Esri.ArcGISRuntime.Data;
#if WINUI 
using Point = Windows.Foundation.Point;
#elif WINDOWS_UWP
using Point = Windows.Foundation.Point;
#endif
#if WINUI
using MapViewDPType = Microsoft.UI.Xaml.DependencyObject;
#elif !MAUI
using MapViewDPType = Esri.ArcGISRuntime.UI.Controls.MapView;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI
#endif
{
    /// <summary>
    /// Helper class for controlling a <see cref="MapView"/> instance in an MVVM pattern, while allowing operations on the view from ViewModel.
    /// </summary>
    public class MapViewController : GeoViewController
    {
        /// <summary>
        /// Gets a reference to the MapView this controller is currently connected to.
        /// </summary>
        protected new MapView? ConnectedView => base.ConnectedView as MapView;

        /// <summary>
        /// Attaches a Mapview to the controller, or detaches if <c>null</c>.
        /// </summary>
        /// <param name="mapView">The <see cref="MapView"/> to attach, or <c>null</c> is detaching.</param>
        public void Attach(MapView mapView)
        {
            base.Attach(mapView);
        }

        /// <summary>
        /// Raised when the <see cref="MapViewController"/> has been attached to a <see cref="MapView"/>.
        /// </summary>
        /// <param name="mapView"></param>
        protected virtual void OnMapViewAttached(MapView mapView) { }

        /// <summary>
        /// Raised when the <see cref="MapViewController"/> has been detached from a <see cref="MapView"/>.
        /// </summary>
        /// <param name="mapView"></param>
        protected virtual void OnMapViewDetached(MapView mapView) { }

        /// <summary>
        /// Raised when the attached <see cref="MapView"/> loads into the active view.
        /// </summary>
        /// <param name="mapView">MapView that was loaded</param>
        protected virtual void OnMapViewLoaded(MapView mapView) { }

        /// <summary>
        /// Raised when the attached <see cref="MapView"/> unloads from the active view.
        /// </summary>
        /// <param name="mapView">MapView that was unloaded</param>
        protected virtual void OnMapViewUnloaded(MapView mapView) { }

        /// <inheritdoc />
        protected sealed override void OnGeoViewAttached(GeoView geoView)
        {
            if (geoView is MapView mapView)
                OnMapViewAttached(mapView);
            else
                base.OnGeoViewAttached(geoView);
        }

        /// <inheritdoc />
        protected sealed override void OnGeoViewDetached(GeoView geoView)
        {
            if (geoView is MapView mapView)
                OnMapViewDetached(mapView);
            else
                base.OnGeoViewDetached(geoView);
        }

        /// <inheritdoc />
        protected sealed override void OnGeoViewLoaded(GeoView geoView)
        {
            if (geoView is MapView mapView)
                OnMapViewLoaded(mapView);
            else
                base.OnGeoViewLoaded(geoView);
        }

        /// <inheritdoc />
        protected sealed override void OnGeoViewUnloaded(GeoView geoView)
        {
            if (geoView is MapView mapView)
                OnMapViewUnloaded(mapView);
            else
                base.OnGeoViewUnloaded(geoView);
        }

        #region Identify

        /// <inheritdoc cref="MapView.IdentifyGeometryEditorAsync(Point, double)" />
        public virtual async Task<IdentifyGeometryEditorResult?> IdentifyGeometryEditorAsync(Point screenPoint, double tolerance) =>
            ConnectedView is null
                ? null
                : await ConnectedView.IdentifyGeometryEditorAsync(screenPoint, tolerance).ConfigureAwait(false);

        #endregion

#if MAUI
        /// <summary>
        /// Identifies the <see cref="MapViewController"/> attached property for MAUI.
        /// </summary>
        public static readonly BindableProperty MapViewControllerProperty =
            BindableProperty.CreateAttached(
                "MapViewController",
                typeof(MapViewController),
                typeof(MapViewController),
                null,
                propertyChanged: OnMapViewControllerChanged
            );

        private static void OnMapViewControllerChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is MapView mapView)
            {
                if (oldValue is MapViewController controllerOld)
                {
                    controllerOld.DetachFromGeoView(mapView);
                }

                if (newValue is MapViewController controllerNew)
                {
                    controllerNew.AttachToGeoView(mapView);
                }
            }
            else
            {
                throw new InvalidOperationException("This property must be attached to a MapView.");
            }
        }

        /// <summary>
        /// Gets the value of the <see cref="MapViewController"/> attached property from the specified <see cref="MapView"/>.
        /// </summary>
        public static MapViewController? GetMapViewController(BindableObject mapView) =>
            mapView?.GetValue(MapViewControllerProperty) as MapViewController;

        /// <summary>
        /// Sets the value of the <see cref="MapViewController"/> attached property on the specified <see cref="MapView"/>.
        /// </summary>
        public static void SetMapViewController(BindableObject mapView, MapViewController? value) =>
            mapView?.SetValue(MapViewControllerProperty, value);

#else
        /// <summary>
        /// Identifies the <see cref="MapViewController"/> attached property for WPF/UWP/WinUI.
        /// </summary>
        public static readonly DependencyProperty MapViewControllerProperty =
            DependencyProperty.RegisterAttached(
                "MapViewController",
                typeof(MapViewController),
                typeof(MapViewController),
                new PropertyMetadata(null, OnMapViewControllerChanged)
            );

        /// <summary>
        /// Gets the value of the <see cref="MapViewController"/> XAML attached property from the specified <see cref="MapView"/>.
        /// </summary>
        public static MapViewController? GetMapViewController(MapViewDPType mapView) =>
            mapView?.GetValue(MapViewControllerProperty) as MapViewController;

        /// <summary>
        /// Sets the value of the <see cref="MapViewController"/> XAML attached property on the specified <see cref="MapView"/>.
        /// </summary>
        /// <param name="mapView">The target <see cref="MapView"/> on which to set the <see cref="MapViewController.MapViewController"/> XAML attached property</param>
        /// <param name="value"></param>
        public static void SetMapViewController(MapViewDPType mapView, MapViewController? value) =>
            mapView?.SetValue(MapViewControllerProperty, value);

        private static void OnMapViewControllerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MapView mapView)
            {
                if (e.OldValue is MapViewController controllerOld)
                {
                    controllerOld.DetachFromGeoView(mapView);
                }

                if (e.NewValue is MapViewController controllerNew)
                {
                    controllerNew.AttachToGeoView(mapView);
                }
            }
            else
            {
                throw new InvalidOperationException("This property must be attached to a MapView.");
            }
        }
#endif
    }
}
