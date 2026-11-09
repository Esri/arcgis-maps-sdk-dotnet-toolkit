// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using System;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;

// Disambiguate from Microsoft.Maui.Graphics.PointF, a MAUI global using.
using PointF = System.Drawing.PointF;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

/// <summary>
/// Describes where an <see cref="OrientedImageMarker"/> is anchored: to a pixel of one image, drawn only while that
/// image is displayed, or to a world location that is projected onto each image.
/// </summary>
/// <remarks>
/// Create through <see cref="FromImagePoint(OrientedImage, PointF)"/> or <see cref="FromLocation(MapPoint)"/>. To keep
/// a tapped location across images, anchor the marker to a world location.
/// </remarks>
public readonly record struct OrientedImageMarkerPosition
{
    private OrientedImageMarkerPosition(OrientedImage? image, PointF? imagePoint, MapPoint? location)
    {
        Image = image;
        ImagePoint = imagePoint;
        Location = location;
    }

    /// <summary>
    /// Gets the image the pixel belongs to, or <c>null</c> if the marker is anchored to a world location.
    /// </summary>
    public OrientedImage? Image { get; }

    /// <summary>
    /// Gets the image-space pixel the marker is anchored to, or <c>null</c> if it is anchored to a world location.
    /// </summary>
    public PointF? ImagePoint { get; }

    /// <summary>
    /// Gets the world location the marker is anchored to, or <c>null</c> if it is anchored to an image pixel.
    /// </summary>
    public MapPoint? Location { get; }

    /// <summary>
    /// Creates a position anchored to a pixel of <paramref name="image"/>, for example from an
    /// <see cref="OrientedImageViewport.ImageTapped"/> event.
    /// </summary>
    /// <param name="image">The image the pixel belongs to. The marker shows only on this image.</param>
    /// <param name="imagePoint">The image-space pixel coordinate.</param>
    /// <returns>An image-anchored position.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is <c>null</c>.</exception>
    public static OrientedImageMarkerPosition FromImagePoint(OrientedImage image, PointF imagePoint) =>
        new(image ?? throw new ArgumentNullException(nameof(image)), imagePoint, null);

    /// <summary>
    /// Creates a position anchored to a world location, which is projected onto each image.
    /// </summary>
    /// <param name="location">The world location.</param>
    /// <returns>A world-anchored position.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="location"/> is <c>null</c>.</exception>
    public static OrientedImageMarkerPosition FromLocation(MapPoint location) =>
        new(null, null, location ?? throw new ArgumentNullException(nameof(location)));
}
