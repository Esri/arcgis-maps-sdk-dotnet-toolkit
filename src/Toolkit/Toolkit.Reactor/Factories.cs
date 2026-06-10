using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
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
        ControlRegistry.Register(static () => new BookmarksViewHandler());
        ControlRegistry.Register(static () => new FeatureDataFieldHandler());
        ControlRegistry.Register(static () => new FloorFilterHandler());
        ControlRegistry.Register(static () => new LegendHandler());
        ControlRegistry.Register(static () => new OfflineMapAreasViewHandler());
        ControlRegistry.Register(static () => new MeasureToolbarHandler());
        ControlRegistry.Register(static () => new OverviewMapHandler());
        ControlRegistry.Register(static () => new PopupViewerHandler());
        ControlRegistry.Register(static () => new ScaleLineHandler());
        ControlRegistry.Register(static () => new SearchViewHandler());
        ControlRegistry.Register(static () => new SymbolDisplayHandler());
        ControlRegistry.Register(static () => new TimeSliderHandler());
        ControlRegistry.Register(static () => new FeatureFormViewHandler());
        ControlRegistry.Register(static () => new UtilityNetworkTraceToolHandler());
    }
}
