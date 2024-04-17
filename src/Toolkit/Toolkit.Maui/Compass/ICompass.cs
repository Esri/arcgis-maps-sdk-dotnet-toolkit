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

namespace Esri.ArcGISRuntime.Toolkit.Maui;

/// <summary>
/// Defines the public API for the MAUI compass control.
/// </summary>
[Obsolete("No longer in use")]
public interface ICompass : IView
{
    /// <summary>
    /// Gets or sets a value indicating whether the compass will hide itself when the map rotation is 0 degrees.
    /// </summary>
    bool AutoHide { get; set; }

    /// <summary>
    /// Gets or sets the GeoView that the compass is connected to.
    /// </summary>
    GeoView? GeoView { get; set; }

    /// <summary>
    /// Gets or sets the heading of the compass.
    /// </summary>
    double Heading { get; set; }
}