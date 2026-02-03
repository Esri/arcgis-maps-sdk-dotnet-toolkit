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
#if WINDOWS_XAML
using Windows.Foundation;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// View for searching with locators or custom search sources.
    /// </summary>
#if WINDOWS_XAML
    [TemplatePart(Name = "PART_SuggestionList", Type = typeof(ListView))]
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchView"/> class.
        /// </summary>
        public SearchView()
        {
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
        private ListView? _suggestionList;

        // UWP listview automatically selects first item when doing grouping; using this flag to be able to ignore that first selection.
        private bool _groupListSelectionFlag;

        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_suggestionList != null)
            {
                _suggestionList.SelectionChanged -= SuggestionList_SelectionChanged;
                _suggestionList = null;
            }

            var listview = GetTemplateChild("PART_SuggestionList");

            if (listview is ListView newlistview)
            {
                _suggestionList = newlistview;
                _suggestionList.SelectedIndex = -1;
                _suggestionList.SelectionChanged += SuggestionList_SelectionChanged;
            }
        }

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
                SearchViewModel?.AcceptSuggestion(suggestion);
            }

            if (_suggestionList != null)
            {
                _suggestionList.SelectedIndex = -1;
            }
        }
#endif

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
            SearchViewModel?.RepeatSearchHere();
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
#if WINDOWS_XAML
            UpdateGroupingForUWP();
#endif
        }

#if WINDOWS_XAML
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