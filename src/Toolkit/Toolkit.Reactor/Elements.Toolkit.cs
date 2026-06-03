using System.Collections.Generic;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

/// <summary>
/// Represents a declarative <see cref="Compass"/> element.
/// </summary>
/// <param name="GeoView">The declarative geoview associated with the compass.</param>
/// <param name="AutoHide"><see langword="true"/> to hide the compass when the geoview is north aligned; otherwise, <see langword="false"/>.</param>
public record CompassElement(GeoViewElement? GeoView = null, bool AutoHide = true) : Element
{
    /// <summary>
    /// Gets or sets the heading, in degrees, shown by the compass.
    /// </summary>
    public double Heading { get; set; }

    internal Action<Compass>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="BasemapGallery"/> element.
/// </summary>
/// <param name="GeoModel">The geo model whose basemap is displayed and updated by the gallery.</param>
public record BasemapGalleryElement(GeoModel? GeoModel = null) : Element
{
    /// <summary>
    /// Gets or sets the visual style used to present gallery items.
    /// </summary>
    public UI.BasemapGalleryViewStyle GalleryViewStyle { get; set; }

    /// <summary>
    /// Gets or sets the action invoked when the selected basemap changes.
    /// </summary>
    public Action<UI.BasemapGalleryItem?>? OnBasemapSelected { get; init; }

    /// <summary>
    /// Gets or sets the currently selected basemap item.
    /// </summary>
    public BasemapGalleryItem? SelectedBasemap { get; init; }

    internal Action<BasemapGallery>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="BookmarksView"/> element.
/// </summary>
/// <param name="GeoView">The geoview whose bookmarks are displayed and navigated.</param>
public record BookmarksViewElement(GeoView? GeoView = null) : Element
{
    /// <summary>
    /// Gets or sets the bookmark collection displayed by the view.
    /// </summary>
    public IEnumerable<Bookmark>? BookmarksOverride { get; set; }

    /// <summary>
    /// Gets or sets the action invoked when a bookmark is selected.
    /// </summary>
    public Action<Bookmark>? OnBookmarkSelected { get; init; }

    internal Action<BookmarksView>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="FeatureDataField"/> element.
/// </summary>
/// <param name="Feature">The feature whose field is displayed or edited.</param>
/// <param name="FieldName">The field name to display or edit.</param>
public record FeatureDataFieldElement(Feature? Feature = null, string? FieldName = null) : Element
{
    /// <summary>
    /// Gets or sets a value indicating whether the field is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the bound field value.
    /// </summary>
    public object? BindingValue { get; set; }

    /// <summary>
    /// Gets or sets the action invoked before the field value is committed.
    /// </summary>
    public Action<AttributeValueChangedEventArgs>? OnValueChanging { get; init; }

    /// <summary>
    /// Gets or sets the action invoked after the field value is committed.
    /// </summary>
    public Action<AttributeValueChangedEventArgs>? OnValueChanged { get; init; }

    internal Action<FeatureDataField>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="FloorFilter"/> element.
/// </summary>
/// <param name="GeoView">The floor-aware geoview associated with the filter.</param>
public record FloorFilterElement(GeoView? GeoView = null) : Element
{
    /// <summary>
    /// Gets or sets a value indicating whether the browse view is open.
    /// </summary>
    public bool IsBrowseOpen { get; set; }

    internal Action<FloorFilter>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="Legend"/> element.
/// </summary>
/// <param name="GeoView">The geoview whose legend is displayed.</param>
public record LegendElement(GeoView? GeoView = null) : Element
{
    /// <summary>
    /// Gets or sets a value indicating whether layers outside the visible scale range are filtered out.
    /// </summary>
    public bool FilterByVisibleScaleRange { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether hidden layers are filtered out.
    /// </summary>
    public bool FilterHiddenLayers { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the layer order is reversed.
    /// </summary>
    public bool ReverseLayerOrder { get; set; } = true;

    internal Action<Legend>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="MeasureToolbar"/> element.
/// </summary>
/// <param name="MapView">The map view measured by the toolbar.</param>
public record MeasureToolbarElement(MapView? MapView = null) : Element
{
    internal Action<MeasureToolbar>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="OverviewMap"/> element.
/// </summary>
/// <param name="GeoView">The geoview tracked by the overview map.</param>
public record OverviewMapElement(GeoView? GeoView = null) : Element
{
    /// <summary>
    /// Gets or sets the map displayed in the overview map.
    /// </summary>
    public Map? Map { get; set; }

    /// <summary>
    /// Gets or sets the symbol used to draw the visible area.
    /// </summary>
    public Symbology.Symbol? AreaSymbol { get; set; }

    /// <summary>
    /// Gets or sets the symbol used to draw point-based viewpoints.
    /// </summary>
    public Symbology.Symbol? PointSymbol { get; set; }

    /// <summary>
    /// Gets or sets the scale factor applied to the overview viewpoint.
    /// </summary>
    public double ScaleFactor { get; set; } = 25.0;

    internal Action<OverviewMap>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="PopupViewer"/> element.
/// </summary>
/// <param name="Popup">The popup displayed by the viewer.</param>
public record PopupViewerElement(Popup? Popup = null) : Element
{
    /// <summary>
    /// Gets or sets the vertical scrollbar visibility for popup content.
    /// </summary>
    public ScrollBarVisibility VerticalScrollBarVisibility { get; set; } = ScrollBarVisibility.Auto;

    /// <summary>
    /// Gets or sets the action invoked when a popup attachment is clicked.
    /// </summary>
    public Action<PopupAttachmentClickedEventArgs>? OnPopupAttachmentClicked { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when a hyperlink is clicked.
    /// </summary>
    public Action<HyperlinkClickedEventArgs>? OnHyperlinkClicked { get; init; }

    internal Action<PopupViewer>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="ScaleLine"/> element.
/// </summary>
/// <param name="MapView">The map view used to derive the scale.</param>
public record ScaleLineElement(MapView? MapView = null) : Element
{
    /// <summary>
    /// Gets or sets the explicit map scale used by the control.
    /// </summary>
    public double MapScale { get; set; } = double.NaN;

    /// <summary>
    /// Gets or sets the target width of the scale line.
    /// </summary>
    public double TargetWidth { get; set; }

    internal Action<ScaleLine>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="SearchView"/> element.
/// </summary>
/// <param name="GeoView">The geoview searched by the control.</param>
public record SearchViewElement(GeoView? GeoView = null) : Element
{
    /// <summary>
    /// Gets or sets the search view model.
    /// </summary>
    public SearchViewModel? SearchViewModel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the default world geocoder is enabled.
    /// </summary>
    public bool EnableDefaultWorldGeocoder { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the repeat-search-here button is shown.
    /// </summary>
    public bool EnableRepeatSearchHereButton { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the selected result details are shown.
    /// </summary>
    public bool EnableIndividualResultDisplay { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the default result list is shown.
    /// </summary>
    public bool EnableResultListView { get; set; } = true;

    /// <summary>
    /// Gets or sets the zoom buffer applied when zooming to multiple results.
    /// </summary>
    public double MultipleResultZoomBuffer { get; set; } = 1.5;

    /// <summary>
    /// Gets or sets the no-results message.
    /// </summary>
    public string? NoResultMessage { get; set; }

    /// <summary>
    /// Gets or sets the all-sources button text.
    /// </summary>
    public string? AllSourceSelectText { get; set; }

    /// <summary>
    /// Gets or sets the clear-search tooltip text.
    /// </summary>
    public string? ClearSearchTooltipText { get; set; }

    /// <summary>
    /// Gets or sets the search tooltip text.
    /// </summary>
    public string? SearchTooltipText { get; set; }

    /// <summary>
    /// Gets or sets the repeat-search button text.
    /// </summary>
    public string? RepeatSearchButtonText { get; set; }

    internal Action<SearchView>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="SymbolDisplay"/> element.
/// </summary>
/// <param name="Symbol">The symbol rendered by the control.</param>
public record SymbolDisplayElement(Symbology.Symbol? Symbol = null) : Element
{
    internal Action<SymbolDisplay>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="TimeSlider"/> element.
/// </summary>
public record TimeSliderElement : Element
{
    /// <summary>
    /// Gets or sets the current time extent.
    /// </summary>
    public TimeExtent? CurrentExtent { get; set; }

    /// <summary>
    /// Gets or sets the full time extent.
    /// </summary>
    public TimeExtent? FullExtent { get; set; }

    /// <summary>
    /// Gets or sets the time step interval.
    /// </summary>
    public TimeValue? TimeStepInterval { get; set; }

    /// <summary>
    /// Gets or sets the playback direction.
    /// </summary>
    public PlaybackDirection PlaybackDirection { get; set; }

    /// <summary>
    /// Gets or sets the playback loop mode.
    /// </summary>
    public LoopMode PlaybackLoopMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the start time is pinned.
    /// </summary>
    public bool IsStartTimePinned { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the end time is pinned.
    /// </summary>
    public bool IsEndTimePinned { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether playback is running.
    /// </summary>
    public bool IsPlaying { get; set; }

    /// <summary>
    /// Gets or sets the full-extent label format.
    /// </summary>
    public string? FullExtentLabelFormat { get; set; }

    /// <summary>
    /// Gets or sets the current-extent label format.
    /// </summary>
    public string? CurrentExtentLabelFormat { get; set; }

    /// <summary>
    /// Gets or sets the time-step label format.
    /// </summary>
    public string? TimeStepIntervalLabelFormat { get; set; }

    /// <summary>
    /// Gets or sets the label display mode.
    /// </summary>
    public TimeSliderLabelMode LabelMode { get; set; }

    /// <summary>
    /// Gets or sets the action invoked when the current extent changes.
    /// </summary>
    public Action<TimeExtentChangedEventArgs>? OnCurrentExtentChanged { get; init; }

    internal Action<TimeSlider>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="FeatureFormView"/> element.
/// </summary>
/// <param name="FeatureForm">The feature form displayed by the control.</param>
public record FeatureFormViewElement(FeatureForm? FeatureForm = null) : Element
{
    /// <summary>
    /// Gets or sets when validation errors are shown.
    /// </summary>
    public ValidationErrorVisibility ErrorsVisibility { get; set; } = ValidationErrorVisibility.Visible;

    /// <summary>
    /// Gets or sets the vertical scrollbar visibility for the form content.
    /// </summary>
    public ScrollBarVisibility VerticalScrollBarVisibility { get; set; } = ScrollBarVisibility.Auto;

    /// <summary>
    /// Gets or sets a value indicating whether internal navigation is enabled.
    /// </summary>
    public bool IsNavigationEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the action invoked when an attachment is clicked.
    /// </summary>
    public Action<FormAttachmentClickedEventArgs>? OnFormAttachmentClicked { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when the barcode button is clicked.
    /// </summary>
    public Action<BarcodeButtonClickedEventArgs>? OnBarcodeButtonClicked { get; init; }

    internal Action<FeatureFormView>[] Setters { get; init; } = [];
}

/// <summary>
/// Represents a declarative <see cref="UtilityNetworkTraceTool"/> element.
/// </summary>
/// <param name="GeoView">The geoview where starting points and results are displayed.</param>
public record UtilityNetworkTraceToolElement(GeoView? GeoView = null) : Element
{
    /// <summary>
    /// Gets or sets a value indicating whether the tool zooms to results automatically.
    /// </summary>
    public bool AutoZoomToTraceResults { get; set; } = true;

    /// <summary>
    /// Gets or sets the symbol used for starting points.
    /// </summary>
    public Symbology.Symbol? StartingPointSymbol { get; set; }

    /// <summary>
    /// Gets or sets the symbol used for point results.
    /// </summary>
    public Symbology.Symbol? ResultPointSymbol { get; set; }

    /// <summary>
    /// Gets or sets the symbol used for line results.
    /// </summary>
    public Symbology.Symbol? ResultLineSymbol { get; set; }

    /// <summary>
    /// Gets or sets the symbol used for polygon results.
    /// </summary>
    public Symbology.Symbol? ResultFillSymbol { get; set; }

    /// <summary>
    /// Gets or sets the action invoked when the selected utility network changes.
    /// </summary>
    public Action<UtilityNetworkChangedEventArgs>? OnUtilityNetworkChanged { get; init; }

    /// <summary>
    /// Gets or sets the action invoked when a trace completes.
    /// </summary>
    public Action<UtilityNetworkTraceCompletedEventArgs>? OnTraceCompleted { get; init; }

    internal Action<UtilityNetworkTraceTool>[] Setters { get; init; } = [];
}
