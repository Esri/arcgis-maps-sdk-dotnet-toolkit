using Esri.ArcGISRuntime.Toolkit;
using Microsoft.UI.Reactor.Hooks;

namespace Toolkit.SampleApp.Reactor.Samples.Toolkit;

public sealed class OfflineMapAreasViewPage : Component
{
    private readonly MapAreaOption[] maps =
    [
        new("Naperville - Preplanned", new Map(new Uri("https://www.arcgis.com/home/item.html?id=acc027394bc84c2fb04d1ed317aac674"))),
        new("US Breweries - On-Demand", new Map(new Uri("https://www.arcgis.com/home/item.html?id=3da658f2492f4cfd8494970ef489d2c5"))),
        new("Naperville - Offline Disabled", new Map(new Uri("https://www.arcgis.com/home/item.html?id=b95fe18073bc4f7788f0375af2bb445e"))),
        new("No Map", null),
    ];

    public override Element Render()
    {
        var (selectedOnlineMapIndex, setSelectedOnlineMapIndex) = UseState(0);
        var (selectedOfflineMapIndex, setSelectedOfflineMapIndex) = UseState(-1);
        var (selectedMap, setSelectedMap) = UseState(maps[0].Map);
        var (_, setOfflineInfosVersion) = UseState(0L);

        var offlineMapInfos = OfflineManager.Shared.OfflineMapInfos.ToArray();
        var selectedOnlineMap = selectedOnlineMapIndex >= 0 && selectedOnlineMapIndex < maps.Length
            ? maps[selectedOnlineMapIndex].Map
            : null;
        var selectedOfflineMapInfo = selectedOfflineMapIndex >= 0 && selectedOfflineMapIndex < offlineMapInfos.Length
            ? offlineMapInfos[selectedOfflineMapIndex]
            : null;

        return Grid(
            columns: [GridSize.Star(), GridSize.Px(400)],
            rows: [GridSize.Star()],

            MapView(selectedMap),

            Border(
                Grid(
                    columns: [GridSize.Star()],
                    rows: [GridSize.Auto, GridSize.Auto, GridSize.Star(), GridSize.Auto],

                    Border(
                        VStack(
                            12,
                            ComboBox(
                                maps.Select(map => map.Name).ToArray(),
                                selectedIndex: selectedOnlineMapIndex,
                                onSelectedIndexChanged: index =>
                                {
                                    if (index < 0 || index >= maps.Length)
                                    {
                                        return;
                                    }

                                    setSelectedOnlineMapIndex(index);
                                    setSelectedOfflineMapIndex(-1);
                                    setSelectedMap(maps[index].Map);
                                })
                            .Header("Select Online Map")
                            .Width(320),

                            ComboBox(
                                offlineMapInfos.Select(info => info.Title ?? info.Id).ToArray(),
                                selectedIndex: selectedOfflineMapIndex,
                                onSelectedIndexChanged: index =>
                                {
                                    if (index < 0 || index >= offlineMapInfos.Length)
                                    {
                                        return;
                                    }

                                    setSelectedOfflineMapIndex(index);
                                    setSelectedOnlineMapIndex(-1);
                                })
                            .Header("Select Offline Map Info")
                            .Width(320))
                    )
                    .Padding(16)
                    .Background(Theme.CardBackground)
                    .WithBorder(Theme.CardStroke)
                    .CornerRadius(12),

                    TextBlock("Map Areas")
                        .ApplyStyle("SubtitleTextBlockStyle")
                        .HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Center)
                        .Margin(0, 16, 0, 12)
                        .Grid(row: 1),

                    (OfflineMapAreasView(selectedOnlineMap, selectedOfflineMapInfo) with
                    {
                        OnSelectedMapChanged = map => setSelectedMap(map),
                    })
                    .Grid(row: 2),

                    Button(
                        "Go Online",
                        () => setSelectedMap(selectedOnlineMap))
                    .IsEnabled(selectedOnlineMap is not null && selectedMap != selectedOnlineMap)
                    .HorizontalAlignment(Microsoft.UI.Xaml.HorizontalAlignment.Stretch)
                    .Margin(0, 12, 0, 0)
                    .Grid(row: 3))
                .Padding(16)
            )
            .Margin(12)
            .Background(Theme.SubtleFill)
            .WithBorder(Theme.CardStroke)
            .CornerRadius(12)
            .Grid(column: 1)
            .OnMount(_ =>
            {
                if (OfflineManager.Shared.OfflineMapInfos is System.Collections.Specialized.INotifyCollectionChanged offlineMapInfosCollection)
                {
                    offlineMapInfosCollection.CollectionChanged += (_, _) =>
                        setOfflineInfosVersion(DateTime.UtcNow.Ticks);
                }
            })
        );
    }

    private sealed record MapAreaOption(string Name, Map? Map);
}
