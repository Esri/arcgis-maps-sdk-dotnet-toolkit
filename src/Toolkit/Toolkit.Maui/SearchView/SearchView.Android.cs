#if ANDROID
using Android.Views;
using AndroidX.RecyclerView.Widget;
using NativeView = Android.Views.View;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class SearchView
{
    private RecyclerView? _nativeSourcesView;
    private RecyclerView? _nativeSuggestionsView;
    private RecyclerView? _nativeResultView;
    private ListItemKeyListener? _listItemKeyListener;

    partial void ConnectKeyboardNavigation()
    {
        _listItemKeyListener ??= new(this);

        foreach (var list in new[] { PART_SourcesView, PART_SuggestionsView, PART_ResultView })
        {
            if (list != null)
            {
                list.HandlerChanged += List_HandlerChanged;
            }
        }

        WireNativeLists();
    }

    partial void DisconnectKeyboardNavigation()
    {
        foreach (var list in new[] { PART_SourcesView, PART_SuggestionsView, PART_ResultView })
        {
            if (list != null)
            {
                list.HandlerChanged -= List_HandlerChanged;
            }
        }

        UnwireNativeLists();
    }

    private void List_HandlerChanged(object? sender, EventArgs e) => WireNativeLists();

    private void WireNativeLists()
    {
        UnwireNativeLists();

        _nativeSourcesView = PART_SourcesView?.Handler?.PlatformView as RecyclerView;
        _nativeSuggestionsView = PART_SuggestionsView?.Handler?.PlatformView as RecyclerView;
        _nativeResultView = PART_ResultView?.Handler?.PlatformView as RecyclerView;

        foreach (var list in new[] { _nativeSourcesView, _nativeSuggestionsView, _nativeResultView })
        {
            if (list != null)
            {
                list.AddOnChildAttachStateChangeListener(_listItemKeyListener!);
                for (var index = 0; index < list.ChildCount; index++)
                {
                    SetItemHandlers(list.GetChildAt(index), true);
                }
            }
        }
    }

    private void UnwireNativeLists()
    {
        foreach (var list in new[] { _nativeSourcesView, _nativeSuggestionsView, _nativeResultView })
        {
            if (list != null)
            {
                if (_listItemKeyListener != null)
                {
                    list.RemoveOnChildAttachStateChangeListener(_listItemKeyListener);
                }
                for (var index = 0; index < list.ChildCount; index++)
                {
                    SetItemHandlers(list.GetChildAt(index), false);
                }
            }
        }

        _nativeSourcesView = null;
        _nativeSuggestionsView = null;
        _nativeResultView = null;
    }

    partial void OnSourceListOpened()
    {
        if (PART_SourceSelectButton?.Handler?.PlatformView is Android.Views.View sourceButton &&
            sourceButton.IsFocused &&
            !sourceButton.IsInTouchMode &&
            PART_SourcesView?.Handler?.PlatformView is RecyclerView sourcesView)
        {
            FocusFirstItem(sourcesView);
        }
    }

    partial void OnSourceSelected()
    {
        if (PART_SourcesView?.Handler?.PlatformView is RecyclerView sourcesView &&
            sourcesView.HasFocus &&
            !sourcesView.IsInTouchMode &&
            PART_Entry?.Handler?.PlatformView is Android.Views.View entry)
        {
            entry.Post(() => entry.RequestFocus());
        }
    }

    partial void OnSuggestionSelected()
    {
        if (PART_SuggestionsView?.Handler?.PlatformView is RecyclerView suggestionsView &&
            suggestionsView.HasFocus &&
            !suggestionsView.IsInTouchMode)
        {
            _focusResultsWhenAvailable = true;
        }
    }

    partial void OnResultFocusRequested() => Dispatcher.Dispatch(() =>
    {
        if (PART_ResultView?.Handler?.PlatformView is RecyclerView resultView)
        {
            FocusFirstItem(resultView);
        }
    });

    private static void FocusFirstItem(RecyclerView recyclerView)
    {
        recyclerView.ScrollToPosition(0);
        recyclerView.Post(() => recyclerView.FindViewHolderForAdapterPosition(0)?.ItemView.RequestFocus());
    }

    private void ListItem_KeyPress(object? sender, NativeView.KeyEventArgs e)
    {
        if (sender is not NativeView focusedView ||
            FindParentList(focusedView) is not RecyclerView listView ||
            listView.FindContainingViewHolder(focusedView) is not RecyclerView.ViewHolder focusedItem)
        {
            return;
        }

        // Key handlers are attached to every item descendant so list navigation works when a child has focus.
        // Do not treat Enter, Space, or Tab on an interactive custom-template control as a list-item action.
        if (!ReferenceEquals(focusedView, focusedItem.ItemView) && focusedView.Clickable)
        {
            return;
        }

        if (e.KeyCode is Keycode.Enter or Keycode.NumpadEnter or Keycode.Space)
        {
            e.Handled = true;
            if (e.Event.Action == KeyEventActions.Up)
            {
                focusedItem.ItemView.PerformClick();
            }

            return;
        }

        if (e.KeyCode == Keycode.Tab)
        {
            if (e.Event.Action == KeyEventActions.Down)
            {
                e.Handled = MoveFocusOutOfList(listView, focusedView, e.Event.IsShiftPressed);
            }

            return;
        }

        if (e.KeyCode is Keycode.DpadDown or Keycode.DpadUp)
        {
            e.Handled = true;
            var position = focusedItem.BindingAdapterPosition;
            if (e.Event.Action == KeyEventActions.Down && position != RecyclerView.NoPosition)
            {
                FocusListItem(
                    listView,
                    position,
                    e.KeyCode == Keycode.DpadDown ? 1 : -1);
            }
        }
    }

    private void SetItemHandlers(NativeView? itemView, bool attach)
    {
        if (itemView == null)
        {
            return;
        }

        SetKeyHandler(itemView, attach);
        itemView.Click -= SourceItem_Click;
        if (attach && ReferenceEquals(itemView.Parent, _nativeSourcesView))
        {
            itemView.Click += SourceItem_Click;
        }
    }

    private void SourceItem_Click(object? sender, EventArgs e)
    {
        if (sender is not NativeView itemView ||
            !_sourceSelectToggled ||
            PART_SourcesView?.ItemsSource is not IList<string> sources ||
            _nativeSourcesView?.FindContainingViewHolder(itemView)?.BindingAdapterPosition is not int position ||
            position < 0 || position >= sources.Count)
        {
            return;
        }

        var selectedSource = sources[position];
        if (Equals(PART_SourcesView.SelectedItem, selectedSource))
        {
            // Activating the selected source does not raise SelectionChanged, so complete it explicitly.
            SelectSource(selectedSource);
        }
        else
        {
            PART_SourcesView.SelectedItem = selectedSource;
        }
    }

    private bool MoveFocusOutOfList(RecyclerView listView, NativeView focusedView, bool moveBackward)
    {
        if (listView.RootView is not ViewGroup root)
        {
            return false;
        }

        var direction = moveBackward ? FocusSearchDirection.Backward : FocusSearchDirection.Forward;
        var candidate = focusedView;
        for (var index = 0; index <= (listView.GetAdapter()?.ItemCount ?? 0); index++)
        {
            candidate = FocusFinder.Instance?.FindNextFocus(root, candidate, direction);
            if (candidate == null || !IsInList(candidate, listView))
            {
                if (candidate?.RequestFocus() == true)
                {
                    if (ReferenceEquals(listView, _nativeSourcesView))
                    {
                        _sourceSelectToggled = false;
                        UpdateVisibility();
                    }

                    return true;
                }

                return false;
            }
        }

        return false;
    }

    private static bool IsInList(NativeView view, RecyclerView listView)
    {
        for (var current = view; current != null; current = current.Parent as NativeView)
        {
            if (ReferenceEquals(current, listView))
            {
                return true;
            }
        }

        return false;
    }

    private static RecyclerView? FindParentList(NativeView view)
    {
        for (var parent = view.Parent; parent != null; parent = parent.Parent)
        {
            if (parent is RecyclerView recyclerView)
            {
                return recyclerView;
            }
        }

        return null;
    }

    private void SetKeyHandler(NativeView? view, bool attach)
    {
        if (view == null)
        {
            return;
        }

        if (attach)
        {
            view.KeyPress -= ListItem_KeyPress;
            view.KeyPress += ListItem_KeyPress;
        }
        else
        {
            view.KeyPress -= ListItem_KeyPress;
        }

        if (view is ViewGroup group)
        {
            for (var index = 0; index < group.ChildCount; index++)
            {
                SetKeyHandler(group.GetChildAt(index), attach);
            }
        }
    }

    private sealed class ListItemKeyListener : Java.Lang.Object, RecyclerView.IOnChildAttachStateChangeListener
    {
        private readonly SearchView _searchView;

        public ListItemKeyListener(SearchView searchView) => _searchView = searchView;

        public void OnChildViewAttachedToWindow(NativeView view) => _searchView.SetItemHandlers(view, true);

        public void OnChildViewDetachedFromWindow(NativeView view) => _searchView.SetItemHandlers(view, false);
    }

    private static void FocusListItem(RecyclerView listView, int currentPosition, int direction)
    {
        var position = currentPosition + direction;
        if (position >= 0 && position < (listView.GetAdapter()?.ItemCount ?? 0))
        {
            listView.ScrollToPosition(position);
            listView.Post(() =>
            {
                var item = listView.FindViewHolderForAdapterPosition(position)?.ItemView;
                if (item?.Focusable == true)
                {
                    item.RequestFocus();
                }
                else
                {
                    FocusListItem(listView, position, direction);
                }
            });
        }
    }

}
#endif