using System;
using Esri.ArcGISRuntime.Mapping;
using Microsoft.UI.Reactor.Hooks;
using FloorFilterControl = Esri.ArcGISRuntime.Toolkit.UI.Controls.FloorFilter;
using MapViewControl = Esri.ArcGISRuntime.UI.Controls.MapView;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class FloorFilterPage : Component
{
    public override Element Render()
    {
        var mapViewRef = this.UseElementRef<MapViewControl>();
        var map = UseMemo(() => new Map(new Uri("https://www.arcgis.com/home/item.html?id=b4b599a43a474d33946cf0df526426f5")));

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            MapView(map)
                .Ref(mapViewRef),

            FloorFilter()
                .Set((FloorFilterControl control) => control.GeoView = mapViewRef.Current)
                .Width(320)
                .Margin(20)
                .HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Right)
                .VerticalAlignment(Microsoft.UI.Xaml.VerticalAlignment.Top),

            GalleryControls.ControlPanel(
                Caption("Browse a floor-aware map and filter to a site, facility, and level."))
        );
    }
}
