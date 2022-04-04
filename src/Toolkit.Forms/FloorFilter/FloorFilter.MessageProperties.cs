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
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    public partial class FloorFilter
    {
        /// <summary>
        /// Gets or sets the text shown as a placeholder in search/filter boxes in the browsing view.
        /// </summary>
        public string? SearchPlaceholder
        {
            get => GetValue(SearchPlaceholderProperty) as string;
            set => SetValue(SearchPlaceholderProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SearchPlaceholder"/> bindable property.
        /// </summary>
        public static readonly BindableProperty SearchPlaceholderProperty =
            BindableProperty.Create(nameof(SearchPlaceholder), typeof(string), typeof(FloorFilter), "Search");

        /// <summary>
        /// Gets or sets the message shown to the user when a list or filtered list is empty.
        /// </summary>
        public string? NoResultsMessage
        {
            get => GetValue(NoResultsMessageProperty) as string;
            set => SetValue(NoResultsMessageProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="NoResultsMessage"/> bindable property.
        /// </summary>
        public static readonly BindableProperty NoResultsMessageProperty =
            BindableProperty.Create(nameof(NoResultsMessage), typeof(string), typeof(FloorFilter), "No results");

        /// <summary>
        /// Gets or sets the label or tooltip shown on the button that opens the browsing view and in the browsing view header.
        /// </summary>
        public string? BrowseLabel
        {
            get => GetValue(BrowseLabelProperty) as string;
            set => SetValue(BrowseLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BrowseLabel"/> bindable property.
        /// </summary>
        public static readonly BindableProperty BrowseLabelProperty =
            BindableProperty.Create(nameof(BrowseLabel), typeof(string), typeof(FloorFilter), "Browse");

        /// <summary>
        /// Gets or sets the label or tooltip shown for the button that is used to navigate to the list of sites in all facilities when the site browsing view is open.
        /// </summary>
        public string? AllFacilitiesLabel
        {
            get => GetValue(AllFacilitiesLabelProperty) as string;
            set => SetValue(AllFacilitiesLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AllFacilitiesLabel"/> bindable property.
        /// </summary>
        public static readonly BindableProperty AllFacilitiesLabelProperty =
            BindableProperty.Create(nameof(AllFacilitiesLabel), typeof(string), typeof(FloorFilter), "All");
    }
}
