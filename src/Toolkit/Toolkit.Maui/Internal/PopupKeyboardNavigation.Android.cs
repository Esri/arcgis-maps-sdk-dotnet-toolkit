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
using Android.Views;
using AndroidX.RecyclerView.Widget;
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
#endif
