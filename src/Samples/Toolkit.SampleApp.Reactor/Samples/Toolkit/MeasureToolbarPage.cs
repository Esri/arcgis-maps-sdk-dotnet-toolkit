using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using Microsoft.UI.Reactor.Hooks;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Editing;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class MeasureToolbarPage : Component
{                
    private GeometryEditor editor = new Esri.ArcGISRuntime.UI.Editing.GeometryEditor();

    public override Element Render()
    {
        var mapViewRef = this.UseElementRef<MapView>();
        var map = UseMemo(() =>
        {
            var result = new Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"));
            result.OperationalLayers.Add(new ArcGISMapImageLayer(new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/USA/MapServer")));
            result.OperationalLayers.Add(new FeatureLayer(new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/Wildfire/FeatureServer/2")));
            result.OperationalLayers.Add(new FeatureLayer(new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/Wildfire/FeatureServer/1")));
            return result;
        });

        var overlays = UseMemo(() =>
        {
            var random = new Random(42);
            var overlay = new GraphicsOverlay();
            for (var i = 0; i < 8; i++)
            {
                var center = new MapPoint(random.Next(-130, -70), random.Next(25, 50), SpatialReferences.Wgs84);
                overlay.Graphics.Add(new Graphic(GeometryEngine.Buffer(center, random.Next(1, 4))));
            }

            return new GraphicsOverlayCollection
            {
                overlay,
            };
        });

        return Grid(columns: [GridSize.Star()], rows: [GridSize.Star()],
            MapView(map)
                .GraphicsOverlays(overlays)
                .Ref(mapViewRef) with
            {
                GeometryEditor = editor
            },

            MeasureToolbar()
                .Set((MeasureToolbar control) => control.MapView = mapViewRef.Current)
                .Margin(20)
                .HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Left)
                .VerticalAlignment(Microsoft.UI.Xaml.VerticalAlignment.Top)
        );
    }
}
