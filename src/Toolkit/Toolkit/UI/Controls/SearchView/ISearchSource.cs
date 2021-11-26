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
        CalloutDefinition? DefaultCalloutDefinition { get; set; }

        /// <summary>
        /// Gets or sets the symbol to be used for results by default.
        /// </summary>
        Symbol? DefaultSymbol { get; set; }

        /// <summary>
        /// Gets or sets the default zoom scale to be used for results from this source.</summary>
        /// <remarks>This value should be ignored when the underlying provider (e.g. LocatorTask) provides a viewpoint.
        /// Otherwise this zoom scale should be used to create the viewpoint for point results.
        /// </remarks>
        double DefaultZoomScale { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of results to return when completing a search.
        /// </summary>
        int MaximumResults { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of results to return when requesting search suggestions.
        /// </summary>
        int MaximumSuggestions { get; set; }

        /// <summary>
        /// Gets or sets the area to be used as a constraint for searches and suggestions.
        /// </summary>
        Geometry.Geometry? SearchArea { get; set; }

        /// <summary>
        /// Gets or sets the point to be used as an input to searches and suggestions.
        /// </summary>
        Geometry.MapPoint? PreferredSearchLocation { get; set; }

        /// <summary>
        /// Gets a list of suggestions for the given query.
        /// </summary>
        /// <param name="queryString">Text of the query.</param>
        /// <param name="cancellationToken">Token used to cancel requests (e.g. because the search text has changed).</param>
        /// <returns>Task returning a list of suggestions.</returns>
        Task<IList<SearchSuggestion>> SuggestAsync(string queryString, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a list of search results for the given query.
        /// </summary>
        /// <param name="queryString">Text of the query.</param>
        /// <param name="cancellationToken">Token used to cancel requests (e.g. because the search was changed or canceled).</param>
        /// <returns>Task returning list of search results.</returns>
        Task<IList<SearchResult>> SearchAsync(string queryString, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a list of search results for the given suggestions.
        /// </summary>
        /// <param name="suggestion">Suggestion to use for the search.</param>
        /// <param name="cancellationToken">Token used to cancel searches.</param>
        /// <returns>Task returning a list of results.</returns>
        Task<IList<SearchResult>> SearchAsync(SearchSuggestion suggestion, CancellationToken cancellationToken);

        /// <summary>
        /// Repeats the last search, with results restricted to the current visible area.
        /// </summary>
        /// <param name="queryString">Text to be used for the query.</param>
        /// <param name="queryExtent">Extent used to limit the results.</param>
        /// <param name="cancellationToken">Token used to cancel search.</param>
        /// <returns>Task returning a list of results.</returns>
        Task<IList<SearchResult>> RepeatSearchAsync(string queryString, Envelope queryExtent, CancellationToken cancellationToken);

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
    }
}
