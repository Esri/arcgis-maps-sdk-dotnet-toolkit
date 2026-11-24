using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

public static partial class Factories
{
    /// <summary>
    /// Creates a declarative <see cref="MapViewElement"/>.
    /// </summary>
    /// <param name="map">The map displayed by the view.</param>
    /// <param name="onTapped">The action invoked when the underlying <see cref="MapView"/> is tapped.</param>
    /// <returns>A new <see cref="MapViewElement"/> instance.</returns>
    public static MapViewElement MapView(Map? map = null, Action<GeoViewInputEventArgs>? onTapped = null) => new(map, onTapped);



    /// <summary>
    /// Configures the location display for a map view element.
    /// </summary>
    /// <param name="element">The map view element to configure.</param>
    /// <param name="enabled"><see langword="true"/> to enable the location display; otherwise, <see langword="false"/>.</param>
    /// <param name="autoPanMode">The auto-pan mode to apply to the location display.</param>
    /// <returns>The configured map view element.</returns>
    public static MapViewElement LocationDisplay(this MapViewElement element, bool enabled, LocationDisplayAutoPanMode autoPanMode = LocationDisplayAutoPanMode.Off)
    {
        return element with
        {
            LocationDisplay = new LocationDisplayElement()
            {
                IsEnabled = enabled,
                AutoPanMode = autoPanMode
            }
        };
    }


}