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

public partial class MapViewControllerSampleVM : ObservableObject
{
    [ObservableProperty]
    private bool isPopupOpen;

    [ObservableProperty]
    private string? popupText;

    public Map Map { get; } = new Map(BasemapStyle.OpenStreets);

    public MyMapViewController Controller { get; } = new MyMapViewController();

    [RelayCommand]
    public void OnToggleGeometryEditor()
    {
        if (Controller.GeometryEditor?.Stop() is null)
        {
            Controller.GeometryEditor?.Start(GeometryType.Polygon);
            if (Controller.GeometryEditor?.SnapSettings is SnapSettings snapSettings)
            {
                snapSettings.IsEnabled = true;
                snapSettings.Tolerance = 10;
                snapSettings.SyncSourceSettings();
            }
        }
    }

    [RelayCommand]
    public async Task OnMapViewTapped(GeoViewInputEventArgs eventArgs) => await Identify(eventArgs.Position, eventArgs.Location);

    public async Task Identify(Point location, MapPoint? mapLocation)
    {
        try
        {
            var result = await Controller.IdentifyGeometryEditorAsync(location, 10);

            if (result is IdentifyGeometryEditorResult editorResult)
            {
                var summary = $"Selected {result.Elements.Count} element(s):\n" +
                              string.Join("\n", result.Elements.Select(e => e.GetType().Name));
                if (Controller.GeometryEditor is { IsStarted: true })
                {
                    summary += "\n\nGeometry Editor is active.";
                }

                PopupText = summary;
                IsPopupOpen = true;
            }
            else if (mapLocation is not null)
            {
                PopupText = "No Geometry found";
                IsPopupOpen = true;
            }
        }
        catch (ArcGISException ex)
        {
            PopupText = $"Failed to identify Geometry\n{ex.Message}";
            IsPopupOpen = true;
        }
    }
}

public class MyMapViewController : UI.MapViewController
{
    public GeometryEditor? GeometryEditor => ConnectedView?.GeometryEditor;
}