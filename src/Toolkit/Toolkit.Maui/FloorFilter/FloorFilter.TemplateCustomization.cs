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

using Esri.ArcGISRuntime.Mapping.Floor;

namespace Esri.ArcGISRuntime.Toolkit.Maui
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
        /// Identifies the <see cref="LevelDataTemplate"/> bindable property.
        /// </summary>
        public static readonly BindableProperty LevelDataTemplateProperty =
            BindableProperty.Create(nameof(LevelDataTemplate), typeof(DataTemplate), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the template used to present <see cref="FloorFacility"/> items for a single site, or when there are no sites.
        /// </summary>
        public DataTemplate? FacilityDataTemplate
        {
            get => GetValue(FacilityDataTemplateProperty) as DataTemplate;
            set => SetValue(FacilityDataTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FacilityDataTemplate"/> bindable property.
        /// </summary>
        public static readonly BindableProperty FacilityDataTemplateProperty =
            BindableProperty.Create(nameof(FacilityDataTemplate), typeof(DataTemplate), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the data template used to present <see cref="FloorFacility"/> items in the browsing view when facilities are being shown from multiple sites.
        /// </summary>
        public DataTemplate? DifferentiatingFacilityDataTemplate
        {
            get => GetValue(DifferentiatingFacilityDataTemplateProperty) as DataTemplate;
            set => SetValue(DifferentiatingFacilityDataTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DifferentiatingFacilityDataTemplate"/> bindable property.
        /// </summary>
        public static readonly BindableProperty DifferentiatingFacilityDataTemplateProperty =
            BindableProperty.Create(nameof(DifferentiatingFacilityDataTemplate), typeof(DataTemplate), typeof(FloorFilter), null);

        /// <summary>
        /// Gets or sets the data template used to present <see cref="FloorSite"/> items in the browsing view.
        /// </summary>
        public DataTemplate? SiteDataTemplate
        {
            get => GetValue(SiteDataTemplateProperty) as DataTemplate;
            set => SetValue(SiteDataTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SiteDataTemplate"/> bindable property.
        /// </summary>
        public static readonly BindableProperty SiteDataTemplateProperty =
            BindableProperty.Create(nameof(SiteDataTemplate), typeof(DataTemplate), typeof(FloorFilter), null);
    }
}
