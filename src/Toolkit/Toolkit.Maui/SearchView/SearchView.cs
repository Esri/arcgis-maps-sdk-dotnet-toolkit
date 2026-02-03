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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using Grid = Microsoft.Maui.Controls.Grid;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

/// <summary>
/// View for searching with locators and custom search sources.
/// </summary>
public partial class SearchView : TemplatedView, INotifyPropertyChanged
{
    // Controls how long the control waits after typing stops before looking for suggestions.
    private const int TypingDelayMilliseconds = 75;
    private GeoModel? _lastUsedGeomodel;
    private readonly GraphicsOverlay _resultOverlay;

    // Flag indicates whether control is waiting after user finished typing.
    private bool _waitFlag;

    private bool _configureMapFlag;

    // Flag indicating that query text is changing as a result of selecting a suggestion; view should not request suggestions in response to the user suggesting a selection.
    private bool _acceptingSuggestionFlag;

    private bool _sourceSelectToggled;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchView"/> class.
    /// </summary>
    public SearchView()
    {
        ResultTemplate = DefaultResultTemplate;
        SuggestionTemplate = DefaultSuggestionTemplate;
        ControlTemplate = DefaultControlTemplate;
        SuggestionGroupHeaderTemplate = DefaultSuggestionGroupHeaderTemplate;

        string suffix = DeviceInfo.Platform == DevicePlatform.WinUI ? "-small" : string.Empty;
        if (GetTemplateChild(nameof(PART_SourceSelectButton)) is ImageButton newSourceButton)
        {
            newSourceButton.Source = ImageSource.FromResource($"Esri.ArcGISRuntime.Toolkit.Maui.Assets.caret-down{suffix}.png", Assembly.GetAssembly(typeof(SearchView)));
        }

        if (GetTemplateChild(nameof(PART_SearchButton)) is ImageButton newsearchButton)
        {
            newsearchButton.Source = ImageSource.FromResource($"Esri.ArcGISRuntime.Toolkit.Maui.Assets.search{suffix}.png", Assembly.GetAssembly(typeof(SearchView)));
        }

        if (GetTemplateChild(nameof(PART_CancelButton)) is ImageButton cancelButton)
        {
            cancelButton.Source = new FontImageSource { Glyph = ToolkitIcons.X, FontFamily = ToolkitIcons.FontFamilyName, Color = Color.FromArgb("#6E6E6E") };
        }

        BindingContext = this;
        SearchViewModel = new SearchViewModel();
        InitializeLocalizedStrings();
        _resultOverlay = new GraphicsOverlay { Id = "SearchView_Result_Overlay" };
        ClearCommand = new DelegateCommand(HandleClearSearchCommand);
        SearchCommand = new DelegateCommand(HandleSearchCommand);
        RepeatSearchHereCommand = new DelegateCommand(HandleRepeatSearchHereCommand);
        Loaded += SearchView_Loaded;
    }

    private void SearchView_Loaded(object? sender, EventArgs e)
    {
        if (GeoView != null)
        {
            HandleViewpointChanged();
        }
        _ = ConfigureForCurrentConfiguration();
    }

    private void InitializeLocalizedStrings()
    {
        NoResultMessage = Properties.Resources.GetString("SearchViewNoResults");
        AllSourcesSelectText = Properties.Resources.GetString("SearchViewAllSourcesSelect");
        RepeatSearchButtonText = Properties.Resources.GetString("SearchViewRepeatSearch");
    }

    /// <summary>
    /// Gets a command that clears the current search.
    /// </summary>
    public ICommand ClearCommand { get; private set; }

    /// <summary>
    /// Gets a command that starts a search with current parameters.
    /// </summary>
    public ICommand SearchCommand { get; private set; }

    /// <summary>
    ///  Gets a command that repeats the last search with new geometry.
    /// </summary>
    public ICommand RepeatSearchHereCommand { get; private set; }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        if (PART_SourceSelectButton != null)
        {
            PART_SourceSelectButton.Clicked -= PART_SourceSelectButton_Clicked;
        }

        if (PART_Entry != null)
        {
            PART_Entry.TextChanged -= PART_Entry_TextChanged;
        }

        if (PART_CancelButton != null)
        {
            PART_CancelButton.Clicked -= PART_CancelButton_Clicked;
        }

        if (PART_SearchButton != null)
        {
            PART_SearchButton.Clicked -= PART_SearchButton_Clicked;
        }

        if (PART_SourcesView != null)
        {
            PART_SourcesView.SelectionChanged -= PART_SourcesView_SelectionChanged;
        }

        if (PART_SuggestionsView != null)
        {
            PART_SuggestionsView.SelectionChanged -= PART_SuggestionsView_ItemSelected;
            PART_SuggestionsView.ItemsSource = null;
        }

        if (PART_ResultView != null)
        {
            PART_ResultView.SelectionChanged -= PART_ResultView_ItemSelected;
            PART_ResultView.ItemsSource = null;
        }

        if (PART_RepeatButton != null)
        {
            PART_RepeatButton.Clicked -= PART_RepeatButton_Clicked;
        }

        base.OnApplyTemplate();

        if (GetTemplateChild(nameof(PART_SourceSelectButton)) is ImageButton newSourceButton)
        {
            PART_SourceSelectButton = newSourceButton;
            PART_SourceSelectButton.Clicked += PART_SourceSelectButton_Clicked;
        }

        if (GetTemplateChild(nameof(PART_Entry)) is Entry newEntry)
        {
            PART_Entry = newEntry;
            PART_Entry.Text = SearchViewModel?.CurrentQuery;
            PART_Entry.Placeholder = SearchViewModel?.ActivePlaceholder;
            PART_Entry.TextChanged += PART_Entry_TextChanged;
        }

        if (GetTemplateChild(nameof(PART_CancelButton)) is ImageButton newCancel)
        {
            PART_CancelButton = newCancel;
            PART_CancelButton.IsVisible = !string.IsNullOrEmpty(PART_Entry?.Text);
            PART_CancelButton.Clicked += PART_CancelButton_Clicked;
        }

        if (GetTemplateChild(nameof(PART_SearchButton)) is ImageButton newSearch)
        {
            PART_SearchButton = newSearch;
            PART_SearchButton.Clicked += PART_SearchButton_Clicked;
        }

        if (GetTemplateChild(nameof(PART_ResultLabel)) is Label newResultLabel)
        {
            PART_ResultLabel = newResultLabel;
            PART_ResultLabel.Text = NoResultMessage;
        }

        if (GetTemplateChild(nameof(PART_ResultContainer)) is Grid newResultContainer)
        {
            PART_ResultContainer = newResultContainer;
        }

        if (GetTemplateChild(nameof(PART_SourcesView)) is CollectionView newSourceSelectView)
        {
            PART_SourcesView = newSourceSelectView;
            PART_SourcesView.SelectionChanged += PART_SourcesView_SelectionChanged;
        }

        if (GetTemplateChild(nameof(PART_ResultView)) is CollectionView newResultList)
        {
            PART_ResultView = newResultList;
            PART_ResultView.ItemTemplate = ResultTemplate;
            PART_ResultView.SelectionChanged += PART_ResultView_ItemSelected;
        }

        if (GetTemplateChild(nameof(PART_SuggestionsView)) is CollectionView newSuggestionList)
        {
            PART_SuggestionsView = newSuggestionList;
            PART_SuggestionsView.ItemTemplate = SuggestionTemplate;
            PART_SuggestionsView.SelectionChanged += PART_SuggestionsView_ItemSelected;
            PART_SuggestionsView.IsGrouped = SearchViewModel?.Sources?.Count > 1 && SearchViewModel?.ActiveSource == null;
        }

        if (GetTemplateChild(nameof(PART_RepeatButton)) is Button newRepeatButton)
        {
            PART_RepeatButton = newRepeatButton;
            PART_RepeatButton.Text = RepeatSearchButtonText;
            PART_RepeatButton.Clicked += PART_RepeatButton_Clicked;
        }

        if (GetTemplateChild(nameof(PART_RepeatButtonContainer)) is Grid newRepeatButtonContainer)
        {
            PART_RepeatButtonContainer = newRepeatButtonContainer;
        }

        UpdateVisibility();
    }

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

    private void PART_SourcesView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SearchViewModel == null)
        {
            return;
        }

        var selectedSource = e.CurrentSelection.FirstOrDefault() as string;

        if (selectedSource == null || selectedSource == AllSourcesSelectText || (AllSourcesSelectText == null && selectedSource == "All"))
        {
            SearchViewModel.ActiveSource = null;
        }
        else 
        {
            SearchViewModel.ActiveSource = SearchViewModel.Sources.First(source => source.DisplayName == selectedSource);
        }

        _sourceSelectToggled = false;
        UpdateVisibility();
    }

    private void PART_RepeatButton_Clicked(object? sender, EventArgs e) => SearchViewModel?.RepeatSearchHere();

    private void PART_SuggestionsView_ItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SearchSuggestion suggestion)
        {
            PART_SuggestionsView?.SetValue(CollectionView.SelectedItemProperty, null);

            _ = AcceptSuggestion(suggestion);
        }
    }

    private void PART_ResultView_ItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count > 0 && SearchViewModel is SearchViewModel vm)
        {
            vm.SelectedResult = e.CurrentSelection.First() as SearchResult;
            PART_ResultView?.SetValue(CollectionView.SelectedItemProperty, null);
        }
    }

    private void PART_SourceSelectButton_Clicked(object? sender, EventArgs e)
    {
        _sourceSelectToggled = !_sourceSelectToggled;

        if (_sourceSelectToggled)
        {
            UpdateSearchSourceList();
        }

        UpdateVisibility();
    }

    private void PART_Entry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (SearchViewModel != null)
        {
            SearchViewModel.CurrentQuery = e.NewTextValue;
        }
    }

    private void PART_CancelButton_Clicked(object? sender, EventArgs e)
    {
        SearchViewModel?.CancelSearch();
        SearchViewModel?.ClearSearch();
    }

    private void PART_SearchButton_Clicked(object? sender, EventArgs e) => SearchViewModel?.CommitSearch();

    private async Task ConfigureForCurrentConfiguration()
    {
        if (!EnableDefaultWorldGeocoder || _configureMapFlag)
        {
            return;
        }

        _configureMapFlag = true;

        try
        {
            await (SearchViewModel?.ConfigureDefaultWorldGeocoder() ?? Task.CompletedTask);
        }
        catch (Exception)
        {
            // Ignore
        }
        finally
        {
            _configureMapFlag = false;
        }
    }

    private async Task AcceptSuggestion(SearchSuggestion suggestion)
    {
        if (SearchViewModel == null || _acceptingSuggestionFlag)
        {
            return;
        }

        _acceptingSuggestionFlag = true;
        try
        {
            await SearchViewModel.AcceptSuggestion(suggestion);
        }
        finally
        {
            _acceptingSuggestionFlag = false;
        }
    }

    private void AddResultToGeoView(SearchResult result)
    {
        if (result?.GeoElement is Graphic graphic)
        {
            _resultOverlay.Graphics.Add(graphic);
        }
    }

    #region State definitions

    private bool ResultViewVisibility
    {
        get
        {
            if (!EnableResultListView)
            {
                return false;
            }

            if (!EnableIndividualResultDisplay && (SearchViewModel?.SearchMode == SearchResultMode.Single || SearchViewModel?.SelectedResult != null))
            {
                return false;
            }

            if (SearchViewModel?.Results?.Any() == true)
            {
                return true;
            }

            return false;
        }
    }

    private bool SuggestionsViewVisibility => (SearchViewModel?.Suggestions?.Any() == true) && SearchViewModel?.Results == null;

    private bool SourceSelectVisibility => SearchViewModel?.Sources?.Count > 1;

    private bool ResultLabelVisibility => (SearchViewModel?.Suggestions != null && SearchViewModel?.Suggestions?.Count == 0) ||
        (SearchViewModel?.Results != null && SearchViewModel?.Results?.Count == 0);

    private bool RepeatSearchButtonVisibility => EnableRepeatSearchHereButton && (SearchViewModel?.IsEligibleForRequery ?? false) && !SuggestionsViewVisibility;

    private bool SourcePopupVisibility => _sourceSelectToggled && SearchViewModel?.Sources.Count > 1;

    #endregion State definitions

    #region events

    private static void OnResultTemplateChanged(BindableObject sender, object? oldValue, object? newValue)
    {
        if (sender is SearchView originatingView && originatingView.PART_ResultView != null)
        {
            originatingView.PART_ResultView.ItemTemplate = newValue as DataTemplate;
        }
    }

    private static void OnSuggestionTemplateChanged(BindableObject sender, object? oldValue, object? newValue)
    {
        if (sender is SearchView originatingView && originatingView.PART_SuggestionsView != null)
        {
            originatingView.PART_SuggestionsView.ItemTemplate = newValue as DataTemplate;
        }
    }

    private static void OnSuggestionGroupHeaderTemplateChanged(BindableObject sender, object? oldValue, object? newValue)
    {
        if (sender is SearchView originatingview && originatingview.PART_SuggestionsView != null)
        {
            originatingview.PART_SuggestionsView.GroupHeaderTemplate = newValue as DataTemplate;
        }
    }

    private static void OnGeoViewPropertyChanged(BindableObject sender, object? oldValue, object? newValue)
    {
        if (sender is SearchView sendingView)
        {
            if (oldValue is GeoView oldGeoView)
            {
                oldGeoView.DismissCallout();
                oldGeoView.ViewpointChanged -= sendingView.GeoView_ViewpointChanged;
                sendingView._lastUsedGeomodel = null;
                (oldGeoView as INotifyPropertyChanged).PropertyChanged -= sendingView.HandleMapChange;
                if (oldGeoView.GraphicsOverlays?.Contains(sendingView._resultOverlay) ?? false)
                {
                    oldGeoView.GraphicsOverlays.Remove(sendingView._resultOverlay);
                }
            }

            sendingView.HandleViewpointChanged();

            if (newValue is GeoView newGeoView)
            {
                (newGeoView as INotifyPropertyChanged).PropertyChanged += sendingView.HandleMapChange;
                newGeoView.ViewpointChanged += sendingView.GeoView_ViewpointChanged;
                newGeoView.GraphicsOverlays?.Add(sendingView._resultOverlay);
            }
        }
    }

    private static void OnEnableDefaultWorldGeocoderPropertyChanged(BindableObject sender, object? oldValue, object? newValue)
    {
        if (sender is SearchView sendingView)
        {
            _ = sendingView.ConfigureForCurrentConfiguration();
        }
    }

    private static void OnEnableRepeatSearchButtonChanged(BindableObject sender, object? oldValue, object? newValue) => (sender as SearchView)?.UpdateVisibility();

    private static void OnViewModelChanged(BindableObject sender, object? oldValue, object? newValue)
    {
        if (sender is SearchView sendingView)
        {
            if (oldValue is SearchViewModel oldModel)
            {
                oldModel.PropertyChanged -= sendingView.SearchViewModel_PropertyChanged;
                if (oldModel.Sources is INotifyCollectionChanged oldSources)
                {
                    oldSources.CollectionChanged -= sendingView.Sources_CollectionChanged;
                }
            }

            if (newValue is SearchViewModel newModel)
            {
                sendingView.PART_Entry?.SetValue(Entry.TextProperty, newModel.CurrentQuery);
                sendingView.PART_Entry?.SetValue(Entry.PlaceholderProperty, newModel.ActivePlaceholder);
                sendingView.PART_SuggestionsView?.SetValue(CollectionView.IsGroupedProperty, newModel.Sources?.Count > 1 && newModel.ActiveSource == null);
                newModel.PropertyChanged += sendingView.SearchViewModel_PropertyChanged;
                if (newModel.Sources is INotifyCollectionChanged newSources)
                {
                    newSources.CollectionChanged += sendingView.Sources_CollectionChanged;
                }
            }
        }
    }

    private static void OnEnableResultListViewChanged(BindableObject sender, object? oldValue, object? newValue) =>
        (sender as SearchView)?.UpdateVisibility();

    private static void OnRepeatSearchButtonTextChanged(BindableObject sender, object? oldValue, object? newValue) =>
        (sender as SearchView)?.PART_RepeatButton?.SetValue(Button.TextProperty, newValue);

    private static void OnNoResultMessagePropertyChanged(BindableObject sender, object? oldValue, object? newValue) =>
        (sender as SearchView)?.PART_ResultLabel?.SetValue(Label.TextProperty, newValue);

    private void Sources_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateSearchSourceList();
        UpdateVisibility();
    }

    private void UpdateSearchSourceList()
    {
        if (PART_SourcesView == null || SearchViewModel == null)
        {
            return;
        }

        var sources = new[] { AllSourcesSelectText ?? "All" }.Concat(SearchViewModel.Sources.Select(source => source.DisplayName)).ToList();
        PART_SourcesView.ItemsSource = sources;

        if (SearchViewModel.ActiveSource == null)
        {
            PART_SourcesView.SelectedItem = sources[0];
        }
        else
        {
            PART_SourcesView.SelectedItem = sources[SearchViewModel.Sources.IndexOf(SearchViewModel.ActiveSource) + 1];
        }
    }

    private void HandleMapChange(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Mapping.Map) || e.PropertyName == nameof(Scene))
        {
            return;
        }

        if (e.PropertyName == nameof(MapView.DrawStatus) && _lastUsedGeomodel == null)
        {
            if (GeoView is MapView mv && mv.Map is Mapping.Map map)
            {
                _lastUsedGeomodel = map;
            }
            else if (GeoView is SceneView sv && sv.Scene is Scene scene)
            {
                _lastUsedGeomodel = scene;
            }
        }
    }

    private void SearchViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (SearchViewModel == null)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(SearchViewModel.ActivePlaceholder):
                PART_Entry?.SetValue(Entry.PlaceholderProperty, SearchViewModel.ActivePlaceholder);
                UpdateVisibility();
                break;
            case nameof(SearchViewModel.Suggestions):
                // Only group if there are multiple sources
                bool groupingEnabled = SearchViewModel.Sources.Count > 1 && SearchViewModel.ActiveSource == null;
                PART_SuggestionsView?.SetValue(CollectionView.IsGroupedProperty, groupingEnabled);
                if (groupingEnabled)
                {
                    var grouped = SearchViewModel.Suggestions?.GroupBy(item => item.OwningSource);

                    // IGrouping.Key is being linked away in release mode, breaking the group header display. This ugly block of code prevents that.
                    // https://docs.microsoft.com/en-us/xamarin/android/deploy-test/linker#falseflag
                    bool falseFlag = false;
                    if (falseFlag && grouped != null)
                    {
                        Console.WriteLine(grouped.First().Key);
                    }

                    PART_SuggestionsView?.SetValue(CollectionView.ItemsSourceProperty, grouped ?? new IGrouping<ISearchSource, SearchSuggestion>[] {});
                }
                else
                {
                    PART_SuggestionsView?.SetValue(CollectionView.ItemsSourceProperty, SearchViewModel.Suggestions ?? new List<SearchSuggestion>());
                }

                UpdateVisibility();
                break;
            case nameof(SearchViewModel.Results):
                PART_ResultView?.SetValue(CollectionView.ItemsSourceProperty, SearchViewModel.Results ?? new List<SearchResult>());
                _ = HandleResultsCollectionChanged();
                break;
            case nameof(SearchViewModel.CurrentQuery):
                PART_CancelButton?.SetValue(View.IsVisibleProperty, !string.IsNullOrEmpty(SearchViewModel.CurrentQuery));
                PART_Entry?.SetValue(Entry.TextProperty, SearchViewModel.CurrentQuery);
                _ = HandleQueryChanged();
                break;
            case nameof(SearchViewModel.SearchMode):
                UpdateVisibility();
                break;
            case nameof(SearchViewModel.SelectedResult):
                _ = HandleSelectedResultChanged();
                break;
            case nameof(SearchViewModel.IsEligibleForRequery):
                UpdateVisibility();
                break;
            case nameof(SearchViewModel.Sources):
                UpdateSearchSourceList();
                UpdateVisibility();
                break;
        }
    }

    private void GeoView_ViewpointChanged(object? sender, EventArgs e) => HandleViewpointChanged();

    /// <summary>
    /// Updates <see cref="SearchViewModel"/> with the current viewpoint.
    /// </summary>
    private void HandleViewpointChanged()
    {
        if (!IsLoaded || SearchViewModel == null)
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
        UpdateVisibility();

        if (SearchViewModel?.SelectedResult is SearchResult selectedResult)
        {
            PART_ResultView?.SetValue(CollectionView.SelectedItemProperty, selectedResult);
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
            PART_ResultView?.SetValue(CollectionView.SelectedItemProperty, null);
            GeoView?.DismissCallout();
        }
    }

    private async Task HandleResultsCollectionChanged()
    {
        if (SearchViewModel == null)
        {
            return;
        }

        UpdateVisibility();

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

    private void UpdateVisibility()
    {
        PART_SuggestionsView?.SetValue(View.IsVisibleProperty, SuggestionsViewVisibility);
        PART_ResultView?.SetValue(View.IsVisibleProperty, ResultViewVisibility);
        PART_ResultContainer?.SetValue(View.IsVisibleProperty, ResultLabelVisibility);
        PART_ResultLabel?.SetValue(View.IsVisibleProperty, ResultLabelVisibility);
        PART_SourceSelectButton?.SetValue(View.IsVisibleProperty, SourceSelectVisibility);
        PART_RepeatButton?.SetValue(View.IsVisibleProperty, RepeatSearchButtonVisibility);
        PART_RepeatButtonContainer?.SetValue(View.IsVisibleProperty, RepeatSearchButtonVisibility);
        PART_SourcesView?.SetValue(View.IsVisibleProperty, SourcePopupVisibility);
    }

    #endregion events

    #region properties

    /// <summary>
    /// Gets or sets the template used to display suggestions.
    /// </summary>
    public DataTemplate? SuggestionTemplate
    {
        get => GetValue(SuggestionTemplateProperty) as DataTemplate;
        set => SetValue(SuggestionTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the template used to display results.
    /// </summary>
    public DataTemplate? ResultTemplate
    {
        get => GetValue(ResultTemplateProperty) as DataTemplate;
        set => SetValue(ResultTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the template used to display the header that groups suggestion results by source.
    /// </summary>
    public DataTemplate? SuggestionGroupHeaderTemplate
    {
        get => GetValue(SuggestionGroupHeaderTemplateProperty) as DataTemplate;
        set => SetValue(SuggestionGroupHeaderTemplateProperty, value);
    }

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
    /// Gets or sets the text to show in the button for selecting all search sources.
    /// </summary>
    public string? AllSourcesSelectText
    {
        get => GetValue(AllSourcesSelectTextProperty) as string;
        set => SetValue(AllSourcesSelectTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the text to show in the 'Repeat search' button.
    /// </summary>
    public string? RepeatSearchButtonText
    {
        get => GetValue(RepeatSearchButtonTextProperty) as string;
        set => SetValue(RepeatSearchButtonTextProperty, value);
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
    /// See <see cref="SearchViewModel.RepeatSearchHere"/> and <see cref="SearchViewModel.IsEligibleForRequery"/> to enable a custom button implementation.
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
    #endregion properties

    #region bindable properties

    /// <summary>
    /// Identifies the <see cref="ResultTemplate"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ResultTemplateProperty =
        BindableProperty.Create(nameof(ResultTemplate), typeof(DataTemplate), typeof(SearchView), propertyChanged: OnResultTemplateChanged);

    /// <summary>
    /// Identifies the <see cref="SuggestionTemplate"/> bindable property.
    /// </summary>
    public static readonly BindableProperty SuggestionTemplateProperty =
        BindableProperty.Create(nameof(SuggestionTemplate), typeof(DataTemplate), typeof(SearchView), propertyChanged: OnSuggestionTemplateChanged);

    /// <summary>
    /// Identifies the <see cref="SuggestionGroupHeaderTemplate"/> bindable property.
    /// </summary>
    public static readonly BindableProperty SuggestionGroupHeaderTemplateProperty =
        BindableProperty.Create(nameof(SuggestionGroupHeaderTemplate), typeof(DataTemplate), typeof(SearchView), propertyChanged: OnSuggestionGroupHeaderTemplateChanged);

    /// <summary>
    /// Identifies the <see cref="NoResultMessage"/> bindable property.
    /// </summary>
    public static readonly BindableProperty NoResultMessageProperty =
        BindableProperty.Create(nameof(NoResultMessage), typeof(string), typeof(SearchView), propertyChanged: OnNoResultMessagePropertyChanged);

    /// <summary>
    /// Identifies the <see cref="GeoView"/> bindable property.
    /// </summary>
    public static readonly BindableProperty GeoViewProperty =
        BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(SearchView), null, propertyChanged: OnGeoViewPropertyChanged);

    /// <summary>
    /// Identifies the <see cref="EnableDefaultWorldGeocoder"/> bindable property.
    /// </summary>
    public static readonly BindableProperty EnableDefaultWorldGeocoderProperty =
        BindableProperty.Create(nameof(EnableDefaultWorldGeocoder), typeof(bool), typeof(SearchView), true, propertyChanged: OnEnableDefaultWorldGeocoderPropertyChanged);

    /// <summary>
    /// Identifies the <see cref="EnableRepeatSearchHereButton"/> bindable proeprty.
    /// </summary>
    public static readonly BindableProperty EnableRepeatSearchHereButtonProperty =
        BindableProperty.Create(nameof(EnableRepeatSearchHereButton), typeof(bool), typeof(SearchView), true, propertyChanged: OnEnableRepeatSearchButtonChanged);

    /// <summary>
    /// Identifies the <see cref="SearchViewModel"/> bindable property.
    /// </summary>
    public static readonly BindableProperty SearchViewModelProperty =
        BindableProperty.Create(nameof(SearchViewModel), typeof(SearchViewModel), typeof(SearchView), null, propertyChanged: OnViewModelChanged);

    /// <summary>
    /// Identifies the <see cref="EnableResultListView"/> bindable property.
    /// </summary>
    public static readonly BindableProperty EnableResultListViewProperty =
        BindableProperty.Create(nameof(EnableResultListView), typeof(bool), typeof(SearchView), true, propertyChanged: OnEnableResultListViewChanged);

    /// <summary>
    /// Identifies the <see cref="EnableIndividualResultDisplay"/> bindable property.
    /// </summary>
    public static readonly BindableProperty EnableIndividualResultDisplayProperty =
        BindableProperty.Create(nameof(EnableIndividualResultDisplay), typeof(bool), typeof(SearchView), false, propertyChanged: OnEnableResultListViewChanged);

    /// <summary>
    /// Identifies the <see cref="MultipleResultZoomBuffer"/> bindable property.
    /// </summary>
    public static readonly BindableProperty MultipleResultZoomBufferProperty =
        BindableProperty.Create(nameof(MultipleResultZoomBuffer), typeof(double), typeof(SearchView), 64.0);

    /// <summary>
    /// Identifies the <see cref="AllSourcesSelectText"/> bindable property.
    /// </summary>
    public static readonly BindableProperty AllSourcesSelectTextProperty =
        BindableProperty.Create(nameof(AllSourcesSelectText), typeof(string), typeof(SearchView), null);

    /// <summary>
    /// Identifies the <see cref="RepeatSearchButtonText"/> bindable property.
    /// </summary>
    public static readonly BindableProperty RepeatSearchButtonTextProperty =
        BindableProperty.Create(nameof(RepeatSearchButtonText), typeof(string), typeof(SearchView), propertyChanged: OnRepeatSearchButtonTextChanged);
    #endregion bindable properties
}
