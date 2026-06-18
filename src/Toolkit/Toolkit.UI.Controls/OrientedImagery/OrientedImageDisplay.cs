using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using System.Collections.ObjectModel;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

/// <summary>
/// A control that displays an oriented image footprint and allows interaction with it.
/// </summary>
public partial class OrientedImageDisplay : Control
{
    private OrientedImageFootprint? _footprint;
    private ObservableCollection<Graphic>? _markers;

    /// <summary>
    /// Creates a new instance of the <see cref="OrientedImageDisplay"/> class.
    /// </summary>
    public OrientedImageDisplay() : base()
    {
        // todo
    }

    /// <summary>
    /// Occurs when the user clicks on the oriented image display.
    /// </summary>
    public event EventHandler<ImageClickedEventArgs>? ImageClicked;

    /// <summary>
    /// Gets or sets the markers to render on the oriented image display.
    /// </summary>
    public ObservableCollection<Graphic>? Markers {
        get => _markers;
        set {
            if (_markers == value) return;

            // unhook listeners from old collection, clear image markers, and hook listeners to new collection

            _markers = value;
        }
    }

    /// <summary>
    /// Gets or sets footprint of the oriented image to display.
    /// </summary>
    public OrientedImageFootprint? Footprint
    {
        get => _footprint;
        set
        {
            // Whatever logic needs to happen to load, choose a inner display, etc
            _footprint = value;
        }
    }

    /// <summary>
    /// Whether or not to automatically update the visualized footprint when the display viewport changes.
    /// </summary>
    public bool AutoUpdateFootprint { get; set; }

    /// <summary>
    /// Event arguments for the <see cref="OrientedImageDisplay.ImageClicked"/> event.
    /// </summary>
    public class ImageClickedEventArgs : EventArgs
    {
        /// <summary>
        /// The location of the clicked position in map coordinates.
        /// </summary>
        public MapPoint Location { get; private set; }

        /// <summary>
        /// Constructs a new instance of the <see cref="ImageClickedEventArgs"/> class with the specified location.
        /// </summary>
        public ImageClickedEventArgs(MapPoint location)
        {
            Location = location;
        }
    }
}