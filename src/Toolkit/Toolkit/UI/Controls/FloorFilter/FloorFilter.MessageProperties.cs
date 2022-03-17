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

#if !XAMARIN && !WINDOWS_UWP

using System.Windows;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
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
        /// Identifies the <see cref="SearchPlaceholder"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchPlaceholderProperty =
            DependencyProperty.Register(nameof(SearchPlaceholder), typeof(string), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the message shown to the user when a list or filtered list is empty.
        /// </summary>
        public string? NoResultsMessage
        {
            get => GetValue(NoResultsMessageProperty) as string;
            set => SetValue(NoResultsMessageProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="NoResultsMessage"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NoResultsMessageProperty =
            DependencyProperty.Register(nameof(NoResultsMessage), typeof(string), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the label or tooltip shown on the button that opens the browsing view and in the browsing view header.
        /// </summary>
        public string? BrowseLabel
        {
            get => GetValue(BrowseLabelProperty) as string;
            set => SetValue(BrowseLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BrowseLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BrowseLabelProperty =
            DependencyProperty.Register(nameof(BrowseLabel), typeof(string), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the label or tooltip shown on the button that zooms to the currently selected site or facility.
        /// </summary>
        public string? ZoomToLabel
        {
            get => GetValue(ZoomToLabelProperty) as string;
            set => SetValue(ZoomToLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ZoomToLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomToLabelProperty =
            DependencyProperty.Register(nameof(ZoomToLabel), typeof(string), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the label or tooltip shown on the expand/collapse button when the <see cref="FloorFilter"/> is expanded.
        /// </summary>
        public string? CollapseLabel
        {
            get => GetValue(CollapseLabelProperty) as string;
            set => SetValue(CollapseLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CollapseLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CollapseLabelProperty =
            DependencyProperty.Register(nameof(CollapseLabel), typeof(string), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the label or tooltip shown on the expand/collapse button when the <see cref="FloorFilter"/> is collapsed.
        /// </summary>
        public string? ExpandLabel
        {
            get => GetValue(ExpandLabelProperty) as string;
            set => SetValue(ExpandLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ExpandLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpandLabelProperty =
            DependencyProperty.Register(nameof(ExpandLabel), typeof(string), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the label or tooltip shown for the button that is used to close the browsing view.
        /// </summary>
        public string? CloseLabel
        {
            get => GetValue(CloseLabelProperty) as string;
            set => SetValue(CloseLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CloseLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseLabelProperty =
            DependencyProperty.Register(nameof(CloseLabel), typeof(string), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the label or tooltip shown for the button that shows all levels for a selected facility.
        /// </summary>
        public string? AllFloorsLabel
        {
            get => GetValue(AllFloorsLabelProperty) as string;
            set => SetValue(AllFloorsLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AllFloorsLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllFloorsLabelProperty =
            DependencyProperty.Register(nameof(AllFloorsLabel), typeof(string), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the label or tooltip shown for the button that is used to navigate to the list of sites in all facilities when the site browsing view is open.
        /// </summary>
        public string? AllFacilitiesLabel
        {
            get => GetValue(AllFacilitiesLabelProperty) as string;
            set => SetValue(AllFacilitiesLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AllFacilitiesLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllFacilitiesLabelProperty =
            DependencyProperty.Register(nameof(AllFacilitiesLabel), typeof(string), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the label or tooltip text shown on the back button used to navigate from the facilities to the site list when the browsing view is open.
        /// </summary>
        public string? BackButtonLabel
        {
            get => GetValue(BackButtonLabelProperty) as string;
            set => SetValue(BackButtonLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BackButtonLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BackButtonLabelProperty =
            DependencyProperty.Register(nameof(BackButtonLabel), typeof(string), typeof(FloorFilter), null);
    }
}

#endif