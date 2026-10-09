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
using Esri.ArcGISRuntime.Mapping;
using PointF = System.Drawing.PointF;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

/// <summary>
/// Provides data for a tap on an oriented image, such as the <see cref="OrientedImageViewport.ImageTapped"/> event.
/// </summary>
public class OrientedImageTappedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageTappedEventArgs"/> class.
    /// </summary>
    /// <param name="imagePoint">The tapped position in image (pixel) coordinates.</param>
    /// <param name="image">The oriented image that was tapped.</param>
    /// <param name="marker">The marker the tap hit, or <c>null</c> if it hit no marker.</param>
    public OrientedImageTappedEventArgs(PointF imagePoint, OrientedImage image, OrientedImageMarker? marker = null)
    {
        ImagePoint = imagePoint;
        Image = image;
        Marker = marker;
    }

    /// <summary>
    /// Gets the tapped position in image (pixel) coordinates.
    /// </summary>
    /// <remarks>Use <see cref="OrientedImage.ImageToLocationAsync"/> on <see cref="Image"/> to get the world location.</remarks>
    /// <value>The tapped image coordinate.</value>
    public PointF ImagePoint { get; }

    /// <summary>
    /// Gets the oriented image that was tapped.
    /// </summary>
    /// <value>The tapped oriented image.</value>
    public OrientedImage Image { get; }

    /// <summary>
    /// Gets the marker the tap hit, or <c>null</c> if the tap did not hit a marker.
    /// </summary>
    /// <value>The tapped marker, or <c>null</c>.</value>
    public OrientedImageMarker? Marker { get; }
}
