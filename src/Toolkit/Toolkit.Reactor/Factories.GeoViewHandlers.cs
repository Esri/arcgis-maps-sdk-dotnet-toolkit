using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
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
            geoview.GraphicsOverlays = element.GraphicsOverlays;

            var bind = ctx.BindFor(geoview, element);
            bind.OnCustomEvent<GeoViewInputEventArgs>(
                subscribe: static (c, h) => ((GeoView)c).GeoViewTapped += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, args) => cur.OnTapped?.Invoke(args));

            ctx.ApplySetters(element.Setters, geoview);
            return geoview;
        }

        protected abstract TControl Mount(MountContext ctx, TElement element);

        void IElementHandler<TElement, TControl>.Update(UpdateContext ctx, TElement oldEl, TElement newEl, TControl control)
        {
            Reconciler.SetElementTag(control, newEl);
            Update(ctx, oldEl, newEl, control);
            if (oldEl.GraphicsOverlays != newEl.GraphicsOverlays)
            {
                control.GraphicsOverlays = newEl.GraphicsOverlays;
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
            mapView.LocationDisplay.IsEnabled = element.LocationDisplay.IsEnabled;
            mapView.LocationDisplay.AutoPanMode = element.LocationDisplay.AutoPanMode;
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

            if (oldEl.LocationDisplay.IsEnabled != newEl.LocationDisplay.IsEnabled)
            {
                control.LocationDisplay.IsEnabled = newEl.LocationDisplay.IsEnabled;
            }

            if (oldEl.LocationDisplay.AutoPanMode != newEl.LocationDisplay.AutoPanMode)
            {
                control.LocationDisplay.AutoPanMode = newEl.LocationDisplay.AutoPanMode;
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
            return sceneView;
        }

        protected override void Update(UpdateContext ctx, SceneViewElement oldEl, SceneViewElement newEl, SceneView control)
        {
            if (oldEl.Scene != newEl.Scene)
            {
                control.Scene = newEl.Scene;
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
            return localSceneView;
        }

        protected override void Update(UpdateContext ctx, LocalSceneViewElement oldEl, LocalSceneViewElement newEl, LocalSceneView control)
        {
            if (oldEl.Scene != newEl.Scene)
            {
                control.Scene = newEl.Scene;
            }
        }

        protected override void Unmount(UnmountContext ctx, LocalSceneView ctrl)
        {
            ctrl.Scene = null;
            ctx.ReturnControl(ctrl);
        }
    }
}
