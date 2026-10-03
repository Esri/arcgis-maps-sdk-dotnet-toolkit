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
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
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

using MarkerListener = WeakEventListener<OrientedImageInnerDisplay, INotifyPropertyChanged, object, PropertyChangedEventArgs>;

// Base of OrientedImageDisplay's inner displays. Owns the presentation session (one footprint and one cancellation
// token per SetFootprint), the reported state, the marker subscriptions and the auto-update-footprint plumbing.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Platform view types are not IDisposable by convention. The session CTS is cancel-only (no timer), so it needs no disposal; canceling it on supersede is the release.")]
#if MAUI
internal abstract class OrientedImageInnerDisplay : ContentView
#else
internal abstract class OrientedImageInnerDisplay : ContentControl
#endif
{
    private IEnumerable<OrientedImageMarker>? _markerSource;
    private List<OrientedImageMarker> _markers = []; // Snapshot read from the source
    private WeakEventListener<OrientedImageInnerDisplay, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>? _markersListener;
    private readonly Dictionary<OrientedImageMarker, MarkerListener> _markerListeners = [];
    private readonly Dictionary<OrientedImageMarker, INotifyPropertyChanged> _markerSymbols = [];

    // One listener per symbol, however many markers share it.
    private readonly Dictionary<INotifyPropertyChanged, (MarkerListener Listener, HashSet<OrientedImageMarker> Markers)> _symbolListeners =
        new(ReferenceEqualityComparer.Instance);
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

    /// <summary>Gets a value indicating whether the display has a presented, unlocked image and no <see cref="Error"/>.
    /// Independent of <see cref="IsBusy"/>: a presented display stays interactive while it redraws.</summary>
    public bool IsInteractive { get; private set; }

    /// <summary>Gets the error that prevents the display from showing its image, or <c>null</c> when there is none.</summary>
    public Exception? Error { get; private set; }

    /// <summary>Occurs when <see cref="IsBusy"/>, <see cref="IsInteractive"/>, or <see cref="Error"/> changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Occurs when the user taps the image; a tapped marker (if any) is carried on the event args.</summary>
    public event EventHandler<OrientedImageTappedEventArgs>? ImageTapped;

    /// <summary>Gets the footprint of the current presentation session.</summary>
    protected OrientedImageFootprint? Footprint { get; private set; }

    /// <summary>Gets the markers rendered over the image, in the app's collection order.
    /// The list is replaced on each change, never modified, so a reference to it is a stable snapshot.</summary>
    protected IReadOnlyList<OrientedImageMarker> Markers => _markers;

    /// <summary>Gets the session token: canceled when a later <see cref="SetFootprint"/> supersedes this one, and
    /// before the first one. Capture it before an await and re-check it before touching display state.</summary>
    protected CancellationToken SessionToken => _sessionCts?.Token ?? new CancellationToken(canceled: true);

    /// <summary>Gets or sets a presentation failure to surface through <see cref="Error"/>; derived displays record
    /// asynchronous render/device failures here and then call <see cref="UpdateState"/>. Cleared per session.</summary>
    protected Exception? PresentationError { get; set; }

    // The focusable inner view, which carries the automation name and id.
#if MAUI
    protected abstract View AutomationTarget { get; }
#elif WPF
    protected abstract System.Windows.DependencyObject AutomationTarget { get; }
#else
    protected abstract Microsoft.UI.Xaml.DependencyObject AutomationTarget { get; }
#endif

    /// <summary>Gets a value indicating whether a presented image is ready for interaction (state permitting).</summary>
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
    public void SetMarkers(IEnumerable<OrientedImageMarker>? markers)
    {
        if (ReferenceEquals(_markerSource, markers))
            return;

        _markersListener?.Detach();
        _markersListener = null;
        _markerSource = markers;

        if (markers is INotifyCollectionChanged incc)
        {
            // Weak: the app-owned collection must not keep a discarded display alive through this subscription.
            _markersListener = new WeakEventListener<OrientedImageInnerDisplay, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>(this, incc)
            {
                OnEventAction = static (instance, source, eventArgs) => instance.SyncMarkers(),
                OnDetachAction = static (instance, source, weakEventListener) => source.CollectionChanged -= weakEventListener.OnEvent,
            };
            incc.CollectionChanged += _markersListener.OnEvent;
        }

        SyncMarkers();
    }

    // Reads the source and reports which markers were added and removed since the last read. Comparing by marker
    // identity keeps the display in step with the source, whatever notifications it raises. A marker that appears more
    // than once is shown once, and null items are skipped.
    private void SyncMarkers()
    {
        _markers = _markerSource?.OfType<OrientedImageMarker>().Distinct().ToList() ?? [];

        // The markers being listened to are those from the last read.
        var current = new HashSet<OrientedImageMarker>(_markers);
        List<OrientedImageMarker> removed = _markerListeners.Keys.Where(marker => !current.Contains(marker)).ToList();
        List<OrientedImageMarker> added = _markers.Where(marker => !_markerListeners.ContainsKey(marker)).ToList();
        removed.ForEach(StopListening);
        added.ForEach(ListenTo);
        OnMarkersChanged(added, removed);
    }

    // Weak, like the collection subscription: an app-owned long-lived marker or symbol must not keep the display alive.
    private MarkerListener Listen(INotifyPropertyChanged source, Action<OrientedImageInnerDisplay, object?, PropertyChangedEventArgs> onEvent)
    {
        var listener = new MarkerListener(this, source)
        {
            OnEventAction = onEvent,
            OnDetachAction = static (_, eventSource, weakEventListener) => eventSource.PropertyChanged -= weakEventListener.OnEvent,
        };
        source.PropertyChanged += listener.OnEvent;
        return listener;
    }

    private void ListenTo(OrientedImageMarker marker)
    {
        _markerListeners[marker] = Listen(marker, static (instance, sender, eventArgs) => instance.OnMarkerPropertyChanged(sender, eventArgs));
        ListenToSymbol(marker);
    }

    // The SDK symbol is what is drawn, and it notifies its own changes.
    private void ListenToSymbol(OrientedImageMarker marker)
    {
        StopListeningToSymbol(marker);
        if (marker.Symbol is not INotifyPropertyChanged symbol)
            return;

        if (!_symbolListeners.TryGetValue(symbol, out var entry))
        {
            entry = (Listen(symbol, static (instance, sender, _) => instance.OnSymbolPropertyChanged(sender)), new HashSet<OrientedImageMarker>());
            _symbolListeners[symbol] = entry;
        }

        entry.Markers.Add(marker);
        _markerSymbols[marker] = symbol;
    }

    private void StopListeningToSymbol(OrientedImageMarker marker)
    {
        if (!_markerSymbols.Remove(marker, out INotifyPropertyChanged? symbol))
            return;

        (MarkerListener listener, HashSet<OrientedImageMarker> markers) = _symbolListeners[symbol];
        markers.Remove(marker);
        if (markers.Count == 0)
        {
            listener.Detach();
            _symbolListeners.Remove(symbol);
        }
    }

    private void StopListening(OrientedImageMarker marker)
    {
        if (_markerListeners.Remove(marker, out var listener))
            listener.Detach();
        StopListeningToSymbol(marker);
    }

    private void OnSymbolPropertyChanged(object? sender)
    {
        if (sender is INotifyPropertyChanged symbol && _symbolListeners.TryGetValue(symbol, out var entry))
        {
            foreach (OrientedImageMarker marker in entry.Markers.ToArray())
                OnMarkerChanged(marker, nameof(OrientedImageMarker.Symbol));
        }
    }

    private void OnMarkerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not OrientedImageMarker marker)
            return;

        if (e.PropertyName is null or nameof(OrientedImageMarker.Symbol))
            ListenToSymbol(marker);
        OnMarkerChanged(marker, e.PropertyName);
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
            UpdateFootprint(); // push the current view immediately; the view may be static until interaction
        else
            _updateCts?.Cancel(); // don't let an in-flight update land after auto-update was turned off
    }

    /// <summary>Sets the background color shown where the image does not fill the display.</summary>
    /// <param name="color">The background color, or <see cref="System.Drawing.Color.Empty"/> to keep the display's default.</param>
    public abstract void SetBackgroundColor(System.Drawing.Color color);

    /// <summary>Labels the focusable inner view for screen readers with the name the app gave the control.</summary>
    /// <param name="name">The app's name, or <c>null</c> or empty for the localized default.</param>
    public void SetAutomationName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            name = Properties.Resources.GetString("OrientedImageDisplayAutomationName") ?? "Oriented image display";
#if WPF
        System.Windows.Automation.AutomationProperties.SetName(AutomationTarget, name);
#elif WINDOWS_XAML
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(AutomationTarget, name);
#elif MAUI
        SemanticProperties.SetDescription(AutomationTarget, name);
#endif
    }

    /// <summary>Converts a position in the display, in device-independent pixels, to the image coordinate under it.</summary>
    /// <returns>The image coordinate, or <c>null</c> when no image is under the position or the display is not interactive.</returns>
    public abstract System.Drawing.PointF? ScreenToImage(double x, double y);

    /// <summary>Gives the focusable inner view the automation id the app gave the control, so UI tests can find it.</summary>
    /// <param name="id">The app's automation id, or <c>null</c> or empty for none.</param>
    public void SetAutomationId(string? id)
    {
#if WPF
        System.Windows.Automation.AutomationProperties.SetAutomationId(AutomationTarget, id ?? string.Empty);
#elif WINDOWS_XAML
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(AutomationTarget, id ?? string.Empty);
#elif MAUI
        // An element's AutomationId can be set once, so only a first, non-empty id is applied.
        if (!string.IsNullOrEmpty(id) && AutomationTarget.AutomationId is null)
            AutomationTarget.AutomationId = id;
#endif
    }

    // Makes the loaded image visible. Runs inside the load skeleton's try: throw (or let cancellation throw) to
    // record a presentation failure; check the token after every await before touching display state.
    protected abstract Task PresentAsync(OrientedImage image, Uri dataUri, CancellationToken token);

    // Blanks the presentation synchronously: visuals, dimensions and on-image markers.
    protected abstract void ClearPresentation();

    // Marker hooks; implementations re-render. Both can run off the UI thread, because the app raises the events
    // behind them. A change inside the marker's symbol is reported as a change of Symbol.
    protected abstract void OnMarkersChanged(IReadOnlyList<OrientedImageMarker> added, IReadOnlyList<OrientedImageMarker> removed);

    protected abstract void OnMarkerChanged(OrientedImageMarker marker, string? propertyName);

    // The image pixel a marker sits on: an image point on its own image only, or a world location through the camera
    // model. Null when the marker isn't on the image.
    protected static async Task<System.Drawing.PointF?> ResolveMarkerPixelAsync(OrientedImageMarkerPosition position, OrientedImage? image)
    {
        if (image is null)
            return null;

        if (position.ImagePoint is System.Drawing.PointF imagePoint)
            return ReferenceEquals(position.Image, image) ? imagePoint : null;

        // A transform tried before the image loads makes later transforms on it fail.
        if (position.Location is not Esri.ArcGISRuntime.Geometry.MapPoint location || image.LoadStatus != LoadStatus.Loaded)
            return null;

        try
        {
            System.Drawing.PointF pixel = await image.LocationToImageAsync(location).ConfigureAwait(false);

            // A location at or behind the camera can project to NaN or infinity.
            return float.IsFinite(pixel.X) && float.IsFinite(pixel.Y) ? pixel : null;
        }
        catch
        {
            return null;
        }
    }

    // Subscribe/unsubscribe the platform view-change event that should drive UpdateFootprint.
    protected abstract void OnAutoUpdateFootprintChanged(bool enabled);

    // Pushes the current view through the UpdateFootprintAsync overload for the image type, or returns null when the
    // view can't be computed yet. Take the token from NextFootprintUpdateToken last, so a bad view never cancels a good update.
    protected abstract Task? BeginFootprintUpdate(OrientedImageFootprint footprint);

    // The load skeleton finished for the current session (present, clear, or failure) - state and image dimensions
    // are settled. Derived displays re-resolve dimension-dependent visuals (e.g. panoramic markers) here.
    protected virtual void OnPresentCompleted()
    {
    }

    // Error precedence: the image's own load error, then anything the presentation recorded (decode/present/render).
    protected virtual Exception? ResolveError() => Footprint?.OrientedImage?.LoadError ?? PresentationError;

    /// <summary>Raises <see cref="ImageTapped"/>.</summary>
    protected void RaiseImageTapped(OrientedImageTappedEventArgs args) => ImageTapped?.Invoke(this, args);

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

    // Pushes the current view to the footprint. Latest wins: NextFootprintUpdateToken cancels the in-flight update.
    protected async void UpdateFootprint()
    {
        if (!_autoUpdate || Footprint is not OrientedImageFootprint footprint)
            return;

        try
        {
            if (BeginFootprintUpdate(footprint) is Task update)
                await update;
        }
        catch
        {
            // Ignore cancellation/failures from a superseded update.
        }
    }

    // Cancels the in-flight footprint update and returns the token for the next one.
    protected CancellationToken NextFootprintUpdateToken()
    {
        _updateCts?.Cancel();
        _updateCts = new CancellationTokenSource();
        return _updateCts.Token;
    }

    private async Task SetFootprintAsync(OrientedImageFootprint? footprint, CancellationToken token)
    {
        OrientedImage? image = footprint?.OrientedImage;
        bool imageChanged = !ReferenceEquals(Footprint?.OrientedImage, image);

        // Cancel abandoned downloads during paging. This also cancels other callers loading the same image.
        if (imageChanged)
            Footprint?.OrientedImage?.CancelLoad();

        // An in-flight footprint update must not mutate a footprint this display no longer manages.
        if (!ReferenceEquals(Footprint, footprint))
            _updateCts?.Cancel();

        Footprint = footprint;
        PresentationError = null;

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
            // A superseded load's late exception must not mark the newer image as failed.
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
