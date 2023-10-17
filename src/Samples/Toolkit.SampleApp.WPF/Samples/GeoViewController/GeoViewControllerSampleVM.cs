#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.GeoViewController;
public partial class GeoViewControllerSampleVM
{
    public Map Map { get; } = new Map(new Uri("https://www.arcgis.com/home/item.html?id=9f3a674e998f461580006e626611f9ad"));

    public IMyMapViewController Controller { get; } = new MyMapViewController();

    [RelayCommand]
    public async Task OnGeoViewTapped(GeoViewInputEventArgs eventArgs) => await Identify(eventArgs.Position, eventArgs.Location);
    
    public async Task Identify(Point location, MapPoint? mapLocation)
    {
        Controller.DismissCallout();
        var result = await Controller.IdentifyLayersAsync(location, 10);
        if (result.FirstOrDefault()?.GeoElements?.FirstOrDefault() is GeoElement element)
        {
            Controller.ShowCalloutForGeoElement(element, location, new Esri.ArcGISRuntime.UI.CalloutDefinition(element));
            _ = Controller.PanToAsync(mapLocation);
        }
        else if (mapLocation is not null)
        {
            Controller.ShowCalloutAt(mapLocation, new Esri.ArcGISRuntime.UI.CalloutDefinition("No features found"));
        }
    }
}

// Custom controller that extends the toolkit controller
public class MyMapViewController : UI.GeoViewController, IMyMapViewController
{
    public MapView? ConnectedMapView => ConnectedView as MapView;

    public Task PanToAsync(MapPoint? center)
    {
        if (center is null)
            return Task.FromResult(false);
        return ConnectedMapView?.SetViewpointCenterAsync(center) ?? Task.FromResult(false);
    }

    public MapPoint? ScreenToLocation(Point screenLocation) => ConnectedMapView?.ScreenToLocation(screenLocation);
}

// Custom interface for testability of VM
public interface IMyMapViewController
{
    void DismissCallout();
    void ShowCalloutForGeoElement(GeoElement element, Point tapPosition, ArcGISRuntime.UI.CalloutDefinition definition);
    void ShowCalloutAt(MapPoint location, ArcGISRuntime.UI.CalloutDefinition definition);
    Task<IReadOnlyList<IdentifyLayerResult>> IdentifyLayersAsync(Point screenPoint, double tolerance, bool returnPopupsOnly = false, CancellationToken cancellationToken = default);
    MapPoint? ScreenToLocation(Point screenLocation);
    Task PanToAsync(MapPoint? center);
}