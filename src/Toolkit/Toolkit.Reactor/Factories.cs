using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Core.V1Protocol;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

/// <summary>
/// Provides factory and fluent extension methods for Reactor elements backed by ArcGIS Maps SDK for .NET controls.
/// </summary>
public static partial class Factories
{
    static Factories()
    {
        ReactorApp.RegisterControlAssembly(new Esri_ArcGISRuntime_WinUI_XamlTypeInfo.XamlMetaDataProvider());
        ReactorApp.RegisterControlAssembly(new Esri_ArcGISRuntime_Toolkit_WinUI_XamlTypeInfo.XamlMetaDataProvider());

        // Core
        ControlRegistry.Register(static () => new MapViewHandler());
        ControlRegistry.Register(static () => new SceneViewHandler());
        ControlRegistry.Register(static () => new LocalSceneViewHandler());

        // Toolkit
        ControlRegistry.Register(static () => new CompassHandler());
        ControlRegistry.Register(static () => new BasemapGalleryHandler());
    }

    private abstract class GeoViewHandler<TElement, TControl> : IElementHandler<TElement, TControl> where TElement : GeoViewElement where TControl : GeoView
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
                handler: (cur, args) => element.OnTapped?.Invoke(args));
            ctx.ApplySetters(element.Setters, geoview);
            return geoview;
        }

        protected abstract TControl Mount(MountContext ctx, TElement element);

        void IElementHandler<TElement, TControl>.Update(UpdateContext ctx, TElement oldEl, TElement newEl, TControl control)
        {
            Reconciler.SetElementTag(control, newEl);
            Update(ctx, oldEl, newEl, control);
            if (oldEl.GraphicsOverlays != newEl.GraphicsOverlays)
                control.GraphicsOverlays = newEl.GraphicsOverlays;
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
                control.Map = newEl.Map;
            if (oldEl.GeometryEditor != newEl.GeometryEditor)
                control.GeometryEditor = newEl.GeometryEditor;
            if (oldEl.LocationDisplay.IsEnabled != newEl.LocationDisplay.IsEnabled)
                control.LocationDisplay.IsEnabled = newEl.LocationDisplay.IsEnabled;
            if (oldEl.LocationDisplay.AutoPanMode != newEl.LocationDisplay.AutoPanMode)
                control.LocationDisplay.AutoPanMode = newEl.LocationDisplay.AutoPanMode;
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
                control.Scene = newEl.Scene;
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
                control.Scene = newEl.Scene;
        }
        
        protected override void Unmount(UnmountContext ctx, LocalSceneView ctrl)
        {
            ctrl.Scene = null;
            ctx.ReturnControl(ctrl);
        }
    }

    private sealed class CompassHandler : IElementHandler<CompassElement, Compass>
    {
        public Compass Mount(MountContext ctx, CompassElement element)
        {
            var compass = new Compass() { AutoHide = element.AutoHide };
            ctx.ApplySetters(element.Setters, compass);
            return compass;
        }

        public void Update(UpdateContext ctx, CompassElement oldEl, CompassElement newEl, Compass control)
        {
            if (oldEl.AutoHide != newEl.AutoHide)
                control.AutoHide = newEl.AutoHide;
            ctx.ApplySetters(newEl.Setters, control);
        }
    }

    private sealed class BasemapGalleryHandler : IElementHandler<BasemapGalleryElement, BasemapGallery>
    {
        public BasemapGallery Mount(MountContext ctx, BasemapGalleryElement element)
        {
            var gallery = new BasemapGallery
            {
                GeoModel = element.GeoModel,
                GalleryViewStyle = element.GalleryViewStyle,
            };
            var bind = ctx.BindFor(gallery, element);

            bind.OnCustomEvent<UI.BasemapGalleryItem>(
                subscribe: static (c, h) => ((BasemapGallery)c).BasemapSelected += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: (cur, args) => element.OnBasemapSelected?.Invoke(args));

            ctx.ApplySetters(element.Setters, gallery);
            return gallery;
        }

        public void Update(UpdateContext ctx, BasemapGalleryElement oldEl, BasemapGalleryElement newEl, BasemapGallery control)
        {
            if (oldEl.GeoModel != newEl.GeoModel)
                control.GeoModel = newEl.GeoModel;
            if (oldEl.GalleryViewStyle != newEl.GalleryViewStyle)
                control.GalleryViewStyle = newEl.GalleryViewStyle;
            ctx.ApplySetters(newEl.Setters, control);
        }
    }
}