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
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;

// Disambiguate from MAUI types from global usings
using PointF = System.Drawing.PointF;
using Color = System.Drawing.Color;

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
/// Set the <see cref="Footprint"/> to display the associated image.
/// Supports planar and panoramic/360 images. Does not support video.
/// </remarks>
public partial class OrientedImageDisplay
{
    private const string DisplayHostName = "PART_DisplayHost";

    private DisplayHostElement? _displayHost;
    private OrientedImageRasterDisplay? _rasterDisplay;
#if WPF || WINDOWS_XAML
    private OrientedImagePanoramicDisplay? _panoramicDisplay;
#endif
    private IOrientedImageDisplay? _activeDisplay;
    private Exception? _unsupportedError;

    // Default marker symbol: a filled blue circle used when a marker has no Symbol.
    internal static readonly SimpleMarkerSymbol DefaultMarkerSymbol =
        new(SimpleMarkerSymbolStyle.Circle, Color.FromArgb(255, 0, 122, 194), 10);

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
    /// Occurs when the user taps the oriented image.
    /// </summary>
    /// <remarks>
    /// Raised for every tap on the image; the image coordinates are always populated.
    /// If the tap also hit a marker, that marker is carried on <see cref="ImageClickedEventArgs.Marker"/>.
    /// </remarks>
    public event EventHandler<ImageClickedEventArgs>? ImageClicked;

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
    /// calls <see cref="OrientedImageFootprint.UpdateFootprintAsync(System.Collections.Generic.IEnumerable{System.Drawing.PointF}, System.Threading.CancellationToken)"/>
    /// so the footprint rendered on the map stays in sync. The footprint itself is not drawn by this control.
    /// </remarks>
    /// <value>A value indicating whether the footprint is automatically updated. The default is <c>false</c>.</value>
    public bool AutoUpdateFootprint
    {
        get => (bool)GetValue(AutoUpdateFootprintProperty);
        set => SetValue(AutoUpdateFootprintProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether the control is busy loading, initializing, or drawing its image
    /// (that is, not in a steady state).
    /// </summary>
    /// <remarks>
    /// Use this to show progress (for example, a busy indicator). It is independent of <see cref="IsInteractive"/>:
    /// a loaded image can be interacted with while it redraws, so both can be <c>true</c> at once.
    /// </remarks>
    /// <value><c>true</c> while the active display is loading, initializing, or drawing; otherwise <c>false</c>.</value>
    public bool IsBusy => (bool)GetValue(IsBusyProperty);

    /// <summary>
    /// Gets a value indicating whether the control is ready to interact with: it has a loaded image,
    /// its view can be panned/zoomed, and there is no critical <see cref="Error"/>.
    /// </summary>
    /// <remarks>
    /// Use this to enable or disable UI that acts on the displayed image (for example, controls that add markers).
    /// It is <c>false</c> before an image is loaded, while an unsupported image type or a load/render error is present,
    /// and becomes <c>true</c> once the image is shown and the view is unlocked.
    /// </remarks>
    /// <value><c>true</c> when the image is loaded and the view can be interacted with; otherwise <c>false</c>.</value>
    public bool IsInteractive => (bool)GetValue(IsInteractiveProperty);

    /// <summary>
    /// Gets the error preventing the image from being shown, or <c>null</c> when there is none.
    /// </summary>
    /// <remarks>
    /// Surfaces the active display's failure (for example, an <see cref="OrientedImage"/> load error or a layer
    /// rendering error). While <see cref="Error"/> is non-<c>null</c>, <see cref="IsInteractive"/> is <c>false</c>.
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
    /// Identifies the <see cref="IsBusy"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsBusyProperty =
        PropertyHelper.CreateProperty<bool, OrientedImageDisplay>(nameof(IsBusy));

    /// <summary>
    /// Identifies the <see cref="IsInteractive"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsInteractiveProperty =
        PropertyHelper.CreateProperty<bool, OrientedImageDisplay>(nameof(IsInteractive));

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
        IOrientedImageDisplay? display = SelectDisplay(type);

        // A non-null image type with no display is an unsupported type (video, or panoramic on platforms without a
        // panoramic display yet); surface it as an explicit error so a host can tell that apart from "nothing loaded".
        _unsupportedError = display is null && type is not null
            ? new NotSupportedException($"Oriented image type '{type}' is not supported by this control yet.")
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

            // Release the outgoing display's content so an inactive display doesn't retain its load, map/device content,
            // or marker subscriptions. Each display's null-footprint/markers path clears itself (see the raster display).
            _activeDisplay.SetMarkers(null);
            _activeDisplay.SetFootprint(null);
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
        }

        UpdateState();
    }

    private void OnDisplayStateChanged(object? sender, EventArgs e) => UpdateState();

    private void OnDisplayImageClicked(object? sender, ImageClickedEventArgs e) => ImageClicked?.Invoke(this, e);

    // Surfaces the active display's state as the control's own read-only IsBusy/IsInteractive/Error.
    // An unsupported image type has no display, so its error is reported here directly.
    private void UpdateState()
    {
        SetValue(IsBusyProperty, _unsupportedError is null && (_activeDisplay?.IsBusy ?? false));
        SetValue(IsInteractiveProperty, _unsupportedError is null && (_activeDisplay?.IsInteractive ?? false));
        SetValue(ErrorProperty, _unsupportedError ?? _activeDisplay?.Error);
    }

    // Selects the inner display for an image type: planar -> raster, panoramic -> panoramic (Windows only for now),
    // video (and panoramic where no panoramic display exists yet) -> none (surfaced as an unsupported-type error).
    private IOrientedImageDisplay? SelectDisplay(OrientedImageType? type)
    {
        if (type is null || IsPlanar(type.Value))
            return _rasterDisplay ??= new OrientedImageRasterDisplay();
#if WPF || WINDOWS_XAML
        if (type.Value == OrientedImageType.Panoramic)
            return _panoramicDisplay ??= new OrientedImagePanoramicDisplay();
#endif
        return null;
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
        /// <param name="marker">The marker the tap hit, or <c>null</c> if it hit no marker.</param>
        public ImageClickedEventArgs(PointF imagePoint, OrientedImage image, OrientedImageMarker? marker = null)
        {
            ImagePoint = imagePoint;
            Image = image;
            Marker = marker;
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

        /// <summary>
        /// Gets the marker the tap hit, or <c>null</c> if the tap did not hit a marker.
        /// </summary>
        /// <remarks>
        /// Inspect this to act on a marker tap (use case 4); ignore it to handle only raw image clicks. The image
        /// coordinates are populated either way.
        /// </remarks>
        /// <value>The tapped marker, or <c>null</c>.</value>
        public OrientedImageMarker? Marker { get; }
    }
}
