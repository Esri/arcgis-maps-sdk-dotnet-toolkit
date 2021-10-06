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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.UI;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Search source intended for use with the World Geocode Service and similarly-configured geocode services.
    /// </summary>
    public class SmartLocatorSearchSource : LocatorSearchSource
    {
        /// <summary>
        /// Gets or sets the minimum number of results to attempt to return.
        /// If there are too few results, the search is repeated with loosened parameters until enough results are accumulated.
        /// </summary>
        /// <remarks>
        /// If no search is successful, it is still possible to have a total number of results less than this threshold.
        /// Does not apply to repeated search with area constraint.
        /// Set to zero to disable search repeat behavior. Defaults to 1.
        /// </remarks>
        public int RepeatSearchResultThreshold { get; set; } = 0;

        /// <summary>
        /// Gets or sets the minimum number of suggestions to attempt to return.
        /// If there are too few suggestions, request is repeated with loosened constraints until enough suggestions are accumulated.
        /// </summary>
        /// <remarks>
        /// If no search is successful, it is still possible to have a total number of results less than this threshold.
        /// Does not apply to repeated search with area constraint.
        /// Set to zero to disable search repeat behavior. Defaults to 6.
        /// </remarks>
        public int RepeatSuggestResultThreshold { get; set; } = 6;

        /// <summary>
        /// Gets or sets the web style used to find symbols for results.
        /// When set, symbols are found for results based on the result's `Type` field, if available.
        /// </summary>
        /// <remarks>
        /// Defaults to the style identified by the name "Esri2DPointSymbolsStyle".
        /// The default Esri 2D point symbol has good results for many of the types returned by the world geocode service.
        /// You can use this property to customize result icons by publishing a web style, taking care to ensure that symbol keys match the `Type` attribute returned by the locator.
        /// </remarks>
        public SymbolStyle? ResultSymbolStyle { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartLocatorSearchSource"/> class.
        /// </summary>
        /// <param name="locator">Locator to use.</param>
        /// <param name="style">Symbol style to use to find results.</param>
        public SmartLocatorSearchSource(LocatorTask locator, SymbolStyle? style)
            : base(locator)
        {
            if (style != null)
            {
                ResultSymbolStyle = style;
            }

            _ = EnsureLoaded();
        }

        private async Task EnsureLoaded()
        {
            if (Locator.LoadStatus == LoadStatus.Loaded)
            {
                return;
            }

            try
            {
                await Locator.RetryLoadAsync();
            }
            catch (Exception)
            {
                // TODO  - decide how tohandle this
            }

            if (Locator.LocatorInfo is LocatorInfo info)
            {
                // Locators from online services have descriptions but not names.
                if (!string.IsNullOrWhiteSpace(info.Name) && info.Name != Locator.Uri?.ToString())
                {
                    DisplayName = info.Name;
                }
                else if (!string.IsNullOrWhiteSpace(info.Description))
                {
                    DisplayName = info.Description;
                }
            }

            // Add attributes expected from the World Geocoder Service if present, otherwise default to all attributes.
            if (Locator.Uri == new Uri("https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer"))
            {
                var desiredAttributes = new[] { "LongLabel", "Type" };
                if (Locator.LocatorInfo?.ResultAttributes?.Any() ?? false)
                {
                    foreach (var attr in desiredAttributes)
                    {
                        if (Locator.LocatorInfo.ResultAttributes.Where(at => at.Name == attr).Any())
                        {
                            GeocodeParameters.ResultAttributeNames.Add(attr);
                        }
                    }
                }
                else
                {
                    GeocodeParameters.ResultAttributeNames.Add("*");
                }
            }
        }

        private async Task<SearchResult> GeocodeResultToSearchResult(GeocodeResult r)
        {
            var symbol = await SymbolForResult(r);
            string subtitle = $"Match percent: {r.Score}";
            if (r.Attributes.ContainsKey("LongLabel") && r.Attributes["LongLabel"]?.ToString() is string subtitleString)
            {
                subtitle = subtitleString;
            }

            var viewpoint = r.Extent == null ? null : new Mapping.Viewpoint(r.Extent);

            return new SearchResult(r.Label, subtitle, this, new Graphic(r.DisplayLocation, r.Attributes, symbol), viewpoint) { CalloutDefinition = DefaultCalloutDefinition };
        }

        private async Task<Symbol> SymbolForResult(GeocodeResult r)
        {
            if (r.Attributes.ContainsKey("Type"))
            {
                var typeAttrs = r.Attributes["Type"];

                //var firstResult = await _esriStyle.GetSymbolAsync(new[] { typeAttrs.ToString().Replace(' ', '-').ToLower() });
                //if (firstResult != null)
                //{
                    //return firstResult;
                //}
                var symbParams = new SymbolStyleSearchParameters();
                symbParams.Names.Add(typeAttrs.ToString());
                symbParams.NamesStrictlyMatch = false;
                var symbolResult = await ResultSymbolStyle.SearchSymbolsAsync(symbParams);

                if (symbolResult.Any())
                {
                    return await symbolResult.First().GetSymbolAsync();
                }
            }

            return DefaultSymbol;
        }

        /// <summary>
        /// Converts geocode result list into list of results, applying result limits and calling necessary callbacks.
        /// </summary>
        private async Task<IList<SearchResult>> ResultToSearchResult(IReadOnlyList<GeocodeResult> input)
        {
            IEnumerable<SearchResult> results = await Task.WhenAll(input.Select(i => GeocodeResultToSearchResult(i)));

            if (ResultCustomizationCallback != null)
            {
                results = results.Select(ResultCustomizationCallback).OfType<SearchResult>();
            }

            return results.Take(MaximumResults).ToList();
        }

        /// <summary>
        /// Converts suggest result list into list of suggestions, applying result limits and calling necessary callbacks.
        /// </summary>
        private IList<SearchSuggestion> SuggestionToSearchSuggestion(IReadOnlyList<SuggestResult> input)
        {
            var results = input.Select(i => SuggestResultToSearchSuggestion(i));

            if (SuggestionCustomizationCallback != null)
            {
                results = results.Select(SuggestionCustomizationCallback).OfType<SearchSuggestion>();
            }

            return results.Take(MaximumResults).ToList();
        }

        /// <inheritdoc />
        public override async Task<IList<SearchResult>> SearchAsync(SearchSuggestion suggestion, CancellationToken? cancellationToken)
        {
            await EnsureLoaded();

            cancellationToken?.ThrowIfCancellationRequested();

            var results = await Locator.GeocodeAsync(suggestion.UnderlyingObject as SuggestResult, cancellationToken ?? CancellationToken.None);

            cancellationToken?.ThrowIfCancellationRequested();

            return await ResultToSearchResult(results);
        }

        /// <summary>
        /// Creates a basic search suggestion for the given suggest result.
        /// </summary>
        private SearchSuggestion SuggestResultToSearchSuggestion(SuggestResult r)
        {
            return new SearchSuggestion(r.Label, this) { IsCollection = r.IsCollection, UnderlyingObject = r };
        }

        /// <inheritdoc/>
        public override async Task<IList<SearchSuggestion>> SuggestAsync(string queryString, CancellationToken? cancellationToken)
        {
            await EnsureLoaded();

            cancellationToken?.ThrowIfCancellationRequested();

            SuggestParameters.PreferredSearchLocation = PreferredSearchLocation;
            SuggestParameters.MaxResults = MaximumSuggestions;

            // TODO = implement repeat suggest behavior
            var results = await Locator.SuggestAsync(queryString, SuggestParameters, cancellationToken ?? CancellationToken.None);
            cancellationToken?.ThrowIfCancellationRequested();

            return SuggestionToSearchSuggestion(results);
        }

        /// <inheritdoc/>
        public override async Task<IList<SearchResult>> SearchAsync(string queryString, CancellationToken? cancellationToken)
        {
            await EnsureLoaded();

            cancellationToken?.ThrowIfCancellationRequested();

            // Reset spatial parameters
            GeocodeParameters.PreferredSearchLocation = PreferredSearchLocation;
            GeocodeParameters.SearchArea = SearchArea;
            GeocodeParameters.MaxResults = MaximumResults;

            // TODO = implement repeat search behavior
            var results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken ?? CancellationToken.None);
            cancellationToken?.ThrowIfCancellationRequested();
            return await ResultToSearchResult(results);
        }
    }
}
