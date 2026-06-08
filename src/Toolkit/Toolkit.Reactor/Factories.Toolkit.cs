using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Reactor.Input;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

public static partial class Factories
{
    /// <summary>
    /// Creates a declarative <see cref="CompassElement"/>.
    /// </summary>
    /// <param name="geoView">The declarative geoview associated with the compass.</param>
    /// <param name="autoHide"><see langword="true"/> to hide the compass when the geoview is north aligned; otherwise, <see langword="false"/>.</param>
    /// <returns>A new <see cref="CompassElement"/> instance.</returns>
    public static CompassElement Compass(ElementRef<GeoView>? geoView, bool autoHide = true) =>
        new(geoView, autoHide);

    /// <summary>
    /// Creates a declarative <see cref="BasemapGalleryElement"/>.
    /// </summary>
    /// <param name="geomodel">The geo model whose basemap is displayed and updated by the gallery.</param>
    /// <returns>A new <see cref="BasemapGalleryElement"/> instance.</returns>
    public static BasemapGalleryElement BasemapGallery(GeoModel geomodel) =>
        new(geomodel);

    /// <summary>
    /// Creates a declarative <see cref="BookmarksViewElement"/>.
    /// </summary>
    /// <param name="geoView">The geoview whose bookmarks are displayed.</param>
    /// <returns>A new <see cref="BookmarksViewElement"/> instance.</returns>
    public static BookmarksViewElement BookmarksView(ElementRef<GeoView>? geoView = null) => new(geoView);

    /// <summary>
    /// Creates a declarative <see cref="FeatureDataFieldElement"/>.
    /// </summary>
    /// <param name="feature">The feature whose field is displayed or edited.</param>
    /// <param name="fieldName">The field name to display or edit.</param>
    /// <returns>A new <see cref="FeatureDataFieldElement"/> instance.</returns>
    public static FeatureDataFieldElement FeatureDataField(Feature? feature = null, string? fieldName = null) => new(feature, fieldName);

    /// <summary>
    /// Creates a declarative <see cref="FloorFilterElement"/>.
    /// </summary>
    /// <param name="geoView">The floor-aware geoview associated with the filter.</param>
    /// <returns>A new <see cref="FloorFilterElement"/> instance.</returns>
    public static FloorFilterElement FloorFilter(ElementRef<GeoView>? geoView = null) => new(geoView);

    /// <summary>
    /// Creates a declarative <see cref="LegendElement"/>.
    /// </summary>
    /// <param name="geoView">The geoview whose legend is displayed.</param>
    /// <returns>A new <see cref="LegendElement"/> instance.</returns>
    public static LegendElement Legend(ElementRef<GeoView>? geoView = null) => new(geoView);

    /// <summary>
    /// Creates a declarative <see cref="MeasureToolbarElement"/>.
    /// </summary>
    /// <param name="mapView">The map view measured by the toolbar.</param>
    /// <returns>A new <see cref="MeasureToolbarElement"/> instance.</returns>
    public static MeasureToolbarElement MeasureToolbar(ElementRef<MapView>? mapView = null) => new(mapView);

    /// <summary>
    /// Creates a declarative <see cref="OverviewMapElement"/>.
    /// </summary>
    /// <param name="geoView">The geoview tracked by the overview map.</param>
    /// <returns>A new <see cref="OverviewMapElement"/> instance.</returns>
    public static OverviewMapElement OverviewMap(ElementRef<GeoView>? geoView = null) => new(geoView);

    /// <summary>
    /// Creates a declarative <see cref="PopupViewerElement"/>.
    /// </summary>
    /// <param name="popup">The popup displayed by the viewer.</param>
    /// <returns>A new <see cref="PopupViewerElement"/> instance.</returns>
    public static PopupViewerElement PopupViewer(Popup? popup = null) => new(popup);

    /// <summary>
    /// Creates a declarative <see cref="ScaleLineElement"/>.
    /// </summary>
    /// <param name="mapView">The map view used to derive the scale.</param>
    /// <returns>A new <see cref="ScaleLineElement"/> instance.</returns>
    public static ScaleLineElement ScaleLine(ElementRef<MapView>? mapView = null) => new(mapView);

    /// <summary>
    /// Creates a declarative <see cref="SearchViewElement"/>.
    /// </summary>
    /// <param name="geoView">The geoview searched by the control.</param>
    /// <returns>A new <see cref="SearchViewElement"/> instance.</returns>
    public static SearchViewElement SearchView(ElementRef<GeoView>? geoView = null) => new(geoView);

    /// <summary>
    /// Creates a declarative <see cref="SymbolDisplayElement"/>.
    /// </summary>
    /// <param name="symbol">The symbol rendered by the control.</param>
    /// <returns>A new <see cref="SymbolDisplayElement"/> instance.</returns>
    public static SymbolDisplayElement SymbolDisplay(Symbol? symbol = null) => new(symbol);

    /// <summary>
    /// Creates a declarative <see cref="TimeSliderElement"/>.
    /// </summary>
    /// <param name="fullExtent">The overall time extent.</param>
    /// <param name="currentExtent">The current time extent.</param>
    /// <returns>A new <see cref="TimeSliderElement"/> instance.</returns>
    public static TimeSliderElement TimeSlider(TimeExtent? fullExtent = null, TimeExtent? currentExtent = null) =>
        new()
        {
            FullExtent = fullExtent,
            CurrentExtent = currentExtent,
        };

    /// <summary>
    /// Creates a declarative <see cref="FeatureFormViewElement"/>.
    /// </summary>
    /// <param name="featureForm">The feature form displayed by the control.</param>
    /// <returns>A new <see cref="FeatureFormViewElement"/> instance.</returns>
    public static FeatureFormViewElement FeatureFormView(FeatureForm? featureForm = null) => new(featureForm);

    /// <summary>
    /// Creates a declarative <see cref="UtilityNetworkTraceToolElement"/>.
    /// </summary>
    /// <param name="geoView">The geoview where starting points and results are displayed.</param>
    /// <returns>A new <see cref="UtilityNetworkTraceToolElement"/> instance.</returns>
    public static UtilityNetworkTraceToolElement UtilityNetworkTraceTool(ElementRef<GeoView>? geoView = null) => new(geoView);

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="Compass"/>.
    /// </summary>
    public static CompassElement Set(this CompassElement element, Action<Compass> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="BasemapGallery"/>.
    /// </summary>
    public static BasemapGalleryElement Set(this BasemapGalleryElement element, Action<BasemapGallery> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="BookmarksView"/>.
    /// </summary>
    public static BookmarksViewElement Set(this BookmarksViewElement element, Action<BookmarksView> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="FeatureDataField"/>.
    /// </summary>
    public static FeatureDataFieldElement Set(this FeatureDataFieldElement element, Action<FeatureDataField> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="FloorFilter"/>.
    /// </summary>
    public static FloorFilterElement Set(this FloorFilterElement element, Action<FloorFilter> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="Legend"/>.
    /// </summary>
    public static LegendElement Set(this LegendElement element, Action<Legend> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="MeasureToolbar"/>.
    /// </summary>
    public static MeasureToolbarElement Set(this MeasureToolbarElement element, Action<MeasureToolbar> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="OverviewMap"/>.
    /// </summary>
    public static OverviewMapElement Set(this OverviewMapElement element, Action<OverviewMap> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="PopupViewer"/>.
    /// </summary>
    public static PopupViewerElement Set(this PopupViewerElement element, Action<PopupViewer> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="ScaleLine"/>.
    /// </summary>
    public static ScaleLineElement Set(this ScaleLineElement element, Action<ScaleLine> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="SearchView"/>.
    /// </summary>
    public static SearchViewElement Set(this SearchViewElement element, Action<SearchView> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="SymbolDisplay"/>.
    /// </summary>
    public static SymbolDisplayElement Set(this SymbolDisplayElement element, Action<SymbolDisplay> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="TimeSlider"/>.
    /// </summary>
    public static TimeSliderElement Set(this TimeSliderElement element, Action<TimeSlider> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="FeatureFormView"/>.
    /// </summary>
    public static FeatureFormViewElement Set(this FeatureFormViewElement element, Action<FeatureFormView> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Registers a custom configuration action that runs against the mounted <see cref="UtilityNetworkTraceTool"/>.
    /// </summary>
    public static UtilityNetworkTraceToolElement Set(this UtilityNetworkTraceToolElement element, Action<UtilityNetworkTraceTool> configure) =>
        element with { Setters = [.. element.Setters, configure] };

    /// <summary>
    /// Sets the view style used to present basemap gallery items.
    /// </summary>
    /// <param name="element">The basemap gallery element to configure.</param>
    /// <param name="style">The basemap gallery view style.</param>
    /// <returns>The configured basemap gallery element.</returns>
    public static BasemapGalleryElement ViewStyle(this BasemapGalleryElement element, UI.BasemapGalleryViewStyle style)
    {
        element.GalleryViewStyle = style;
        return element;
    }
}
