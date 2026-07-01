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
using System.Collections.ObjectModel;
using Esri.ArcGISRuntime.Mapping;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

/// <summary>
/// The contract every inner display of <see cref="OrientedImageDisplay"/> satisfies. The control selects an
/// implementation by image type, hosts it (each implementation is also a platform view), pushes the current
/// footprint/markers/state into it, and surfaces its reported state and interactions as its own public API.
/// </summary>
/// <remarks>
/// The contract is the common denominator of every display (raster display, future 360/panoramic and video displays).
/// It is intentionally behavior-only and platform-neutral; the control hosts the implementation as a view without the
/// contract referencing a platform view type.
/// </remarks>
internal interface IOrientedImageDisplay
{
    /// <summary>Sets the footprint whose oriented image should be displayed.</summary>
    /// <param name="footprint">The footprint to display, or <c>null</c> to clear.</param>
    void SetFootprint(OrientedImageFootprint? footprint);

    /// <summary>Sets the markers rendered over the image.</summary>
    /// <param name="markers">The markers to render, or <c>null</c>.</param>
    void SetMarkers(ObservableCollection<OrientedImageMarker>? markers);

    /// <summary>Enables or disables automatic recomputation of the footprint as the view changes.</summary>
    /// <param name="enabled">Whether the footprint is automatically updated.</param>
    void SetAutoUpdateFootprint(bool enabled);

    /// <summary>Sets the background color shown where the image does not fill the display.</summary>
    /// <param name="color">The background color, or <see cref="System.Drawing.Color.Empty"/> to keep the display's default.</param>
    void SetBackgroundColor(System.Drawing.Color color);

    /// <summary>
    /// Gets a value indicating whether the display is busy loading, initializing, or drawing (not in a steady state).
    /// Use it to show progress.
    /// </summary>
    bool IsBusy { get; }

    /// <summary>
    /// Gets a value indicating whether the display is ready to interact with: it has a loaded image, the view can be
    /// panned/zoomed, and there is no critical <see cref="Error"/>. Independent of <see cref="IsBusy"/>;
    /// a loaded display stays interactive while it redraws.
    /// </summary>
    bool IsInteractive { get; }

    /// <summary>Gets the error that prevents the display from showing its image, or <c>null</c> when there is none.</summary>
    Exception? Error { get; }

    /// <summary>Occurs when <see cref="IsBusy"/>, <see cref="IsInteractive"/>, or <see cref="Error"/> changes.</summary>
    event EventHandler? StateChanged;

    /// <summary>Occurs when the user taps the image; a tapped marker (if any) is carried on the event args.</summary>
    event EventHandler<OrientedImageDisplay.ImageClickedEventArgs>? ImageClicked;
}
