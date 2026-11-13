#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

/// <summary>
/// Provides event data and an option to intercept default behavior for taps on the viewport of an <see cref="OrientedImageryViewer"/>.
/// </summary>
public sealed class OrientedImageryViewerImageTappedArgs : OrientedImageTappedEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryViewerImageTappedArgs"/> class.
    /// </summary>
    /// <param name="orientedImageTappedEventArgs">The event arguments supplied by the original <see cref="OrientedImageViewport.ImageTapped"/> event.</param>
    public OrientedImageryViewerImageTappedArgs(OrientedImageTappedEventArgs orientedImageTappedEventArgs) :
        base(orientedImageTappedEventArgs.ImagePoint, orientedImageTappedEventArgs.Image, orientedImageTappedEventArgs.Marker)
    {
        Handled = false;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the default <see cref="OrientedImageryViewer"/> image tap logic should be skipped for the current tap event.
    /// </summary>
    public bool Handled { get; set; }
}

/// <summary>
/// Provides event data and an option to intercept default behavior for taps on the <see cref="GeoView"/> connected to an <see cref="OrientedImageryViewer"/>.
/// </summary>
public sealed class OrientedImageryViewerGeoViewTappedArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryViewerGeoViewTappedArgs"/> class.
    /// </summary>
    /// <param name="geoViewInputEventArgs">The event arguments supplied by the original <see cref="GeoView.GeoViewTapped"/> event.</param>
    public OrientedImageryViewerGeoViewTappedArgs(GeoViewInputEventArgs geoViewInputEventArgs)
    {
        GeoViewInputEventArgs = geoViewInputEventArgs;
    }

    /// <summary>
    /// Gets the event arguments supplied by the original <see cref="GeoView.GeoViewTapped"/> event.
    /// </summary>
    public GeoViewInputEventArgs GeoViewInputEventArgs { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the default <see cref="OrientedImageryViewer"/> GeoView tap logic should be skipped for the current tap event.
    /// </summary>
    public bool Handled { get; set; }
}
