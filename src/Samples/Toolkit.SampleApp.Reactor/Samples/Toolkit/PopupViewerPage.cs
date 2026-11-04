using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI.Controls;
using PopupViewerControl = Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer;
using MapViewControl = Esri.ArcGISRuntime.UI.Controls.MapView;
using PopupAttachmentClickedEventArgs = Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupAttachmentClickedEventArgs;
using Microsoft.UI.Reactor.Hooks;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class PopupViewerPage : Component
{
    private readonly Map map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=9f3a674e998f461580006e626611f9ad"));

    public override Element Render()
    {
        var mapViewRef = this.UseElementRef<MapViewControl>();
        var (popup, setPopup) = UseState<Popup?>(null);

        return Grid(columns: [GridSize.Star(), GridSize.Px(360)], rows: [GridSize.Star()],
            MapView(
                map,
                async args =>
                {
                    if (mapViewRef.Current is null)
                    {
                        return;
                    }

                    var identifyResults = await mapViewRef.Current.IdentifyLayersAsync(args.Position, 3, false);
                    setPopup(GetPopup(identifyResults));
                })
                .Ref(mapViewRef),

            Border(
                popup is null
                    ? Caption("Tap a feature with popups enabled to open the popup viewer.")
                    : PopupViewer(popup) with
                    {
                        OnPopupAttachmentClicked = HandlePopupAttachmentClicked,
                    })
            .Padding(16)
            .Background(Theme.CardBackground)
            .WithBorder(Theme.CardStroke)
            .CornerRadius(12)
            .Margin(12)
            .Grid(column: 1)
        );
    }

    private static Popup? GetPopup(IEnumerable<IdentifyLayerResult> results)
    {
        foreach (var result in results)
        {
            var popup = GetPopup(result);
            if (popup is not null)
            {
                return popup;
            }
        }

        return null;
    }

    private static Popup? GetPopup(IdentifyLayerResult result)
    {
        var popup = result.Popups.FirstOrDefault();
        if (popup is not null)
        {
            return popup;
        }

        var geoElement = result.GeoElements.FirstOrDefault();
        if (geoElement is not null)
        {
            if (result.LayerContent is IPopupSource popupSource && popupSource.PopupDefinition is PopupDefinition definition)
            {
                return new Popup(geoElement, definition);
            }

            return Esri.ArcGISRuntime.Mapping.Popups.Popup.FromGeoElement(geoElement);
        }

        foreach (var sublayerResult in result.SublayerResults)
        {
            var subPopup = GetPopup(sublayerResult);
            if (subPopup is not null)
            {
                return subPopup;
            }
        }

        return null;
    }

    private static async void HandlePopupAttachmentClicked(PopupAttachmentClickedEventArgs args)
    {
        if (args.Attachment.IsLocal)
        {
            return;
        }

        try
        {
            if (args.Attachment.LoadStatus == LoadStatus.NotLoaded)
            {
                args.Handled = true;
                await args.Attachment.LoadAsync();
            }
            else if (args.Attachment.LoadStatus == LoadStatus.FailedToLoad)
            {
                args.Handled = true;
                await args.Attachment.RetryLoadAsync();
            }
            else if (args.Attachment.LoadStatus == LoadStatus.Loading)
            {
                args.Handled = true;
                args.Attachment.CancelLoad();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
