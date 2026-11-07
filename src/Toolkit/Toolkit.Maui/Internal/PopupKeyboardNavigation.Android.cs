// /*******************************************************************************
//  * Copyright 2026 Esri
//  *
//  * Licensed under the Apache License, Version 2.0 (the "License");
//  * you may not use this file except in compliance with the License.
//  * You may obtain a copy of the License at
//  *
//  * http://www.apache.org/licenses/LICENSE-2.0
//  *
//  * Unless required by applicable law or agreed to in writing, software
//  * distributed under the License is distributed on an "AS IS" BASIS,
//  * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  * See the License for the specific language governing permissions and
//  * limitations under the License.
//  ******************************************************************************/

#if __ANDROID__
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using AndroidX.Core.View;
using AndroidX.Core.View.Accessibility;
using AndroidX.RecyclerView.Widget;
using Esri.ArcGISRuntime.Mapping.Popups;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System.Globalization;
using NativeView = Android.Views.View;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal static class PopupKeyboardNavigation
{
    internal static void Attach(ItemsView view) => view.Behaviors.Add(new ItemsFocusBehavior());

    internal static bool IsActivationKey(Keycode key) =>
        key is Keycode.Enter or Keycode.NumpadEnter or Keycode.DpadCenter or Keycode.Space;

    internal static bool IsTabNavigation(Keycode key, KeyEvent e) =>
        key == Keycode.Tab && (e.HasNoModifiers || e.HasModifiers(MetaKeyStates.ShiftOn));

    private sealed class ItemsFocusBehavior : Behavior<ItemsView>, IDisposable
    {
        private readonly Dictionary<NativeView, FocusState> _originalFocus = new();
        private ItemsView? _view;
        private RecyclerView? _recycler;
        private FocusListener? _listener;
        private PagerSnapHelper? _snapHelper;
        private bool _disposed;

        protected override void OnAttachedTo(ItemsView view)
        {
            base.OnAttachedTo(view);
            _view = view;
            view.HandlerChanged += OnConnected;
            view.HandlerChanging += OnHandlerChanging;
            view.Loaded += OnConnected;
            view.Unloaded += OnUnloaded;
            Connect();
        }

        protected override void OnDetachingFrom(ItemsView view)
        {
            Disconnect();
            view.HandlerChanged -= OnConnected;
            view.HandlerChanging -= OnHandlerChanging;
            view.Loaded -= OnConnected;
            view.Unloaded -= OnUnloaded;
            _view = null;
            base.OnDetachingFrom(view);
        }

        public void Dispose()
        {
            _disposed = true;
            Disconnect();
            GC.SuppressFinalize(this);
        }

        private void OnConnected(object? sender, EventArgs e) => Connect();

        private void OnHandlerChanging(object? sender, HandlerChangingEventArgs e) => Disconnect();

        private void OnUnloaded(object? sender, EventArgs e) => Disconnect();

        private void Connect()
        {
            if (_disposed)
                return;
            var recycler = _view?.Handler?.PlatformView as RecyclerView;
            if (ReferenceEquals(recycler, _recycler))
            {
                UpdateFocus();
                return;
            }

            Disconnect();
            _recycler = recycler;
            if (recycler is null)
                return;

            _listener = new FocusListener(this);
            recycler.AddOnChildAttachStateChangeListener(_listener);
            if (_view is CarouselView)
            {
                _snapHelper = new PagerSnapHelper();
                recycler.AddOnScrollListener(_listener);
                recycler.AddOnUnhandledKeyEventListener(_listener);
            }
            UpdateFocus();
        }

        private void Disconnect()
        {
            if (_recycler is not null && _listener is not null)
            {
                _recycler.RemoveOnChildAttachStateChangeListener(_listener);
                _recycler.RemoveOnScrollListener(_listener);
                _recycler.RemoveOnUnhandledKeyEventListener(_listener);
            }
            _listener?.Dispose();
            _snapHelper?.Dispose();
            _listener = null;
            _snapHelper = null;
            foreach (var (view, state) in _originalFocus)
                state.Restore(view);
            _originalFocus.Clear();
            _recycler = null;
        }

        private void UpdateFocus()
        {
            if (_recycler is not { } recycler)
                return;

            var current = recycler.GetLayoutManager() is { } layout ? _snapHelper?.FindSnapView(layout) : null;
            int position = current is null ? RecyclerView.NoPosition : recycler.GetChildAdapterPosition(current);
            ConfigureContainer(recycler, DescendantFocusability.AfterDescendants);
            for (int i = 0; i < recycler.ChildCount; i++)
            {
                if (recycler.GetChildAt(i) is not NativeView item)
                    continue;

                bool active = _snapHelper is null || recycler.GetChildAdapterPosition(item) == position;
                if (!active && item.FindFocus() is NativeView focused && !MoveOutside(focused, false))
                    focused.ClearFocus();
                // Keep attachment rows and the displayed slide reachable, not their wrappers or recycled slides.
                ConfigureContainer(item, active ? DescendantFocusability.AfterDescendants : DescendantFocusability.BlockDescendants);
            }
        }

        private void ConfigureContainer(NativeView view, DescendantFocusability descendants)
        {
            if (!_originalFocus.ContainsKey(view))
                _originalFocus.Add(view, new FocusState(view.Focusable, view.FocusableInTouchMode, (view as ViewGroup)?.DescendantFocusability));
            view.FocusableInTouchMode = false;
            view.Focusable = false;
            if (view is ViewGroup group)
                group.DescendantFocusability = descendants;
        }

        private void RestoreItem(NativeView view)
        {
            if (_originalFocus.Remove(view, out var state))
                state.Restore(view);
        }

        private bool MoveOutside(NativeView focused, bool backward)
        {
            if (_recycler?.RootView is not ViewGroup root)
                return false;

            var direction = backward ? FocusSearchDirection.Backward : FocusSearchDirection.Forward;
            var visited = new HashSet<NativeView>();
            NativeView? candidate = focused;
            while (candidate is not null && visited.Add(candidate))
            {
                candidate = FocusFinder.Instance?.FindNextFocus(root, candidate, direction);
                if (candidate is not null && !IsInsideCarousel(candidate))
                    return candidate.RequestFocus(direction);
            }
            return false;
        }

        private bool IsInsideCarousel(NativeView view)
        {
            for (NativeView? parent = view; parent is not null; parent = parent.Parent as NativeView)
            {
                if (ReferenceEquals(parent, _recycler))
                    return true;
            }
            return false;
        }

        private readonly record struct FocusState(bool Focusable, bool TouchFocusable, DescendantFocusability? Descendants)
        {
            internal void Restore(NativeView view)
            {
                view.FocusableInTouchMode = TouchFocusable;
                view.Focusable = Focusable;
                if (Descendants is { } descendants && view is ViewGroup group)
                    group.DescendantFocusability = descendants;
            }
        }

        private sealed class FocusListener : RecyclerView.OnScrollListener,
            RecyclerView.IOnChildAttachStateChangeListener, NativeView.IOnUnhandledKeyEventListener
        {
            private readonly ItemsFocusBehavior _owner;

            internal FocusListener(ItemsFocusBehavior owner) => _owner = owner;

            public void OnChildViewAttachedToWindow(NativeView view) => _owner.UpdateFocus();

            public void OnChildViewDetachedFromWindow(NativeView view) => _owner.RestoreItem(view);

            public override void OnScrolled(RecyclerView recyclerView, int dx, int dy) => _owner.UpdateFocus();

            public bool OnUnhandledKeyEvent(NativeView? view, KeyEvent? e) =>
                e is { Action: KeyEventActions.Down } && IsTabNavigation(e.KeyCode, e) &&
                _owner._recycler?.FindFocus() is NativeView focused && _owner.MoveOutside(focused, e.IsShiftPressed);
        }
    }
}

internal sealed class PopupMediaPager : StackLayout
{
    private readonly CarouselView _carousel;
    private readonly IndicatorView _indicators;
    private ViewGroup? _platformView;
    private AccessibilityManager? _accessibilityManager;
    private int _previousId = -1;
    private int _nextId = -1;
    private bool _accessiblePager;
    private bool? _carouselExcluded;
    private bool? _indicatorsExcluded;

    internal PopupMediaPager(CarouselView carousel, IndicatorView indicators)
    {
        _carousel = carousel;
        _indicators = indicators;
        // Keep dot selection from being treated as selection of an item in the outer ScrollView.
        AutomationProperties.SetIsInAccessibleTree(indicators, true);
        Children.Add(carousel);
        Children.Add(indicators);
        Loaded += (_, _) => Connect();
        Unloaded += (_, _) => Disconnect();
    }

    private IList<PopupMedia>? Media => _carousel.ItemsSource as IList<PopupMedia>;

    private PopupMedia? CurrentMedia => Media is { } media && _carousel.Position >= 0 && _carousel.Position < media.Count
        ? media[_carousel.Position] : null;

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        Disconnect();
        base.OnHandlerChanging(args);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (IsLoaded)
            Connect();
    }

    private void Connect()
    {
        if (Handler?.PlatformView is not ViewGroup platformView || ReferenceEquals(platformView, _platformView))
            return;

        Disconnect();
        _platformView = platformView;
        _accessibilityManager = platformView.Context?.GetSystemService(Android.Content.Context.AccessibilityService) as AccessibilityManager;
        if (_accessibilityManager is not null)
            _accessibilityManager.TouchExplorationStateChange += OnTouchExplorationChanged;
        _carousel.PropertyChanged += OnMediaChanged;
        _indicators.PropertyChanged += OnMediaChanged;
        _indicators.HandlerChanged += OnIndicatorHandlerChanged;
        platformView.KeyPress += OnKeyPress;
        UpdateAccessibility();
    }

    private void Disconnect()
    {
        if (_accessibilityManager is not null)
            _accessibilityManager.TouchExplorationStateChange -= OnTouchExplorationChanged;
        _accessibilityManager = null;
        _carousel.PropertyChanged -= OnMediaChanged;
        _indicators.PropertyChanged -= OnMediaChanged;
        _indicators.HandlerChanged -= OnIndicatorHandlerChanged;
        if (_platformView is not null)
        {
            UpdateAccessibility();
            _platformView.KeyPress -= OnKeyPress;
        }
        _platformView = null;
    }

    private void OnMediaChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // The carousel already reports changes to these bound indicator properties.
        if (ReferenceEquals(sender, _indicators) && e.PropertyName is nameof(IndicatorView.ItemsSource) or nameof(IndicatorView.Position))
            return;

        if (e.PropertyName is nameof(CarouselView.ItemsSource) or nameof(CarouselView.Position) or nameof(CarouselView.CurrentItem) or
            nameof(CarouselView.Loop) or nameof(IndicatorView.Count) or nameof(IndicatorView.MaximumVisible) or
            nameof(IndicatorView.HideSingle) or nameof(IndicatorView.IndicatorTemplate) or nameof(IndicatorView.IndicatorLayout))
            Dispatcher.Dispatch(UpdateAccessibility);
    }

    private void OnIndicatorHandlerChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(UpdateAccessibility);

    private void OnTouchExplorationChanged(object? sender, AccessibilityManager.TouchExplorationStateChangeEventArgs e) =>
        Dispatcher.Dispatch(UpdateAccessibility);

    private void UpdateAccessibility()
    {
        if (_platformView is not { } host)
            return;

        UpdateIndicators();
        bool enabled = _accessibilityManager?.IsTouchExplorationEnabled == true;
        if (enabled && !_accessiblePager)
        {
            _carouselExcluded = AutomationProperties.GetExcludedWithChildren(_carousel);
            _indicatorsExcluded = AutomationProperties.GetExcludedWithChildren(_indicators);
        }
        if (enabled || _accessiblePager)
        {
            AutomationProperties.SetExcludedWithChildren(_carousel, enabled ? true : _carouselExcluded);
            AutomationProperties.SetExcludedWithChildren(_indicators, enabled ? true : _indicatorsExcluded);
        }
        _accessiblePager = enabled;

        int count = Media?.Count ?? 0;
        var media = enabled ? CurrentMedia : null;
        bool hasMedia = media is not null;
        bool canOpen = Primitives.PopupMediaView.GetImageLink(media) is not null;
        bool canPage = hasMedia && count > 1;
        host.ImportantForAccessibility = hasMedia ? ImportantForAccessibility.Yes : ImportantForAccessibility.No;
        host.ScreenReaderFocusable = hasMedia;
        host.DescendantFocusability = enabled ? DescendantFocusability.BlockDescendants : DescendantFocusability.AfterDescendants;

        string? description = null;
        if (media is not null)
        {
            var position = string.Format(CultureInfo.CurrentCulture, Properties.Resources.GetString("PopupViewerMediaPosition"), _carousel.Position + 1, count);
            description = string.Join(", ", new[] { media.AlternativeText, media.Title, media.Caption, position }
                .Where(text => !string.IsNullOrWhiteSpace(text)).Distinct(StringComparer.Ordinal));
        }
        if (host.ContentDescription != description)
            host.ContentDescription = description;
        host.Focusable = canPage || canOpen || media is { Type: not PopupMediaType.Image };

        UpdateAction(ref _previousId, "PopupViewerPreviousMediaButtonText", -1, CanMove(-1));
        UpdateAction(ref _nextId, "PopupViewerNextMediaButtonText", 1, CanMove(1));
        if (host.Clickable != canOpen)
        {
            if (canOpen)
            {
                host.Click += OnClick;
                using var label = new Java.Lang.String(Properties.Resources.GetString("PopupViewerViewHyperlinkText"));
                ViewCompat.ReplaceAccessibilityAction(host, AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionClick!, label, null);
            }
            else
            {
                host.Click -= OnClick;
                ViewCompat.RemoveAccessibilityAction(host, (int)Android.Views.Accessibility.Action.Click);
            }
            host.Clickable = canOpen;
        }
    }

    private void UpdateAction(ref int id, string labelKey, int direction, bool available)
    {
        if (_platformView is not { } host)
            return;
        if (available && id == -1)
        {
            using var label = new Java.Lang.String(Properties.Resources.GetString(labelKey));
            // The native view retains the command until its action is removed.
            id = ViewCompat.AddAccessibilityAction(host, label, new MediaAction(this, direction));
            if (id == -1)
                throw new InvalidOperationException("Unable to register the popup media accessibility action.");
        }
        else if (!available && id != -1)
        {
            ViewCompat.RemoveAccessibilityAction(host, id);
            id = -1;
        }
    }

    internal bool CanMove(int direction) =>
        _accessiblePager && _platformView?.Enabled == true && Media is { Count: > 1 } media &&
        _carousel.Position >= 0 && _carousel.Position < media.Count &&
        (_carousel.Loop || (_carousel.Position + direction >= 0 && _carousel.Position + direction < media.Count));

    internal bool Move(int direction)
    {
        if (!CanMove(direction) || _platformView is not { } host || Media is not { } media)
            return false;
        int position = _carousel.Position + direction;
        if (_carousel.Loop)
            position = (position + media.Count) % media.Count;
        _carousel.Position = position;
        _carousel.ScrollTo(position, position: ScrollToPosition.Center, animate: false);
        UpdateAccessibility();
        if (ReferenceEquals(host, _platformView))
        {
            // Restore the current-item focus after TalkBack closes its action menu.
            host.Post(() =>
            {
                if (_accessiblePager && ReferenceEquals(host, _platformView) && CurrentMedia is not null)
                    host.PerformAccessibilityAction(Android.Views.Accessibility.Action.AccessibilityFocus, null);
            });
        }
        return true;
    }

    private void OnClick(object? sender, EventArgs e)
    {
        if (_accessiblePager && _platformView?.Enabled == true)
            Primitives.PopupMediaView.OpenImageLink(CurrentMedia);
    }

    private void OnKeyPress(object? sender, NativeView.KeyEventArgs e)
    {
        e.Handled = PopupKeyboardNavigation.IsActivationKey(e.KeyCode) && e.Event?.HasNoModifiers == false;
        if (_accessiblePager && _platformView?.IsFocused == true &&
            e.Event is { Action: KeyEventActions.Down, HasNoModifiers: true } && e.KeyCode is Keycode.DpadLeft or Keycode.DpadRight)
        {
            int direction = e.KeyCode == Keycode.DpadRight ? 1 : -1;
            Move(_platformView.LayoutDirection == Android.Views.LayoutDirection.Rtl ? -direction : direction);
            e.Handled = true;
        }
    }

    private void UpdateIndicators()
    {
        if (_indicators.Handler?.PlatformView is not Microsoft.Maui.Platform.MauiPageControl pageControl)
            return;

        string format = Properties.Resources.GetString("PopupViewerMediaPosition");
        int selected = Math.Max(0, Math.Min(_indicators.Position, pageControl.ChildCount - 1));
        for (int i = 0; i < pageControl.ChildCount; i++)
        {
            if (pageControl.GetChildAt(i) is Android.Widget.ImageView indicator)
            {
                string description = string.Format(CultureInfo.CurrentCulture, format, i + 1, _indicators.Count);
                if (indicator.ContentDescription != description)
                    indicator.ContentDescription = description;
                indicator.Selected = i == selected;
            }
        }
    }

    private sealed class MediaAction(PopupMediaPager pager, int direction) : Java.Lang.Object, IAccessibilityViewCommand
    {
        public bool Perform(NativeView view, AccessibilityViewCommandCommandArguments? arguments) => pager.Move(direction);
    }
}

internal sealed class PopupMediaPagerHandler : LayoutHandler
{
    protected override LayoutViewGroup CreatePlatformView()
    {
        var view = new MediaPagerLayout(Context!) { CrossPlatformLayout = VirtualView };
        view.SetClipChildren(false);
        return view;
    }

    private sealed class MediaPagerLayout(Context context) : LayoutViewGroup(context)
    {
        private PopupMediaPager? Pager => CrossPlatformLayout is PopupMediaPager pager &&
            ReferenceEquals(pager.Handler?.PlatformView, this) ? pager : null;

        public override void OnInitializeAccessibilityNodeInfo(AccessibilityNodeInfo? info)
        {
            base.OnInitializeAccessibilityNodeInfo(info);
            if (info is null || Pager is not { } pager)
                return;

            bool previous = pager.CanMove(-1);
            bool next = pager.CanMove(1);
            info.Scrollable = previous || next;
            if (previous)
                info.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionScrollBackward);
            if (next)
                info.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionScrollForward);
        }

        public override bool PerformAccessibilityAction(Android.Views.Accessibility.Action action, Bundle? arguments) =>
            action switch
            {
                Android.Views.Accessibility.Action.ScrollBackward => Pager?.Move(-1) == true,
                Android.Views.Accessibility.Action.ScrollForward => Pager?.Move(1) == true,
                _ => base.PerformAccessibilityAction(action, arguments),
            };
    }
}
#endif
