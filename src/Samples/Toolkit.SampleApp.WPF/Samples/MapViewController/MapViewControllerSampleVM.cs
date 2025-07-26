#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UI.Editing;

namespace Esri.ArcGISRuntime.Toolkit.Samples.MapViewController;

public partial class MapViewControllerSampleVM
{
    public Map Map { get; } = new Map(BasemapStyle.OpenStreets);

    public MyMapViewController Controller { get; } = new MyMapViewController();

    [RelayCommand]
    public async Task OnGeoViewTapped(GeoViewInputEventArgs eventArgs) => await Identify(eventArgs.Position, eventArgs.Location);

    [RelayCommand]
    public void OnToggleGeometryEditor()
    {
        if (Controller.GeometryEditor?.Stop() is null)
        {
            Controller.GeometryEditor?.Start(GeometryType.Point);
            if (Controller.GeometryEditor?.SnapSettings is SnapSettings snapSettings)
            {
                snapSettings.IsEnabled = true;
            }
        }
    }

    public async Task Identify(Point location, MapPoint? mapLocation)
    {
        try
        {
            var result = await Controller.IdentifyGeometryEditorAsync(location, 10);

            if (result is IdentifyGeometryEditorResult editorResult)
            {
                var summary = $"Selected {result.Elements.Count} element(s):\n" +
                              string.Join("\n", result.Elements.Select(e => e.GetType().Name));

                MapPoint? calloutLocation = null;
                if (result.Elements.FirstOrDefault() is GeometryEditorElement firstElem)
                    calloutLocation = firstElem switch
                    {
                        GeometryEditorVertex pt => pt.Point,
                        GeometryEditorPart part => part.Part.StartPoint,
                        GeometryEditorMidVertex midVertex => midVertex.Point,
                        GeometryEditorGeometry geometry => geometry.Geometry?.Extent?.GetCenter(),
                        _ => null
                    };

                calloutLocation ??= mapLocation;

                if (calloutLocation != null)
                    Controller.ShowCalloutAt(calloutLocation, new Esri.ArcGISRuntime.UI.CalloutDefinition(summary));
            }
            else if (mapLocation is not null)
            {
                Controller.ShowCalloutAt(mapLocation, new Esri.ArcGISRuntime.UI.CalloutDefinition("No Geometry found"));
            }
        }
        catch (ArcGISException ex)
        {
            Controller.ShowCalloutAt(mapLocation, new Esri.ArcGISRuntime.UI.CalloutDefinition("Failed to identfy Geometry", ex.Message));
        }
    }
}

public class MyMapViewController : UI.MapViewController
{
    public GeometryEditor? GeometryEditor => ConnectedView?.GeometryEditor;
}