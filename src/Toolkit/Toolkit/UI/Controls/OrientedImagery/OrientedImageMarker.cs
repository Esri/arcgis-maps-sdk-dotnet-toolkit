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

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// Disambiguate from Microsoft.UI.Xaml.Controls.Symbol (a WinUI global using).
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

/// <summary>
/// A point of interest rendered over the image in an <see cref="OrientedImageDisplay"/>.
/// </summary>
/// <remarks>
/// <para>
/// Markers are authored and owned by the application; <see cref="OrientedImageDisplay"/> renders whatever is in its
/// <see cref="OrientedImageDisplay.Markers"/> collection and never modifies it.
/// </para>
/// <para>
/// A marker is anchored either to an image pixel or to a world location (see <see cref="OrientedImageMarkerPosition"/>).
/// <see cref="Position"/>, <see cref="Symbol"/>, and <see cref="IsVisible"/> raise <see cref="PropertyChanged"/> so the
/// control updates the rendered marker in place.
/// </para>
/// </remarks>
public sealed class OrientedImageMarker : INotifyPropertyChanged
{
    private OrientedImageMarkerPosition _position;
    private Symbol? _symbol;
    private bool _isVisible = true;
    private object? _tag;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageMarker"/> class.
    /// </summary>
    /// <param name="position">The position the marker is anchored to.</param>
    /// <param name="symbol">The symbol used to draw the marker, or <c>null</c> to use the display's default.</param>
    public OrientedImageMarker(OrientedImageMarkerPosition position, Symbol? symbol = null)
    {
        _position = position;
        _symbol = symbol;
    }

    /// <summary>Gets or sets the position the marker is anchored to.</summary>
    /// <value>The marker position.</value>
    public OrientedImageMarkerPosition Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    /// <summary>Gets or sets the symbol used to draw the marker. If <c>null</c>, the display's default is used.</summary>
    /// <value>The marker symbol, or <c>null</c>.</value>
    public Symbol? Symbol
    {
        get => _symbol;
        set => SetProperty(ref _symbol, value);
    }

    /// <summary>Gets or sets a value indicating whether the marker is shown. The default is <c>true</c>.</summary>
    /// <value><c>true</c> if the marker is shown; otherwise <c>false</c>.</value>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    /// <summary>Gets or sets an arbitrary value associated with the marker (for example, a domain identifier).</summary>
    /// <remarks>Not used by the control; provided so applications can correlate <see cref="OrientedImageDisplay.ImageClickedEventArgs.Marker"/>.</remarks>
    /// <value>The associated value, or <c>null</c>.</value>
    public object? Tag
    {
        get => _tag;
        set => SetProperty(ref _tag, value);
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
