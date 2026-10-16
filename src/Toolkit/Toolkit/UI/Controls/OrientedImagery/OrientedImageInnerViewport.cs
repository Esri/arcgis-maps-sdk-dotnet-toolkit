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
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

// Base of the planar and panoramic inner viewports, each of which presents through a platform view that the control
// hosts. Each SetFootprint call starts a new presentation session.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "The cancellation sources are cancel-only (no timer), so they need no disposal; canceling on supersede is the release.")]
internal abstract class OrientedImageInnerViewport
{
    private static readonly TimeSpan FootprintUpdateInterval = TimeSpan.FromMilliseconds(250);

    private IEnumerable<OrientedImageMarker>? _markerSource;
    private List<OrientedImageMarker> _markers = [];
    private WeakEventListener<OrientedImageInnerViewport, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>? _markersListener;
    private readonly Dictionary<OrientedImageMarker, MarkerSubscription> _markerListeners = [];
    private CancellationTokenSource? _sessionCts;
    private CancellationTokenSource? _updateCts;
    private int _lookAtCalls; // A LookAtAsync call that sees a newer count reports false
    private MapPoint? _heldLookAt;
    private Task _present = Task.CompletedTask; // the current session's SetFootprintAsync
    private bool _autoUpdate;
    private bool _isLoading; // true through PresentAsync, not only while the image loads
    private DateTime _lastFootprintUpdate; // MinValue, so the first update runs at once
    private bool _footprintUpdatePending;

    // The focusable view, which carries the automation name and id.
#if MAUI
    public abstract View View { get; }
#elif WPF
    public abstract System.Windows.FrameworkElement View { get; }
#else
    public abstract Microsoft.UI.Xaml.FrameworkElement View { get; }
#endif

    public bool IsBusy { get; private set; }

    // False while an image loads or after an error.
    public bool IsInteractive { get; private set; }

    public Exception? Error { get; private set; }

    public event EventHandler? StateChanged;

    public event EventHandler<OrientedImageTappedEventArgs>? ImageTapped;

    protected OrientedImageFootprint? Footprint { get; private set; }

    // In collection order. Each change replaces the list, so a captured reference is a stable snapshot.
    protected IReadOnlyList<OrientedImageMarker> Markers => _markers;

    // Canceled when a later SetFootprint call supersedes the session, and before the first call. Capture it before an
    // await and check it again before touching viewport state.
    protected CancellationToken SessionToken => _sessionCts?.Token ?? new CancellationToken(canceled: true);

    // Derived viewports record asynchronous render or device failures here, then call UpdateState.
    protected Exception? PresentationError { get; set; }

    protected abstract bool IsPresentationInteractive { get; }

    public void SetFootprint(OrientedImageFootprint? footprint)
    {
        _sessionCts?.Cancel();
        _sessionCts = new CancellationTokenSource();
        _present = SetFootprintAsync(footprint, _sessionCts.Token);
    }

    public void SetMarkers(IEnumerable<OrientedImageMarker>? markers)
    {
        if (ReferenceEquals(_markerSource, markers))
            return;

        _markersListener?.Detach();
        _markersListener = null;
        _markerSource = markers;

        if (markers is INotifyCollectionChanged incc)
        {
            // Weak, so the app's collection can't keep a discarded viewport alive.
            _markersListener = new WeakEventListener<OrientedImageInnerViewport, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>(this, incc)
            {
                OnEventAction = static (instance, source, eventArgs) => instance.SyncMarkers(),
                OnDetachAction = static (instance, source, weakEventListener) => source.CollectionChanged -= weakEventListener.OnEvent,
            };
            incc.CollectionChanged += _markersListener.OnEvent;
        }

        SyncMarkers();
    }

    // Diffs by identity against the last read, so the viewport stays in step whatever notifications the source raises.
    private void SyncMarkers()
    {
        _markers = _markerSource?.OfType<OrientedImageMarker>().Distinct().ToList() ?? [];

        var current = new HashSet<OrientedImageMarker>(_markers);
        List<OrientedImageMarker> removed = _markerListeners.Keys.Where(marker => !current.Contains(marker)).ToList();
        List<OrientedImageMarker> added = _markers.Where(marker => !_markerListeners.ContainsKey(marker)).ToList();
        foreach (OrientedImageMarker marker in removed)
        {
            _markerListeners[marker].Detach();
            _markerListeners.Remove(marker);
        }
        foreach (OrientedImageMarker marker in added)
            _markerListeners[marker] = new MarkerSubscription(this, marker);
        OnMarkersChanged(added, removed);
    }

    // Weak, so app-owned markers and symbols can't keep the viewport alive.
    private sealed class MarkerSubscription
    {
        private readonly OrientedImageInnerViewport _viewport;
        private readonly OrientedImageMarker _marker;
        private readonly WeakEventListener<MarkerSubscription, INotifyPropertyChanged, object, PropertyChangedEventArgs> _markerListener;
        private WeakEventListener<MarkerSubscription, INotifyPropertyChanged, object, PropertyChangedEventArgs>? _symbolListener;

        public MarkerSubscription(OrientedImageInnerViewport viewport, OrientedImageMarker marker)
        {
            _viewport = viewport;
            _marker = marker;
            _markerListener = Listen(marker, static (subscription, _, args) => subscription.OnMarkerPropertyChanged(args));
            ListenToSymbol();
        }

        private WeakEventListener<MarkerSubscription, INotifyPropertyChanged, object, PropertyChangedEventArgs> Listen(
            INotifyPropertyChanged source, Action<MarkerSubscription, object?, PropertyChangedEventArgs> onChanged)
        {
            var listener = new WeakEventListener<MarkerSubscription, INotifyPropertyChanged, object, PropertyChangedEventArgs>(this, source)
            {
                OnEventAction = onChanged,
                OnDetachAction = static (_, eventSource, weakListener) => eventSource.PropertyChanged -= weakListener.OnEvent,
            };
            source.PropertyChanged += listener.OnEvent;
            return listener;
        }

        private void ListenToSymbol()
        {
            StopListeningToSymbol();
            if (_marker.Symbol is INotifyPropertyChanged symbol)
                _symbolListener = Listen(symbol, static (subscription, _, _) => subscription.OnSymbolPropertyChanged());
        }

        private void OnMarkerPropertyChanged(PropertyChangedEventArgs args)
        {
            if (args.PropertyName is null or nameof(OrientedImageMarker.Symbol))
                ListenToSymbol();
            _viewport.OnMarkerChanged(_marker, args.PropertyName);
        }

        private void OnSymbolPropertyChanged() => _viewport.OnMarkerChanged(_marker, nameof(OrientedImageMarker.Symbol));

        private void StopListeningToSymbol()
        {
            _symbolListener?.Detach();
            _symbolListener = null;
        }

        public void Detach()
        {
            _markerListener.Detach();
            StopListeningToSymbol();
        }
    }

    public void SetAutoUpdateFootprint(bool enabled)
    {
        if (enabled == _autoUpdate)
            return;

        _autoUpdate = enabled;
        OnAutoUpdateFootprintChanged(enabled);
        if (enabled)
            UpdateFootprint(); // the view may not change until the user interacts
        else
            _updateCts?.Cancel();
    }

    // Color.Empty restores the viewport's default background.
    public abstract void SetBackgroundColor(System.Drawing.Color color);

    // A hidden view lets the control's background show where the view would paint its own backdrop.
    protected void SetViewVisible(bool visible)
    {
#if MAUI
        View.IsVisible = visible;
#elif WPF
        View.Visibility = visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
#else
        View.Visibility = visible ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
#endif
    }

    public void SetAutomationName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            name = Properties.Resources.GetString("OrientedImageViewportAutomationName") ?? "Oriented image";
#if WPF
        System.Windows.Automation.AutomationProperties.SetName(View, name);
#elif WINDOWS_XAML
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(View, name);
#elif MAUI
        SemanticProperties.SetDescription(View, name);
#endif
    }

    // x and y are in device-independent pixels. Returns null off the image or while not interactive.
    public abstract System.Drawing.PointF? ScreenToImage(double x, double y);

    public void SetAutomationId(string? id)
    {
#if WPF
        System.Windows.Automation.AutomationProperties.SetAutomationId(View, id ?? string.Empty);
#elif WINDOWS_XAML
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(View, id ?? string.Empty);
#elif MAUI
        // MAUI lets an element's AutomationId be set only once, so only the first non-empty id applies.
        if (!string.IsNullOrEmpty(id) && View.AutomationId is null)
            View.AutomationId = id;
#endif
    }

    // A call made while the image loads is held, so the image opens facing it.
    public async Task<bool> LookAtAsync(MapPoint target)
    {
        int call = ++_lookAtCalls;
        if (Footprint?.OrientedImage is not OrientedImage image || (!_isLoading && !IsInteractive))
            return false;

        if (_isLoading)
        {
            // After the load, the call proceeds like a new one to report the result.
            _heldLookAt = target;
            await _present;
            if (call != _lookAtCalls || !IsInteractive)
                return false;
        }

        // A device loss or render failure during the conversion ends interactivity without superseding the call.
        LookAtTarget? pixel = await ConvertLookAtTargetAsync(image, target);
        return call == _lookAtCalls && IsInteractive && pixel is LookAtTarget p && TryLookAt(p);
    }

    // Shows the image from the local file at path, facing the target from ConvertHeldLookAtAsync if there is one. Throw
    // to record a presentation failure, and check the token after every await.
    protected abstract Task PresentAsync(OrientedImage image, string path, CancellationToken token);

    // Centers the target at the current zoom, or as near as the view allows, and returns false if it can't. Called
    // only while the viewport is interactive.
    protected abstract bool TryLookAt(LookAtTarget target);

    // AimAtRow is false for a map location without z, because its row comes from a guessed elevation.
    protected readonly record struct LookAtTarget(System.Drawing.PointF Pixel, bool AimAtRow);

    protected async Task<LookAtTarget?> ConvertHeldLookAtAsync(OrientedImage image) =>
        _heldLookAt is MapPoint target ? await ConvertLookAtTargetAsync(image, target) : null;

    // Clears synchronously, including the image dimensions and marker placements.
    protected abstract void ClearPresentation();

    // Both marker hooks can run off the UI thread, because the app raises the events behind them. A change inside a
    // marker's symbol arrives as a Symbol change.
    protected abstract void OnMarkersChanged(IReadOnlyList<OrientedImageMarker> added, IReadOnlyList<OrientedImageMarker> removed);

    protected abstract void OnMarkerChanged(OrientedImageMarker marker, string? propertyName);

    // An image point applies only to its own image; a map location goes through the camera model.
    protected static async Task<System.Drawing.PointF?> ResolveMarkerPixelAsync(OrientedImageMarkerPosition position, OrientedImage? image)
    {
        if (image is null)
            return null;

        if (position.ImagePoint is System.Drawing.PointF imagePoint)
            return ReferenceEquals(position.Image, image) ? imagePoint : null;

        return position.Location is MapPoint location ? await LocationToPixelAsync(image, location).ConfigureAwait(false) : null;
    }

    private static async Task<System.Drawing.PointF?> LocationToPixelAsync(OrientedImage image, MapPoint location)
    {
        // A transform tried before the image loads makes later transforms on it fail.
        if (image.LoadStatus != LoadStatus.Loaded)
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

    // Subscribes to or unsubscribes from the view changes that drive UpdateFootprint.
    protected abstract void OnAutoUpdateFootprintChanged(bool enabled);

    // Starts UpdateFootprintAsync for the current view, or returns null when the view can't be computed. Take the token
    // from NextFootprintUpdateToken last, so a bad view never cancels a good update.
    protected abstract Task? BeginFootprintUpdate(OrientedImageFootprint footprint);

    // Runs when the session's load ends without being superseded, with state and image dimensions settled.
    protected virtual void OnPresentCompleted()
    {
    }

    protected virtual Exception? ResolveError() => Footprint?.OrientedImage?.LoadError ?? PresentationError;

    protected void RaiseImageTapped(OrientedImageTappedEventArgs args) => ImageTapped?.Invoke(this, args);

    protected void UpdateState()
    {
        Exception? error = ResolveError();
        bool busy = _isLoading;
        bool interactive = !_isLoading && error is null && IsPresentationInteractive;
        if (busy == IsBusy && interactive == IsInteractive && ReferenceEquals(error, Error))
            return;

        IsBusy = busy;
        IsInteractive = interactive;
        Error = error;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    // Navigation raises a view change per frame, so runs are at most four per second, with a trailing run after the
    // last change. Latest wins: NextFootprintUpdateToken cancels the update in flight.
    protected async void UpdateFootprint()
    {
        if (!_autoUpdate || Footprint is null || _footprintUpdatePending)
            return;

        TimeSpan wait = _lastFootprintUpdate + FootprintUpdateInterval - DateTime.UtcNow;
        if (wait > TimeSpan.Zero)
        {
            _footprintUpdatePending = true;
            await Task.Delay(wait);
            _footprintUpdatePending = false;
        }

        if (!_autoUpdate || Footprint is not OrientedImageFootprint footprint)
            return;

        _lastFootprintUpdate = DateTime.UtcNow;
        try
        {
            if (BeginFootprintUpdate(footprint) is Task update)
                await update;
        }
        catch
        {
            // Ignore cancellation by a newer update and any failure.
        }
    }

    protected CancellationToken NextFootprintUpdateToken()
    {
        _updateCts?.Cancel();
        _updateCts = new CancellationTokenSource();
        return _updateCts.Token;
    }

    private static async Task<LookAtTarget?> ConvertLookAtTargetAsync(OrientedImage image, MapPoint target)
    {
        System.Drawing.PointF? pixel = await LocationToPixelAsync(image, target);
        return pixel is System.Drawing.PointF p ? new LookAtTarget(p, target.HasZ) : null;
    }

    private async Task SetFootprintAsync(OrientedImageFootprint? footprint, CancellationToken token)
    {
        OrientedImage? image = footprint?.OrientedImage;
        bool imageChanged = !ReferenceEquals(Footprint?.OrientedImage, image);

        // Cancel an abandoned download when paging, which also cancels other callers loading the same image.
        if (imageChanged)
            Footprint?.OrientedImage?.CancelLoad();

        // An update in flight must not change a footprint that this viewport has stopped managing.
        if (!ReferenceEquals(Footprint, footprint))
            _updateCts?.Cancel();

        // A footprint change supersedes look-at calls in progress; a held one reports false when it wakes.
        _lookAtCalls++;
        _heldLookAt = null;

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
            await image.RetryLoadAsync();
            token.ThrowIfCancellationRequested();

            // Loading downloads the image or its first attachment to a local file. An attachment without an image
            // leaves nothing to display.
            if (image.DataUri is not Uri uri)
            {
                ClearPresentation();
                return;
            }

            await PresentAsync(image, uri.LocalPath, token);
        }
        catch (OperationCanceledException)
        {
            // Superseded or torn down, which is not an error.
        }
        catch (Exception ex)
        {
            // A failed load leaves nothing behind the error. A superseded load's late exception must not touch the
            // newer image.
            if (!token.IsCancellationRequested)
            {
                PresentationError = ex;
                ClearPresentation();
            }
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
