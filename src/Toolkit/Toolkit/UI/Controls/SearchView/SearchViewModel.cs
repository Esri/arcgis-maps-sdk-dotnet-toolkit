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

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public class SearchViewModel : INotifyPropertyChanged
    {
        private ISearchSource _activeSource;
        private SearchResult _selectedResult;
        private string _currentQuery;
        private string _defaultPlaceholder = "Find a place or address";
        private SearchResultMode _searchMode = SearchResultMode.Automatic;
        private Geometry.Geometry _queryArea;
        private MapPoint _queryCenter;
        private List<SearchResult> _results;
        private List<SearchSuggestion> _suggestions;

        private CancellationTokenSource _activeSearchCancellation;
        private CancellationTokenSource _activeSuggestCancellation;

        public ISearchSource ActiveSource { get => _activeSource; set { SetPropertyChanged(value, ref _activeSource); UpdateSuggestions(); } }
        public SearchResult SelectedResult { get => _selectedResult; set => SetPropertyChanged(value, ref _selectedResult); }
        public string CurrentQuery { get => _currentQuery; set => SetPropertyChanged(value, ref _currentQuery); }
        public string DefaultPlaceholder { get => _defaultPlaceholder; set => SetPropertyChanged(value, ref _defaultPlaceholder); }
        public SearchResultMode SearchMode { get => _searchMode; set => SetPropertyChanged(value, ref _searchMode); }
        public Geometry.Geometry QueryArea { get => _queryArea; 
            set {
                SetPropertyChanged(value, ref _queryArea);
                if (Results != null && value != null)
                {
                    IsEligibleForRequery = true;
                }
                } 
            }
        public MapPoint QueryCenter { get => _queryCenter; set => SetPropertyChanged(value, ref _queryCenter); }

        public ObservableCollection<ISearchSource> Sources { get; } = new ObservableCollection<ISearchSource>();
        public List<SearchResult> Results { get => _results; private set => SetPropertyChanged(value, ref _results); }
        public List<SearchSuggestion> Suggestions { get => _suggestions; private set => SetPropertyChanged(value, ref _suggestions); }

        private bool _viewpointChangedSinceResultReturned;

        public bool IsEligibleForRequery
        {
            get => _viewpointChangedSinceResultReturned;
            private set => SetPropertyChanged(value, ref _viewpointChangedSinceResultReturned);
        }

        public async Task CommitSearch(bool restrictToViewArea = false)
        {
            if (_activeSearchCancellation != null)
                _activeSearchCancellation.Cancel();

            using (CancellationTokenSource searchCancellation = new CancellationTokenSource(2000))
            {
                try
                {
                    _activeSearchCancellation = searchCancellation;
                    Suggestions = null;
                    Results = null;
                    IsEligibleForRequery = false;
                    var sourcesToSearch = SourcesToSearch();

                    var queryRestrictionArea = restrictToViewArea ? QueryArea : null;

                    var allResults = await Task.WhenAll(sourcesToSearch.Select(s => s.SearchAsync(CurrentQuery, QueryCenter, queryRestrictionArea, _activeSearchCancellation.Token)));

                    Results = allResults.SelectMany(l => l).ToList();
                }
                catch (Exception)
                {

                }
                finally
                {
                    _activeSearchCancellation = null;
                }
            }

        }

        public async Task UpdateSuggestions()
        {
            if (_activeSuggestCancellation != null)
                _activeSuggestCancellation.Cancel();

            using (CancellationTokenSource suggestCancellation = new CancellationTokenSource())
            {
                try
                {
                    _activeSuggestCancellation = suggestCancellation;
                    Suggestions = null;
                    if (string.IsNullOrWhiteSpace(CurrentQuery))
                    {
                        return;
                    }
                    var sourcesToSearch = SourcesToSearch();

                    var allSuggestions = await Task.WhenAll(sourcesToSearch.Select(s => s.SuggestAsync(CurrentQuery, QueryCenter, suggestCancellation.Token)));

                    Suggestions = allSuggestions.SelectMany(l => l).ToList();
                }
                catch (Exception)
                {
                }
                finally
                {
                    _activeSuggestCancellation = null;
                }
            }
        }

        public async Task AcceptSuggestion(SearchSuggestion suggestion)
        {
            if (_activeSearchCancellation != null)
                _activeSearchCancellation.Cancel();

            using (CancellationTokenSource searchCancellation = new CancellationTokenSource(2000))
            {
                try
                {
                    _activeSearchCancellation = searchCancellation;
                    Suggestions = null;
                    Results = null;
                    SelectedResult = null;
                    IsEligibleForRequery = false;

                    if (suggestion == null) return;

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
                                Results = results.ToList();
                            else
                                Results = new List<SearchResult>() { results.First() };

                            if (Results?.Count == 1)
                                SelectedResult = Results.First();
                            break;
                    }
                }
                catch (Exception)
                {

                }
                finally
                {
                    _activeSearchCancellation = null;
                }
            }

        }

        public async Task ConfigureFromMap(object map)
        {
            if (map is Map mp)
            {
                await mp.RetryLoadAsync();
            }
            else if (map is Scene sp)
            {
                await sp.RetryLoadAsync();
            }
            // Clear existing properties
            this.Sources.Clear();
            this.ActiveSource = null;
            this.CurrentQuery = null;
            // TODO - enable localization
            this.DefaultPlaceholder = "Find a place or address";

            // Read default search hint from JSON

            // Add ArcGIS Online
            var locatorSource = new LocatorSearchSource(new LocatorTask(new Uri("https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer")));
            Sources.Add(locatorSource);

            // Add What3Words
            LocatorTask sampleTask = new LocatorTask(new Uri($"http://sampleserver6.arcgisonline.com/arcgis/rest/services/Locators/SanDiego/GeocodeServer"));

            var sampleSource = new LocatorSearchSource(sampleTask);
            Sources.Add(sampleSource);

            // Add any layers from JSON

            // Add any offline locators if map is MMPK
            if (map is Map mpx && mpx.Item is LocalItem localItem)
            {
                try
                {
                    var package = await MobileMapPackage.OpenAsync(localItem.Path);
                    if (package?.LocatorTask is LocatorTask offlineTask)
                    {
                        Sources.Add(new LocatorSearchSource(offlineTask));
                    }
                }
                catch (Exception ex)
                {
                    // TODO - handle
                }
            }
        }

        public void ClearSearch()
        {
            SelectedResult = null;
            Results = null;
            Suggestions = null;
            CurrentQuery = null;
            IsEligibleForRequery = false;
        }

        public void CancelSearch()
        {
            _activeSearchCancellation?.Cancel();
        }

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

        private void SetPropertyChanged<T>(T value, ref T field, [CallerMemberName] string propertyName = "")
        {
            if (!Equals(value, field))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        private void OnPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
