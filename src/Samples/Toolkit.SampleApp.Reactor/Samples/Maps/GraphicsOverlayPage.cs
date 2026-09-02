using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.Toolkit.Reactor;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using WinRT;

namespace Toolkit.SampleApp.Reactor.Samples.Maps;

using Microsoft.UI.Reactor.Hooks;

public sealed class GraphicsOverlayPage : Component
{
    private Map map = new Map(BasemapStyle.ArcGISLightGray);

    public override Element Render()
    {
        var mapviewRef = this.UseElementRef<MapView>();

        var overlays = UseMemo(() =>
        {
            var overlay = new GraphicsOverlay()
            {
                Renderer = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 12))
            };
            overlay.Graphics.Add(new Graphic(new MapPoint(-122.431297, 37.773972, SpatialReferences.Wgs84)));
            return new GraphicsOverlayCollection
            {
                overlay
            };
        });

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
               MapView(
                 map: map,
                 onTapped: (args) =>
                 {
                     overlays[0].Graphics.Add(new Graphic(args.Location));
                     mapviewRef.Current!.SetViewpointAsync(new Viewpoint(args.Location!));
                 })
                .GraphicsOverlays(overlays).Ref(mapviewRef),
              GalleryControls.ControlPanel(VStack(
                Caption("Click the map to add graphics"),
                Button("Clear Graphics", () => overlays[0].Graphics.Clear()).HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Stretch)
                ))
        );
    }
}