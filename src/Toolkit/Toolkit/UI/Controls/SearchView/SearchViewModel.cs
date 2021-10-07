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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Backing controller for a search experience, intended for use with SearchView.
    /// SearchView supports searching, with search-as-you-type for multiple search providers via <see cref="ISearchSource"/>.
    /// </summary>
    public class SearchViewModel : INotifyPropertyChanged
    {
        private const int QueryTimeoutMilliseconds = 2000;
        private ISearchSource? _activeSource;
        private SearchResult? _selectedResult;
        private string? _currentQuery;
        private string? _defaultPlaceholder = "Find a place or address";
        private SearchResultMode _searchMode = SearchResultMode.Automatic;
        private Geometry.Geometry? _queryArea;
        private MapPoint? _queryCenter;
        private List<SearchResult>? _results;
        private List<SearchSuggestion>? _suggestions;

        private bool _searchInProgress;
        private bool _suggestInProgress;

        private CancellationTokenSource? _activeSearchCancellation;
        private CancellationTokenSource? _activeSuggestCancellation;

        /// <summary>
        /// Gets or sets the active search source, if one is selected. If there is no selection, all sources will be used for the search.
        /// </summary>
        public ISearchSource? ActiveSource
        {
            get => _activeSource;
            set
            {
                SetPropertyChanged(value, ref _activeSource, nameof(ActiveSource), nameof(ActivePlaceholder));
                _ = UpdateSuggestions();
            }
        }

        public bool IsSearchInProgress
        {
            get => _searchInProgress;
            set => SetPropertyChanged(value, ref _searchInProgress, nameof(IsSearchInProgress), nameof(IsWaiting));
        }

        public bool IsSuggestInProgress
        {
            get => _suggestInProgress;
            set => SetPropertyChanged(value, ref _suggestInProgress, nameof(IsSuggestInProgress), nameof(IsWaiting));
        }

        public bool IsWaiting
        {
            get => IsSearchInProgress || IsSuggestInProgress;
        }

        /// <summary>
        /// Gets or sets the selected search result.
        /// </summary>
        public SearchResult? SelectedResult { get => _selectedResult; set => SetPropertyChanged(value, ref _selectedResult); }

        /// <summary>
        /// Gets or sets the current query.
        /// </summary>
        /// <remarks>
        /// Call <see cref="UpdateSuggestions"/> to refresh suggestions after updating the query.
        /// </remarks>
        public string? CurrentQuery { get => _currentQuery; set => SetPropertyChanged(value, ref _currentQuery); }

        /// <summary>
        /// Gets or sets the default placeholder to use when there is no <see cref="ActiveSource"/> or the <see cref="ActiveSource"/> does not have a placeholder defined.
        /// Consumers should always display the <see cref="ActivePlaceholder"/> in the UI, rather than accessing this property directly.
        /// </summary>
        public string? DefaultPlaceholder { get => _defaultPlaceholder; set => SetPropertyChanged(value, ref _defaultPlaceholder); }

        /// <summary>
        /// Gets the correct placeholder to display in the UI.
        /// </summary>
        public string? ActivePlaceholder
        {
            get
            {
                if (ActiveSource?.Placeholder is string placeholder)
                {
                    return placeholder;
                }

                return DefaultPlaceholder;
            }
        }

        /// <summary>
        /// Gets or sets the search mode, which defaults to <see cref="SearchResultMode.Automatic"/>.
        /// This mode controls how many results are displayed when a search is performed.
        /// </summary>
        public SearchResultMode SearchMode { get => _searchMode; set => SetPropertyChanged(value, ref _searchMode); }

        /// <summary>
        /// Gets or sets the query area to use when searching and getting suggestions.
        /// When used in conjunction with a <see cref="GeoView"/>, this property should be set every time navigation completes,
        /// to enable automatic update of the <see cref="IsEligibleForRequery"/> property.
        /// </summary>
        public Geometry.Geometry? QueryArea
        {
            get => _queryArea;
            set
            {
                SetPropertyChanged(value, ref _queryArea);

                if (Results != null && value != null)
                {
                    // TODO - update for new logic
                    IsEligibleForRequery = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the center point around which results should be returned.
        /// </summary>
        public MapPoint? QueryCenter
        {
            get => _queryCenter;
            set => SetPropertyChanged(value, ref _queryCenter);
        }

        /// <summary>
        /// Gets or sets a value indicating whether changes to the query area should be ignored. This is used to prevent <see cref="IsEligibleForRequery"/> becoming true because the view zoomed to a result.
        /// </summary>
        /// <remarks>
        /// Set this value to true when the GeoView is being navigated to show results, and set to false when the navigation completes.
        /// </remarks>
        public bool IgnoreAreaChangesFlag { get; set; }

        /// <summary>
        /// Gets the list of available search sources, which can be updated dynamically.
        /// </summary>
        /// <remarks>See <see cref="ConfigureFromMap(GeoModel?)"/> for a convenient method to populate this collection automatically.</remarks>
        public ObservableCollection<ISearchSource> Sources { get; } = new ObservableCollection<ISearchSource>();

        /// <summary>
        /// Gets the list of search results for the most-recently completed query.
        /// Clearing a search via <see cref="ClearSearch"/> will set this collection to <c>null</c>.
        /// </summary>
        public List<SearchResult>? Results { get => _results; private set => SetPropertyChanged(value, ref _results); }

        /// <summary>
        /// Gets the list of search suggestions. This value is set after calls to <see cref="UpdateSuggestions"/>.
        /// </summary>
        public List<SearchSuggestion>? Suggestions { get => _suggestions; private set => SetPropertyChanged(value, ref _suggestions); }

        private bool _viewpointChangedSinceResultReturned;

        /// <summary>
        /// Gets a value indicating whether spatial parameters have changed enough to justify displaying a 'Repeat Search Here' button.
        /// </summary>
        /// <remarks>
        /// 'Repeat Search Here' is a common pattern in applications that return multiple search results.
        /// </remarks>
        public bool IsEligibleForRequery
        {
            get => _viewpointChangedSinceResultReturned;
            private set => SetPropertyChanged(value, ref _viewpointChangedSinceResultReturned);
        }

        /// <summary>
        /// Submits the current query as a fresh search.
        /// </summary>
        /// <remarks>
        /// The search will return results from the <see cref="ActiveSource"/> or all <see cref="Sources"/> if <see cref="ActiveSource"/> is <c>null</c>.
        /// Search in progress can be cancelled by calling <see cref="CancelSearch"/>.
        /// </remarks>
        public async Task CommitSearch()
        {
            if (_activeSearchCancellation != null)
            {
                _activeSearchCancellation.Cancel();
            }

            using (CancellationTokenSource searchCancellation = new CancellationTokenSource(QueryTimeoutMilliseconds))
            {
                try
                {
                    IsSearchInProgress = true;
                    _activeSearchCancellation = searchCancellation;
                    Suggestions = null;
                    Results = null;
                    IsEligibleForRequery = false;
                    var sourcesToSearch = SourcesToSearch();

                    foreach (var source in sourcesToSearch)
                    {
                        source.SearchArea = QueryArea;
                        source.PreferredSearchLocation = QueryCenter;
                    }

                    var allResults = await Task.WhenAll(sourcesToSearch.Select(s => s.SearchAsync(CurrentQuery, _activeSearchCancellation.Token)));

                    Results = allResults.SelectMany(l => l).ToList();
                }
                catch (Exception)
                {
                    // TODO - decide on error handling
                }
                finally
                {
                    _activeSearchCancellation = null;
                    IsSearchInProgress = false;
                }
            }
        }

        /// <summary>
        /// Repeats the current search, with results confined to the area defined by <see cref="QueryArea"/>.
        /// </summary>
        /// <remarks>
        /// This is used to allow users to narrow down search results using a geographic constraint.
        /// This is especially useful when a search results in multiple results.
        /// </remarks>
        public async Task RepeatSearchHere()
        {
            // TODO - be smarter about remembering suggestions and using those for repeated searches if possible
            if (_activeSearchCancellation != null)
            {
                _activeSearchCancellation.Cancel();
            }

            using (CancellationTokenSource searchCancellation = new CancellationTokenSource(QueryTimeoutMilliseconds))
            {
                try
                {
                    IsSearchInProgress = true;
                    _activeSearchCancellation = searchCancellation;
                    Suggestions = null;
                    Results = null;
                    IsEligibleForRequery = false;
                    var sourcesToSearch = SourcesToSearch();
                    foreach (var source in sourcesToSearch)
                    {
                        source.SearchArea = QueryArea;
                        source.PreferredSearchLocation = QueryCenter;
                    }

                    var allResults = await Task.WhenAll(sourcesToSearch.Select(s => s.RepeatSearchAsync(CurrentQuery, QueryArea?.Extent, _activeSearchCancellation.Token)));

                    Results = allResults.SelectMany(l => l).ToList();
                }
                catch (Exception)
                {
                    // TODO - decide how to handle exceptions.
                }
                finally
                {
                    _activeSearchCancellation = null;
                    IsSearchInProgress = false;
                }
            }
        }

        /// <summary>
        /// Updates <see cref="Suggestions"/> for the current query.
        /// </summary>
        public async Task UpdateSuggestions()
        {
            if (_activeSuggestCancellation != null)
            {
                _activeSuggestCancellation.Cancel();
            }

            using (CancellationTokenSource suggestCancellation = new CancellationTokenSource(QueryTimeoutMilliseconds))
            {
                try
                {
                    IsSuggestInProgress = true;
                    _activeSuggestCancellation = suggestCancellation;
                    Suggestions = null;
                    if (string.IsNullOrWhiteSpace(CurrentQuery))
                    {
                        return;
                    }

                    var sourcesToSearch = SourcesToSearch();
                    foreach (var source in sourcesToSearch)
                    {
                        source.SearchArea = QueryArea;
                        source.PreferredSearchLocation = QueryCenter;
                    }

                    var allSuggestions = await Task.WhenAll(sourcesToSearch.Select(s => s.SuggestAsync(CurrentQuery, suggestCancellation.Token)));

                    Suggestions = allSuggestions.SelectMany(l => l).ToList();
                }
                catch (Exception)
                {
                }
                finally
                {
                    _activeSuggestCancellation = null;
                    IsSuggestInProgress = false;
                }
            }
        }

        /// <summary>
        /// Initiates a search using a suggestion as the query.
        /// </summary>
        /// <param name="suggestion">A suggestion from <see cref="Suggestions"/> to be used as basis for the search.</param>
        /// <remarks>This will update <see cref="CurrentQuery"/> to match the selected suggestion.</remarks>
        public async Task AcceptSuggestion(SearchSuggestion suggestion)
        {
            if (_activeSearchCancellation != null)
            {
                _activeSearchCancellation.Cancel();
            }

            using (CancellationTokenSource searchCancellation = new CancellationTokenSource(QueryTimeoutMilliseconds))
            {
                try
                {
                    IsSearchInProgress = true;
                    _activeSearchCancellation = searchCancellation;
                    Suggestions = null;
                    Results = null;
                    SelectedResult = null;
                    IsEligibleForRequery = false;

                    if (suggestion == null)
                    {
                        return;
                    }

                    // Update the UI just so it matches user expectation
                    CurrentQuery = suggestion.DisplayTitle;

                    var selectedSource = suggestion.OwningSource;
                    var results = await selectedSource.SearchAsync(suggestion, searchCancellation.Token);

                    switch (SearchMode)
                    {
                        case SearchResultMode.Single:
                            Results = new List<SearchResult> { results.First() };
                            SelectedResult = Results.First();
                            break;
                        case SearchResultMode.Multiple:
                            Results = results.ToList();
                            break;
                        case SearchResultMode.Automatic:
                            if (suggestion.IsCollection)
                            {
                                Results = results.ToList();
                            }
                            else
                            {
                                Results = new List<SearchResult>() { results.First() };
                            }

                            if (Results?.Count == 1)
                            {
                                SelectedResult = Results.First();
                            }

                            break;
                    }
                }
                catch (Exception)
                {
                    // TODO - decide how to report.
                }
                finally
                {
                    _activeSearchCancellation = null;
                    IsSearchInProgress = false;
                }
            }
        }

        /// <summary>
        /// Configures the view for the given map or scene.
        /// If no value is provided, the Esri World Geocoder is used as a single source by default.
        /// </summary>
        /// <param name="model">Optional web map or scene to use for configuration.</param>
        /// <remarks>
        /// Automatic configuration is not fully supported when using maps or scenes from offline packages.
        /// </remarks>
        public async Task ConfigureFromMap(GeoModel? model)
        {
            if (model is Map mp)
            {
                await mp.RetryLoadAsync();
            }
            else if (model is Scene sp)
            {
                await sp.RetryLoadAsync();
            }

            // Clear existing properties
            ClearSearch();
            Sources.Clear();
            ActiveSource = null;

            DefaultPlaceholder = "Find a place or address";

            // TODO = Read default search hint from JSON
            // TODO = Add any locators specified in JSON
            // TODO = Determine if default locator source should be added
            // TODO = Add any layer search sources specified in JSON
            bool includeDefault = true;

            if (includeDefault)
            {
                var locatorSource = new SmartLocatorSearchSource(
                    new LocatorTask(new Uri("https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer")),
                    await Symbology.SymbolStyle.OpenAsync("Esri2DPointSymbolsStyle", null));
                Sources.Add(locatorSource);
            }
        }

        /// <summary>
        /// Cancels any active search/suggest tasks, then clears all results and the current query.
        /// </summary>
        public void ClearSearch()
        {
            _activeSearchCancellation?.Cancel();
            _activeSuggestCancellation?.Cancel();
            SelectedResult = null;
            Results = null;
            Suggestions = null;
            CurrentQuery = null;
            IsEligibleForRequery = false;
        }

        /// <summary>
        /// Cancels any active search task.
        /// </summary>
        public void CancelSearch()
        {
            _activeSearchCancellation?.Cancel();
        }

        /// <summary>
        /// Cancels any active suggest task.
        /// </summary>
        public void CancelSuggestion()
        {
            _activeSuggestCancellation?.Cancel();
        }

        private List<ISearchSource> SourcesToSearch()
        {
            var selectedSources = new List<ISearchSource>();
            if (ActiveSource == null)
            {
                selectedSources.AddRange(Sources);
            }
            else
            {
                selectedSources.Add(ActiveSource);
            }

            return selectedSources;
        }

        #region INPC helpers
        private void SetPropertyChanged<T>(T value, ref T field, [CallerMemberName] string propertyName = "")
        {
            if (!Equals(value, field))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        private void SetPropertyChanged<T>(T value, ref T field, params string[] notifiedProperties)
        {
            if (!Equals(value, field))
            {
                field = value;

                foreach (var property in notifiedProperties)
                {
                    OnPropertyChanged(property);
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}
