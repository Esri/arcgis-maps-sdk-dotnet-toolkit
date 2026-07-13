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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
using PointF = System.Drawing.PointF;
#if WPF
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;
#elif WINDOWS_XAML
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

// The shared skeleton of OrientedImageDisplay's inner displays (raster, panoramic, future video). It owns:
//
//  - The presentation session: one footprint + one cancellation token per SetFootprint call, which cancels the
//    previous session's token. The token IS the staleness check ("superseded == canceled"): it is threaded
//    through every await, and any code that touches display state after an await must capture it beforehand
//    and re-check it.
//  - The transition checklist: cancel the outgoing image's load and in-flight footprint updates, blank the
//    presentation immediately on an image change (the old image must never stay visible or clickable while
//    state describes the new one), reset loading/error, update the automation name.
//  - Reported state (IsBusy/IsInteractive/Error/StateChanged), the marker-collection subscription, and the
//    auto-update-footprint plumbing (latest-wins cancellation; canceled on disable and session end).
//
// Derived displays implement only their unique parts: PresentAsync (make the loaded image visible),
// ClearPresentation (blank it), footprint-corner projection, marker rendering, and background color.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Platform view types are not IDisposable by convention. The session CTS is cancel-only (no timer), so it needs no disposal; canceling it on supersede is the release.")]
#if MAUI
internal abstract class OrientedImageInnerDisplay : ContentView
#else
internal abstract class OrientedImageInnerDisplay : ContentControl
#endif
{
    private ObservableCollection<OrientedImageMarker>? _markers;
    private WeakEventListener<OrientedImageInnerDisplay, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>? _markersListener;
    private CancellationTokenSource? _sessionCts;
    private CancellationTokenSource? _updateCts;
    private bool _autoUpdate;
    private bool _isLoading;

    private protected OrientedImageInnerDisplay()
    {
#if !MAUI
        // ContentControl content defaults to Left/Top; the inner view has to be stretched to fill.
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;

        // The inner view is the focusable element.
        IsTabStop = false;
#endif
    }

    /// <summary>Gets a value indicating whether the display is busy loading, initializing, or drawing (not in a steady state).</summary>
    public bool IsBusy { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the display is ready to interact with: it has a presented image, the view can
    /// be panned/zoomed, and there is no critical <see cref="Error"/>. Independent of <see cref="IsBusy"/>; a
    /// presented display stays interactive while it redraws.
    /// </summary>
    public bool IsInteractive { get; private set; }

    /// <summary>Gets the error that prevents the display from showing its image, or <c>null</c> when there is none.</summary>
    public Exception? Error { get; private set; }

    /// <summary>Occurs when <see cref="IsBusy"/>, <see cref="IsInteractive"/>, or <see cref="Error"/> changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Occurs when the user taps the image; a tapped marker (if any) is carried on the event args.</summary>
    public event EventHandler<OrientedImageDisplay.ImageClickedEventArgs>? ImageClicked;

    /// <summary>Gets the footprint of the current presentation session.</summary>
    protected OrientedImageFootprint? Footprint { get; private set; }

    /// <summary>Gets the app-owned markers rendered over the image, or <c>null</c>.</summary>
    protected ObservableCollection<OrientedImageMarker>? Markers => _markers;

    /// <summary>
    /// Gets the current presentation session's cancellation token: canceled when a later
    /// <see cref="SetFootprint"/> superseded the footprint it accompanied. Capture it before an await and
    /// re-check it after, before touching display state; canceled until the first <see cref="SetFootprint"/>.
    /// </summary>
    protected CancellationToken SessionToken => _sessionCts?.Token ?? new CancellationToken(canceled: true);

    /// <summary>
    /// Gets or sets the presentation failure surfaced through <see cref="Error"/> after the image's own load error.
    /// The load skeleton records load/present exceptions here; derived displays may record asynchronous
    /// render/device failures. Cleared when a new session starts. Call <see cref="UpdateState"/> after setting.
    /// </summary>
    protected Exception? PresentationError { get; set; }

    // The platform automation-name policy needs the concrete focusable inner view.
#if MAUI
    protected abstract View AutomationNameTarget { get; }
#elif WPF
    protected abstract System.Windows.DependencyObject AutomationNameTarget { get; }
#else
    protected abstract Microsoft.UI.Xaml.DependencyObject AutomationNameTarget { get; }
#endif

    /// <summary>Gets a value indicating whether a presented image is ready for interaction (state gates aside).</summary>
    protected abstract bool IsPresentationInteractive { get; }

    /// <summary>Gets a value indicating whether the presentation itself is busy (e.g. still drawing) beyond loading.</summary>
    protected virtual bool IsPresentationBusy => false;

    /// <summary>Sets the footprint whose oriented image should be displayed.</summary>
    /// <param name="footprint">The footprint to display, or <c>null</c> to clear.</param>
    public void SetFootprint(OrientedImageFootprint? footprint)
    {
        // Each call supersedes the previous presentation session (see SessionToken).
        _sessionCts?.Cancel();
        _sessionCts = new CancellationTokenSource();
        _ = SetFootprintAsync(footprint, _sessionCts.Token);
    }

    /// <summary>Sets the markers rendered over the image.</summary>
    /// <param name="markers">The markers to render, or <c>null</c>.</param>
    public void SetMarkers(ObservableCollection<OrientedImageMarker>? markers)
    {
        if (ReferenceEquals(_markers, markers))
            return;

        _markersListener?.Detach();
        _markersListener = null;
        _markers = markers;

        if (markers is INotifyCollectionChanged incc)
        {
            // Weak: the app-owned collection must not keep a discarded display alive through this subscription.
            _markersListener = new WeakEventListener<OrientedImageInnerDisplay, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>(this, incc)
            {
                OnEventAction = static (instance, source, eventArgs) => instance.OnMarkersChanged(),
                OnDetachAction = static (instance, source, weakEventListener) => source.CollectionChanged -= weakEventListener.OnEvent,
            };
            incc.CollectionChanged += _markersListener.OnEvent;
        }

        OnMarkersChanged();
    }

    /// <summary>Enables or disables automatic recomputation of the footprint as the view changes.</summary>
    /// <param name="enabled">Whether the footprint is automatically updated.</param>
    public void SetAutoUpdateFootprint(bool enabled)
    {
        if (enabled == _autoUpdate)
            return;

        _autoUpdate = enabled;
        OnAutoUpdateFootprintChanged(enabled);
        if (enabled)
            UpdateFootprintCorners(); // push the current view immediately; the view may be static until interaction
        else
            _updateCts?.Cancel(); // don't let an in-flight update land after auto-update was turned off
    }

    /// <summary>Sets the background color shown where the image does not fill the display.</summary>
    /// <param name="color">The background color, or <see cref="System.Drawing.Color.Empty"/> to keep the display's default.</param>
    public abstract void SetBackgroundColor(System.Drawing.Color color);

    // Makes the loaded image visible. Runs inside the load skeleton's try: throw (or let cancellation throw) to
    // record a presentation failure; check the token after every await before touching display state.
    protected abstract Task PresentAsync(OrientedImage image, Uri dataUri, CancellationToken token);

    // Blanks the presentation NOW (visuals, dimensions, on-image markers). Called synchronously when the displayed
    // image changes or goes away, and by derived code when a present attempt yields nothing displayable.
    protected abstract void ClearPresentation();

    // A new marker collection was set or the current one changed; rebuild subscriptions and re-render.
    protected abstract void OnMarkersChanged();

    // Subscribe/unsubscribe the platform view-change event that should drive UpdateFootprintCorners.
    protected abstract void OnAutoUpdateFootprintChanged(bool enabled);

    // Projects the current view onto the image as an ordered pixel ring; false when it can't be computed yet.
    protected abstract bool TryGetFootprintCorners(out IReadOnlyList<PointF> corners);

    // The load skeleton finished for the current session (present, clear, or failure) - state and image dimensions
    // are settled. Derived displays re-resolve dimension-dependent visuals (e.g. panoramic markers) here.
    protected virtual void OnPresentCompleted()
    {
    }

    // Error precedence: the image's own load error, then anything the presentation recorded (decode/present/render).
    protected virtual Exception? ResolveError() => Footprint?.OrientedImage?.LoadError ?? PresentationError;

    /// <summary>Raises <see cref="ImageClicked"/>.</summary>
    protected void RaiseImageClicked(OrientedImageDisplay.ImageClickedEventArgs args) => ImageClicked?.Invoke(this, args);

    // Resolves the display's state from its sources and raises StateChanged when it changes.
    // IsBusy means "loading, or the presentation is busy"; IsInteractive means "presented, unlocked, no error".
    protected void UpdateState()
    {
        Exception? error = ResolveError();
        bool busy = _isLoading || IsPresentationBusy;
        bool interactive = !_isLoading && error is null && IsPresentationInteractive;
        if (busy == IsBusy && interactive == IsInteractive && ReferenceEquals(error, Error))
            return;

        IsBusy = busy;
        IsInteractive = interactive;
        Error = error;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    // Projects the current view onto the image (TryGetFootprintCorners) and pushes the ring to the footprint.
    // Runs on view changes while auto-update is enabled, and once when a new image finishes presenting.
    // Cancel-prior, so a stale view never wins a race; superseding the footprint or disabling auto-update cancels too.
    protected async void UpdateFootprintCorners()
    {
        if (!_autoUpdate || Footprint is not OrientedImageFootprint footprint)
            return;

        if (!TryGetFootprintCorners(out IReadOnlyList<PointF> corners))
            return;

        _updateCts?.Cancel();
        CancellationTokenSource cts = new();
        _updateCts = cts;
        try
        {
            await footprint.UpdateFootprintAsync(corners, cts.Token);
        }
        catch
        {
            // Ignore cancellation/failures from a superseded update.
        }
    }

    // Gives the focusable inner view a meaningful screen-reader label instead of a generic one.
    protected void UpdateAutomationName()
    {
        string name = Footprint?.OrientedImage?.Type is OrientedImageType type
            ? string.Format(CultureInfo.CurrentCulture, Properties.Resources.GetString("OrientedImageDisplayImageAutomationNameFormat") ?? "Oriented image, {0}", type)
            : Properties.Resources.GetString("OrientedImageDisplayAutomationName") ?? "Oriented image display";
#if WPF
        System.Windows.Automation.AutomationProperties.SetName(AutomationNameTarget, name);
#elif WINDOWS_XAML
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(AutomationNameTarget, name);
#elif MAUI
        SemanticProperties.SetDescription(AutomationNameTarget, name);
#endif
    }

    private async Task SetFootprintAsync(OrientedImageFootprint? footprint, CancellationToken token)
    {
        OrientedImage? image = footprint?.OrientedImage;
        bool imageChanged = !ReferenceEquals(Footprint?.OrientedImage, image);

        // Replacing a still-loading image: cancel it (no-op if already loaded).
        if (imageChanged)
            Footprint?.OrientedImage?.CancelLoad();

        // An in-flight footprint-corner update must not mutate a footprint this display no longer manages.
        if (!ReferenceEquals(Footprint, footprint))
            _updateCts?.Cancel();

        Footprint = footprint;
        PresentationError = null;
        UpdateAutomationName();

        if (imageChanged)
            ClearPresentation();

        if (image is null)
        {
            _isLoading = false;
            UpdateState();
            OnPresentCompleted();
            return;
        }

        _isLoading = true;
        UpdateState();
        try
        {
            // The image resolves its DataUri during load (downloads the image file or first attachment).
            await image.RetryLoadAsync();
            token.ThrowIfCancellationRequested();

            if (image.DataUri is not Uri uri)
            {
                // Loaded with nothing displayable (attachment without image, or a load failure surfaced via Error).
                ClearPresentation();
                return;
            }

            await PresentAsync(image, uri, token);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer footprint, or torn down; not an error to surface.
        }
        catch (Exception ex)
        {
            // Only the session that still owns the display may record a failure; a superseded load's late
            // exception must not mark the newer image's state as failed.
            if (!token.IsCancellationRequested)
                PresentationError = ex;
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                _isLoading = false;
                UpdateState();
                OnPresentCompleted();
            }
        }
    }
}
