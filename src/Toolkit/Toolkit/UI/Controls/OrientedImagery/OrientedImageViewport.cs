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
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;

// Disambiguate from MAUI global usings.
using Color = System.Drawing.Color;
using PointF = System.Drawing.PointF;

#if WPF
using DisplayHostElement = System.Windows.Controls.Border;
using Point = System.Windows.Point;
#elif WINDOWS_XAML
using DisplayHostElement = Microsoft.UI.Xaml.Controls.Border;
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
/// Shows planar and panoramic still images; a video image sets <see cref="Error"/>. Planar images are displayed
/// with a <see cref="Esri.ArcGISRuntime.Mapping.RasterLayer"/>, so they require the
/// <see cref="Esri.ArcGISRuntime.LicenseLevel.Standard"/> license level or higher.
/// </remarks>
public partial class OrientedImageViewport
{
    private const string DisplayHostName = "PART_DisplayHost";

    private DisplayHostElement? _displayHost;
    private OrientedImageRasterViewport? _rasterViewport;
#if WPF || WINDOWS_XAML || __ANDROID__ || __IOS__ || (MAUI && WINDOWS)
    private OrientedImagePanoramicViewport? _panoramicViewport;
#endif
    private OrientedImageInnerViewport? _activeViewport;
    private Exception? _unsupportedError;

    // A handler of a state property can set Footprint while UpdateState writes the properties. The nested UpdateState
    // then only marks the state dirty, and the outer one recomputes instead of writing stale values.
    private bool _updatingState;
    private bool _stateDirty;

    internal static readonly SimpleMarkerSymbol DefaultMarkerSymbol =
        new(SimpleMarkerSymbolStyle.Circle, Color.FromArgb(255, 0, 122, 194), 10);

    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageViewport"/> class.
    /// </summary>
    public OrientedImageViewport()
    {
#if MAUI
        ControlTemplate = DefaultControlTemplate;
#else
        DefaultStyleKey = typeof(OrientedImageViewport);
#endif
#if WINDOWS_XAML
        RegisterPropertyChangedCallback(Microsoft.UI.Xaml.Automation.AutomationProperties.NameProperty, (_, _) => PushAutomationProperties());
        RegisterPropertyChangedCallback(Microsoft.UI.Xaml.Automation.AutomationProperties.AutomationIdProperty, (_, _) => PushAutomationProperties());
#endif
    }

    // Focus lands inside the active inner viewport, so the name and automation id go there too.
    private void PushAutomationProperties()
    {
        if (_activeViewport is null)
            return;
#if WPF
        _activeViewport.SetAutomationName(System.Windows.Automation.AutomationProperties.GetName(this));
        _activeViewport.SetAutomationId(System.Windows.Automation.AutomationProperties.GetAutomationId(this));
#elif WINDOWS_XAML
        _activeViewport.SetAutomationName(Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(this));
        _activeViewport.SetAutomationId(Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(this));
#else
        _activeViewport.SetAutomationName(Microsoft.Maui.Controls.SemanticProperties.GetDescription(this));
        _activeViewport.SetAutomationId(AutomationId);
#endif
    }

    /// <summary>
    /// Occurs when the user taps the oriented image.
    /// </summary>
    /// <remarks>Also raised for a tap on a marker; see <see cref="OrientedImageTappedEventArgs.Marker"/>.</remarks>
    public event EventHandler<OrientedImageTappedEventArgs>? ImageTapped;

    /// <summary>
    /// Occurs when <see cref="IsBusy"/>, <see cref="IsInteractive"/>, or <see cref="Error"/> changes.
    /// </summary>
    /// <remarks>Raised once per change, after all three properties hold their new values.</remarks>
    public event EventHandler? StateChanged;

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
    /// If the collection implements <see cref="INotifyCollectionChanged"/>, the control follows its changes; otherwise
    /// it reads the collection once. The control follows changes to each marker and never modifies the collection.
    /// </remarks>
    public IEnumerable<OrientedImageMarker>? Markers
    {
        get => GetValue(MarkersProperty) as IEnumerable<OrientedImageMarker>;
        set => SetValue(MarkersProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the footprint is updated when the visible part of the image changes.
    /// </summary>
    /// <remarks>
    /// Panning or zooming passes the visible part of the image to the footprint's <c>UpdateFootprintAsync</c>, so a
    /// footprint drawn on the map stays in sync. The control does not draw the footprint.
    /// </remarks>
    /// <value><c>true</c> to update the footprint; otherwise <c>false</c>. The default is <c>true</c>.</value>
    public bool AutoUpdateFootprint
    {
        get => (bool)GetValue(AutoUpdateFootprintProperty);
        set => SetValue(AutoUpdateFootprintProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether the control is loading or presenting its image.
    /// </summary>
    /// <remarks>Redraws of a shown image, such as during panning, do not count.</remarks>
    public bool IsBusy => (bool)GetValue(IsBusyProperty);

    /// <summary>
    /// Gets a value indicating whether the image is loaded, can be panned and zoomed, and has no <see cref="Error"/>.
    /// </summary>
    public bool IsInteractive => (bool)GetValue(IsInteractiveProperty);

    /// <summary>
    /// Gets the error preventing the image from being shown, or <c>null</c> when there is none.
    /// </summary>
    /// <remarks>
    /// Reports image load failures, rendering failures, and image types the control cannot show. While set,
    /// <see cref="IsInteractive"/> is <c>false</c>.
    /// </remarks>
    public Exception? Error => GetValue(ErrorProperty) as Exception;

    /// <summary>
    /// Gets or sets the background color shown where the image does not fill the viewport, and while no image is shown.
    /// </summary>
    /// <remarks>
    /// The default, <see cref="System.Drawing.Color.Empty"/>, shows a grid behind planar images and a dark backdrop
    /// behind panoramas, and leaves an empty or failed viewport transparent.
    /// </remarks>
    public System.Drawing.Color DisplayBackgroundColor
    {
        get => (System.Drawing.Color)GetValue(DisplayBackgroundColorProperty);
        set => SetValue(DisplayBackgroundColorProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="Footprint"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FootprintProperty =
        PropertyHelper.CreateProperty<OrientedImageFootprint, OrientedImageViewport>(nameof(Footprint), null, (s, oldValue, newValue) => s.UpdateViewport());

    /// <summary>
    /// Identifies the <see cref="Markers"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkersProperty =
        PropertyHelper.CreateProperty<IEnumerable<OrientedImageMarker>, OrientedImageViewport>(nameof(Markers), null, (s, oldValue, newValue) => s._activeViewport?.SetMarkers(newValue));

    /// <summary>
    /// Identifies the <see cref="AutoUpdateFootprint"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty AutoUpdateFootprintProperty =
        PropertyHelper.CreateProperty<bool, OrientedImageViewport>(nameof(AutoUpdateFootprint), true, (s, oldValue, newValue) => s._activeViewport?.SetAutoUpdateFootprint(newValue));

    private static readonly DependencyPropertyKey IsBusyPropertyKey =
        PropertyHelper.CreateReadOnlyProperty<bool, OrientedImageViewport>(nameof(IsBusy));

    private static readonly DependencyPropertyKey IsInteractivePropertyKey =
        PropertyHelper.CreateReadOnlyProperty<bool, OrientedImageViewport>(nameof(IsInteractive));

    private static readonly DependencyPropertyKey ErrorPropertyKey =
        PropertyHelper.CreateReadOnlyProperty<Exception, OrientedImageViewport>(nameof(Error));

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
        PropertyHelper.CreateProperty<System.Drawing.Color, OrientedImageViewport>(nameof(DisplayBackgroundColor), System.Drawing.Color.Empty, (s, oldValue, newValue) => s.OnDisplayBackgroundColorChanged(newValue));

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

        // The inner viewport's view is still parented to the old template's host; release it so the new host can adopt it.
        if (previousHost is not null && !ReferenceEquals(previousHost, _displayHost))
            SetHostContent(previousHost, null);

        PaintHostBackground();

        // Re-presenting an existing inner viewport would reload the image and cancel in-flight work, so only host it.
        if (_activeViewport is null)
            UpdateViewport();
        else
            HostActiveViewport();
    }

    private void OnDisplayBackgroundColorChanged(System.Drawing.Color color)
    {
        _activeViewport?.SetBackgroundColor(color);
        PaintHostBackground();
    }

    // The host shows the color where no inner viewport paints: while the viewport is empty or has failed.
    private void PaintHostBackground()
    {
        if (_displayHost is null)
            return;

        System.Drawing.Color color = DisplayBackgroundColor;
#if WPF
        _displayHost.Background = color.IsEmpty ? null : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
#elif WINDOWS_XAML
        _displayHost.Background = color.IsEmpty ? null : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B));
#else
        // A null color leaves the native background as it was on iOS, so an empty color paints transparent.
        _displayHost.BackgroundColor = Microsoft.Maui.Graphics.Color.FromRgba(color.R, color.G, color.B, color.A);
#endif
    }

    private void UpdateViewport()
    {
        OrientedImage? image = Footprint?.OrientedImage;
        OrientedImageInnerViewport? viewport = SelectViewport(image);

        _unsupportedError = viewport is null && image is not null
            ? new NotSupportedException($"Oriented image type '{image.Type}' is not supported by this control yet.")
            : null;

        // The state is published once the incoming viewport has the footprint, so a switch between
        // inner viewports goes straight to loading.
        ActivateViewport(viewport);

        if (viewport is not null)
        {
            viewport.SetFootprint(Footprint);
            viewport.SetMarkers(Markers);
            viewport.SetAutoUpdateFootprint(AutoUpdateFootprint);
            viewport.SetBackgroundColor(DisplayBackgroundColor);
        }

        UpdateState();
    }

    // Internal for tests.
    internal void SetActiveViewport(OrientedImageInnerViewport? viewport)
    {
        ActivateViewport(viewport);
        UpdateState();
    }

    // Subscribes before the caller pushes the footprint, so the viewport's first notifications aren't missed.
    // The caller publishes the state afterwards.
    private void ActivateViewport(OrientedImageInnerViewport? viewport)
    {
        if (ReferenceEquals(_activeViewport, viewport))
        {
            HostActiveViewport();
            return;
        }

        if (_activeViewport is not null)
        {
            _activeViewport.StateChanged -= OnViewportStateChanged;
            _activeViewport.ImageTapped -= OnViewportImageTapped;

            // The outgoing viewport stays cached, so release its image and marker subscriptions.
            _activeViewport.SetMarkers(null);
            _activeViewport.SetFootprint(null);
        }

        _activeViewport = viewport;
        HostActiveViewport();

        if (viewport is not null)
        {
            viewport.StateChanged += OnViewportStateChanged;
            viewport.ImageTapped += OnViewportImageTapped;
            PushAutomationProperties();
        }
    }

    private void HostActiveViewport()
    {
        if (_displayHost is not null)
            SetHostContent(_displayHost, _activeViewport);
    }

    // Every footprint change re-hosts the active viewport. WinUI's Border unloads a child that is set again, and an
    // unloaded panoramic surface releases its device, so only a different view is set.
    private static void SetHostContent(DisplayHostElement host, OrientedImageInnerViewport? viewport)
    {
#if MAUI
        if (!ReferenceEquals(host.Content, viewport?.View))
            host.Content = viewport?.View;
#else
        if (!ReferenceEquals(host.Child, viewport?.View))
            host.Child = viewport?.View;
#endif
    }

    private void OnViewportStateChanged(object? sender, EventArgs e) => UpdateState();

    private void OnViewportImageTapped(object? sender, OrientedImageTappedEventArgs e) => ImageTapped?.Invoke(this, e);

    /// <summary>
    /// Converts a position in the control to the image coordinate under it, as a tap there would.
    /// </summary>
    /// <param name="screenPosition">The position relative to the control, in device-independent pixels.</param>
    /// <returns>The image coordinate, or <c>null</c> when no image is under the position
    /// or the control is not interactive.</returns>
    public PointF? ScreenToImage(Point screenPosition)
    {
        if (_activeViewport is null || !IsInteractive)
            return null;

        // The active inner viewport's view may sit inside the template's border.
#if WPF
        Point local = TranslatePoint(screenPosition, _activeViewport.View);
#elif WINDOWS_XAML
        Point local = TransformToVisual(_activeViewport.View).TransformPoint(screenPosition);
#else
        var local = new Point(screenPosition.X - _displayHost!.X - _activeViewport.View.X, screenPosition.Y - _displayHost.Y - _activeViewport.View.Y);
#endif
        return _activeViewport.ScreenToImage(local.X, local.Y);
    }

    /// <summary>
    /// Centers the view on a map location, keeping the zoom.
    /// </summary>
    /// <remarks>
    /// For a target without a z-value, a panorama turns toward it and keeps its pitch. Called while the image loads,
    /// the target becomes the initial view. The view does not animate.
    /// </remarks>
    /// <param name="target">The map location to look at.</param>
    /// <returns><c>true</c> when the target is at the view center, or as near it as the view allows; <c>false</c>
    /// when there is no image, the target can't be converted, a planar image doesn't contain it, or a newer call or
    /// a change of <see cref="Footprint"/> replaces this call.</returns>
    public Task<bool> LookAtAsync(MapPoint target)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        return _activeViewport?.LookAtAsync(target) ?? Task.FromResult(false);
    }

    private void UpdateState()
    {
        if (_updatingState)
        {
            _stateDirty = true;
            return;
        }

        var oldState = (IsBusy, IsInteractive, Error);
        _updatingState = true;
        try
        {
            do
            {
                _stateDirty = false;
                bool busy = _unsupportedError is null && (_activeViewport?.IsBusy ?? false);
                bool interactive = _unsupportedError is null && (_activeViewport?.IsInteractive ?? false);
                Exception? error = _unsupportedError ?? _activeViewport?.Error;
                if (busy == IsBusy && interactive == IsInteractive && ReferenceEquals(error, Error))
                    break;

                SetValue(IsBusyPropertyKey, busy);
                if (!_stateDirty)
                    SetValue(IsInteractivePropertyKey, interactive);
                if (!_stateDirty)
                    SetValue(ErrorPropertyKey, error);
            }
            while (_stateDirty);
        }
        finally
        {
            _updatingState = false;
        }

        // Raised outside the guard, so a StateChanged handler that changes the footprint runs a full update of its own.
        if ((IsBusy, IsInteractive, Error) != oldState)
            StateChanged?.Invoke(this, EventArgs.Empty);
    }

    // No image needs no inner viewport, so a panorama-only app never creates the planar viewport's MapView.
    private OrientedImageInnerViewport? SelectViewport(OrientedImage? image)
    {
        if (image is null || IsVideo(image.Type))
            return null;

        if (!IsPanoramic(image.Type, image.Attributes))
            return _rasterViewport ??= new OrientedImageRasterViewport();
#if WPF || WINDOWS_XAML || __ANDROID__ || __IOS__ || (MAUI && WINDOWS)
        return _panoramicViewport ??= new OrientedImagePanoramicViewport();
#else
        return null; // no panoramic surface on the neutral MAUI target
#endif
    }

    // As in the SDK's image transforms, an image spanning 360 degrees is panoramic whatever its type.
    internal static bool IsPanoramic(OrientedImageType type, IDictionary<string, object?> attributes) =>
        type == OrientedImageType.Image360 ||
        (attributes.TryGetValue("HorizontalFieldOfView", out object? value) && value is double degrees && degrees == 360);

    private static bool IsVideo(OrientedImageType type) => type is
        OrientedImageType.Aerial360Video or
        OrientedImageType.AerialFrameVideo or
        OrientedImageType.Terrestrial360Video or
        OrientedImageType.TerrestrialFrameVideo;
}
