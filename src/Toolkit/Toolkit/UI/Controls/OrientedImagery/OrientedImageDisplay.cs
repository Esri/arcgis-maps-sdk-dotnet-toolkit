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
using System.Collections.Generic;
using System.Collections.Specialized;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;

// Disambiguate from MAUI types from global usings
using Color = System.Drawing.Color;
using PointF = System.Drawing.PointF;

#if WPF
using DisplayHostElement = System.Windows.Controls.ContentPresenter;
using Point = System.Windows.Point;
#elif WINDOWS_XAML
using DisplayHostElement = Microsoft.UI.Xaml.Controls.ContentPresenter;
using Point = Windows.Foundation.Point;
#elif MAUI
using DisplayHostElement = Microsoft.Maui.Controls.ContentView;
using Point = Microsoft.Maui.Graphics.Point;
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
/// Shows planar and panoramic still images. Video isn't supported. A video image sets <see cref="Error"/>.
/// </remarks>
public partial class OrientedImageDisplay
{
    private const string DisplayHostName = "PART_DisplayHost";

    private DisplayHostElement? _displayHost;
    private OrientedImageRasterDisplay? _rasterDisplay;
#if WPF || WINDOWS_XAML || __ANDROID__ || __IOS__ || (MAUI && WINDOWS)
    private OrientedImagePanoramicDisplay? _panoramicDisplay;
#endif
    private OrientedImageInnerDisplay? _activeDisplay;
    private Exception? _unsupportedError;

    internal static readonly SimpleMarkerSymbol DefaultMarkerSymbol =
        new(SimpleMarkerSymbolStyle.Circle, Color.FromArgb(255, 0, 122, 194), 10);

    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageDisplay"/> class.
    /// </summary>
    public OrientedImageDisplay()
    {
#if MAUI
        ControlTemplate = DefaultControlTemplate;
#else
        DefaultStyleKey = typeof(OrientedImageDisplay);
#endif
#if WINDOWS_XAML
        RegisterPropertyChangedCallback(Microsoft.UI.Xaml.Automation.AutomationProperties.NameProperty, (_, _) => PushAutomationProperties());
        RegisterPropertyChangedCallback(Microsoft.UI.Xaml.Automation.AutomationProperties.AutomationIdProperty, (_, _) => PushAutomationProperties());
#endif
    }

    // Focus lands on the active display's inner view, so the name and automation id go there too.
    private void PushAutomationProperties()
    {
        if (_activeDisplay is null)
            return;
#if WPF
        _activeDisplay.SetAutomationName(System.Windows.Automation.AutomationProperties.GetName(this));
        _activeDisplay.SetAutomationId(System.Windows.Automation.AutomationProperties.GetAutomationId(this));
#elif WINDOWS_XAML
        _activeDisplay.SetAutomationName(Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(this));
        _activeDisplay.SetAutomationId(Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(this));
#else
        _activeDisplay.SetAutomationName(Microsoft.Maui.Controls.SemanticProperties.GetDescription(this));
        _activeDisplay.SetAutomationId(AutomationId);
#endif
    }

    /// <summary>
    /// Occurs when the user taps the oriented image.
    /// </summary>
    /// <remarks>
    /// Raised for every tap on the image; the image coordinates are always populated.
    /// If the tap also hit a marker, that marker is carried on <see cref="OrientedImageTappedEventArgs.Marker"/>.
    /// </remarks>
    public event EventHandler<OrientedImageTappedEventArgs>? ImageTapped;

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
    /// Like an items source, this accepts any collection. If the collection is observable (implements
    /// <see cref="INotifyCollectionChanged"/>), the control follows its changes; otherwise it reads the collection
    /// once when assigned. Either way, the control follows changes to the markers themselves and never modifies the
    /// collection.
    /// </remarks>
    /// <value>The markers drawn over the image, or <c>null</c>.</value>
    public IEnumerable<OrientedImageMarker>? Markers
    {
        get => GetValue(MarkersProperty) as IEnumerable<OrientedImageMarker>;
        set => SetValue(MarkersProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the displayed footprint is automatically recomputed when the
    /// viewport changes.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, panning or zooming pushes the visible part of the image to the footprint so the footprint drawn on the map stays in sync:
    /// a planar image calls <see cref="OrientedImageFootprint.UpdateFootprintAsync(System.Collections.Generic.IEnumerable{System.Drawing.PointF}, System.Threading.CancellationToken)"/>,
    /// a 360 image calls <see cref="OrientedImageFootprint.UpdateFootprintAsync(double, double, double, double, System.Threading.CancellationToken)"/>. This control does not draw the footprint.
    /// </remarks>
    /// <value>A value indicating whether the footprint is automatically updated. The default is <c>true</c>.</value>
    public bool AutoUpdateFootprint
    {
        get => (bool)GetValue(AutoUpdateFootprintProperty);
        set => SetValue(AutoUpdateFootprintProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether the control is loading, initializing, or drawing its image.
    /// </summary>
    /// <remarks>Independent of <see cref="IsInteractive"/>: a loaded image stays interactive while it redraws.</remarks>
    /// <value><c>true</c> while the active display is loading, initializing, or drawing; otherwise <c>false</c>.</value>
    public bool IsBusy => (bool)GetValue(IsBusyProperty);

    /// <summary>
    /// Gets a value indicating whether the image is loaded, can be panned and zoomed, and has no <see cref="Error"/>.
    /// </summary>
    /// <remarks>Use this to enable UI that acts on the displayed image, such as controls that add markers.</remarks>
    /// <value><c>true</c> when the image is loaded and the view can be interacted with; otherwise <c>false</c>.</value>
    public bool IsInteractive => (bool)GetValue(IsInteractiveProperty);

    /// <summary>
    /// Gets the error preventing the image from being shown, or <c>null</c> when there is none.
    /// </summary>
    /// <remarks>An image load or layer rendering error from the active display. While non-<c>null</c>, <see cref="IsInteractive"/> is <c>false</c>.</remarks>
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
        PropertyHelper.CreateProperty<IEnumerable<OrientedImageMarker>, OrientedImageDisplay>(nameof(Markers), null, (s, oldValue, newValue) => s._activeDisplay?.SetMarkers(newValue));

    /// <summary>
    /// Identifies the <see cref="AutoUpdateFootprint"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty AutoUpdateFootprintProperty =
        PropertyHelper.CreateProperty<bool, OrientedImageDisplay>(nameof(AutoUpdateFootprint), true, (s, oldValue, newValue) => s._activeDisplay?.SetAutoUpdateFootprint(newValue));

    // Computed state, read-only where the platform supports it (see PropertyHelper.CreateReadOnlyProperty).
    private static readonly DependencyPropertyKey IsBusyPropertyKey =
        PropertyHelper.CreateReadOnlyProperty<bool, OrientedImageDisplay>(nameof(IsBusy));

    private static readonly DependencyPropertyKey IsInteractivePropertyKey =
        PropertyHelper.CreateReadOnlyProperty<bool, OrientedImageDisplay>(nameof(IsInteractive));

    private static readonly DependencyPropertyKey ErrorPropertyKey =
        PropertyHelper.CreateReadOnlyProperty<Exception, OrientedImageDisplay>(nameof(Error));

    /// <summary>
    /// Identifies the <see cref="IsBusy"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsBusyProperty = PropertyHelper.GetProperty(IsBusyPropertyKey);

    /// <summary>
    /// Identifies the <see cref="IsInteractive"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsInteractiveProperty = PropertyHelper.GetProperty(IsInteractivePropertyKey);

    /// <summary>
    /// Identifies the <see cref="Error"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ErrorProperty = PropertyHelper.GetProperty(ErrorPropertyKey);

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
        DisplayHostElement? previousHost = _displayHost;
        _displayHost = GetTemplateChild(DisplayHostName) as DisplayHostElement;

        // On a template re-apply the active display is still parented to the discarded template's host; release
        // it there or the new host cannot adopt it.
        if (previousHost is not null && !ReferenceEquals(previousHost, _displayHost))
            previousHost.Content = null;

        if (_displayHost is null)
            return; // a template without the host part shows nothing; a later template can re-host

        // First host: run the full pipeline (a footprint set before the template existed was deferred by UpdateDisplay).
        // Re-applied template: only re-host; re-presenting would reload the image and cancel in-flight work.
        if (previousHost is null)
            UpdateDisplay();
        else
            HostActiveDisplay();
    }

    private void UpdateDisplay()
    {
        if (_displayHost is null)
            return; // Template not applied yet; OnApplyTemplate will call again.

        OrientedImage? image = Footprint?.OrientedImage;
        OrientedImageInnerDisplay? display = SelectDisplay(image);

        // Reports an image that no display can show, such as a video, instead of looking unloaded.
        _unsupportedError = display is null && image is not null
            ? new NotSupportedException($"Oriented image type '{image.Type}' is not supported by this control yet.")
            : null;

        SetActiveDisplay(display);

        if (display is not null)
        {
            PushAutomationProperties();
            display.SetFootprint(Footprint);
            display.SetMarkers(Markers);
            display.SetAutoUpdateFootprint(AutoUpdateFootprint);
            display.SetBackgroundColor(DisplayBackgroundColor);
        }
    }

    // Subscribes before the caller pushes state in, so the display's first notifications aren't missed.
    private void SetActiveDisplay(OrientedImageInnerDisplay? display)
    {
        if (ReferenceEquals(_activeDisplay, display))
        {
            // Same display (including null -> null):
            // re-host and publish state anyway; only this path surfaces a recomputed _unsupportedError.
            HostActiveDisplay();
            UpdateState();
            return;
        }

        if (_activeDisplay is not null)
        {
            _activeDisplay.StateChanged -= OnDisplayStateChanged;
            _activeDisplay.ImageTapped -= OnDisplayImageTapped;

            // Release the outgoing display's image, map/device content and marker subscriptions.
            _activeDisplay.SetMarkers(null);
            _activeDisplay.SetFootprint(null);
        }

        _activeDisplay = display;
        HostActiveDisplay();

        if (display is not null)
        {
            display.StateChanged += OnDisplayStateChanged;
            display.ImageTapped += OnDisplayImageTapped;
        }

        UpdateState();
    }

    private void HostActiveDisplay()
    {
        _displayHost!.Content = _activeDisplay;
    }

    private void OnDisplayStateChanged(object? sender, EventArgs e) => UpdateState();

    internal event EventHandler? StateChanged;

    private void OnDisplayImageTapped(object? sender, OrientedImageTappedEventArgs e) => ImageTapped?.Invoke(this, e);

    /// <summary>
    /// Converts a position in the control to the image coordinate under it, as a tap there would.
    /// </summary>
    /// <remarks>
    /// For workflows that act on a position without a pointer, such as the center of the view under keyboard control.
    /// Markers are not identified; <see cref="ImageTapped"/> reports those for taps.
    /// </remarks>
    /// <param name="screenPosition">The position relative to the control, in device-independent pixels.</param>
    /// <returns>The image coordinate, or <c>null</c> when no image is under the position or the control is not
    /// interactive.</returns>
    public PointF? ScreenToImage(Point screenPosition)
    {
        if (_activeDisplay is null || !IsInteractive)
            return null;

        // The active display may sit inside the template's border.
#if WPF
        Point local = TranslatePoint(screenPosition, _activeDisplay);
#elif WINDOWS_XAML
        Point local = TransformToVisual(_activeDisplay).TransformPoint(screenPosition);
#else
        var local = new Point(screenPosition.X - _displayHost!.X - _activeDisplay.X, screenPosition.Y - _displayHost.Y - _activeDisplay.Y);
#endif
        return _activeDisplay.ScreenToImage(local.X, local.Y);
    }

    // Only fires StateChanged when there is an actual change.
    private void UpdateState()
    {
        // An unsupported image type has no display, so its error is merged in here.
        bool busy = _unsupportedError is null && (_activeDisplay?.IsBusy ?? false);
        bool interactive = _unsupportedError is null && (_activeDisplay?.IsInteractive ?? false);
        Exception? error = _unsupportedError ?? _activeDisplay?.Error;
        if (busy == IsBusy && interactive == IsInteractive && ReferenceEquals(error, Error))
            return;

        SetValue(IsBusyPropertyKey, busy);
        SetValue(IsInteractivePropertyKey, interactive);
        SetValue(ErrorPropertyKey, error);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private OrientedImageInnerDisplay? SelectDisplay(OrientedImage? image)
    {
        if (image is null)
            return _rasterDisplay ??= new OrientedImageRasterDisplay();

        if (IsVideo(image.Type))
            return null;

        if (!IsPanoramic(image.Type, image.Attributes))
            return _rasterDisplay ??= new OrientedImageRasterDisplay();
#if WPF || WINDOWS_XAML || __ANDROID__ || __IOS__ || (MAUI && WINDOWS)
        return _panoramicDisplay ??= new OrientedImagePanoramicDisplay();
#else
        return null; // no panoramic surface on the neutral MAUI target
#endif
    }

    // As in the SDK's image transforms, an image that spans 360 degrees is panoramic whatever its type.
    internal static bool IsPanoramic(OrientedImageType type, IDictionary<string, object?> attributes) =>
        type == OrientedImageType.Image360 ||
        (attributes.TryGetValue("HorizontalFieldOfView", out object? value) && value is double degrees && degrees == 360);

    private static bool IsVideo(OrientedImageType type) => type is
        OrientedImageType.Aerial360Video or
        OrientedImageType.AerialFrameVideo or
        OrientedImageType.Terrestrial360Video or
        OrientedImageType.TerrestrialFrameVideo;
}
