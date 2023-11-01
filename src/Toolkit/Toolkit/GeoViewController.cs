#nullable enable
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using System.Diagnostics;
#if WINUI 
using Point = Windows.Foundation.Point;
using LoadedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
#elif WINDOWS_UWP
using Point = Windows.Foundation.Point;
using LoadedEventArgs = Windows.UI.Xaml.RoutedEventArgs;
#else
using LoadedEventArgs = System.EventArgs;
#endif
#if WINUI
using GeoViewDPType = Microsoft.UI.Xaml.DependencyObject;
#elif !MAUI
using GeoViewDPType = Esri.ArcGISRuntime.UI.Controls.GeoView;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI
#endif
{
    /// <summary>
    /// Helper class for separating GeoView and ViewModel in an MVVM pattern, while allowing operations on the view from the ViewModel.
    /// </summary>
    public class GeoViewController
    {
        private WeakReference<GeoView>? _geoViewWeak;
        private WeakEventListener<GeoViewController, GeoView, object?, LoadedEventArgs>? _loadedListener;
        private WeakEventListener<GeoViewController, GeoView, object?, LoadedEventArgs>? _unloadedListener;

        /// <summary>
        /// Gets a reference to the GeoView this controller is currently connected to.
        /// </summary>
        protected GeoView? ConnectedView => _geoViewWeak?.TryGetTarget(out var view) == true ? view : null;

        /// <summary>
        /// Attaches a geoview to the controller, or detaches if <c>null</c>.
        /// </summary>
        /// <param name="geoView">The <see cref="GeoView"/> to attach, or <c>null</c> is detaching.</param>
        public void Attach(GeoView? geoView)
        {
            if (geoView is null)
            {
                if (ConnectedView is not null)
                    DetachFromGeoView(ConnectedView);
            }
            else
                AttachToGeoView(geoView);
        }

        private void AttachToGeoView(GeoView geoView)
        {
            var connectedView = ConnectedView;
            if (connectedView == geoView)
            {
                return;
            }
            if (connectedView != null)
            {
                Trace.WriteLine("Warning: GeoViewController already connected to a GeoView - Moving GeoViewController to a new GeoView", "ArcGIS Maps SDK Toolkit");
                DetachFromGeoView(connectedView);
                connectedView = null;
            }
            // All event listeners must be weak to avoid holding on to the GeoView if the GeoViewController stays alive
            _geoViewWeak = new WeakReference<GeoView>(geoView);
            _loadedListener = new WeakEventListener<GeoViewController, GeoView, object?, LoadedEventArgs>(this, geoView)
            {
                OnEventAction = static (instance, source, eventArgs) => instance.GeoView_Loaded((GeoView)source!),
                OnDetachAction = static (instance, source, weakEventListener) => source.Loaded -= weakEventListener.OnEvent,
            };
            geoView.Loaded += _loadedListener.OnEvent;

            _unloadedListener = new WeakEventListener<GeoViewController, GeoView, object?, LoadedEventArgs>(this, geoView)
            {
                OnEventAction = static (instance, source, eventArgs) => instance.GeoView_Unloaded((GeoView)source!),
                OnDetachAction = static (instance, source, weakEventListener) => source.Unloaded -= weakEventListener.OnEvent,
            };
            geoView.Loaded += _loadedListener.OnEvent;
            geoView.Unloaded += _unloadedListener.OnEvent;
            OnGeoViewAttached(geoView);
            if (geoView.IsLoaded)
                GeoView_Loaded(geoView);
        }

        private void DetachFromGeoView(GeoView geoView)
        {
            var connectedView = ConnectedView;
            if (connectedView != null && connectedView == geoView)
            {
                _loadedListener?.Detach();
                _unloadedListener?.Detach();
                if (connectedView.IsLoaded)
                    GeoView_Unloaded(geoView);
                OnGeoViewDetached(connectedView);
                _geoViewWeak = null;
            }
        }

        /// <summary>
        /// Raised when the <see cref="GeoViewController"/> has been attached to a <see cref="GeoView"/>.
        /// </summary>
        /// <param name="geoView"></param>
        protected virtual void OnGeoViewAttached(GeoView geoView)
        {
        }

        /// <summary>
        /// Raised when the <see cref="GeoViewController"/> has been detached from a <see cref="GeoView"/>.
        /// </summary>
        /// <param name="geoView"></param>
        protected virtual void OnGeoViewDetached(GeoView geoView)
        {
        }

        /// <summary>
        /// Raised when the attached <see cref="GeoView"/> loads into the active view.
        /// </summary>
        /// <param name="geoView">GeoView that was loaded</param>
        protected virtual void OnGeoViewLoaded(GeoView geoView)
        {
        }

        /// <summary>
        /// Raised when the attached <see cref="GeoView"/> unloads from the active view.
        /// </summary>
        /// <param name="geoView">GeoView that was unloaded</param>
        protected virtual void OnGeoViewUnloaded(GeoView geoView)
        {
        }

        private void GeoView_Loaded(GeoView geoView)
        {
            Debug.Assert(geoView == ConnectedView && ConnectedView is not null);
            OnGeoViewLoaded(geoView);
        }

        private void GeoView_Unloaded(GeoView sender)
        {
            Debug.Assert(sender == ConnectedView && ConnectedView is not null);
            OnGeoViewUnloaded(sender);
        }

        #region Viewpoints

        /// <inheritdoc cref="GeoView.GetCurrentViewpoint(ViewpointType)"/>
        public virtual Viewpoint? GetCurrentViewpoint(ViewpointType viewpointType) => ConnectedView?.GetCurrentViewpoint(viewpointType);

        /// <inheritdoc cref="GeoView.SetViewpointAsync(Viewpoint)"/>
        public virtual Task<bool> SetViewpointAsync(Viewpoint viewpoint) => ConnectedView?.SetViewpointAsync(viewpoint) ?? Task.FromResult(false);

        /// <inheritdoc cref="GeoView.SetViewpointAsync(Viewpoint, TimeSpan)"/>
        public virtual Task<bool> SetViewpointAsync(Viewpoint viewpoint, TimeSpan duration) => ConnectedView?.SetViewpointAsync(viewpoint, duration) ?? Task.FromResult(false);

        /// <inheritdoc cref="GeoView.SetViewpoint(Viewpoint)"/>
        public virtual void SetViewpoint(Viewpoint viewpoint) => ConnectedView?.SetViewpoint(viewpoint);

        #endregion Viewpoints

        #region Identify
        
        /// <inheritdoc cref="GeoView.IdentifyLayersAsync(Point, double, bool, CancellationToken)"/>
        public virtual Task<IReadOnlyList<IdentifyLayerResult>> IdentifyLayersAsync(Point screenPoint, double tolerance, bool returnPopupsOnly = false, CancellationToken cancellationToken = default) =>
            ConnectedView?.IdentifyLayersAsync(screenPoint, tolerance, returnPopupsOnly, cancellationToken) ??
                Task.FromResult<IReadOnlyList<IdentifyLayerResult>>(Array.Empty<IdentifyLayerResult>());
        
        /// <inheritdoc cref="GeoView.IdentifyLayerAsync(Layer, Point, double, bool, CancellationToken)"/>
        public virtual Task<IdentifyLayerResult> IdentifyLayerAsync(Layer layer, Point screenPoint, double tolerance, bool returnPopupsOnly = false, CancellationToken cancellationToken = default) =>
            ConnectedView?.IdentifyLayerAsync(layer, screenPoint, tolerance, returnPopupsOnly, cancellationToken) ??
                Task.FromResult<IdentifyLayerResult>(null!);

        /// <inheritdoc cref="GeoView.IdentifyGraphicsOverlaysAsync(Point, double, bool, long)" />
        public virtual Task<IReadOnlyList<IdentifyGraphicsOverlayResult>> IdentifyGraphicsOverlaysAsync(Point screenPoint, double tolerance, bool returnPopupsOnly = false, long maximumResultsPerOverlay = 1) =>
            ConnectedView?.IdentifyGraphicsOverlaysAsync(screenPoint, tolerance, returnPopupsOnly, maximumResultsPerOverlay) ??
                Task.FromResult<IReadOnlyList<IdentifyGraphicsOverlayResult>>(Array.Empty<IdentifyGraphicsOverlayResult>());

        /// <inheritdoc cref="GeoView.IdentifyGraphicsOverlaysAsync(Point, double, bool, long)" />
        public virtual Task<IdentifyGraphicsOverlayResult> IdentifyGraphicsOverlayAsync(GraphicsOverlay overlay, Point screenPoint, double tolerance, bool returnPopupsOnly = false, long maximumResults = 1) =>
            ConnectedView?.IdentifyGraphicsOverlayAsync(overlay, screenPoint, tolerance, returnPopupsOnly, maximumResults) ??
                Task.FromResult<IdentifyGraphicsOverlayResult>(null!);
        #endregion Identify

        #region Callouts

        /// <inheritdoc cref="GeoView.DismissCallout()" />
        public virtual void DismissCallout() => ConnectedView?.DismissCallout();

        /// <inheritdoc cref="GeoView.ShowCalloutAt(Geometry.MapPoint, CalloutDefinition)" />
        public virtual void ShowCalloutAt(Esri.ArcGISRuntime.Geometry.MapPoint location, CalloutDefinition definition) => ConnectedView?.ShowCalloutAt(location, definition);

        /// <inheritdoc cref="GeoView.ShowCalloutForGeoElement(GeoElement, Point, CalloutDefinition)" />
        public virtual void ShowCalloutForGeoElement(GeoElement element, Point tapPosition, CalloutDefinition definition) => ConnectedView?.ShowCalloutForGeoElement(element, tapPosition, definition);
        #endregion Callouts

#if MAUI
        /// <summary>
        /// Identifies the <see cref="GeoViewController"/> attached property.
        /// </summary>
        public static readonly BindableProperty GeoViewControllerProperty =
            BindableProperty.CreateAttached(
                "GeoViewController",
                typeof(GeoViewController),
                typeof(GeoViewController),
                null,
                propertyChanged: OnGeoViewControllerChanged
            );

        private static void OnGeoViewControllerChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is GeoView geoView)
            {
                if (oldValue is GeoViewController controllerOld)
                {
                    controllerOld.DetachFromGeoView(geoView);
                }

                if (newValue is GeoViewController controllerNew)
                {
                    controllerNew.AttachToGeoView(geoView);
                }
            }
            else
            {
                throw new InvalidOperationException("This property must be attached to a GeoView.");
            }
        }
#else
        /// <summary>
        /// Identifies the <see cref="GeoViewController"/> attached property.
        /// </summary>
        public static readonly DependencyProperty GeoViewControllerProperty =
            DependencyProperty.RegisterAttached(
                "GeoViewController",
                typeof(GeoViewController),
                typeof(GeoViewController),
                new PropertyMetadata(null, OnGeoViewControllerChanged)
            );

        /// <summary>
        /// Gets the value of the <see cref="GeoViewController.GeoViewController"/> XAML attached property from the specified <see cref="GeoView"/>.
        /// </summary>
        /// <param name="geoView">The <see cref="GeoView"/> from which to read the property value.</param>
        /// <returns>The value of the <see cref="GeoViewController.GeoViewController"/> XAML attached property on the target element.</returns>
        public static GeoViewController? GetGeoViewController(GeoViewDPType geoView) => geoView.GetValue(GeoViewControllerProperty) as GeoViewController;

        /// <summary>
        /// Sets the value of the <see cref="GeoViewController.GeoViewController"/> XAML attached property on the specified object.
        /// </summary>
        /// <param name="geoView">The target <see cref="GeoView"/> on which to set the <see cref="GeoViewController.GeoViewController"/> XAML attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetGeoViewController(GeoViewDPType geoView, GeoViewController? value) => geoView.SetValue(GeoViewControllerProperty, value);

        private static void OnGeoViewControllerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GeoView geoView)
            {
                if (e.OldValue is GeoViewController controllerOld)
                {
                    controllerOld.DetachFromGeoView(geoView);
                }

                if (e.NewValue is GeoViewController controllerNew)
                {
                    controllerNew.AttachToGeoView(geoView);
                }
            }
            else
            {
                throw new InvalidOperationException("This property must be attached to a GeoView.");
            }
        }
#endif
    }
}
