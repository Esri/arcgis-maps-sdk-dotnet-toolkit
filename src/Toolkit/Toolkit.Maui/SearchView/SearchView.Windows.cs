#if WINDOWS
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
using NativeAutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;
using NativeControl = Microsoft.UI.Xaml.Controls.Control;
using NativeVisibility = Microsoft.UI.Xaml.Visibility;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class SearchView
{
    private NativeButton? _nativeSourceSelectButton;
    private NativeButton? _nativeSearchButton;
    private TextBox? _nativeEntry;
    private ListViewBase? _nativeSourcesView;
    private ListViewBase? _nativeSuggestionsView;
    private ListViewBase? _nativeResultView;
    private UIElement? _focusTargetAfterSuggestions;
    private ListViewItem? _focusSourceSuggestion;
    private KeyEventHandler? _suggestionsViewKeyDownHandler;
    private PointerEventHandler? _sourceSelectButtonPointerPressedHandler;
    private bool _sourceSelectOpenedByPointer;
    private bool _sourceSelectionByKeyboard;

    partial void ConnectKeyboardNavigation()
    {
        SubscribeToHandlerChanges();
        WireNativeControls();
    }

    partial void DisconnectKeyboardNavigation()
    {
        UnsubscribeFromHandlerChanges();
        UnwireNativeControls();
        _sourceSelectOpenedByPointer = false;
        _sourceSelectionByKeyboard = false;
    }

    partial void OnSourceListOpened()
    {
        if (_sourceSelectOpenedByPointer)
        {
            _sourceSelectOpenedByPointer = false;
            return;
        }

        _ = _nativeSourceSelectButton?.DispatcherQueue.TryEnqueue(() => FocusFirstItem(_nativeSourcesView));
    }

    partial void OnSourceSelected()
    {
        if (!_sourceSelectionByKeyboard)
        {
            return;
        }

        _sourceSelectionByKeyboard = false;
        Dispatcher.Dispatch(() => _nativeEntry?.Focus(FocusState.Keyboard));
    }

    partial void OnResultFocusRequested() => Dispatcher.Dispatch(() => FocusFirstItem(_nativeResultView));

    private void SubscribeToHandlerChanges()
    {
        if (PART_SourceSelectButton != null)
        {
            PART_SourceSelectButton.HandlerChanged += TemplatePart_HandlerChanged;
        }

        if (PART_SearchButton != null)
        {
            PART_SearchButton.HandlerChanged += TemplatePart_HandlerChanged;
        }

        if (PART_Entry != null)
        {
            PART_Entry.HandlerChanged += TemplatePart_HandlerChanged;
        }

        if (PART_SourcesView != null)
        {
            PART_SourcesView.HandlerChanged += TemplatePart_HandlerChanged;
        }

        if (PART_SuggestionsView != null)
        {
            PART_SuggestionsView.HandlerChanged += TemplatePart_HandlerChanged;
        }

        if (PART_ResultView != null)
        {
            PART_ResultView.HandlerChanged += TemplatePart_HandlerChanged;
        }
    }

    private void UnsubscribeFromHandlerChanges()
    {
        if (PART_SourceSelectButton != null)
        {
            PART_SourceSelectButton.HandlerChanged -= TemplatePart_HandlerChanged;
        }

        if (PART_SearchButton != null)
        {
            PART_SearchButton.HandlerChanged -= TemplatePart_HandlerChanged;
        }

        if (PART_Entry != null)
        {
            PART_Entry.HandlerChanged -= TemplatePart_HandlerChanged;
        }

        if (PART_SourcesView != null)
        {
            PART_SourcesView.HandlerChanged -= TemplatePart_HandlerChanged;
        }

        if (PART_SuggestionsView != null)
        {
            PART_SuggestionsView.HandlerChanged -= TemplatePart_HandlerChanged;
        }

        if (PART_ResultView != null)
        {
            PART_ResultView.HandlerChanged -= TemplatePart_HandlerChanged;
        }
    }

    private void TemplatePart_HandlerChanged(object? sender, EventArgs e) => WireNativeControls();

    private void WireNativeControls()
    {
        UnwireNativeControls();

        _nativeSourceSelectButton = PART_SourceSelectButton?.Handler?.PlatformView as NativeButton;
        _nativeSearchButton = PART_SearchButton?.Handler?.PlatformView as NativeButton;
        _nativeEntry = PART_Entry?.Handler?.PlatformView as TextBox;
        _nativeSourcesView = PART_SourcesView?.Handler?.PlatformView as ListViewBase;
        _nativeSuggestionsView = PART_SuggestionsView?.Handler?.PlatformView as ListViewBase;
        _nativeResultView = PART_ResultView?.Handler?.PlatformView as ListViewBase;

        if (_nativeSourceSelectButton != null)
        {
            _sourceSelectButtonPointerPressedHandler ??= SourceSelectButton_PointerPressed;
            _nativeSourceSelectButton.AddHandler(UIElement.PointerPressedEvent, _sourceSelectButtonPointerPressedHandler, true);
        }

        if (_nativeSearchButton != null)
        {
            _nativeSearchButton.KeyDown += SearchButton_KeyDown;
        }

        if (_nativeSourcesView != null)
        {
            NativeAutomationProperties.SetName(_nativeSourcesView, Properties.Resources.GetString("SearchViewSearchSources"));
            _nativeSourcesView.KeyDown += SourcesView_KeyDown;
            _nativeSourcesView.PointerPressed += SourcesView_PointerPressed;
        }

        if (_nativeSuggestionsView != null)
        {
            NativeAutomationProperties.SetName(_nativeSuggestionsView, Properties.Resources.GetString("SearchViewSearchSuggestions"));
            _nativeSuggestionsView.IsTabStop = false;
            _suggestionsViewKeyDownHandler ??= SuggestionsView_KeyDown;
            _nativeSuggestionsView.AddHandler(UIElement.KeyDownEvent, _suggestionsViewKeyDownHandler, true);
        }

        if (_nativeResultView != null)
        {
            NativeAutomationProperties.SetName(_nativeResultView, Properties.Resources.GetString("SearchViewSearchResults"));
        }
    }

    private void UnwireNativeControls()
    {
        ClearFocusTargetAfterSuggestions();

        if (_nativeSourceSelectButton != null)
        {
            if (_sourceSelectButtonPointerPressedHandler != null)
            {
                _nativeSourceSelectButton.RemoveHandler(UIElement.PointerPressedEvent, _sourceSelectButtonPointerPressedHandler);
            }
        }

        if (_nativeSearchButton != null)
        {
            _nativeSearchButton.KeyDown -= SearchButton_KeyDown;
        }

        if (_nativeSourcesView != null)
        {
            _nativeSourcesView.KeyDown -= SourcesView_KeyDown;
            _nativeSourcesView.PointerPressed -= SourcesView_PointerPressed;
        }

        if (_nativeSuggestionsView != null)
        {
            if (_suggestionsViewKeyDownHandler != null)
            {
                _nativeSuggestionsView.RemoveHandler(UIElement.KeyDownEvent, _suggestionsViewKeyDownHandler);
            }
        }

        _nativeSourceSelectButton = null;
        _nativeSearchButton = null;
        _nativeEntry = null;
        _nativeSourcesView = null;
        _nativeSuggestionsView = null;
        _nativeResultView = null;
    }

    private void SourceSelectButton_PointerPressed(object sender, PointerRoutedEventArgs e) =>
        _sourceSelectOpenedByPointer = !_sourceSelectToggled;

    private void SearchButton_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Tab && !IsShiftPressed() && FocusFirstItem(_nativeSuggestionsView))
        {
            e.Handled = true;
        }
    }

    private void SourcesView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            e.Handled = CloseSourcesAndFocus(_nativeSourceSelectButton);
            return;
        }

        if (e.Key == VirtualKey.Tab)
        {
            e.Handled = CloseSourcesAndFocus(IsShiftPressed() ? _nativeSourceSelectButton : _nativeEntry);
            return;
        }

        _sourceSelectionByKeyboard = true;
        if (e.Key is VirtualKey.Enter or VirtualKey.Space)
        {
            _ = _nativeSourcesView?.DispatcherQueue.TryEnqueue(() =>
            {
                if (_sourceSelectionByKeyboard)
                {
                    CloseSourcesAndFocus(_nativeEntry);
                }
            });
        }
    }

    private void SourcesView_PointerPressed(object sender, PointerRoutedEventArgs e) => _sourceSelectionByKeyboard = false;

    private bool CloseSourcesAndFocus(NativeControl? target)
    {
        _sourceSelectToggled = false;
        UpdateVisibility();
        _sourceSelectionByKeyboard = false;
        return target?.Focus(FocusState.Keyboard) == true;
    }

    private void SuggestionsView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Enter or VirtualKey.Space)
        {
            _focusResultsWhenAvailable = true;
            return;
        }

        if (e.Key != VirtualKey.Tab)
        {
            return;
        }

        if (IsShiftPressed())
        {
            e.Handled = _nativeSearchButton?.Focus(FocusState.Keyboard) == true;
        }
        else if (FindAncestor<ListViewItem>(e.OriginalSource as DependencyObject) is ListViewItem suggestionItem)
        {
            e.Handled = MoveFocusPastSuggestions(suggestionItem);
        }
    }

    private bool MoveFocusPastSuggestions(ListViewItem suggestionItem)
    {
        if (_nativeSuggestionsView == null)
        {
            return false;
        }

        var tabStops = GetRealizedTabStops(_nativeSuggestionsView);
        foreach (var item in tabStops)
        {
            item.IsTabStop = false;
        }

        try
        {
            var moved = FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
            ClearFocusTargetAfterSuggestions();
            if (moved && FocusManager.GetFocusedElement(_nativeSuggestionsView.XamlRoot) is UIElement focusTarget)
            {
                _focusSourceSuggestion = suggestionItem;
                _focusTargetAfterSuggestions = focusTarget;
                focusTarget.KeyDown += FocusTargetAfterSuggestions_KeyDown;
            }

            return moved;
        }
        finally
        {
            foreach (var item in tabStops)
            {
                item.IsTabStop = true;
            }
        }
    }

    private void FocusTargetAfterSuggestions_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Tab && IsShiftPressed() &&
            ReferenceEquals(sender, _focusTargetAfterSuggestions) &&
            _focusSourceSuggestion?.Visibility == NativeVisibility.Visible &&
            _focusSourceSuggestion.Focus(FocusState.Keyboard))
        {
            e.Handled = true;
        }
    }

    private void ClearFocusTargetAfterSuggestions()
    {
        if (_focusTargetAfterSuggestions != null)
        {
            _focusTargetAfterSuggestions.KeyDown -= FocusTargetAfterSuggestions_KeyDown;
        }

        _focusTargetAfterSuggestions = null;
        _focusSourceSuggestion = null;
    }

    private static bool FocusFirstItem(ListViewBase? listView)
    {
        if (listView?.Visibility != NativeVisibility.Visible || !listView.IsEnabled || listView.Items.Count == 0)
        {
            return false;
        }

        listView.ScrollIntoView(listView.Items[0]);
        listView.UpdateLayout();
        return listView.ContainerFromIndex(0) is ListViewItem item && item.Focus(FocusState.Keyboard);
    }

    private static List<ListViewItem> GetRealizedTabStops(ListViewBase listView)
    {
        var items = new List<ListViewItem>();
        for (var index = 0; index < listView.Items.Count; index++)
        {
            if (listView.ContainerFromIndex(index) is ListViewItem item && item.IsTabStop)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private static T? FindAncestor<T>(DependencyObject? element)
        where T : DependencyObject
    {
        while (element != null)
        {
            if (element is T match)
            {
                return match;
            }

            element = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element);
        }

        return null;
    }

    private static bool IsShiftPressed() =>
        InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
}
#endif