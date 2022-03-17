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
using Esri.ArcGISRuntime.Mapping.Floor;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class FloorFilter
    {
        /// <summary>
        /// Gets or sets the template used to present <see cref="FloorLevel"/> items.
        /// </summary>
        public DataTemplate? LevelDataTemplate
        {
            get => GetValue(LevelDataTemplateProperty) as DataTemplate;
            set => SetValue(LevelDataTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="LevelDataTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LevelDataTemplateProperty =
            DependencyProperty.Register(nameof(LevelDataTemplate), typeof(DataTemplate), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the template used to present <see cref="FloorFacility"/> items for a single site, or when there are no sites.
        /// </summary>
        public DataTemplate? FacilityDataTemplate
        {
            get => GetValue(FacilityDataTemplateProperty) as DataTemplate;
            set => SetValue(FacilityDataTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FacilityDataTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FacilityDataTemplateProperty =
            DependencyProperty.Register(nameof(FacilityDataTemplate), typeof(DataTemplate), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the data template used to present <see cref="FloorFacility"/> items in the browsing view when facilities are being shown from multiple sites.
        /// </summary>
        public DataTemplate? DifferentiatingFacilityDataTemplate
        {
            get => GetValue(DifferentiatingFacilityDataTemplateProperty) as DataTemplate;
            set => SetValue(DifferentiatingFacilityDataTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DifferentiatingFacilityDataTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DifferentiatingFacilityDataTemplateProperty =
            DependencyProperty.Register(nameof(DifferentiatingFacilityDataTemplate), typeof(DataTemplate), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the data template used to present <see cref="FloorSite"/> items in the browsing view.
        /// </summary>
        public DataTemplate? SiteDataTemplate
        {
            get => GetValue(SiteDataTemplateProperty) as DataTemplate;
            set => SetValue(SiteDataTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SiteDataTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SiteDataTemplateProperty =
            DependencyProperty.Register(nameof(SiteDataTemplate), typeof(DataTemplate), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the style applied to list views.
        /// </summary>
        public Style? CommonListStyle
        {
            get => GetValue(CommonListStyleProperty) as Style;
            set => SetValue(CommonListStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CommonListStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommonListStyleProperty =
            DependencyProperty.Register(nameof(CommonListStyle), typeof(Style), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the style applied to the "Expand"/"Collapse" button.
        /// </summary>
        public Style? ExpandCollapseButtonStyle
        {
            get => GetValue(ExpandCollapseButtonStyleProperty) as Style;
            set => SetValue(ExpandCollapseButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ExpandCollapseButtonStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpandCollapseButtonStyleProperty =
            DependencyProperty.Register(nameof(ExpandCollapseButtonStyle), typeof(Style), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the style applied to the "Browse" button.
        /// </summary>
        public Style? BrowseButtonStyle
        {
            get => GetValue(BrowseButtonStyleProperty) as Style;
            set => SetValue(BrowseButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BrowseButtonStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BrowseButtonStyleProperty =
            DependencyProperty.Register(nameof(BrowseButtonStyle), typeof(Style), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the style that is applied to the "Zoom To" button.
        /// </summary>
        public Style? ZoomToButtonStyle
        {
            get => GetValue(ZoomToButtonStyleProperty) as Style;
            set => SetValue(ZoomToButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ZoomToButtonStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomToButtonStyleProperty =
            DependencyProperty.Register(nameof(ZoomToButtonStyle), typeof(Style), typeof(FloorFilter), null);
    }
}

#endif