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
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Defines the API contract for sources of search results that can be used with <see cref="SearchViewModel"/>.
    /// </summary>
    public interface ISearchSource
    {
        /// <summary>
        /// Gets or sets the display name for the source, which may be displayed in the UI.
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the placeholder used for this source, which may be displaye in the UI.
        /// </summary>
        string? Placeholder { get; set; }

        /// <summary>
        /// Gets or sets the callout definition used for results by default.
        /// </summary>
        public CalloutDefinition? DefaultCalloutDefinition { get; set; }

        /// <summary>
        /// Gets or sets the symbol to be used for results by default.
        /// </summary>
        public Symbol? DefaultSymbol { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of results to return when completing a search.
        /// </summary>
        int MaximumResults { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of results to return when requesting search suggestions.
        /// </summary>
        int MaximumSuggestions { get; set; }

        /// <summary>
        /// Gets a list of suggestions for the given query.
        /// </summary>
        /// <param name="queryString">Text of the query.</param>
        /// <param name="preferredSearchLocation">Area around which results should be returned.</param>
        /// <param name="cancellationToken">Token used to cancel requests (e.g. because the search text has changed).</param>
        /// <returns>Task returning a list of suggestions.</returns>
        Task<IList<SearchSuggestion>> SuggestAsync(string queryString, MapPoint? preferredSearchLocation, CancellationToken? cancellationToken);

        /// <summary>
        /// Gets a list of search results for the given query.
        /// </summary>
        /// <param name="queryString">Text of the query.</param>
        /// <param name="preferredSearchLocation">Point around which results should be returned.</param>
        /// <param name="searchArea">Area used as a constraint on the search.</param>
        /// <param name="cancellationToken">Token used to cancel requests (e.g. because the search was changed or canceled).</param>
        /// <returns>Task returning list of search results.</returns>
        Task<IList<SearchResult>> SearchAsync(string queryString, MapPoint? preferredSearchLocation, Geometry.Geometry? searchArea, CancellationToken? cancellationToken);

        /// <summary>
        /// Gets a list of search results for the given suggestions.
        /// </summary>
        /// <param name="suggestion">Suggestion to use for the search.</param>
        /// <param name="cancellationToken">Token used to cancel searches.</param>
        /// <returns>Task returning a list of results.</returns>
        Task<IList<SearchResult>> SearchAsync(SearchSuggestion suggestion, CancellationToken? cancellationToken);

        /// <summary>
        /// Used to notify the source when the <see cref="SearchViewModel"/> has selected a result.
        /// </summary>
        /// <remarks>This can be used to implement custom selection behavior (e.g. when using a <see cref="FeatureLayer"/>).</remarks>
        /// <param name="result">The given search result.</param>
        void NotifySelected(SearchResult result);

        /// <summary>
        /// Used to notify the source when the <see cref="SearchViewModel"/> has deselected a result, or all results if <paramref name="result"/> is null.
        /// </summary>
        /// <remarks>This can be used to implement custom selection behavior (e.g. when using a <see cref="FeatureLayer"/>).</remarks>
        /// <param name="result">The result that has been deselected.</param>
        void NotifyDeselected(SearchResult? result);

        /// <summary>
        /// Gets or sets a callback used to customize search results before they are returned.
        /// </summary>
        /// <remarks>
        /// This can be used to customize results before display without writing a custom search source.
        /// Implementers of <see cref="ISearchSource"/> must call this function if defined and update results before returning them.
        /// If the function is defined and returns null for a result, that result should be removed from the result list.
        /// </remarks>
        Func<SearchResult, SearchResult?>? ResultCustomizationCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback used to customize search suggestions before they are returned.
        /// </summary>
        /// <remarks>
        /// This can be used to customize suggestion before display without writing a custom search source.
        /// Implementers of <see cref="ISearchSource"/> must call this function if defined and update suggestion before returning them.
        /// If the function is defined and returns null for a suggestion, that suggestion should be removed from the suggestion list.
        /// </remarks>
        Func<SearchSuggestion, SearchSuggestion?>? SuggestionCustomizationCallback { get; set; }
    }
}
