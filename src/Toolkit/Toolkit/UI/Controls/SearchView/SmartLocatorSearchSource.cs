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
        public int RepeatSearchResultThreshold { get; set; } = 0;
        public int RepeatSuggestResultThreshold { get; set; } = 3;

        public SymbolStyle? SymbolStyleDictionary { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartLocatorSearchSource"/> class.
        /// </summary>
        /// <param name="locator">Locator to use.</param>
        /// <param name="style">Symbol style to use to find results.</param>
        public SmartLocatorSearchSource(LocatorTask locator, SymbolStyle? style)
            : base(locator)
        {
            SymbolStyleDictionary = style;
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
            catch (Exception ex)
            {

                //TODO  - decide how tohandle this
            }

            if (Locator.LocatorInfo is LocatorInfo info)
            {
                // Locators from online services have descriptions but not names.
                if (!string.IsNullOrWhiteSpace(info.Name))
                {
                    DisplayName = info.Name;
                }
                else if (!string.IsNullOrWhiteSpace(info.Description))
                {
                    DisplayName = info.Description;
                }
            }

            // Add attributes expected from the World Geocoder Service if present, otherwise default to all attributes.
            var desiredAttributes = new[] { "LongLabel", "Type" };
            if (Locator?.LocatorInfo?.ResultAttributes?.Any() ?? false)
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
                {
                    //return firstResult;
                }
                var symbParams = new SymbolStyleSearchParameters();
                symbParams.Names.Add(typeAttrs.ToString());
                symbParams.NamesStrictlyMatch = false;
                var symbolResult = await SymbolStyleDictionary.SearchSymbolsAsync(symbParams);

                if (symbolResult.Any())
                {
                    return await symbolResult.First().GetSymbolAsync();
                }
            }

            return DefaultSymbol;
        }

        public void NotifySelected(SearchResult result)
        {
            // This space intentionally left blank.
        }

        public void NotifyDeselected(SearchResult result)
        {
            // This space intentionally left blank.
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

        // TODO = abstract away commonalities between SearchAsync with suggestion and string
        public override async Task<IList<SearchResult>> SearchAsync(SearchSuggestion suggestion, CancellationToken? cancellationToken)
        {
            await EnsureLoaded();

            cancellationToken?.ThrowIfCancellationRequested();

            var results = await Locator.GeocodeAsync(suggestion.UnderlyingObject as SuggestResult, cancellationToken ?? CancellationToken.None);

            return await ResultToSearchResult(results);
        }

        /// <summary>
        /// Creates a basic search suggestion for the given suggest result.
        /// </summary>
        private SearchSuggestion SuggestResultToSearchSuggestion(SuggestResult r)
        {
            return new SearchSuggestion(r.Label, this) { IsCollection = r.IsCollection };
        }

        public async Task<IList<SearchSuggestion>> SuggestAsync(string queryString, MapPoint? preferredSearchLocation, CancellationToken? cancellationToken)
        {
            await EnsureLoaded();

            cancellationToken?.ThrowIfCancellationRequested();

            SuggestParameters.PreferredSearchLocation = preferredSearchLocation;
            SuggestParameters.MaxResults = MaximumSuggestions;

            // TODO = implement repeat suggest behavior

            var results = await Locator.SuggestAsync(queryString, SuggestParameters, cancellationToken ?? CancellationToken.None);

            return SuggestionToSearchSuggestion(results);
        }

        public async Task<IList<SearchResult>> SearchAsync(string queryString, MapPoint? preferredSearchLocation, Geometry.Geometry? searchArea, CancellationToken? cancellationToken)
        {
            await EnsureLoaded();

            cancellationToken?.ThrowIfCancellationRequested();

            // Reset spatial parameters
            GeocodeParameters.PreferredSearchLocation = preferredSearchLocation;
            GeocodeParameters.SearchArea = searchArea;
            GeocodeParameters.MaxResults = MaximumResults;


            // TODO = implement repeat search behavior

            var results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken ?? CancellationToken.None);
            return await ResultToSearchResult(results);
        }
    }
}
