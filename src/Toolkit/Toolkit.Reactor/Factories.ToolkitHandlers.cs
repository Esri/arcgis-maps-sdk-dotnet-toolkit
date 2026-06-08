using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Core.V1Protocol;
using WinRT;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

/// <summary>
/// Provides factory and fluent extension methods for Reactor elements backed by ArcGIS Maps SDK for .NET controls.
/// </summary>
public static partial class Factories
{
    private sealed class CompassHandler : IElementHandler<CompassElement, Compass>
    {
        public Compass Mount(MountContext ctx, CompassElement element)
        {
            var compass = new Compass
            {
                GeoView = element.GeoView?.Current,
                AutoHide = element.AutoHide,
                Heading = element.Heading,
            };
            ctx.ApplySetters(element.Setters, compass);
            return compass;
        }

        public void Update(UpdateContext ctx, CompassElement oldEl, CompassElement newEl, Compass control)
        {
            if (oldEl.GeoView?.Current != newEl.GeoView?.Current)
            {
                control.GeoView = newEl.GeoView?.Current;
            }
            if (oldEl.AutoHide != newEl.AutoHide)
            {
                control.AutoHide = newEl.AutoHide;
            }

            if (oldEl.Heading != newEl.Heading)
            {
                control.Heading = newEl.Heading;
            }

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
                SelectedBasemap = element.SelectedBasemap,
            };
            var bind = ctx.BindFor(gallery, element);
            bind.OnCustomEvent<UI.BasemapGalleryItem>(
                subscribe: static (c, h) => ((BasemapGallery)c).BasemapSelected += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: (cur, args) => cur.OnBasemapSelected?.Invoke(args));

            ctx.ApplySetters(element.Setters, gallery);
            return gallery;
        }

        public void Update(UpdateContext ctx, BasemapGalleryElement oldEl, BasemapGalleryElement newEl, BasemapGallery control)
        {
            if (oldEl.GeoModel != newEl.GeoModel)
            {
                control.GeoModel = newEl.GeoModel;
            }

            if (oldEl.GalleryViewStyle != newEl.GalleryViewStyle)
            {
                control.GalleryViewStyle = newEl.GalleryViewStyle;
            }

            if (oldEl.SelectedBasemap != newEl.SelectedBasemap)
            {
                control.SelectedBasemap = newEl.SelectedBasemap;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }
    }

    private sealed class BookmarksViewHandler : IElementHandler<BookmarksViewElement, BookmarksView>
    {
        public BookmarksView Mount(MountContext ctx, BookmarksViewElement element)
        {
            var bookmarksView = new BookmarksView
            {
                GeoView = element.GeoView?.Current,
                BookmarksOverride = element.BookmarksOverride,
            };
            var bind = ctx.BindFor(bookmarksView, element);
            bind.OnCustomEvent<Bookmark>(
                subscribe: static (c, h) => ((BookmarksView)c).BookmarkSelected += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: (cur, args) => cur.OnBookmarkSelected?.Invoke(args));

            ctx.ApplySetters(element.Setters, bookmarksView);
            return bookmarksView;
        }

        public void Update(UpdateContext ctx, BookmarksViewElement oldEl, BookmarksViewElement newEl, BookmarksView control)
        {
            if (oldEl.GeoView?.Current != newEl.GeoView?.Current)
            {
                control.GeoView = newEl.GeoView?.Current;
            }

            if (oldEl.BookmarksOverride != newEl.BookmarksOverride)
            {
                control.BookmarksOverride = newEl.BookmarksOverride;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }
    }

    private sealed class FeatureDataFieldHandler : IElementHandler<FeatureDataFieldElement, FeatureDataField>
    {
        public FeatureDataField Mount(MountContext ctx, FeatureDataFieldElement element)
        {
            var field = new FeatureDataField
            {
                Feature = element.Feature,
                FieldName = element.FieldName,
                IsReadOnly = element.IsReadOnly,
            };
            if (element.BindingValue is not null)
            {
                field.BindingValue = element.BindingValue;
            }

            var bind = ctx.BindFor(field, element);
            bind.OnCustomEvent<AttributeValueChangedEventArgs>(
                subscribe: static (c, h) => ((FeatureDataField)c).ValueChanging += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, args) => cur.OnValueChanging?.Invoke(args));
            bind.OnCustomEvent<AttributeValueChangedEventArgs>(
                subscribe: static (c, h) => ((FeatureDataField)c).ValueChanged += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, args) => cur.OnValueChanged?.Invoke(args));

            ctx.ApplySetters(element.Setters, field);
            return field;
        }

        public void Update(UpdateContext ctx, FeatureDataFieldElement oldEl, FeatureDataFieldElement newEl, FeatureDataField control)
        {
            if (oldEl.Feature != newEl.Feature)
            {
                control.Feature = newEl.Feature;
            }

            if (oldEl.FieldName != newEl.FieldName)
            {
                control.FieldName = newEl.FieldName;
            }

            if (oldEl.IsReadOnly != newEl.IsReadOnly)
            {
                control.IsReadOnly = newEl.IsReadOnly;
            }

            if (oldEl.BindingValue != newEl.BindingValue && newEl.BindingValue is not null)
            {
                control.BindingValue = newEl.BindingValue;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }
    }

    private sealed class FloorFilterHandler : IElementHandler<FloorFilterElement, FloorFilter>
    {
        public FloorFilter Mount(MountContext ctx, FloorFilterElement element)
        {
            var floorFilter = new FloorFilter
            {
                GeoView = element.GeoView?.Current,
                IsBrowseOpen = element.IsBrowseOpen,
            };
            ctx.ApplySetters(element.Setters, floorFilter);
            return floorFilter;
        }

        public void Update(UpdateContext ctx, FloorFilterElement oldEl, FloorFilterElement newEl, FloorFilter control)
        {
            if (oldEl.GeoView?.Current != newEl.GeoView?.Current)
            {
                control.GeoView = newEl.GeoView?.Current;
            }

            if (oldEl.IsBrowseOpen != newEl.IsBrowseOpen)
            {
                control.IsBrowseOpen = newEl.IsBrowseOpen;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }
    }

    private sealed class LegendHandler : IElementHandler<LegendElement, Legend>
    {
        public Legend Mount(MountContext ctx, LegendElement element)
        {
            var legend = new Legend
            {
                GeoView = element.GeoView?.Current,
                FilterByVisibleScaleRange = element.FilterByVisibleScaleRange,
                FilterHiddenLayers = element.FilterHiddenLayers,
                ReverseLayerOrder = element.ReverseLayerOrder,
            };
            ctx.ApplySetters(element.Setters, legend);
            return legend;
        }

        public void Update(UpdateContext ctx, LegendElement oldEl, LegendElement newEl, Legend control)
        {
            if (oldEl.GeoView?.Current != newEl.GeoView?.Current)
            {
                control.GeoView = newEl.GeoView?.Current;
            }

            if (oldEl.FilterByVisibleScaleRange != newEl.FilterByVisibleScaleRange)
            {
                control.FilterByVisibleScaleRange = newEl.FilterByVisibleScaleRange;
            }

            if (oldEl.FilterHiddenLayers != newEl.FilterHiddenLayers)
            {
                control.FilterHiddenLayers = newEl.FilterHiddenLayers;
            }

            if (oldEl.ReverseLayerOrder != newEl.ReverseLayerOrder)
            {
                control.ReverseLayerOrder = newEl.ReverseLayerOrder;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }
    }

    private sealed class MeasureToolbarHandler : IElementHandler<MeasureToolbarElement, MeasureToolbar>
    {
        public MeasureToolbar Mount(MountContext ctx, MeasureToolbarElement element)
        {
            var toolbar = new MeasureToolbar
            {
                MapView = element.MapView?.Current,
            };
            ctx.ApplySetters(element.Setters, toolbar);
            return toolbar;
        }

        public void Update(UpdateContext ctx, MeasureToolbarElement oldEl, MeasureToolbarElement newEl, MeasureToolbar control)
        {
            if (oldEl.MapView?.Current != newEl.MapView?.Current)
            {
                control.MapView = newEl.MapView?.Current;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }
    }

    private sealed class OverviewMapHandler : IElementHandler<OverviewMapElement, OverviewMap>
    {
        public OverviewMap Mount(MountContext ctx, OverviewMapElement element)
        {
            var overviewMap = new OverviewMap
            {
                GeoView = element.GeoView?.Current,
                ScaleFactor = element.ScaleFactor,
            };
            if (element.Map is not null)
            {
                overviewMap.Map = element.Map;
            }

            if (element.AreaSymbol is not null)
            {
                overviewMap.AreaSymbol = element.AreaSymbol;
            }

            if (element.PointSymbol is not null)
            {
                overviewMap.PointSymbol = element.PointSymbol;
            }

            ctx.ApplySetters(element.Setters, overviewMap);
            return overviewMap;
        }

        public void Update(UpdateContext ctx, OverviewMapElement oldEl, OverviewMapElement newEl, OverviewMap control)
        {
            if (oldEl.GeoView?.Current != newEl.GeoView?.Current)
            {
                control.GeoView = newEl.GeoView?.Current;
            }

            if (oldEl.Map != newEl.Map && newEl.Map is not null)
            {
                control.Map = newEl.Map;
            }

            if (oldEl.AreaSymbol != newEl.AreaSymbol && newEl.AreaSymbol is not null)
            {
                control.AreaSymbol = newEl.AreaSymbol;
            }

            if (oldEl.PointSymbol != newEl.PointSymbol && newEl.PointSymbol is not null)
            {
                control.PointSymbol = newEl.PointSymbol;
            }

            if (oldEl.ScaleFactor != newEl.ScaleFactor)
            {
                control.ScaleFactor = newEl.ScaleFactor;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }
    }

    private sealed class PopupViewerHandler : IElementHandler<PopupViewerElement, PopupViewer>
    {
        public PopupViewer Mount(MountContext ctx, PopupViewerElement element)
        {
            var popupViewer = new PopupViewer
            {
                Popup = element.Popup,
                VerticalScrollBarVisibility = element.VerticalScrollBarVisibility,
            };
            var bind = ctx.BindFor(popupViewer, element);
            bind.OnCustomEvent<PopupAttachmentClickedEventArgs>(
                subscribe: static (c, h) => ((PopupViewer)c).PopupAttachmentClicked += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: (cur, args) => cur.OnPopupAttachmentClicked?.Invoke(args));
            bind.OnCustomEvent<HyperlinkClickedEventArgs>(
                subscribe: static (c, h) => ((PopupViewer)c).HyperlinkClicked += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: (cur, args) => cur.OnHyperlinkClicked?.Invoke(args));

            ctx.ApplySetters(element.Setters, popupViewer);
            return popupViewer;
        }

        public void Update(UpdateContext ctx, PopupViewerElement oldEl, PopupViewerElement newEl, PopupViewer control)
        {
            if (oldEl.Popup != newEl.Popup)
            {
                control.Popup = newEl.Popup;
            }

            if (oldEl.VerticalScrollBarVisibility != newEl.VerticalScrollBarVisibility)
            {
                control.VerticalScrollBarVisibility = newEl.VerticalScrollBarVisibility;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }
    }

    private sealed class ScaleLineHandler : IElementHandler<ScaleLineElement, ScaleLine>
    {
        public ScaleLine Mount(MountContext ctx, ScaleLineElement element)
        {
            var scaleLine = new ScaleLine
            {
                MapView = element.MapView?.Current,
            };

            if (!double.IsNaN(element.MapScale) || element.MapView is null)
            {
                scaleLine.MapScale = element.MapScale;
            }

            if (element.TargetWidth > 0)
            {
                scaleLine.TargetWidth = element.TargetWidth;
            }

            ctx.ApplySetters(element.Setters, scaleLine);
            return scaleLine;
        }

        public void Update(UpdateContext ctx, ScaleLineElement oldEl, ScaleLineElement newEl, ScaleLine control)
        {
            if (oldEl.MapView?.Current != newEl.MapView?.Current)
            {
                control.MapView = newEl.MapView?.Current;
            }

            if (oldEl.MapScale != newEl.MapScale && (!double.IsNaN(newEl.MapScale) || control.MapView is null))
            {
                control.MapScale = newEl.MapScale;
            }

            if (oldEl.TargetWidth != newEl.TargetWidth && newEl.TargetWidth > 0)
            {
                control.TargetWidth = newEl.TargetWidth;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }
    }

    private sealed class SearchViewHandler : IElementHandler<SearchViewElement, SearchView>
    {
        public SearchView Mount(MountContext ctx, SearchViewElement element)
        {
            var searchView = new SearchView
            {
                GeoView = element.GeoView?.Current,
                EnableDefaultWorldGeocoder = element.EnableDefaultWorldGeocoder,
                EnableRepeatSearchHereButton = element.EnableRepeatSearchHereButton,
                EnableIndividualResultDisplay = element.EnableIndividualResultDisplay,
                EnableResultListView = element.EnableResultListView,
                MultipleResultZoomBuffer = element.MultipleResultZoomBuffer,
            };
            if (element.SearchViewModel is not null)
            {
                searchView.SearchViewModel = element.SearchViewModel;
            }

            if (element.NoResultMessage is not null)
            {
                searchView.NoResultMessage = element.NoResultMessage;
            }

            if (element.AllSourceSelectText is not null)
            {
                searchView.AllSourceSelectText = element.AllSourceSelectText;
            }

            if (element.ClearSearchTooltipText is not null)
            {
                searchView.ClearSearchTooltipText = element.ClearSearchTooltipText;
            }

            if (element.SearchTooltipText is not null)
            {
                searchView.SearchTooltipText = element.SearchTooltipText;
            }

            if (element.RepeatSearchButtonText is not null)
            {
                searchView.RepeatSearchButtonText = element.RepeatSearchButtonText;
            }

            ctx.ApplySetters(element.Setters, searchView);
            return searchView;
        }

        public void Update(UpdateContext ctx, SearchViewElement oldEl, SearchViewElement newEl, SearchView control)
        {
            if (oldEl.GeoView?.Current != newEl.GeoView?.Current)
            {
                control.GeoView = newEl.GeoView?.Current;
            }

            if (oldEl.SearchViewModel != newEl.SearchViewModel && newEl.SearchViewModel is not null)
            {
                control.SearchViewModel = newEl.SearchViewModel;
            }

            if (oldEl.EnableDefaultWorldGeocoder != newEl.EnableDefaultWorldGeocoder)
            {
                control.EnableDefaultWorldGeocoder = newEl.EnableDefaultWorldGeocoder;
            }

            if (oldEl.EnableRepeatSearchHereButton != newEl.EnableRepeatSearchHereButton)
            {
                control.EnableRepeatSearchHereButton = newEl.EnableRepeatSearchHereButton;
            }

            if (oldEl.EnableIndividualResultDisplay != newEl.EnableIndividualResultDisplay)
            {
                control.EnableIndividualResultDisplay = newEl.EnableIndividualResultDisplay;
            }

            if (oldEl.EnableResultListView != newEl.EnableResultListView)
            {
                control.EnableResultListView = newEl.EnableResultListView;
            }

            if (oldEl.MultipleResultZoomBuffer != newEl.MultipleResultZoomBuffer)
            {
                control.MultipleResultZoomBuffer = newEl.MultipleResultZoomBuffer;
            }

            if (oldEl.NoResultMessage != newEl.NoResultMessage && newEl.NoResultMessage is not null)
            {
                control.NoResultMessage = newEl.NoResultMessage;
            }

            if (oldEl.AllSourceSelectText != newEl.AllSourceSelectText && newEl.AllSourceSelectText is not null)
            {
                control.AllSourceSelectText = newEl.AllSourceSelectText;
            }

            if (oldEl.ClearSearchTooltipText != newEl.ClearSearchTooltipText && newEl.ClearSearchTooltipText is not null)
            {
                control.ClearSearchTooltipText = newEl.ClearSearchTooltipText;
            }

            if (oldEl.SearchTooltipText != newEl.SearchTooltipText && newEl.SearchTooltipText is not null)
            {
                control.SearchTooltipText = newEl.SearchTooltipText;
            }

            if (oldEl.RepeatSearchButtonText != newEl.RepeatSearchButtonText && newEl.RepeatSearchButtonText is not null)
            {
                control.RepeatSearchButtonText = newEl.RepeatSearchButtonText;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }
    }

    private sealed class SymbolDisplayHandler : IElementHandler<SymbolDisplayElement, SymbolDisplay>
    {
        public SymbolDisplay Mount(MountContext ctx, SymbolDisplayElement element)
        {
            var symbolDisplay = new SymbolDisplay
            {
                Symbol = element.Symbol,
            };
            ctx.ApplySetters(element.Setters, symbolDisplay);
            return symbolDisplay;
        }

        public void Update(UpdateContext ctx, SymbolDisplayElement oldEl, SymbolDisplayElement newEl, SymbolDisplay control)
        {
            if (oldEl.Symbol != newEl.Symbol)
            {
                control.Symbol = newEl.Symbol;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }
    }

    private sealed class TimeSliderHandler : IElementHandler<TimeSliderElement, TimeSlider>
    {
        public TimeSlider Mount(MountContext ctx, TimeSliderElement element)
        {
            var timeSlider = new TimeSlider
            {
                FullExtent = element.FullExtent,
                TimeStepInterval = element.TimeStepInterval,
                CurrentExtent = element.CurrentExtent,
                PlaybackDirection = element.PlaybackDirection,
                PlaybackLoopMode = element.PlaybackLoopMode,
                IsStartTimePinned = element.IsStartTimePinned,
                IsEndTimePinned = element.IsEndTimePinned,
                IsPlaying = element.IsPlaying,
                LabelMode = element.LabelMode,
            };
            if (element.FullExtentLabelFormat is not null)
            {
                timeSlider.FullExtentLabelFormat = element.FullExtentLabelFormat;
            }

            if (element.CurrentExtentLabelFormat is not null)
            {
                timeSlider.CurrentExtentLabelFormat = element.CurrentExtentLabelFormat;
            }

            if (element.TimeStepIntervalLabelFormat is not null)
            {
                timeSlider.TimeStepIntervalLabelFormat = element.TimeStepIntervalLabelFormat;
            }

            var bind = ctx.BindFor(timeSlider, element);
            bind.OnCustomEvent<TimeExtentChangedEventArgs>(
                subscribe: static (c, h) => ((TimeSlider)c).CurrentExtentChanged += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: static (cur, args) => cur.OnCurrentExtentChanged?.Invoke(args));

            ctx.ApplySetters(element.Setters, timeSlider);
            return timeSlider;
        }

        public void Update(UpdateContext ctx, TimeSliderElement oldEl, TimeSliderElement newEl, TimeSlider control)
        {
            if (oldEl.FullExtent != newEl.FullExtent)
            {
                control.FullExtent = newEl.FullExtent;
            }

            if (oldEl.TimeStepInterval != newEl.TimeStepInterval)
            {
                control.TimeStepInterval = newEl.TimeStepInterval;
            }

            if (oldEl.CurrentExtent != newEl.CurrentExtent)
            {
                control.CurrentExtent = newEl.CurrentExtent;
            }

            if (oldEl.PlaybackDirection != newEl.PlaybackDirection)
            {
                control.PlaybackDirection = newEl.PlaybackDirection;
            }

            if (oldEl.PlaybackLoopMode != newEl.PlaybackLoopMode)
            {
                control.PlaybackLoopMode = newEl.PlaybackLoopMode;
            }

            if (oldEl.IsStartTimePinned != newEl.IsStartTimePinned)
            {
                control.IsStartTimePinned = newEl.IsStartTimePinned;
            }

            if (oldEl.IsEndTimePinned != newEl.IsEndTimePinned)
            {
                control.IsEndTimePinned = newEl.IsEndTimePinned;
            }

            if (oldEl.IsPlaying != newEl.IsPlaying)
            {
                control.IsPlaying = newEl.IsPlaying;
            }

            if (oldEl.LabelMode != newEl.LabelMode)
            {
                control.LabelMode = newEl.LabelMode;
            }

            if (oldEl.FullExtentLabelFormat != newEl.FullExtentLabelFormat && newEl.FullExtentLabelFormat is not null)
            {
                control.FullExtentLabelFormat = newEl.FullExtentLabelFormat;
            }

            if (oldEl.CurrentExtentLabelFormat != newEl.CurrentExtentLabelFormat && newEl.CurrentExtentLabelFormat is not null)
            {
                control.CurrentExtentLabelFormat = newEl.CurrentExtentLabelFormat;
            }

            if (oldEl.TimeStepIntervalLabelFormat != newEl.TimeStepIntervalLabelFormat && newEl.TimeStepIntervalLabelFormat is not null)
            {
                control.TimeStepIntervalLabelFormat = newEl.TimeStepIntervalLabelFormat;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }
    }

    private sealed class FeatureFormViewHandler : IElementHandler<FeatureFormViewElement, FeatureFormView>
    {
        public FeatureFormView Mount(MountContext ctx, FeatureFormViewElement element)
        {
            var featureFormView = new FeatureFormView
            {
                FeatureForm = element.FeatureForm,
                ErrorsVisibility = element.ErrorsVisibility,
                VerticalScrollBarVisibility = element.VerticalScrollBarVisibility,
                IsNavigationEnabled = element.IsNavigationEnabled,
            };
            var bind = ctx.BindFor(featureFormView, element);
            bind.OnCustomEvent<FormAttachmentClickedEventArgs>(
                subscribe: static (c, h) => ((FeatureFormView)c).FormAttachmentClicked += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: (cur, args) => cur.OnFormAttachmentClicked?.Invoke(args));
            bind.OnCustomEvent<BarcodeButtonClickedEventArgs>(
                subscribe: static (c, h) => ((FeatureFormView)c).BarcodeButtonClicked += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: (cur, args) => cur.OnBarcodeButtonClicked?.Invoke(args));

            ctx.ApplySetters(element.Setters, featureFormView);
            return featureFormView;
        }

        public void Update(UpdateContext ctx, FeatureFormViewElement oldEl, FeatureFormViewElement newEl, FeatureFormView control)
        {
            if (oldEl.FeatureForm != newEl.FeatureForm)
            {
                control.FeatureForm = newEl.FeatureForm;
            }

            if (oldEl.ErrorsVisibility != newEl.ErrorsVisibility)
            {
                control.ErrorsVisibility = newEl.ErrorsVisibility;
            }

            if (oldEl.VerticalScrollBarVisibility != newEl.VerticalScrollBarVisibility)
            {
                control.VerticalScrollBarVisibility = newEl.VerticalScrollBarVisibility;
            }

            if (oldEl.IsNavigationEnabled != newEl.IsNavigationEnabled)
            {
                control.IsNavigationEnabled = newEl.IsNavigationEnabled;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }
    }

    private sealed class UtilityNetworkTraceToolHandler : IElementHandler<UtilityNetworkTraceToolElement, UtilityNetworkTraceTool>
    {
        public UtilityNetworkTraceTool Mount(MountContext ctx, UtilityNetworkTraceToolElement element)
        {
            var traceTool = new UtilityNetworkTraceTool
            {
                GeoView = element.GeoView?.Current,
                AutoZoomToTraceResults = element.AutoZoomToTraceResults,
            };
            if (element.StartingPointSymbol is not null)
            {
                traceTool.StartingPointSymbol = element.StartingPointSymbol;
            }

            if (element.ResultPointSymbol is not null)
            {
                traceTool.ResultPointSymbol = element.ResultPointSymbol;
            }

            if (element.ResultLineSymbol is not null)
            {
                traceTool.ResultLineSymbol = element.ResultLineSymbol;
            }

            if (element.ResultFillSymbol is not null)
            {
                traceTool.ResultFillSymbol = element.ResultFillSymbol;
            }

            var bind = ctx.BindFor(traceTool, element);
            bind.OnCustomEvent<UtilityNetworkChangedEventArgs>(
                subscribe: static (c, h) => ((UtilityNetworkTraceTool)c).UtilityNetworkChanged += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: (cur, args) => cur.OnUtilityNetworkChanged?.Invoke(args));
            bind.OnCustomEvent<UtilityNetworkTraceCompletedEventArgs>(
                subscribe: static (c, h) => ((UtilityNetworkTraceTool)c).UtilityNetworkTraceCompleted += (sender, args) => h(sender, args),
                unsubscribe: static (_, _) => { },
                handler: (cur, args) => cur.OnTraceCompleted?.Invoke(args));

            ctx.ApplySetters(element.Setters, traceTool);
            return traceTool;
        }

        public void Update(UpdateContext ctx, UtilityNetworkTraceToolElement oldEl, UtilityNetworkTraceToolElement newEl, UtilityNetworkTraceTool control)
        {
            if (oldEl.GeoView?.Current != newEl.GeoView?.Current)
            {
                control.GeoView = newEl.GeoView?.Current;
            }

            if (oldEl.AutoZoomToTraceResults != newEl.AutoZoomToTraceResults)
            {
                control.AutoZoomToTraceResults = newEl.AutoZoomToTraceResults;
            }

            if (oldEl.StartingPointSymbol != newEl.StartingPointSymbol && newEl.StartingPointSymbol is not null)
            {
                control.StartingPointSymbol = newEl.StartingPointSymbol;
            }

            if (oldEl.ResultPointSymbol != newEl.ResultPointSymbol && newEl.ResultPointSymbol is not null)
            {
                control.ResultPointSymbol = newEl.ResultPointSymbol;
            }

            if (oldEl.ResultLineSymbol != newEl.ResultLineSymbol && newEl.ResultLineSymbol is not null)
            {
                control.ResultLineSymbol = newEl.ResultLineSymbol;
            }

            if (oldEl.ResultFillSymbol != newEl.ResultFillSymbol && newEl.ResultFillSymbol is not null)
            {
                control.ResultFillSymbol = newEl.ResultFillSymbol;
            }

            ctx.ApplySetters(newEl.Setters, control);
        }
    }
}
