#if WINDOWS
using System.Linq;
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
    private ListViewBase? _nativeSourcesView;
    private ListViewBase? _nativeSuggestionsView;
    private UIElement? _focusTargetAfterSuggestions;
    private ListViewItem? _focusSourceSuggestion;
    private KeyEventHandler? _sourcesViewKeyDownHandler;
    private KeyEventHandler? _suggestionsViewKeyDownHandler;
    private PointerEventHandler? _sourceSelectButtonPointerPressedHandler;
    private bool _sourceSelectOpenedByPointer;

    partial void ConnectKeyboardNavigation()
    {
        SetHandlerChangedSubscriptions(true);
        WireNativeControls();
    }

    partial void DisconnectKeyboardNavigation()
    {
        SetHandlerChangedSubscriptions(false);
        UnwireNativeControls();
        _sourceSelectOpenedByPointer = false;
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

    partial void OnResultFocusRequested() => Dispatcher.Dispatch(() =>
        FocusFirstItem(PART_ResultView?.Handler?.PlatformView as ListViewBase));

    partial void UpdateSourceSelectAutomationState()
    {
        if (_nativeSourceSelectButton != null)
        {
            var name = Properties.Resources.GetString("SearchViewSelectSearchSource");
            var state = Properties.Resources.GetString(SourcePopupVisibility
                ? "SearchViewExpandedAutomationState"
                : "SearchViewCollapsedAutomationState");
            NativeAutomationProperties.SetName(_nativeSourceSelectButton, $"{name}, {state}");
        }
    }

    private void SetHandlerChangedSubscriptions(bool subscribe)
    {
        foreach (var part in new VisualElement?[]
        {
            PART_SourceSelectButton,
            PART_SearchButton,
            PART_SourcesView,
            PART_SuggestionsView,
            PART_ResultView,
        })
        {
            if (part == null)
            {
                continue;
            }

            if (subscribe)
            {
                part.HandlerChanged += TemplatePart_HandlerChanged;
            }
            else
            {
                part.HandlerChanged -= TemplatePart_HandlerChanged;
            }
        }
    }

    private void TemplatePart_HandlerChanged(object? sender, EventArgs e) => WireNativeControls();

    private void WireNativeControls()
    {
        UnwireNativeControls();

        _nativeSourceSelectButton = PART_SourceSelectButton?.Handler?.PlatformView as NativeButton;
        _nativeSearchButton = PART_SearchButton?.Handler?.PlatformView as NativeButton;
        _nativeSourcesView = PART_SourcesView?.Handler?.PlatformView as ListViewBase;
        _nativeSuggestionsView = PART_SuggestionsView?.Handler?.PlatformView as ListViewBase;

        if (_nativeSourceSelectButton != null)
        {
            _sourceSelectButtonPointerPressedHandler ??= SourceSelectButton_PointerPressed;
            _nativeSourceSelectButton.AddHandler(UIElement.PointerPressedEvent, _sourceSelectButtonPointerPressedHandler, true);
            UpdateSourceSelectAutomationState();
        }

        if (_nativeSearchButton != null)
        {
            _nativeSearchButton.KeyDown += SearchButton_KeyDown;
        }

        if (_nativeSourcesView != null)
        {
            NativeAutomationProperties.SetName(_nativeSourcesView, Properties.Resources.GetString("SearchViewSearchSources"));
            _sourcesViewKeyDownHandler ??= SourcesView_KeyDown;
            _nativeSourcesView.AddHandler(UIElement.KeyDownEvent, _sourcesViewKeyDownHandler, true);
        }

        if (_nativeSuggestionsView != null)
        {
            NativeAutomationProperties.SetName(_nativeSuggestionsView, Properties.Resources.GetString("SearchViewSearchSuggestions"));
            _nativeSuggestionsView.IsTabStop = false;
            _nativeSuggestionsView.ChoosingGroupHeaderContainer += SuggestionsView_ChoosingGroupHeaderContainer;
            _suggestionsViewKeyDownHandler ??= SuggestionsView_KeyDown;
            _nativeSuggestionsView.AddHandler(UIElement.KeyDownEvent, _suggestionsViewKeyDownHandler, true);
        }

        if (PART_ResultView?.Handler?.PlatformView is ListViewBase resultView)
        {
            NativeAutomationProperties.SetName(resultView, Properties.Resources.GetString("SearchViewSearchResults"));
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
            if (_sourcesViewKeyDownHandler != null)
            {
                _nativeSourcesView.RemoveHandler(UIElement.KeyDownEvent, _sourcesViewKeyDownHandler);
            }
        }

        if (_nativeSuggestionsView != null)
        {
            _nativeSuggestionsView.ChoosingGroupHeaderContainer -= SuggestionsView_ChoosingGroupHeaderContainer;
            if (_suggestionsViewKeyDownHandler != null)
            {
                _nativeSuggestionsView.RemoveHandler(UIElement.KeyDownEvent, _suggestionsViewKeyDownHandler);
            }
        }

        _nativeSourceSelectButton = null;
        _nativeSearchButton = null;
        _nativeSourcesView = null;
        _nativeSuggestionsView = null;
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
            e.Handled = CloseSourcesAndFocus(IsShiftPressed() ? _nativeSourceSelectButton : GetNativeEntry());
            return;
        }

        if (e.Key is VirtualKey.Enter or VirtualKey.Space)
        {
            e.Handled = true;
            _ = _nativeSourcesView?.DispatcherQueue.TryEnqueue(() => CloseSourcesAndFocus(GetNativeEntry()));
        }
    }

    private TextBox? GetNativeEntry() => PART_Entry?.Handler?.PlatformView as TextBox;

    private bool CloseSourcesAndFocus(NativeControl? target)
    {
        _sourceSelectToggled = false;
        UpdateVisibility();
        return target?.Focus(FocusState.Keyboard) == true;
    }

    private void SuggestionsView_ChoosingGroupHeaderContainer(ListViewBase sender, ChoosingGroupHeaderContainerEventArgs args)
    {
        args.GroupHeaderContainer ??= new ListViewHeaderItem();
        args.GroupHeaderContainer.IsTabStop = false;

        if (PART_SuggestionsView?.ItemsSource?.Cast<object>().ElementAtOrDefault(args.GroupIndex) is IGrouping<ISearchSource, SearchSuggestion> group &&
            !string.IsNullOrEmpty(group.Key?.DisplayName))
        {
            var header = args.GroupHeaderContainer;
            NativeAutomationProperties.SetName(header, group.Key.DisplayName);
            _ = sender.DispatcherQueue.TryEnqueue(() => SetSuggestionGroupAccessibilityView(sender, args.Group));
        }
    }

    private static void SetSuggestionGroupAccessibilityView(ListViewBase suggestionsView, object group)
    {
        if (suggestionsView.ContainerFromItem(group) is GroupItem groupItem)
        {
            NativeAutomationProperties.SetAccessibilityView(
                groupItem,
                Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
        }
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
        if (_nativeSuggestionsView?.XamlRoot?.Content is not FrameworkElement searchRoot || !searchRoot.IsLoaded)
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
            var moved = _nativeSearchButton?.Focus(FocusState.Programmatic) == true &&
                FocusManager.TryMoveFocus(
                    FocusNavigationDirection.Next,
                    new FindNextElementOptions { SearchRoot = searchRoot });
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