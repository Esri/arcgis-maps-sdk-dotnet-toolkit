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
#if WPF || WINDOWS_XAML
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;

#if WPF
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
#endif

#if WINDOWS_XAML
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// View for searching with locators or custom search sources.
    /// </summary>
    /// <remarks><note type="caution">
    /// If a <see cref="LocalSceneView"/> is set as the <see cref="GeoView"/>, the current search results will not currently be shown on the scene.
    /// </note></remarks>
    [TemplatePart(Name = "PART_SourcePopup", Type = typeof(Popup))]
    [TemplatePart(Name = "PART_AllSourcesButton", Type = typeof(ToggleButton))]
    [TemplatePart(Name = "PART_SourceList", Type = typeof(ListView))]
    [TemplatePart(Name = "QueryEntry", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_SuggestionList", Type = typeof(ListView))]
    [TemplatePart(Name = "PART_ResultList", Type = typeof(ListView))]
    [TemplatePart(Name = "PART_ResultMessage", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_SearchButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_SourceSelectToggle", Type = typeof(ToggleButton))]
#if WINDOWS_XAML
    [TemplatePart(Name = "PART_SuggestionListUnGrouped", Type = typeof(ListView))]
#endif
#pragma warning disable IDE0079
#pragma warning disable CA1001
#if WINUI
    [WinRT.GeneratedBindableCustomProperty]
#endif
    public partial class SearchView : Control
#pragma warning restore CA1001
#pragma warning restore IDE0079
    {
        // Controls how long the control waits after typing stops before looking for suggestions.
        private const int TypingDelayMilliseconds = 75;
        private GeoModel? _lastUsedGeomodel;
        private readonly GraphicsOverlay _resultOverlay;
        private CancellationTokenSource? _configurationCancellationToken;

        // Flag indicates whether control is waiting after user finished typing.
        private bool _waitFlag;

        // Flag indicating that query text is changing as a result of selecting a suggestion; view should not request suggestions in response to the user suggesting a selection.
        private bool _acceptingSuggestionFlag;

        private Popup? _sourcePopup;
        private ToggleButton? _allSourcesButton;
        private ListView? _sourceList;
        private TextBox? _queryEntry;
        private ListView? _suggestionList;
        private ListView? _resultList;
        private TextBlock? _resultMessage;
        private Button? _searchButton;
        private bool _focusResultsWhenAvailable;
        private bool _sourceSelectionByKeyboard;
        private bool _sourceSelectOpenedByPointer;

    #if WINDOWS_XAML
        private readonly KeyEventHandler _suggestionItemKeyDownHandler;
        private readonly KeyEventHandler _focusTargetAfterSuggestionsKeyDownHandler;
        private readonly KeyEventHandler _allSourcesButtonKeyDownHandler;
        private readonly PointerEventHandler _sourceSelectTogglePointerPressedHandler;
    #endif

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchView"/> class.
        /// </summary>
        public SearchView()
        {
#if WINDOWS_XAML
            _suggestionItemKeyDownHandler = SuggestionItem_KeyDown;
            _focusTargetAfterSuggestionsKeyDownHandler = FocusTargetAfterSuggestions_KeyDown;
            _allSourcesButtonKeyDownHandler = AllSourcesButton_KeyDown;
            _sourceSelectTogglePointerPressedHandler = SourceSelectToggle_PointerPressed;
#endif
            DefaultStyleKey = typeof(SearchView);
            DataContext = this;
            SearchViewModel = new SearchViewModel();
            _resultOverlay = new GraphicsOverlay { Id = "SearchView_Result_Overlay" };
#if WINDOWS_XAML
            SetValue(TemplateSettingsProperty, new SearchViewTemplateSettings(this));
#elif WPF
            SetValue(TemplateSettingsPropertyKey, new SearchViewTemplateSettings(this));
#endif
            TemplateSettings.ClearCommand = new DelegateCommand(HandleClearSearchCommand);
            TemplateSettings.SearchCommand = new DelegateCommand(HandleSearchCommand);
            TemplateSettings.RepeatSearchHereCommand = new DelegateCommand(HandleRepeatSearchHereCommand);
            InitializeLocalizedStrings();
        }

        private void InitializeLocalizedStrings()
        {
            NoResultMessage = Properties.Resources.GetString("SearchViewNoResults");
            AllSourceSelectText = Properties.Resources.GetString("SearchViewAllSourcesSelect");
            ClearSearchTooltipText = Properties.Resources.GetString("SearchViewClearSearchTooltip");
            SearchTooltipText = Properties.Resources.GetString("SearchViewSearchTooltip");
            RepeatSearchButtonText = Properties.Resources.GetString("SearchViewRepeatSearch");
        }

#if WINDOWS_XAML
        private ListView? _ungroupedSuggestionList;
        private UIElement? _focusTargetAfterSuggestions;
        private ListViewItem? _focusSourceSuggestion;
        private ToggleButton? _sourceSelectToggle;

        // UWP listview automatically selects first item when doing grouping; using this flag to be able to ignore that first selection.
        private bool _groupListSelectionFlag;

        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            ClearFocusTargetAfterSuggestions();

            if (_sourcePopup != null)
            {
                _sourcePopup.Opened -= SourcePopup_Opened;
            }

            if (_sourceSelectToggle != null)
            {
                _sourceSelectToggle.RemoveHandler(UIElement.PointerPressedEvent, _sourceSelectTogglePointerPressedHandler);
            }

            if (_allSourcesButton != null)
            {
                _allSourcesButton.RemoveHandler(UIElement.KeyDownEvent, _allSourcesButtonKeyDownHandler);
                _allSourcesButton.Click -= AllSourcesButton_Click;
                _allSourcesButton.PointerPressed -= SourceList_PointerPressed;
            }

            if (_sourceList != null)
            {
                _sourceList.KeyDown -= SourceList_KeyDown;
                _sourceList.SelectionChanged -= SourceList_SelectionChanged;
                _sourceList.PointerPressed -= SourceList_PointerPressed;
            }

            if (_suggestionList != null)
            {
                _suggestionList.SelectionChanged -= SuggestionList_SelectionChanged;
                _suggestionList.ChoosingGroupHeaderContainer -= ListView_ChoosingGroupHeaderContainer;
                _suggestionList.ContainerContentChanging -= SuggestionList_ContainerContentChanging;
                RemoveSuggestionItemHandlers(_suggestionList);
            }

            if (_ungroupedSuggestionList != null)
            {
                _ungroupedSuggestionList.ContainerContentChanging -= SuggestionList_ContainerContentChanging;
                RemoveSuggestionItemHandlers(_ungroupedSuggestionList);
            }

            if (_searchButton != null)
            {
                _searchButton.KeyDown -= SearchButton_KeyDown;
            }

            base.OnApplyTemplate();

            GetCommonTemplateParts();
            _ungroupedSuggestionList = GetTemplateChild("PART_SuggestionListUnGrouped") as ListView;

            if (_sourcePopup != null)
            {
                _sourcePopup.Opened += SourcePopup_Opened;
            }

            if (_sourceSelectToggle != null)
            {
                _sourceSelectToggle.AddHandler(UIElement.PointerPressedEvent, _sourceSelectTogglePointerPressedHandler, true);
            }

            if (_allSourcesButton != null)
            {
                _allSourcesButton.AddHandler(UIElement.KeyDownEvent, _allSourcesButtonKeyDownHandler, true);
                _allSourcesButton.Click += AllSourcesButton_Click;
                _allSourcesButton.PointerPressed += SourceList_PointerPressed;
            }

            if (_sourceList != null)
            {
                _sourceList.KeyDown += SourceList_KeyDown;
                _sourceList.SelectionChanged += SourceList_SelectionChanged;
                _sourceList.PointerPressed += SourceList_PointerPressed;
            }

            if (_suggestionList != null)
            {
                _suggestionList.SelectedIndex = -1;
                _suggestionList.IsTabStop = false;
                _suggestionList.SelectionChanged += SuggestionList_SelectionChanged;
                _suggestionList.ChoosingGroupHeaderContainer += ListView_ChoosingGroupHeaderContainer;
                _suggestionList.ContainerContentChanging += SuggestionList_ContainerContentChanging;
            }

            if (_ungroupedSuggestionList != null)
            {
                _ungroupedSuggestionList.IsTabStop = false;
                _ungroupedSuggestionList.ContainerContentChanging += SuggestionList_ContainerContentChanging;
            }

            if (_searchButton != null)
            {
                _searchButton.KeyDown += SearchButton_KeyDown;
            }
        }

        private void SearchButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab && !IsShiftPressed() && FocusFirstVisibleItem(_ungroupedSuggestionList, _suggestionList))
            {
                e.Handled = true;
            }
        }

        private void SourcePopup_Opened(object? sender, object e)
        {
            if (_sourceSelectOpenedByPointer)
            {
                _sourceSelectOpenedByPointer = false;
                return;
            }

            _ = DispatcherQueue.TryEnqueue(() => _allSourcesButton?.Focus(FocusState.Keyboard));
        }

        private void SourceSelectToggle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _sourceSelectOpenedByPointer = !IsSourceSelectOpen;
        }

        private void AllSourcesButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab && IsShiftPressed())
            {
                e.Handled = CloseSourcePopupAndFocusToggle();
            }
            else if (e.Key == VirtualKey.Tab && FocusFirstVisibleItem(_sourceList))
            {
                _sourceSelectionByKeyboard = true;
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                _sourceSelectionByKeyboard = true;
            }
        }

        private void AllSourcesButton_Click(object sender, RoutedEventArgs e)
        {
            if (TakeKeyboardSourceSelection())
            {
                _ = DispatcherQueue.TryEnqueue(() => _queryEntry?.Focus(FocusState.Keyboard));
            }
        }

        private void SourceList_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                var focused = IsShiftPressed()
                    ? _allSourcesButton?.Focus(FocusState.Keyboard) == true
                    : CloseSourcePopupAndFocusQuery();
                e.Handled = focused;
                if (focused)
                {
                    _sourceSelectionByKeyboard = false;
                }
            }
            else
            {
                _sourceSelectionByKeyboard = true;
            }
        }

        private void SourceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && TakeKeyboardSourceSelection())
            {
                _ = DispatcherQueue.TryEnqueue(() => _queryEntry?.Focus(FocusState.Keyboard));
            }
        }

        private void SourceList_PointerPressed(object sender, PointerRoutedEventArgs e) => _sourceSelectionByKeyboard = false;

        private bool CloseSourcePopupAndFocusQuery()
        {
            IsSourceSelectOpen = false;
            return _queryEntry?.Focus(FocusState.Keyboard) == true;
        }

        private bool CloseSourcePopupAndFocusToggle()
        {
            IsSourceSelectOpen = false;
            return _sourceSelectToggle?.Focus(FocusState.Keyboard) == true;
        }

        private static bool IsShiftPressed() => IsKeyPressed(VirtualKey.Shift);

        private static bool IsKeyPressed(VirtualKey key) =>
            InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);

        private static bool IsSuggestionAcceptKeyPressed() =>
            IsKeyPressed(VirtualKey.Enter) || IsKeyPressed(VirtualKey.Space);

        private void SuggestionList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.ItemContainer is not ListViewItem item)
            {
                return;
            }

            item.RemoveHandler(UIElement.KeyDownEvent, _suggestionItemKeyDownHandler);
            if (!args.InRecycleQueue)
            {
                item.AddHandler(UIElement.KeyDownEvent, _suggestionItemKeyDownHandler, true);
            }
        }

        private void SuggestionItem_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Tab)
            {
                return;
            }

            if (IsShiftPressed())
            {
                e.Handled = _searchButton?.Focus(FocusState.Keyboard) == true;
            }
            else if (sender is ListViewItem suggestionItem)
            {
                e.Handled = MoveFocusPastSuggestions(suggestionItem);
            }
        }

        private bool MoveFocusPastSuggestions(ListViewItem suggestionItem)
        {
            if (XamlRoot?.Content is not FrameworkElement searchRoot || !searchRoot.IsLoaded)
            {
                return false;
            }

            var itemTabStops = GetSuggestionItemTabStops();
            foreach (var item in itemTabStops)
            {
                item.IsTabStop = false;
            }

            try
            {
                var moved = _searchButton?.Focus(FocusState.Programmatic) == true &&
                    FocusManager.TryMoveFocus(
                        FocusNavigationDirection.Next,
                        new FindNextElementOptions { SearchRoot = searchRoot });

                ClearFocusTargetAfterSuggestions();
                if (moved && FocusManager.GetFocusedElement(XamlRoot) is UIElement focusTarget)
                {
                    _focusSourceSuggestion = suggestionItem;
                    _focusTargetAfterSuggestions = focusTarget;
                    focusTarget.AddHandler(UIElement.KeyDownEvent, _focusTargetAfterSuggestionsKeyDownHandler, true);
                }

                return moved;
            }
            finally
            {
                foreach (var item in itemTabStops)
                {
                    item.IsTabStop = true;
                }
            }
        }

        private void FocusTargetAfterSuggestions_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab && IsShiftPressed() &&
                ReferenceEquals(sender, _focusTargetAfterSuggestions) &&
                _focusSourceSuggestion?.IsLoaded == true &&
                _focusSourceSuggestion.Focus(FocusState.Keyboard))
            {
                e.Handled = true;
            }
        }

        private void ClearFocusTargetAfterSuggestions()
        {
            _focusTargetAfterSuggestions?.RemoveHandler(
                UIElement.KeyDownEvent,
                _focusTargetAfterSuggestionsKeyDownHandler);
            _focusTargetAfterSuggestions = null;
            _focusSourceSuggestion = null;
        }

        private List<ListViewItem> GetSuggestionItemTabStops()
        {
            var items = new List<ListViewItem>();
            AddRealizedItems(_ungroupedSuggestionList, items);
            AddRealizedItems(_suggestionList, items);
            return items;
        }

        private static void AddRealizedItems(ListView? listView, List<ListViewItem> items)
        {
            if (listView == null)
            {
                return;
            }

            for (var index = 0; index < listView.Items.Count; index++)
            {
                if (listView.ContainerFromIndex(index) is ListViewItem item && item.IsTabStop)
                {
                    items.Add(item);
                }
            }
        }

        private void RemoveSuggestionItemHandlers(ListView listView)
        {
            for (var index = 0; index < listView.Items.Count; index++)
            {
                if (listView.ContainerFromIndex(index) is ListViewItem item)
                {
                    item.RemoveHandler(UIElement.KeyDownEvent, _suggestionItemKeyDownHandler);
                }
            }
        }

        private static bool FocusFirstVisibleItem(params ListView?[] listViews)
        {
            foreach (var listView in listViews)
            {
                if (listView?.Visibility != Visibility.Visible || !listView.IsEnabled || listView.Items.Count == 0)
                {
                    continue;
                }

                listView.ScrollIntoView(listView.Items[0]);
                listView.UpdateLayout();
                if (listView.ContainerFromIndex(0) is ListViewItem item && item.Focus(FocusState.Keyboard))
                {
                    return true;
                }
            }

            return false;
        }

        private void ScheduleResultFocus() =>
            _ = DispatcherQueue.TryEnqueue(() => FocusFirstVisibleItem(_resultList));

        private void ScheduleLiveRegionAnnouncement(FrameworkElement element) =>
            _ = DispatcherQueue.TryEnqueue(() => RaiseLiveRegionChanged(element));

        private void ScheduleNotificationAnnouncement(ListView listView, string announcement) =>
            _ = DispatcherQueue.TryEnqueue(() => RaiseNotificationAnnouncement(listView, announcement));

        private void SuggestionList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_groupListSelectionFlag)
            {
                if (_suggestionList != null)
                {
                    _suggestionList.SelectedIndex = -1;
                }
                return;
            }

            if (e.AddedItems.FirstOrDefault() is SearchSuggestion suggestion)
            {
                _focusResultsWhenAvailable = IsSuggestionAcceptKeyPressed();
                SearchViewModel?.AcceptSuggestion(suggestion);
            }

            if (_suggestionList != null)
            {
                _suggestionList.SelectedIndex = -1;
            }
        }
#elif WPF
        // These template parts are wired for manual keyboard traversal because WPF Popup creates a separate focus scope.
        private ToggleButton? _sourceSelectToggle;
        private IInputElement? _focusTargetAfterSuggestions;

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            // OnApplyTemplate can run multiple times; remove old handlers before attaching to new template instances.
            if (_sourceSelectToggle != null)
            {
                _sourceSelectToggle.Checked -= SourceSelectToggle_Checked;
            }

            if (_sourcePopup != null)
            {
                _sourcePopup.Closed -= SourcePopup_Closed;
            }

            if (_allSourcesButton != null)
            {
                _allSourcesButton.PreviewKeyDown -= AllSourcesButton_PreviewKeyDown;
                _allSourcesButton.Click -= AllSourcesButton_Click;
                _allSourcesButton.PreviewMouseDown -= SourceSelection_PreviewMouseDown;
            }

            if (_sourceList != null)
            {
                _sourceList.PreviewKeyDown -= SourceList_PreviewKeyDown;
                _sourceList.SelectionChanged -= SourceList_SelectionChanged;
                _sourceList.PreviewMouseDown -= SourceSelection_PreviewMouseDown;
            }

            if (_searchButton != null)
            {
                _searchButton.PreviewKeyDown -= SearchButton_PreviewKeyDown;
                _searchButton.PreviewGotKeyboardFocus -= SearchButton_PreviewGotKeyboardFocus;
            }

            if (_suggestionList != null)
            {
                _suggestionList.PreviewKeyDown -= SuggestionList_PreviewKeyDown;
            }

            base.OnApplyTemplate();

            GetCommonTemplateParts();

            if (_sourceSelectToggle != null)
            {
                _sourceSelectToggle.Checked += SourceSelectToggle_Checked;
            }

            if (_sourcePopup != null)
            {
                _sourcePopup.Closed += SourcePopup_Closed;
            }

            if (_allSourcesButton != null)
            {
                _allSourcesButton.PreviewKeyDown += AllSourcesButton_PreviewKeyDown;
                _allSourcesButton.Click += AllSourcesButton_Click;
                _allSourcesButton.PreviewMouseDown += SourceSelection_PreviewMouseDown;
            }

            if (_sourceList != null)
            {
                _sourceList.PreviewKeyDown += SourceList_PreviewKeyDown;
                _sourceList.SelectionChanged += SourceList_SelectionChanged;
                _sourceList.PreviewMouseDown += SourceSelection_PreviewMouseDown;
            }

            if (_searchButton != null)
            {
                _searchButton.PreviewKeyDown += SearchButton_PreviewKeyDown;
                _searchButton.PreviewGotKeyboardFocus += SearchButton_PreviewGotKeyboardFocus;
            }

            if (_suggestionList != null)
            {
                _suggestionList.PreviewKeyDown += SuggestionList_PreviewKeyDown;
            }
        }

        private void SourceSelectToggle_Checked(object sender, RoutedEventArgs e)
        {
            // When the popup opens, move focus explicitly to its first action; Tab does not automatically cross into Popup.
            _ = Dispatcher.BeginInvoke(new Action(() => _allSourcesButton?.Focus()), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void SourcePopup_Closed(object? sender, EventArgs e)
        {
            // Light-dismiss can close the popup directly, so force the bound IsSourceSelectOpen state back to false.
            IsSourceSelectOpen = false;
        }

        private void AllSourcesButton_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) != 0))
            {
                e.Handled = CloseSourcePopupAndFocusToggle();
                return;
            }

            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                _sourceSelectionByKeyboard = true;
            }

            // Enter the list by focusing the first item container so keyboard navigation starts on an actual option.
            if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) == 0 && FocusListItem(_sourceList, 0))
            {
                e.Handled = true;
            }
        }

        private void AllSourcesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TakeKeyboardSourceSelection())
            {
                return;
            }

            _ = Dispatcher.BeginInvoke(new Action(() => _queryEntry?.Focus()), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void SourceList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = CloseSourcePopupAndFocusToggle();
                return;
            }

            if (e.Key == Key.Tab)
            {
                // Keep Tab traversal inside the popup region: Shift+Tab returns to All sources; Tab exits to query.
                var focused = (Keyboard.Modifiers & ModifierKeys.Shift) != 0
                    ? _allSourcesButton?.Focus() == true
                    : CloseSourcePopupAndFocusQuery();

                if (focused)
                {
                    e.Handled = true;
                }

                return;
            }

            _sourceSelectionByKeyboard = true;
        }

        private void SourceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || !TakeKeyboardSourceSelection())
            {
                return;
            }

            _ = Dispatcher.BeginInvoke(new Action(() => _queryEntry?.Focus()), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void SourceSelection_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _sourceSelectionByKeyboard = false;
        }

        private bool CloseSourcePopupAndFocusQuery()
        {
            IsSourceSelectOpen = false;
            return _queryEntry?.Focus() == true;
        }

        private bool CloseSourcePopupAndFocusToggle()
        {
            IsSourceSelectOpen = false;
            return _sourceSelectToggle?.Focus() == true;
        }

        private void SearchButton_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Suggestions are rendered in a Popup, so move focus directly to the first suggestion container.
            if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) == 0 && FocusListItem(_suggestionList, 0))
            {
                e.Handled = true;
            }
        }

        private void SearchButton_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // If Shift+Tab comes from the element after SearchView, re-enter suggestions instead of skipping the popup.
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0 &&
                ReferenceEquals(e.OldFocus, _focusTargetAfterSuggestions) &&
                FocusListItem(_suggestionList, 0))
            {
                e.Handled = true;
            }
        }

        private void SuggestionList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                // Remember intent to move to results after suggestion acceptance triggers async result generation.
                _focusResultsWhenAvailable = true;
                return;
            }

            if (e.Key != Key.Tab)
            {
                return;
            }

            e.Handled = (Keyboard.Modifiers & ModifierKeys.Shift) != 0
                ? _searchButton?.Focus() == true
                : MoveFocusPastSearchView();
        }

        private static bool FocusListItem(ListView? listView, int index)
        {
            if (listView == null || !listView.IsVisible || !listView.IsEnabled || index < 0 || index >= listView.Items.Count)
            {
                return false;
            }

            var item = listView.Items[index];
            listView.ScrollIntoView(item);
            // Realize virtualized item containers before focusing, otherwise ContainerFromItem can return null.
            listView.UpdateLayout();
            return (listView.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem)?.Focus() == true;
        }

        private void ScheduleResultFocus() =>
            _ = Dispatcher.BeginInvoke(new Action(() => FocusListItem(_resultList, 0)), System.Windows.Threading.DispatcherPriority.Loaded);

        private void ScheduleLiveRegionAnnouncement(FrameworkElement element) =>
            _ = Dispatcher.BeginInvoke(new Action(() => RaiseLiveRegionChanged(element)), System.Windows.Threading.DispatcherPriority.Loaded);

        private void ScheduleNotificationAnnouncement(ListView listView, string announcement) =>
            _ = Dispatcher.BeginInvoke(new Action(() => RaiseNotificationAnnouncement(listView, announcement)), System.Windows.Threading.DispatcherPriority.Loaded);

        private bool MoveFocusPastSearchView()
        {
            if (_searchButton == null)
            {
                return false;
            }

            // Temporarily disable internal tab cycling so Tab leaves SearchView, then restore the original navigation mode.
            var tabNavigation = KeyboardNavigation.GetTabNavigation(this);
            KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.None);
            try
            {
                var moved = _searchButton.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                // Store the external destination so reverse traversal can re-enter suggestions from that exact element.
                _focusTargetAfterSuggestions = moved ? Keyboard.FocusedElement : null;
                return moved;
            }
            finally
            {
                KeyboardNavigation.SetTabNavigation(this, tabNavigation);
            }
        }
#endif

        private void GetCommonTemplateParts()
        {
            _sourcePopup = GetTemplateChild("PART_SourcePopup") as Popup;
            _allSourcesButton = GetTemplateChild("PART_AllSourcesButton") as ToggleButton;
            _sourceList = GetTemplateChild("PART_SourceList") as ListView;
            _queryEntry = GetTemplateChild("QueryEntry") as TextBox;
            _suggestionList = GetTemplateChild("PART_SuggestionList") as ListView;
#if WINDOWS_XAML
            _ungroupedSuggestionList = GetTemplateChild("PART_SuggestionListUnGrouped") as ListView;
#endif
            _resultList = GetTemplateChild("PART_ResultList") as ListView;
            _resultMessage = GetTemplateChild("PART_ResultMessage") as TextBlock;
            _searchButton = GetTemplateChild("PART_SearchButton") as Button;
            _sourceSelectToggle = GetTemplateChild("PART_SourceSelectToggle") as ToggleButton;
        }

        private bool TakeKeyboardSourceSelection()
        {
            if (!_sourceSelectionByKeyboard)
            {
                return false;
            }

            _sourceSelectionByKeyboard = false;
            return true;
        }

        private void FocusResultsWhenAvailable()
        {
            if (!_focusResultsWhenAvailable || SearchViewModel?.Results == null)
            {
                return;
            }

            _focusResultsWhenAvailable = false;
            if (SearchViewModel.Results.Count > 0)
            {
                ScheduleResultFocus();
            }
        }

        private async Task RepeatSearchAndFocusResults()
        {
            if (SearchViewModel == null)
            {
                return;
            }

            await SearchViewModel.RepeatSearchHere();
            if (SearchViewModel.Results?.Count > 0)
            {
                ScheduleResultFocus();
            }
        }

        private void AnnounceNoResults()
        {
            if (SearchViewModel?.Suggestions?.Count == 0 || SearchViewModel?.Results?.Count == 0)
            {
                if (_resultMessage != null)
                {
                    ScheduleLiveRegionAnnouncement(_resultMessage);
                }
            }
        }

        private void AnnounceAvailableItems(ListView? listView, string resourceKey)
        {
            var announcement = Properties.Resources.GetString(resourceKey);
            if (listView != null && !string.IsNullOrEmpty(announcement))
            {
                ScheduleNotificationAnnouncement(listView, announcement);
            }
        }

        private void RaiseLiveRegionChanged(FrameworkElement element)
        {
            element.UpdateLayout();

            if (element is ListView listView && listView.Items.Count == 0)
            {
                return;
            }

#if WPF
            if (!element.IsVisible)
            {
                return;
            }

            var peer = UIElementAutomationPeer.FromElement(element) ?? UIElementAutomationPeer.CreatePeerForElement(element);
#elif WINDOWS_XAML
            if (element.Visibility != Visibility.Visible)
            {
                return;
            }

            var peer = FrameworkElementAutomationPeer.FromElement(element) ?? FrameworkElementAutomationPeer.CreatePeerForElement(element);
#endif
            peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }

        private void RaiseNotificationAnnouncement(ListView listView, string announcement)
        {
            listView.UpdateLayout();

#if WPF
            if (listView.Items.Count == 0 || !listView.IsVisible)
            {
                return;
            }

            var peer = UIElementAutomationPeer.FromElement(listView) ?? UIElementAutomationPeer.CreatePeerForElement(listView);
#elif WINDOWS_XAML
            if (listView.Items.Count == 0 || listView.Visibility != Visibility.Visible)
            {
                return;
            }

            var peer = FrameworkElementAutomationPeer.FromElement(listView) ?? FrameworkElementAutomationPeer.CreatePeerForElement(listView);
#endif
            peer?.RaiseNotificationEvent(
                AutomationNotificationKind.Other,
                AutomationNotificationProcessing.MostRecent,
                announcement,
                "SearchViewAvailableItems");
        }

        private async Task ConfigureViewModel()
        {
            if (!EnableDefaultWorldGeocoder)
            {
                return;
            }

            if (_configurationCancellationToken != null)
            {
                _configurationCancellationToken.Cancel();
            }

            _configurationCancellationToken = new CancellationTokenSource();

            try
            {
                await (SearchViewModel?.ConfigureDefaultWorldGeocoder(_configurationCancellationToken.Token) ?? Task.CompletedTask);
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        private void AddResultToGeoView(SearchResult result)
        {
            if (result?.GeoElement is Graphic graphic)
            {
                _resultOverlay.Graphics.Add(graphic);
            }
        }

        #region Binding support

        /// <summary>
        /// Gets or sets the selected suggestion, triggering a search.
        /// </summary>
        public SearchSuggestion? SelectedSuggestion
        {
            get => null;
            set
            {
                // ListView calls selecteditem binding with null when collection is cleared.
                if (value is SearchSuggestion userSelection)
                {
#if WINDOWS_XAML
                    _focusResultsWhenAvailable = IsSuggestionAcceptKeyPressed();
#endif
                    _acceptingSuggestionFlag = true;
                    _ = SearchViewModel?.AcceptSuggestion(userSelection)
                                       .ContinueWith(tt => _acceptingSuggestionFlag = false, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the source selection view is being displayed.
        /// </summary>
        public bool IsSourceSelectOpen
        {
            get => (bool)GetValue(IsSourceSelectOpenProperty);
            set => SetValue(IsSourceSelectOpenProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsSourceSelectOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSourceSelectOpenProperty =
            PropertyHelper.CreateProperty<bool, SearchView>(nameof(IsSourceSelectOpen), false);

        #endregion binding support

        #region events

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchView sender)
            {
                if (e.OldValue is GeoView oldGeoView)
                {
                    oldGeoView.DismissCallout();
                    oldGeoView.ViewpointChanged -= sender.GeoView_ViewpointChanged;
                    sender._lastUsedGeomodel = null;
                    (oldGeoView as INotifyPropertyChanged).PropertyChanged -= sender.HandleMapChange;
                    if (oldGeoView.GraphicsOverlays?.Contains(sender._resultOverlay) ?? false)
                    {
                        oldGeoView.GraphicsOverlays.Remove(sender._resultOverlay);
                    }
                }

                sender.HandleViewpointChanged();

                if (e.NewValue is GeoView newGeoView)
                {
                    (newGeoView as INotifyPropertyChanged).PropertyChanged += sender.HandleMapChange;
                    newGeoView.ViewpointChanged += sender.GeoView_ViewpointChanged;
                    newGeoView.GraphicsOverlays?.Add(sender._resultOverlay);
                    if (newGeoView is LocalSceneView)
                    {
                        System.Diagnostics.Trace.WriteLine("SearchView does not currently support showing the search results on a LocalSceneView.");
                    }
                }

                _ = sender.ConfigureViewModel();
            }
        }

        private static void OnEnableDefualtWorldGeocoderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            _ = (d as SearchView)?.ConfigureViewModel();
        }

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchView sendingView)
            {
                if (e.OldValue is SearchViewModel oldModel)
                {
                    oldModel.PropertyChanged -= sendingView.SearchViewModel_PropertyChanged;
                    if (oldModel.Sources is INotifyCollectionChanged oldSources)
                    {
                        oldSources.CollectionChanged -= sendingView.Sources_CollectionChanged;
                    }
                }

                if (e.NewValue is SearchViewModel newModel)
                {
                    newModel.PropertyChanged += sendingView.SearchViewModel_PropertyChanged;
                    if (newModel.Sources is INotifyCollectionChanged newSources)
                    {
                        newSources.CollectionChanged += sendingView.Sources_CollectionChanged;
                    }
                }
            }
        }

        private void Sources_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            HandleSourcesChange();
        }

        private void HandleSourcesChange()
        {
            TemplateSettings.OnSourceSelectVisibilityChanged();
            IsSourceSelectOpen = false;
        }

        private void HandleMapChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Map) || e.PropertyName == nameof(Scene))
            {
                _ = ConfigureViewModel();
                return;
            }

            // When binding, MapView is unreliable about notifying about map changes, especially when first connecting to the view
            if (e.PropertyName == nameof(MapView.DrawStatus) && _lastUsedGeomodel == null)
            {
                if (GeoView is MapView mv && mv.Map is Map map)
                {
                    _lastUsedGeomodel = map;
                }
                else if (GeoView is SceneView sv && sv.Scene is Scene scene)
                {
                    _lastUsedGeomodel = scene;
                }
                else if (GeoView is LocalSceneView lsv && lsv.Scene is Scene localscene)
                {
                    _lastUsedGeomodel = localscene;
                }

                _ = ConfigureViewModel();
            }
        }

        private static void OnEnableResultListViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as SearchView)?.TemplateSettings.OnResultViewVisibilityChanged();

        private void SearchViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            IsSourceSelectOpen = false;
            switch (e.PropertyName)
            {
                case nameof(SearchViewModel.CurrentQuery):
                    _ = HandleQueryChanged();
                    break;
                case nameof(SearchViewModel.SearchMode):
                    HandleSearchModeChanged();
                    break;
                case nameof(SearchViewModel.Results):
                    _ = HandleResultsCollectionChanged();
                    break;
                case nameof(SearchViewModel.SelectedResult):
                    _ = HandleSelectedResultChanged();
                    break;
                case nameof(SearchViewModel.Suggestions):
                    HandleSuggestionsChanged();
                    break;
                case nameof(SearchViewModel.Sources):
                    HandleSourcesChange();
                    break;
            }
        }

        private void GeoView_ViewpointChanged(object? sender, EventArgs e) => HandleViewpointChanged();

        /// <summary>
        /// Updates <see cref="SearchViewModel"/> with the current viewpoint.
        /// </summary>
        private void HandleViewpointChanged()
        {
            if (SearchViewModel == null)
            {
                return;
            }

            if (GeoView == null)
            {
                SearchViewModel.QueryArea = null;
                SearchViewModel.QueryCenter = null;
                return;
            }

            if (GeoView.GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry is Envelope targetEnvelope)
            {
                SearchViewModel.QueryArea = targetEnvelope;
                SearchViewModel.QueryCenter = targetEnvelope.GetCenter();
            }
        }

        /// <summary>
        /// Implements typing delay behavior; it is best to wait for user to finish typing before asking for suggestions.
        /// </summary>
        /// <remarks>
        /// The view XAML is expected to bind to the viewmodel property directly, in such a matter that the query updates every keystroke.
        /// </remarks>
        private async Task HandleQueryChanged()
        {
            if (_waitFlag || _acceptingSuggestionFlag || SearchViewModel == null)
            {
                return;
            }

            _waitFlag = true;
            await Task.Delay(TypingDelayMilliseconds);
            _waitFlag = false;

            await SearchViewModel.UpdateSuggestions();
        }

        private async Task HandleSelectedResultChanged()
        {
            TemplateSettings.OnResultViewVisibilityChanged();

            if (SearchViewModel?.SelectedResult is SearchResult selectedResult)
            {
                _resultOverlay?.Graphics.Clear();
                AddResultToGeoView(selectedResult);

                if (GeoView != null && selectedResult.CalloutDefinition != null && selectedResult.GeoElement != null)
                {
                    GeoView.ShowCalloutForGeoElement(selectedResult.GeoElement, new Point(0, 0), selectedResult.CalloutDefinition);
                }

                // Zoom to the feature
                if (selectedResult.SelectionViewpoint != null && GeoView != null && SearchViewModel != null)
                {
                    SearchViewModel.IgnoreAreaChangesFlag = true;
                    await GeoView.SetViewpointAsync(selectedResult.SelectionViewpoint);
                    await Task.Delay(1000);
                    SearchViewModel.IgnoreAreaChangesFlag = false;
                }
            }
            else
            {
                GeoView?.DismissCallout();
            }
        }

        private async Task HandleResultsCollectionChanged()
        {
            if (SearchViewModel == null)
            {
                return;
            }

            TemplateSettings.OnResultViewVisibilityChanged();
            TemplateSettings.OnResultMessageVisibilityChanged();
            AnnounceNoResults();
            AnnounceAvailableItems(_resultList, "SearchViewResultsAvailable");
#if WPF || WINDOWS_XAML
            FocusResultsWhenAvailable();
#endif

            if (SearchViewModel.Results == null)
            {
                _resultOverlay?.Graphics?.Clear();
            }
            else if (SearchViewModel.SelectedResult == null && GeoView != null)
            {
                _resultOverlay?.Graphics?.Clear();
                foreach (var result in SearchViewModel.Results)
                {
                    AddResultToGeoView(result);
                }

                var zoomableResults = SearchViewModel.Results
                                        .Select(res => res.GeoElement?.Geometry).OfType<Geometry.Geometry>().ToList();

                if (zoomableResults != null && zoomableResults.Count > 1)
                {
                    SearchViewModel.IgnoreAreaChangesFlag = true;
                    var newViewpoint = GeometryEngine.CombineExtents(zoomableResults);
                    if (GeoView is MapView mv)
                    {
                        await mv.SetViewpointGeometryAsync(newViewpoint, MultipleResultZoomBuffer);
                    }
                    else
                    {
                        await GeoView.SetViewpointAsync(new Viewpoint(newViewpoint));
                    }

                    await Task.Delay(1000);
                    SearchViewModel.IgnoreAreaChangesFlag = false;
                }
            }
        }

        private void HandleSearchModeChanged()
        {
            TemplateSettings.OnResultViewVisibilityChanged();
        }

        #endregion events

        #region commands
        private void HandleClearSearchCommand()
        {
            SearchViewModel?.CancelSearch();
            SearchViewModel?.ClearSearch();
        }

        private void HandleSearchCommand()
        {
            SearchViewModel?.CommitSearch();
        }

        private void HandleRepeatSearchHereCommand()
        {
    #if WPF || WINDOWS_XAML
            _ = RepeatSearchAndFocusResults();
#else
            SearchViewModel?.RepeatSearchHere();
#endif
        }
#endregion commands

        #region properties

        /// <summary>
        /// Gets or sets the GeoView associated with this view.
        /// </summary>
        /// <remarks>
        /// If set, <see cref="SearchView"/> will add a graphics overlay for showing results, and will automatically navigate to show search results.
        /// </remarks>
        public GeoView? GeoView
        {
            get => GetValue(GeoViewProperty) as GeoView;
            set => SetValue(GeoViewProperty, value);
        }

        /// <summary>
        /// Gets or sets a message to show when a search completes with no results.
        /// </summary>
        public string? NoResultMessage
        {
            get => GetValue(NoResultMessageProperty) as string;
            set => SetValue(NoResultMessageProperty, value);
        }

        /// <summary>
        /// Gets or sets the viewmodel that implements core search behavior.
        /// </summary>
        public SearchViewModel? SearchViewModel
        {
            get => GetValue(SearchViewModelProperty) as SearchViewModel;
            set => SetValue(SearchViewModelProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="SearchViewModel"/> will include the Esri World Geocoder service by default.
        /// </summary>
        public bool EnableDefaultWorldGeocoder
        {
            get => (bool)GetValue(EnableDefaultWorldGeocoderProperty);
            set => SetValue(EnableDefaultWorldGeocoderProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a 'Repeat Search' button will be displayed
        /// when the user pans the map a sufficient amount after a search completes.
        /// </summary>
        /// <remarks>
        /// Some consumer applications will display this button in a separate area of the UI from the search bar, often centered over the map.
        /// This property is intended to allow hiding the default button if using a custom 'Repeat Search' implementation.
        /// See <see cref="SearchViewTemplateSettings.RepeatSearchHereCommand"/> and <see cref="SearchViewModel.IsEligibleForRequery"/> to enable a custom button implementation.
        /// </remarks>
        public bool EnableRepeatSearchHereButton
        {
            get => (bool)GetValue(EnableRepeatSearchHereButtonProperty);
            set => SetValue(EnableRepeatSearchHereButtonProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the view will show the selected result.
        /// If false, the result list is hidden automatically when a result is selected.
        /// </summary>
        /// <remarks>
        /// See <see cref="SearchViewModel.SelectedResult"/> to display custom UI for the selected result.
        /// </remarks>
        public bool EnableIndividualResultDisplay
        {
            get => (bool)GetValue(EnableIndividualResultDisplayProperty);
            set => SetValue(EnableIndividualResultDisplayProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the default result list view will be shown.
        /// </summary>
        /// <remarks>
        /// Set this value to false to enable a custom list presentation.
        /// </remarks>
        public bool EnableResultListView
        {
            get => (bool)GetValue(EnableResultListViewProperty);
            set => SetValue(EnableResultListViewProperty, value);
        }

        /// <summary>
        /// Gets or sets the buffer used when zooming to a set of results.
        /// </summary>
        public double MultipleResultZoomBuffer
        {
            get => (double)GetValue(MultipleResultZoomBufferProperty);
            set => SetValue(MultipleResultZoomBufferProperty, value);
        }

        /// <summary>
        /// Gets or sets the text to display for the button used to select all search sources.
        /// </summary>
        public string? AllSourceSelectText
        {
            get => GetValue(AllSourceSelectTextProperty) as string;
            set => SetValue(AllSourceSelectTextProperty, value);
        }

        /// <summary>
        /// Gets or sets the tooltip text to display for the clear/cancel search button.
        /// </summary>
        public string? ClearSearchTooltipText
        {
            get => GetValue(ClearSearchTooltipTextProperty) as string;
            set => SetValue(ClearSearchTooltipTextProperty, value);
        }

        /// <summary>
        /// Gets or sets the tooltip text to display for the search button.
        /// </summary>
        public string? SearchTooltipText
        {
            get => GetValue(SearchTooltipTextProperty) as string;
            set => SetValue(SearchTooltipTextProperty, value);
        }

        /// <summary>
        /// Gets or sets the text to display in the 'Repeat Search' button.
        /// </summary>
        public string? RepeatSearchButtonText
        {
            get => GetValue(RepeatSearchButtonTextProperty) as string;
            set => SetValue(RepeatSearchButtonTextProperty, value);
        }

        #endregion properties

        #region dependency properties

        /// <summary>
        /// Identifies the <see cref="NoResultMessage"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NoResultMessageProperty =
            DependencyProperty.Register(nameof(NoResultMessage), typeof(string), typeof(SearchView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(SearchView), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="EnableDefaultWorldGeocoder"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableDefaultWorldGeocoderProperty =
            DependencyProperty.Register(nameof(EnableDefaultWorldGeocoder), typeof(bool), typeof(SearchView), new PropertyMetadata(true, OnEnableDefualtWorldGeocoderPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="EnableRepeatSearchHereButton"/> dependency proeprty.
        /// </summary>
        public static readonly DependencyProperty EnableRepeatSearchHereButtonProperty =
            DependencyProperty.Register(nameof(EnableRepeatSearchHereButton), typeof(bool), typeof(SearchView), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="SearchViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchViewModelProperty =
            DependencyProperty.Register(nameof(SearchViewModel), typeof(SearchViewModel), typeof(SearchView), new PropertyMetadata(null, OnViewModelChanged));

        /// <summary>
        /// Identifies the <see cref="EnableResultListView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableResultListViewProperty =
            DependencyProperty.Register(nameof(EnableResultListView), typeof(bool), typeof(SearchView), new PropertyMetadata(true, OnEnableResultListViewChanged));

        /// <summary>
        /// Identifies the <see cref="EnableIndividualResultDisplay"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableIndividualResultDisplayProperty =
            DependencyProperty.Register(nameof(EnableIndividualResultDisplay), typeof(bool), typeof(SearchView), new PropertyMetadata(false, OnEnableResultListViewChanged));

        /// <summary>
        /// Identifies the <see cref="MultipleResultZoomBuffer"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MultipleResultZoomBufferProperty =
            DependencyProperty.Register(nameof(MultipleResultZoomBuffer), typeof(double), typeof(SearchView), new PropertyMetadata(64.0));

        /// <summary>
        /// Identifies the <see cref="AllSourceSelectText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllSourceSelectTextProperty =
            DependencyProperty.Register(nameof(AllSourceSelectText), typeof(string), typeof(SearchView), null);

        /// <summary>
        /// Identifies the <see cref="ClearSearchTooltipText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClearSearchTooltipTextProperty =
            DependencyProperty.Register(nameof(ClearSearchTooltipText), typeof(string), typeof(SearchView), null);

        /// <summary>
        /// Identifies the <see cref="SearchTooltipText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchTooltipTextProperty =
            DependencyProperty.Register(nameof(SearchTooltipText), typeof(string), typeof(SearchView), null);

        /// <summary>
        /// Identifies the <see cref="RepeatSearchButtonText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RepeatSearchButtonTextProperty =
            DependencyProperty.Register(nameof(RepeatSearchButtonText), typeof(string), typeof(SearchView), null);
        #endregion dependency properties

        private void HandleSuggestionsChanged()
        {
            TemplateSettings.OnResultViewVisibilityChanged();
            TemplateSettings.OnResultMessageVisibilityChanged();
            AnnounceNoResults();
#if WINDOWS_XAML
            UpdateGroupingForUWP();
            AnnounceAvailableItems(_ungroupedSuggestionList, "SearchViewSuggestionsAvailable");
#endif
            AnnounceAvailableItems(_suggestionList, "SearchViewSuggestionsAvailable");
        }

#if WINDOWS_XAML
        private void ListView_ChoosingGroupHeaderContainer(ListViewBase sender, ChoosingGroupHeaderContainerEventArgs args)
        {
            if (args.Group is SuggestionsGrouped group)
            {
                args.GroupHeaderContainer ??= new ListViewHeaderItem();

                AutomationProperties.SetName(
                    args.GroupHeaderContainer,
                    group.Key?.DisplayName ?? string.Empty);
            }
        }
        private void UpdateGroupingForUWP()
        {
            _groupListSelectionFlag = true;
            if (SearchViewModel?.Suggestions != null)
            {
                GroupedSuggestions = SearchViewModel.Suggestions.GroupBy(m => m.OwningSource, (key, list) => new SuggestionsGrouped(key, list)).ToList();
            }
            else
            {
                GroupedSuggestions = null;
            }

            _groupListSelectionFlag = false;
        }

        /// <summary>
        /// Gets the grouped list of suggestions.
        /// </summary>
        public List<SuggestionsGrouped>? GroupedSuggestions
        {
            get => GetValue(GroupedSuggestionsProperty) as List<SuggestionsGrouped>;
            private set
            {
                SetValue(GroupedSuggestionsProperty, value);
            }
        }
        /// <summary>
        /// Identifies the <see cref="TemplateSettings"/> dependency property.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        private static readonly DependencyProperty GroupedSuggestionsProperty =
            DependencyProperty.Register(nameof(GroupedSuggestions), typeof(List<SuggestionsGrouped>), typeof(SearchView), new PropertyMetadata(null));

        /// <summary>
        /// Class to support grouping suggestions on UWP.
        /// </summary>
#if WINUI
        [WinRT.GeneratedBindableCustomProperty]
#endif
        public partial class SuggestionsGrouped : IGrouping<ISearchSource, SearchSuggestion>
        {
            private readonly List<SearchSuggestion> _suggestions;

            /// <summary>
            /// Initializes a new instance of the <see cref="SuggestionsGrouped"/> class.
            /// </summary>
            internal SuggestionsGrouped(ISearchSource owningSource, IEnumerable<SearchSuggestion> suggestions)
            {
                Key = owningSource;
                _suggestions = suggestions.ToList();
            }

            /// <inheritdoc />
            public ISearchSource Key { get; private set; }

            /// <inheritdoc />
            public IEnumerator<SearchSuggestion> GetEnumerator() => _suggestions.GetEnumerator();

            /// <inheritdoc />
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _suggestions.GetEnumerator();
        }
#endif

        /// <summary>
        /// <see cref="SearchViewTemplateSettings"/> provides a set of properties that are used when you define a new control template for a control that derives from <see cref="SearchView"/>.
        /// </summary>
        public SearchViewTemplateSettings TemplateSettings
        {
#if WINDOWS_XAML
            get => (SearchViewTemplateSettings)GetValue(TemplateSettingsProperty);
#elif WPF
            get => (SearchViewTemplateSettings)GetValue(TemplateSettingsPropertyKey.DependencyProperty);
#endif
        }

#if WINDOWS_XAML
        /// <summary>
        /// Identifies the <see cref="TemplateSettings"/> dependency property.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly DependencyProperty TemplateSettingsProperty =
            DependencyProperty.Register(nameof(TemplateSettings), typeof(SearchViewTemplateSettings), typeof(SearchView), new PropertyMetadata(null));
#elif WPF
        internal static readonly DependencyPropertyKey TemplateSettingsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(TemplateSettings), typeof(SearchViewTemplateSettings), typeof(SearchView), new FrameworkPropertyMetadata());
#endif
    }

    /// <summary>
    /// <see cref="SearchViewTemplateSettings"/> provides a set of properties that are used when you define a new control template for a control that derives from <see cref="SearchView"/>.
    /// </summary>
    /// <remarks>
    /// TemplateSettings properties are always intended to be used in XAML, not code. They are read-only sub-properties of a read-only TemplateSettings property of a parent control.
    /// </remarks>
    /// <seealso cref="SearchView.TemplateSettings"/>
#if WINUI
    [WinRT.GeneratedBindableCustomProperty]
#endif
    public partial class SearchViewTemplateSettings : INotifyPropertyChanged
    {
        private SearchView _owner;
        internal SearchViewTemplateSettings(SearchView owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Gets the visibility for the presentation of the <see cref="SearchView.NoResultMessage"/>.
        /// </summary>
        public Visibility ResultMessageVisibility
        {
            get
            {
                if (_owner.SearchViewModel?.Suggestions?.Count == 0 || _owner.SearchViewModel?.Results?.Count == 0)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }

        internal void OnResultMessageVisibilityChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResultMessageVisibility)));

        /// <summary>
        /// Gets the visibility for the source selection button.
        /// </summary>
        public Visibility SourceSelectVisibility
        {
            get
            {
                if (_owner.SearchViewModel?.Sources.Count > 1)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }

        internal void OnSourceSelectVisibilityChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SourceSelectVisibility)));

        /// <summary>
        /// Gets the visibility for the result list view.
        /// </summary>
        public Visibility ResultViewVisibility
        {
            get
            {
                if (!_owner.EnableResultListView)
                {
                    return Visibility.Collapsed;
                }

                // Ensure no result message is visible
                if ((_owner.SearchViewModel?.Results != null && _owner.SearchViewModel.Results.Count == 0) || (_owner.SearchViewModel?.Suggestions != null && _owner.SearchViewModel.Suggestions.Count == 0))
                {
                    return Visibility.Visible;
                }

                if (!_owner.EnableIndividualResultDisplay && (_owner.SearchViewModel?.SearchMode == SearchResultMode.Single || _owner.SearchViewModel?.SelectedResult != null))
                {
                    return Visibility.Collapsed;
                }

                if (_owner.SearchViewModel?.Results?.Any() == true)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
        }

        internal void OnResultViewVisibilityChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResultViewVisibility)));

        /// <summary>
        /// Gets a command that clears the current search.
        /// </summary>
        public ICommand ClearCommand { get; internal set; }

        /// <summary>
        /// Gets a command that starts a search with current parameters.
        /// </summary>
        public ICommand SearchCommand { get; internal set; }

        /// <summary>
        ///  Gets a command that repeats the last search with new geometry.
        /// </summary>
        public ICommand RepeatSearchHereCommand { get; internal set; }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
#endif