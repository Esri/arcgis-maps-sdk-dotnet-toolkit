using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UI.GeoAnalysis;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Core.V1Protocol;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

public static partial class Factories
{
    private abstract class GeoViewHandler<TElement, TControl> : IElementHandler<TElement, TControl>
        where TElement : GeoViewElement
        where TControl : GeoView
    {
        TControl IElementHandler<TElement, TControl>.Mount(MountContext ctx, TElement element)
        {
            var geoview = Mount(ctx, element);
            Reconciler.SetElementTag(geoview, element);
            geoview.IsAttributionTextVisible = element.IsAttributionTextVisible;
            geoview.ViewInsets = element.ViewInsets;
            geoview.TimeExtent = element.TimeExtent;
            geoview.SelectionProperties = element.SelectionProperties;
            geoview.Labeling = element.Labeling;
            geoview.Grid = element.Grid;
            geoview.GraphicsOverlays = element.GraphicsOverlays;
            geoview.ImageOverlays = element.ImageOverlays;
            geoview.AnalysisOverlays = element.AnalysisOverlays;

            var bind = ctx.BindFor(geoview, element);
            bind.OnCustomEvent<GeoViewInputEventArgs>(
                subscribe: static (c, h) => ((GeoView)c).GeoViewTapped += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, args) => cur.OnTapped?.Invoke(args));
            bind.OnCustomEvent<DrawStatusChangedEventArgs>(
                subscribe: static (c, h) => ((GeoView)c).DrawStatusChanged += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, args) => cur.OnDrawStatusChanged?.Invoke(args));
            bind.OnCustomEvent<LayerViewStateChangedEventArgs>(
                subscribe: static (c, h) => ((GeoView)c).LayerViewStateChanged += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, args) => cur.OnLayerViewStateChanged?.Invoke(args));
            bind.OnCustomEvent<EventArgs>(
                subscribe: static (c, h) => ((GeoView)c).SpatialReferenceChanged += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, _) => cur.OnSpatialReferenceChanged?.Invoke());
            bind.OnCustomEvent<AnalysisViewStateChangedEventArgs>(
                subscribe: static (c, h) => ((GeoView)c).AnalysisViewStateChanged += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, args) => cur.OnAnalysisViewStateChanged?.Invoke(args));
            bind.OnCustomEvent<EventArgs>(
                subscribe: static (c, h) => ((GeoView)c).ViewpointChanged += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, _) => cur.OnViewpointChanged?.Invoke());
            bind.OnCustomEvent<GeoViewInputEventArgs>(
                subscribe: static (c, h) => ((GeoView)c).GeoViewDoubleTapped += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, args) => cur.OnGeoViewDoubleTapped?.Invoke(args));
            bind.OnCustomEvent<GeoViewInputEventArgs>(
                subscribe: static (c, h) => ((GeoView)c).GeoViewHolding += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, args) => cur.OnGeoViewHolding?.Invoke(args));
            bind.OnCustomEvent<EventArgs>(
                subscribe: static (c, h) => ((GeoView)c).NavigationCompleted += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, _) => cur.OnNavigationCompleted?.Invoke());
            bind.OnCustomEvent<Exception?>(
                subscribe: static (c, h) => ((GeoView)c).CriticalErrorChanged += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, args) => cur.OnCriticalErrorChanged?.Invoke(args));
            bind.OnCustomEvent<Exception?>(
                subscribe: static (c, h) => ((GeoView)c).GeoModelErrorChanged += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, args) => cur.OnGeoModelErrorChanged?.Invoke(args));

            ctx.ApplySetters(element.Setters, geoview);
            return geoview;
        }

        protected abstract TControl Mount(MountContext ctx, TElement element);

        void IElementHandler<TElement, TControl>.Update(UpdateContext ctx, TElement oldEl, TElement newEl, TControl control)
        {
            Reconciler.SetElementTag(control, newEl);
            Update(ctx, oldEl, newEl, control);
            if (oldEl.IsAttributionTextVisible != newEl.IsAttributionTextVisible)
            {
                control.IsAttributionTextVisible = newEl.IsAttributionTextVisible;
            }

            if (oldEl.ViewInsets != newEl.ViewInsets)
            {
                control.ViewInsets = newEl.ViewInsets;
            }

            if (oldEl.TimeExtent != newEl.TimeExtent)
            {
                control.TimeExtent = newEl.TimeExtent;
            }

            if (oldEl.SelectionProperties != newEl.SelectionProperties)
            {
                control.SelectionProperties = newEl.SelectionProperties;
            }

            if (oldEl.Labeling != newEl.Labeling)
            {
                control.Labeling = newEl.Labeling;
            }

            if (oldEl.Grid != newEl.Grid)
            {
                control.Grid = newEl.Grid;
            }

            if (oldEl.GraphicsOverlays != newEl.GraphicsOverlays)
            {
                control.GraphicsOverlays = newEl.GraphicsOverlays;
            }

            if (oldEl.ImageOverlays != newEl.ImageOverlays)
            {
                control.ImageOverlays = newEl.ImageOverlays;
            }

            if (oldEl.AnalysisOverlays != newEl.AnalysisOverlays)
            {
                control.AnalysisOverlays = newEl.AnalysisOverlays;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }

        protected abstract void Update(UpdateContext ctx, TElement oldEl, TElement newEl, TControl control);

        void IElementHandler<TElement, TControl>.Unmount(UnmountContext ctx, TControl control)
        {
            Unmount(ctx, control);
            control.GraphicsOverlays = null;
            control.AnalysisOverlays = null;
            control.ImageOverlays = null;
        }

        protected virtual void Unmount(UnmountContext ctx, TControl ctrl)
        {
        }
    }

    private sealed class MapViewHandler : GeoViewHandler<MapViewElement, MapView>
    {
        protected override MapView Mount(MountContext ctx, MapViewElement element)
        {
            var mapView = ctx.RentControl<MapView>();
            mapView.Map = element.Map;
            mapView.GeometryEditor = element.GeometryEditor;
            mapView.WrapAroundMode = element.WrapAroundMode;
            mapView.LocationDisplay.IsEnabled = element.LocationDisplay.IsEnabled;
            mapView.LocationDisplay.AutoPanMode = element.LocationDisplay.AutoPanMode;
            if (element.BackgroundGrid is not null)
            {
                mapView.BackgroundGrid = element.BackgroundGrid;
            }

            if (element.InteractionOptions is not null)
            {
                mapView.InteractionOptions = element.InteractionOptions;
            }

            var bind = ctx.BindFor(mapView, element);
            bind.OnCustomEvent<double>(
                subscribe: static (c, h) => ((INotifyPropertyChanged)c).PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(Esri.ArcGISRuntime.UI.Controls.MapView.MapScale) && sender is Esri.ArcGISRuntime.UI.Controls.MapView mapView)
                    {
                        h(sender, mapView.MapScale);
                    }
                },
                unsubscribe: static (_, _) => { },
                handler: static (cur, value) => cur.OnMapScaleChanged?.Invoke(value));
            bind.OnCustomEvent<double>(
                subscribe: static (c, h) => ((INotifyPropertyChanged)c).PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(Esri.ArcGISRuntime.UI.Controls.MapView.UnitsPerPixel) && sender is Esri.ArcGISRuntime.UI.Controls.MapView mapView)
                    {
                        h(sender, mapView.UnitsPerPixel);
                    }
                },
                unsubscribe: static (_, _) => { },
                handler: static (cur, value) => cur.OnUnitsPerPixelChanged?.Invoke(value));
            bind.OnCustomEvent<double>(
                subscribe: static (c, h) => ((INotifyPropertyChanged)c).PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(Esri.ArcGISRuntime.UI.Controls.MapView.MapRotation) && sender is Esri.ArcGISRuntime.UI.Controls.MapView mapView)
                    {
                        h(sender, mapView.MapRotation);
                    }
                },
                unsubscribe: static (_, _) => { },
                handler: static (cur, value) => cur.OnMapRotationChanged?.Invoke(value));
            bind.OnCustomEvent<Esri.ArcGISRuntime.Geometry.Polygon?>(
                subscribe: static (c, h) => ((INotifyPropertyChanged)c).PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(Esri.ArcGISRuntime.UI.Controls.MapView.VisibleArea) && sender is Esri.ArcGISRuntime.UI.Controls.MapView mapView)
                    {
                        h(sender, mapView.VisibleArea);
                    }
                },
                unsubscribe: static (_, _) => { },
                handler: static (cur, value) => cur.OnVisibleAreaChanged?.Invoke(value));
            return mapView;
        }

        protected override void Update(UpdateContext ctx, MapViewElement oldEl, MapViewElement newEl, MapView control)
        {
            if (oldEl.Map != newEl.Map)
            {
                control.Map = newEl.Map;
            }

            if (oldEl.GeometryEditor != newEl.GeometryEditor)
            {
                control.GeometryEditor = newEl.GeometryEditor;
            }

            if (oldEl.WrapAroundMode != newEl.WrapAroundMode)
            {
                control.WrapAroundMode = newEl.WrapAroundMode;
            }

            if (oldEl.LocationDisplay.IsEnabled != newEl.LocationDisplay.IsEnabled)
            {
                control.LocationDisplay.IsEnabled = newEl.LocationDisplay.IsEnabled;
            }

            if (oldEl.LocationDisplay.AutoPanMode != newEl.LocationDisplay.AutoPanMode)
            {
                control.LocationDisplay.AutoPanMode = newEl.LocationDisplay.AutoPanMode;
            }

            if (oldEl.BackgroundGrid != newEl.BackgroundGrid && newEl.BackgroundGrid is not null)
            {
                control.BackgroundGrid = newEl.BackgroundGrid;
            }

            if (oldEl.InteractionOptions != newEl.InteractionOptions && newEl.InteractionOptions is not null)
            {
                control.InteractionOptions = newEl.InteractionOptions;
            }
        }

        protected override void Unmount(UnmountContext ctx, MapView ctrl)
        {
            ctrl.Map = null;
            ctrl.GeometryEditor = null;
            ctx.ReturnControl(ctrl);
        }
    }

    private sealed class SceneViewHandler : GeoViewHandler<SceneViewElement, SceneView>
    {
        protected override SceneView Mount(MountContext ctx, SceneViewElement element)
        {
            var sceneView = ctx.RentControl<SceneView>();
            sceneView.Scene = element.Scene;
            if (element.CameraController is not null)
            {
                sceneView.CameraController = element.CameraController;
            }

            if (element.AtmosphereEffect is { } atmosphereEffect)
            {
                sceneView.AtmosphereEffect = atmosphereEffect;
            }

            if (element.SunLighting is { } sunLighting)
            {
                sceneView.SunLighting = sunLighting;
            }

            if (element.SunTime is { } sunTime)
            {
                sceneView.SunTime = sunTime;
            }

            if (element.AmbientLightColor is { } ambientLightColor)
            {
                sceneView.AmbientLightColor = ambientLightColor;
            }

            if (element.SpaceEffect is { } spaceEffect)
            {
                sceneView.SpaceEffect = spaceEffect;
            }

            if (element.InteractionOptions is not null)
            {
                sceneView.InteractionOptions = element.InteractionOptions;
            }

            return sceneView;
        }

        protected override void Update(UpdateContext ctx, SceneViewElement oldEl, SceneViewElement newEl, SceneView control)
        {
            if (oldEl.Scene != newEl.Scene)
            {
                control.Scene = newEl.Scene;
            }

            if (oldEl.CameraController != newEl.CameraController && newEl.CameraController is not null)
            {
                control.CameraController = newEl.CameraController;
            }

            if (oldEl.AtmosphereEffect != newEl.AtmosphereEffect && newEl.AtmosphereEffect is { } atmosphereEffect)
            {
                control.AtmosphereEffect = atmosphereEffect;
            }

            if (oldEl.SunLighting != newEl.SunLighting && newEl.SunLighting is { } sunLighting)
            {
                control.SunLighting = sunLighting;
            }

            if (oldEl.SunTime != newEl.SunTime && newEl.SunTime is { } sunTime)
            {
                control.SunTime = sunTime;
            }

            if (oldEl.AmbientLightColor != newEl.AmbientLightColor && newEl.AmbientLightColor is { } ambientLightColor)
            {
                control.AmbientLightColor = ambientLightColor;
            }

            if (oldEl.SpaceEffect != newEl.SpaceEffect && newEl.SpaceEffect is { } spaceEffect)
            {
                control.SpaceEffect = spaceEffect;
            }

            if (oldEl.InteractionOptions != newEl.InteractionOptions && newEl.InteractionOptions is not null)
            {
                control.InteractionOptions = newEl.InteractionOptions;
            }
        }

        protected override void Unmount(UnmountContext ctx, SceneView ctrl)
        {
            ctrl.Scene = null;
            ctx.ReturnControl(ctrl);
        }
    }

    private sealed class LocalSceneViewHandler : GeoViewHandler<LocalSceneViewElement, LocalSceneView>
    {
        protected override LocalSceneView Mount(MountContext ctx, LocalSceneViewElement element)
        {
            var localSceneView = ctx.RentControl<LocalSceneView>();
            localSceneView.Scene = element.Scene;
            if (element.InteractionOptions is not null)
            {
                localSceneView.InteractionOptions = element.InteractionOptions;
            }

            var bind = ctx.BindFor(localSceneView, element);
            bind.OnCustomEvent<IEnumerable<Exception>>(
                subscribe: static (c, h) => ((LocalSceneView)c).WarningsChanged += (sender, _) =>
                {
                    if (sender is LocalSceneView view)
                    {
                        h(sender, view.Warnings);
                    }
                },
                unsubscribe: static (_, _) => { },
                handler: static (cur, warnings) => cur.OnWarningsChanged?.Invoke(warnings));
            return localSceneView;
        }

        protected override void Update(UpdateContext ctx, LocalSceneViewElement oldEl, LocalSceneViewElement newEl, LocalSceneView control)
        {
            if (oldEl.Scene != newEl.Scene)
            {
                control.Scene = newEl.Scene;
            }

            if (oldEl.InteractionOptions != newEl.InteractionOptions && newEl.InteractionOptions is not null)
            {
                control.InteractionOptions = newEl.InteractionOptions;
            }
        }

        protected override void Unmount(UnmountContext ctx, LocalSceneView ctrl)
        {
            ctrl.Scene = null;
            ctx.ReturnControl(ctrl);
        }
    }
}
