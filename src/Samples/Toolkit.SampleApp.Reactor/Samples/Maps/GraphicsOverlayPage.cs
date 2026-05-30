using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.UI;

namespace Toolkit.SampleApp.Reactor.Samples.Maps;

public sealed class GraphicsOverlayPage : Component
{
    public override Element Render()
    {
        var map = UseMemo(() => new Map(BasemapStyle.ArcGISLightGray));
        var overlays = UseMemo(() =>
        {
            var overlay = new Esri.ArcGISRuntime.UI.GraphicsOverlay()
            {
                Renderer = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 12))
            };
            overlay.Graphics.Add(new Esri.ArcGISRuntime.UI.Graphic(new MapPoint(-122.431297, 37.773972, SpatialReferences.Wgs84)));
            return new GraphicsOverlayCollection
            {
                overlay
            };
        });

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            MapView(
                map: map,
                onTapped: (args)=> overlays[0].Graphics.Add(new Graphic(args.Location)))
               .GraphicsOverlays(overlays),
            GalleryControls.ControlPanel(VStack(
                Caption("Click the map to add graphics"),
                Button("Clear Graphics", () => overlays[0].Graphics.Clear()).HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Stretch)
                ))
        );
    }
}