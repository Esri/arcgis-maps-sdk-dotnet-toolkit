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
using Esri.ArcGISRuntime.Toolkit.Internal;

// Disambiguate from Microsoft.Maui.Graphics.PointF (a MAUI global using); image coordinates use System.Drawing.PointF.
using PointF = System.Drawing.PointF;

// The host element that presents the active inner display differs per platform.
#if WPF
using DisplayHostElement = System.Windows.Controls.ContentPresenter;
#elif WINDOWS_XAML
using DisplayHostElement = Microsoft.UI.Xaml.Controls.ContentPresenter;
#elif MAUI
using DisplayHostElement = Microsoft.Maui.Controls.ContentView;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

/// <summary>
/// A control that displays an oriented image and allows interaction with it.
/// </summary>
/// <remarks>
/// <para>
/// The control presents one of several inner displays chosen by the <see cref="Mapping.OrientedImageType"/> of the
/// image referenced by the assigned <see cref="Footprint"/>. This release implements the raster display (a map view
/// hosting the image as a raster layer); panoramic/360 and video displays are not yet available.
/// </para>
/// </remarks>
public partial class OrientedImageDisplay
{
    private const string DisplayHostName = "PART_DisplayHost";

    private DisplayHostElement? _displayHost;
    private OrientedImageRasterDisplay? _rasterDisplay;
    private IOrientedImageDisplay? _activeDisplay;
    private Exception? _unsupportedError;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageDisplay"/> class.
    /// </summary>
    public OrientedImageDisplay()
        : base()
    {
#if MAUI
        // MAUI layout containers are not tab stops by default, so no IsTabStop is needed here.
        ControlTemplate = DefaultControlTemplate;
#else
        DefaultStyleKey = typeof(OrientedImageDisplay);
#endif

        // Default localized screen-reader label for the control (consumers may override AutomationProperties.Name).
        string automationName = Properties.Resources.GetString("OrientedImageDisplayAutomationName") ?? "Oriented image display";
#if WPF
        System.Windows.Automation.AutomationProperties.SetName(this, automationName);
#elif WINDOWS_XAML
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(this, automationName);
#elif MAUI
        Microsoft.Maui.Controls.SemanticProperties.SetDescription(this, automationName);
#endif
    }

    /// <summary>
    /// Occurs when the user taps the oriented image away from any marker.
    /// </summary>
    public event EventHandler<ImageClickedEventArgs>? ImageClicked;

    /// <summary>
    /// Occurs when the user taps a marker rendered over the oriented image.
    /// </summary>
    /// <remarks>A marker tap raises this event instead of <see cref="ImageClicked"/>.</remarks>
    public event EventHandler<MarkerClickedEventArgs>? MarkerClicked;

    /// <summary>
    /// Gets or sets the footprint of the oriented image to display.
    /// </summary>
    /// <value>The footprint whose <see cref="OrientedImageFootprint.OrientedImage"/> is shown by the control.</value>
    public OrientedImageFootprint? Footprint
    {
        get => GetValue(FootprintProperty) as OrientedImageFootprint;
        set => SetValue(FootprintProperty, value);
    }

    /// <summary>
    /// Gets or sets the markers to render on top of the oriented image.
    /// </summary>
    /// <remarks>
    /// The collection is owned by the application; the control renders its contents and never modifies it. See
    /// <see cref="OrientedImageMarker"/> for image- versus world-anchored positioning.
    /// </remarks>
    /// <value>A collection of markers drawn over the image, or <c>null</c>.</value>
    public ObservableCollection<OrientedImageMarker>? Markers
    {
        get => GetValue(MarkersProperty) as ObservableCollection<OrientedImageMarker>;
        set => SetValue(MarkersProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the displayed footprint is automatically recomputed when the
    /// viewport changes.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the control recomputes the visible image corners as the display is panned or zoomed and
    /// calls <see cref="OrientedImageFootprint.UpdateFootprintAsync(OrientedImagePixelCorners, System.Threading.CancellationToken)"/>
    /// so the footprint rendered on the map stays in sync. The footprint itself is not drawn by this control.
    /// </remarks>
    /// <value>A value indicating whether the footprint is automatically updated. The default is <c>false</c>.</value>
    public bool AutoUpdateFootprint
    {
        get => (bool)GetValue(AutoUpdateFootprintProperty);
        set => SetValue(AutoUpdateFootprintProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether the control is loading or drawing its image (that is, not in a steady state).
    /// </summary>
    /// <value><c>true</c> while the active display is loading or drawing; otherwise <c>false</c>.</value>
    public bool IsActive => (bool)GetValue(IsActiveProperty);

    /// <summary>
    /// Gets the error preventing the image from being shown, or <c>null</c> when there is none.
    /// </summary>
    /// <remarks>
    /// Surfaces the active display's failure (for example, an <see cref="OrientedImage"/> load error or a layer
    /// rendering error). A non-<c>null</c> <see cref="Error"/> and <see cref="IsActive"/> are mutually exclusive.
    /// </remarks>
    /// <value>The current error, or <c>null</c>.</value>
    public Exception? Error => GetValue(ErrorProperty) as Exception;

    /// <summary>
    /// Gets or sets the background color shown where the image does not fill the display (for example, the area
    /// exposed when panning or rotating beyond the image).
    /// </summary>
    /// <remarks>The default, <see cref="System.Drawing.Color.Empty"/>, keeps each display's own default background.</remarks>
    /// <value>The display background color.</value>
    public System.Drawing.Color DisplayBackgroundColor
    {
        get => (System.Drawing.Color)GetValue(DisplayBackgroundColorProperty);
        set => SetValue(DisplayBackgroundColorProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="Footprint"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FootprintProperty =
        PropertyHelper.CreateProperty<OrientedImageFootprint, OrientedImageDisplay>(nameof(Footprint), null, (s, oldValue, newValue) => s.UpdateDisplay());

    /// <summary>
    /// Identifies the <see cref="Markers"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkersProperty =
        PropertyHelper.CreateProperty<ObservableCollection<OrientedImageMarker>, OrientedImageDisplay>(nameof(Markers), null, (s, oldValue, newValue) => s._activeDisplay?.SetMarkers(newValue));

    /// <summary>
    /// Identifies the <see cref="AutoUpdateFootprint"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty AutoUpdateFootprintProperty =
        PropertyHelper.CreateProperty<bool, OrientedImageDisplay>(nameof(AutoUpdateFootprint), false, (s, oldValue, newValue) => s._activeDisplay?.SetAutoUpdateFootprint(newValue));

    /// <summary>
    /// Identifies the <see cref="IsActive"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsActiveProperty =
        PropertyHelper.CreateProperty<bool, OrientedImageDisplay>(nameof(IsActive));

    /// <summary>
    /// Identifies the <see cref="Error"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ErrorProperty =
        PropertyHelper.CreateProperty<Exception, OrientedImageDisplay>(nameof(Error));

    /// <summary>
    /// Identifies the <see cref="DisplayBackgroundColor"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty DisplayBackgroundColorProperty =
        PropertyHelper.CreateProperty<System.Drawing.Color, OrientedImageDisplay>(nameof(DisplayBackgroundColor), System.Drawing.Color.Empty, (s, oldValue, newValue) => s._activeDisplay?.SetBackgroundColor(newValue));

    /// <inheritdoc/>
#if WINDOWS_XAML || MAUI
    protected override void OnApplyTemplate()
#elif WPF
    public override void OnApplyTemplate()
#endif
    {
        base.OnApplyTemplate();
        _displayHost = GetTemplateChild(DisplayHostName) as DisplayHostElement;
        UpdateDisplay();
    }

    /// <summary>
    /// Selects the inner display for the current image type, makes it active, and pushes the current state into it.
    /// </summary>
    private void UpdateDisplay()
    {
        if (_displayHost is null)
            return; // Template not applied yet; OnApplyTemplate will call again.

        OrientedImageType? type = Footprint?.OrientedImage?.Type;
        bool supported = type is null || IsPlanar(type.Value);

        // Panoramic/360 and video displays are not implemented yet; surface those types as an explicit error so a host
        // can tell "unsupported type" apart from "nothing loaded" (both otherwise show no content).
        _unsupportedError = supported
            ? null
            : new NotSupportedException($"Oriented image type '{type}' is not supported by this control yet.");

        IOrientedImageDisplay? display = supported
            ? _rasterDisplay ??= new OrientedImageRasterDisplay()
            : null;

        SetActiveDisplay(display);

        if (display is not null)
        {
            display.SetFootprint(Footprint);
            display.SetMarkers(Markers);
            display.SetAutoUpdateFootprint(AutoUpdateFootprint);
            display.SetBackgroundColor(DisplayBackgroundColor);
        }
    }

    // Swaps the active display: moves host content and event subscriptions. Subscribes before the caller pushes state
    // in, so the display's first state/interaction notifications aren't missed.
    private void SetActiveDisplay(IOrientedImageDisplay? display)
    {
        if (ReferenceEquals(_activeDisplay, display))
            return;

        if (_activeDisplay is not null)
        {
            _activeDisplay.StateChanged -= OnDisplayStateChanged;
            _activeDisplay.ImageClicked -= OnDisplayImageClicked;
            _activeDisplay.MarkerClicked -= OnDisplayMarkerClicked;
        }

        _activeDisplay = display;
#if MAUI
        _displayHost!.Content = display as Microsoft.Maui.Controls.View;
#else
        _displayHost!.Content = display;
#endif

        if (display is not null)
        {
            display.StateChanged += OnDisplayStateChanged;
            display.ImageClicked += OnDisplayImageClicked;
            display.MarkerClicked += OnDisplayMarkerClicked;
        }

        UpdateState();
    }

    private void OnDisplayStateChanged(object? sender, EventArgs e) => UpdateState();

    private void OnDisplayImageClicked(object? sender, ImageClickedEventArgs e) => ImageClicked?.Invoke(this, e);

    private void OnDisplayMarkerClicked(object? sender, MarkerClickedEventArgs e) => MarkerClicked?.Invoke(this, e);

    // Surfaces the active display's state as the control's own read-only IsActive/Error. An unsupported image type has
    // no display, so its error is reported here directly.
    private void UpdateState()
    {
        SetValue(IsActiveProperty, _unsupportedError is null && (_activeDisplay?.IsActive ?? false));
        SetValue(ErrorProperty, _unsupportedError ?? _activeDisplay?.Error);
    }

    /// <summary>
    /// Determines whether an image type is presented by the raster display (everything that is not a panoramic or video type).
    /// </summary>
    private static bool IsPlanar(OrientedImageType type) => type switch
    {
        OrientedImageType.Panoramic => false,
        OrientedImageType.Aerial360Video => false,
        OrientedImageType.AerialFrameVideo => false,
        OrientedImageType.Terrestrial360Video => false,
        OrientedImageType.TerrestrialFrameVideo => false,
        _ => true,
    };

    /// <summary>
    /// Event arguments for the <see cref="OrientedImageDisplay.ImageClicked"/> event.
    /// </summary>
    public class ImageClickedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageClickedEventArgs"/> class.
        /// </summary>
        /// <param name="imagePoint">The clicked position in image (pixel) coordinates.</param>
        /// <param name="image">The oriented image that was clicked.</param>
        public ImageClickedEventArgs(PointF imagePoint, OrientedImage image)
        {
            ImagePoint = imagePoint;
            Image = image;
        }

        /// <summary>
        /// Gets the clicked position in image (pixel) coordinates.
        /// </summary>
        /// <remarks>
        /// Use <see cref="OrientedImage.ImageToLocationAsync"/> on <see cref="Image"/> to obtain the corresponding
        /// real-world location.
        /// </remarks>
        /// <value>The clicked image coordinate.</value>
        public PointF ImagePoint { get; }

        /// <summary>
        /// Gets the oriented image that was clicked.
        /// </summary>
        /// <value>The clicked oriented image.</value>
        public OrientedImage Image { get; }
    }

    /// <summary>
    /// Event arguments for the <see cref="OrientedImageDisplay.MarkerClicked"/> event.
    /// </summary>
    public class MarkerClickedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarkerClickedEventArgs"/> class.
        /// </summary>
        /// <param name="marker">The marker that was clicked.</param>
        public MarkerClickedEventArgs(OrientedImageMarker marker)
        {
            Marker = marker;
        }

        /// <summary>
        /// Gets the marker that was clicked.
        /// </summary>
        /// <value>The clicked marker.</value>
        public OrientedImageMarker Marker { get; }
    }
}
