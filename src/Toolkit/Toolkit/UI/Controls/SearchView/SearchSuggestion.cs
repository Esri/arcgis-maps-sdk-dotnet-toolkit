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

using Esri.ArcGISRuntime.Tasks.Geocoding;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Wraps a search suggestion for display.
    /// </summary>
    public class SearchSuggestion
    {
        /// <summary>
        /// Gets or sets the title to use when displaying a suggestion.
        /// </summary>
        public string DisplayTitle { get; set; }

        /// <summary>
        /// Gets or sets the optional subtitle that may be used when displaying a suggestion.
        /// </summary>
        public string? DisplaySubtitle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this result is for a collection (e.g. 'Coffee Shops') or a single result (e.g. 'Starbucks on Grand'). 
        /// </summary>
        public bool IsCollection { get; set; }

        /// <summary>
        /// Gets the <see cref="ISearchSource"/> that created this result.
        /// </summary>
        public ISearchSource OwningSource { get; }

        /// <summary>
        /// Gets or sets any underlying object for the suggestion.
        /// </summary>
        /// <remarks>
        /// This is helpful in cases where an underlying suggestion object is useful when completing a search. For example, when using a locator, the underlying <see cref="SuggestResult"/> should be used when accepting the suggestion for a search.
        /// </remarks>
        public object? UnderlyingObject { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchSuggestion"/> class.
        /// </summary>
        /// <param name="title">Sets <see cref="DisplayTitle"/>.</param>
        /// <param name="owner">Sets <see cref="OwningSource"/>.</param>
        public SearchSuggestion(string title, ISearchSource owner)
        {
            DisplayTitle = title;
            OwningSource = owner;
        }
    }
}
