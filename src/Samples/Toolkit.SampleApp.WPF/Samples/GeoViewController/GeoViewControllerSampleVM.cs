using System;
using System.Linq;
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

    public UI.GeoViewController Controller { get; } = new UI.GeoViewController();

    [RelayCommand]
    public async Task OnGeoViewTapped(GeoViewInputEventArgs eventArgs) => await Identify(eventArgs.Position, eventArgs.Location);

    public async Task Identify(Point location, MapPoint? mapLocation)
    {
        Controller.DismissCallout();
        var result = await Controller.IdentifyLayersAsync(location, 10);
        if (result.FirstOrDefault()?.GeoElements?.FirstOrDefault() is GeoElement element)
        {
            Controller.ShowCalloutForGeoElement(element, location, new Esri.ArcGISRuntime.UI.CalloutDefinition(element));
        }
        else if (mapLocation is not null)
        {
            Controller.ShowCalloutAt(mapLocation, new Esri.ArcGISRuntime.UI.CalloutDefinition("No features found"));
        }
    }
}