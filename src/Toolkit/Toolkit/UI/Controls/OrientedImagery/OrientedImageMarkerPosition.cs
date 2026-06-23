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

// Disambiguate from Microsoft.Maui.Graphics.PointF (a MAUI global using); image coordinates use System.Drawing.PointF.
using PointF = System.Drawing.PointF;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

/// <summary>
/// Describes where an <see cref="OrientedImageMarker"/> is anchored: to a pixel on the currently displayed image,
/// or to a world location that is projected onto each image.
/// </summary>
/// <remarks>
/// A position is created through <see cref="FromImagePoint(PointF)"/> or <see cref="FromLocation(MapPoint)"/>; exactly
/// one of <see cref="ImagePoint"/> and <see cref="Location"/> is non-<c>null</c>.
/// </remarks>
public readonly struct OrientedImageMarkerPosition : IEquatable<OrientedImageMarkerPosition>
{
    private OrientedImageMarkerPosition(PointF? imagePoint, MapPoint? location)
    {
        ImagePoint = imagePoint;
        Location = location;
    }

    /// <summary>
    /// Gets the image-space pixel the marker is anchored to, or <c>null</c> if it is anchored to a world location.
    /// </summary>
    /// <value>The image (pixel) coordinate, or <c>null</c>.</value>
    public PointF? ImagePoint { get; }

    /// <summary>
    /// Gets the world location the marker is anchored to, or <c>null</c> if it is anchored to an image pixel.
    /// </summary>
    /// <value>The world location, or <c>null</c>.</value>
    public MapPoint? Location { get; }

    /// <summary>
    /// Creates a position anchored to a pixel on the currently displayed image (for example, from a click).
    /// </summary>
    /// <param name="imagePoint">The image-space pixel coordinate.</param>
    /// <returns>An image-anchored position.</returns>
    public static OrientedImageMarkerPosition FromImagePoint(PointF imagePoint) => new(imagePoint, null);

    /// <summary>
    /// Creates a position anchored to a world location, re-projected onto each image via the camera model.
    /// </summary>
    /// <param name="location">The world location.</param>
    /// <returns>A world-anchored position.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="location"/> is <c>null</c>.</exception>
    public static OrientedImageMarkerPosition FromLocation(MapPoint location) =>
        new(null, location ?? throw new ArgumentNullException(nameof(location)));

    /// <inheritdoc/>
    public bool Equals(OrientedImageMarkerPosition other) =>
        Nullable.Equals(ImagePoint, other.ImagePoint) && Equals(Location, other.Location);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is OrientedImageMarkerPosition other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => (ImagePoint, Location).GetHashCode();

    /// <summary>Determines whether two positions are equal.</summary>
    /// <param name="left">The first position.</param>
    /// <param name="right">The second position.</param>
    /// <returns><c>true</c> if the positions are equal; otherwise <c>false</c>.</returns>
    public static bool operator ==(OrientedImageMarkerPosition left, OrientedImageMarkerPosition right) => left.Equals(right);

    /// <summary>Determines whether two positions are not equal.</summary>
    /// <param name="left">The first position.</param>
    /// <param name="right">The second position.</param>
    /// <returns><c>true</c> if the positions are not equal; otherwise <c>false</c>.</returns>
    public static bool operator !=(OrientedImageMarkerPosition left, OrientedImageMarkerPosition right) => !left.Equals(right);
}
