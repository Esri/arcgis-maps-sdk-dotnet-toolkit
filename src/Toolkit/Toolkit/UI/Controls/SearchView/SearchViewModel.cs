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
        private Geometry.Geometry? _lastSetArea;
        private MapPoint? _queryCenter;
        private IList<SearchResult>? _results;
        private IList<SearchSuggestion>? _suggestions;
        private SearchSuggestion? _lastSuggestion;

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

        /// <summary>
        /// Gets a value indicating whether a search operation is in progress.
        /// </summary>
        public bool IsSearchInProgress
        {
            get => _searchInProgress;
            private set => SetPropertyChanged(value, ref _searchInProgress, nameof(IsSearchInProgress), nameof(IsWaiting));
        }

        /// <summary>
        /// Gets a value indicating whether a suggestion request is in progress.
        /// </summary>
        public bool IsSuggestInProgress
        {
            get => _suggestInProgress;
            private set => SetPropertyChanged(value, ref _suggestInProgress, nameof(IsSuggestInProgress), nameof(IsWaiting));
        }

        /// <summary>
        /// Gets a value indicating whether a waiting operation (search, suggestion) is in progress.
        /// </summary>
        /// <remarks>
        /// This can be used to implement activity indicator or progress bar display.
        /// </remarks>
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
        public string? DefaultPlaceholder { get => _defaultPlaceholder; set => SetPropertyChanged(value, ref _defaultPlaceholder, nameof(DefaultPlaceholder), nameof(ActivePlaceholder)); }

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
        /// When used in conjunction with a GeoView, this property should be set every time navigation completes,
        /// to enable automatic update of the <see cref="IsEligibleForRequery"/> property.
        /// </summary>
        public Geometry.Geometry? QueryArea
        {
            get => _queryArea;
            set
            {
                if (value != null)
                {
                    value = GeometryEngine.NormalizeCentralMeridian(value);
                }

                SetPropertyChanged(value, ref _queryArea);

                if (IgnoreAreaChangesFlag)
                {
                    // Store set viewpoint for comparison.
                    _lastSetArea = value;
                }
                else if (Results != null && _lastSetArea?.Extent is Envelope oldView && value?.Extent is Envelope newView)
                {
                    double avgSize = (oldView.Width + oldView.Height) / 2;
                    double threshold = avgSize / 4;
                    double distance = GeometryEngine.Distance(oldView.GetCenter(), newView.GetCenter());
                    double newAvgSize = (newView.Width + newView.Height) / 2;
                    IsEligibleForRequery = distance > threshold || newAvgSize > avgSize * 1.25 || newAvgSize < avgSize * 0.75;
                }
            }
        }

        /// <summary>
        /// Gets or sets the center point around which results should be returned.
        /// </summary>
        public MapPoint? QueryCenter
        {
            get => _queryCenter;
            set
            {
                if (value != null)
                {
                    value = GeometryEngine.NormalizeCentralMeridian(value) as MapPoint;
                }

                SetPropertyChanged(value, ref _queryCenter);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether changes to the query area should be ignored. This is used to prevent <see cref="IsEligibleForRequery"/> becoming true because the view zoomed to a result.
        /// </summary>
        /// <remarks>
        /// Set this value to true when the GeoView is being navigated to show results, and set to false when the navigation completes.
        /// </remarks>
        public bool IgnoreAreaChangesFlag { get; set; }

        /// <summary>
        /// Gets or sets the list of available search sources, which can be updated dynamically.
        /// </summary>
        /// <remarks>See <see cref="ConfigureDefaultWorldGeocoder(CancellationToken)"/> for a convenient method to populate this collection automatically.</remarks>
        public IList<ISearchSource> Sources { get; set; } = new ObservableCollection<ISearchSource>();

        /// <summary>
        /// Gets the list of search results for the most-recently completed query.
        /// Clearing a search via <see cref="ClearSearch"/> will set this collection to <c>null</c>.
        /// </summary>
        public IList<SearchResult>? Results { get => _results; private set => SetPropertyChanged(value, ref _results); }

        /// <summary>
        /// Gets the list of search suggestions. This value is set after calls to <see cref="UpdateSuggestions"/>.
        /// </summary>
        public IList<SearchSuggestion>? Suggestions { get => _suggestions; private set => SetPropertyChanged(value, ref _suggestions); }

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

            if (string.IsNullOrWhiteSpace(CurrentQuery))
            {
                return;
            }

            using var searchCancellation = new CancellationTokenSource(QueryTimeoutMilliseconds);
            try
            {
                _activeSearchCancellation = searchCancellation;
                PrepareForNewSearch();
                _lastSuggestion = null;
                var sourcesToSearch = SourcesToSearch();

                foreach (var source in SourcesToSearch())
                {
                    source.SearchArea = QueryArea;
                    source.PreferredSearchLocation = QueryCenter;
                }

                var allResults = await Task.WhenAll(sourcesToSearch.Select(s => s.SearchAsync(CurrentQuery!, _activeSearchCancellation.Token)));

                ApplyNewResult(allResults.SelectMany(l => l).ToList(), null);
            }
            catch (TaskCanceledException)
            {
                ApplyNewResult(new List<SearchResult>(0), null);
            }
            finally
            {
                _activeSearchCancellation = null;
                IsSearchInProgress = false;
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
            if (_activeSearchCancellation != null)
            {
                _activeSearchCancellation.Cancel();
            }

            if (string.IsNullOrWhiteSpace(CurrentQuery) || QueryArea?.Extent == null)
            {
                return;
            }

            using var searchCancellation = new CancellationTokenSource(QueryTimeoutMilliseconds);
            try
            {
                _activeSearchCancellation = searchCancellation;
                PrepareForNewSearch();
                var sourcesToSearch = SourcesToSearch();
                foreach (var source in sourcesToSearch)
                {
                    source.SearchArea = QueryArea;
                    source.PreferredSearchLocation = QueryCenter;
                }

                var allResults = await Task.WhenAll(sourcesToSearch.Select(s => s.RepeatSearchAsync(CurrentQuery!, QueryArea.Extent, _activeSearchCancellation.Token)));

                ApplyNewResult(allResults.SelectMany(l => l).ToList(), _lastSuggestion);
            }
            catch (TaskCanceledException)
            {
                ApplyNewResult(new List<SearchResult>(0), _lastSuggestion);
            }
            finally
            {
                _activeSearchCancellation = null;
                IsSearchInProgress = false;
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

            if (string.IsNullOrWhiteSpace(CurrentQuery))
            {
                Suggestions = null;
                return;
            }

            using var suggestCancellation = new CancellationTokenSource(QueryTimeoutMilliseconds);
            try
            {
                IsSuggestInProgress = true;
                _activeSuggestCancellation = suggestCancellation;
                Suggestions = null;

                var sourcesToSearch = SourcesToSearch();
                foreach (var source in sourcesToSearch)
                {
                    source.SearchArea = QueryArea;
                    source.PreferredSearchLocation = QueryCenter;
                }

                var allSuggestions = await Task.WhenAll(sourcesToSearch.Select(s => s.SuggestAsync(CurrentQuery!, suggestCancellation.Token)));

                Suggestions = allSuggestions.SelectMany(l => l).ToList();
            }
            catch (TaskCanceledException)
            {
                Suggestions = new List<SearchSuggestion>(0);
            }
            finally
            {
                _activeSuggestCancellation = null;
                IsSuggestInProgress = false;
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

            using var searchCancellation = new CancellationTokenSource(QueryTimeoutMilliseconds);
            try
            {
                _activeSearchCancellation = searchCancellation;
                PrepareForNewSearch();

                _lastSuggestion = suggestion;

                // Update the UI just so it matches user expectation
                CurrentQuery = suggestion.DisplayTitle;

                var selectedSource = suggestion.OwningSource;
                var results = await selectedSource.SearchAsync(suggestion, searchCancellation.Token);

                ApplyNewResult(results, suggestion);
            }
            catch (TaskCanceledException)
            {
                ApplyNewResult(new List<SearchResult>(0), suggestion);
            }
            finally
            {
                _activeSearchCancellation = null;
                IsSearchInProgress = false;
            }
        }

        /// <summary>
        /// Cancels any previous operations, clears results, and gets ready for a new search.
        /// </summary>
        private void PrepareForNewSearch()
        {
            SelectedResult = null;
            Suggestions = null;
            Results = null;
            IsEligibleForRequery = false;
            IsSearchInProgress = true;
        }

        private void ApplyNewResult(IList<SearchResult> results, SearchSuggestion? originatingSuggestion)
        {
            if (!results.Any())
            {
                Results = new List<SearchResult>();
                return;
            }

            if (results.Count == 1)
            {
                Results = results.ToList();
                SelectedResult = results.First();
                return;
            }

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
                    if (originatingSuggestion?.IsCollection ?? false)
                    {
                        Results = results.ToList();
                    }
                    else
                    {
                        Results = new List<SearchResult>() { results.First() };
                    }

                    break;
            }

            if (Results?.Count == 1)
            {
                SelectedResult = Results.First();
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
            _lastSuggestion = null;
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

        /// <summary>
        /// Configures the viewmodel with a search source optimized for use with the Esri World Geocoder service.
        /// </summary>
        /// <param name="token">Token used for cancellation.</param>
        public async Task ConfigureDefaultWorldGeocoder(CancellationToken token = default)
        {
            Sources.Clear();
            Sources.Add(await LocatorSearchSource.CreateDefaultSourceAsync(token));
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
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
