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

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.Popups;
using Microsoft.Win32;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a <see cref="RelationshipPopupElement"/>.
    /// </summary>
    public class RelationshipPopupElementView : Control
    {
        private ListBox? itemsList;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelationshipPopupElementView"/> class.
        /// </summary>
        public RelationshipPopupElementView()
        {
            DefaultStyleKey = typeof(RelationshipPopupElementView);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            itemsList = GetTemplateChild("RelatedFeaturesList") as ListView;
            LoadRelationship();
        }

        /// <summary>
        /// Gets or sets the RelationshipPopupElement.
        /// </summary>
        public RelationshipPopupElement? Element
        {
            get { return GetValue(ElementProperty) as RelationshipPopupElement; }
            set { SetValue(ElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(RelationshipPopupElement), typeof(RelationshipPopupElementView), new PropertyMetadata(null, (s, e) => ((RelationshipPopupElementView)s).LoadRelationship()));

        private async void LoadRelationship()
        {
            if (itemsList is not null && Element is not null && GeoElement is ArcGISFeature feature && feature.FeatureTable is ArcGISFeatureTable table)
            {
                try
                {

                    var relationShip = table.LayerInfo.RelationshipInfos.Where(i => i.Id == Element.RelationshipId).FirstOrDefault();
                    if (relationShip != null)
                    {
                        var parameters = new RelatedQueryParameters(relationShip);
                        foreach (var f in Element.OrderByFields)
                            parameters.OrderByFields.Add(f);
                        if (Element.DisplayCount > 0)
                            parameters.MaxFeatures = Element.DisplayCount;
                        var relatedRecords = await table.QueryRelatedFeaturesAsync(feature, parameters);
                        //List<string> records = new List<string>();
                        //foreach (var result in relatedRecords)
                        //    records.Add(result.Feature.Attributes[((ArcGISFeatureTable)result.Feature.FeatureTable).LayerInfo.DisplayFieldName]?.ToString());
                        itemsList.ItemsSource = relatedRecords;
                        //TODO: Show related records correctly
                    }
                }
                catch { }

            }
        }

        /// <summary>
        /// Gets or sets the GeoElement who's attribute will be showed with the <see cref="FieldsPopupElement"/>.
        /// </summary>
        public GeoElement? GeoElement
        {
            get { return GetValue(GeoElementProperty) as GeoElement; }
            set { SetValue(GeoElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoElement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoElementProperty =
            DependencyProperty.Register(nameof(GeoElement), typeof(GeoElement), typeof(RelationshipPopupElementView), new PropertyMetadata(null, (s, e) => ((RelationshipPopupElementView)s).LoadRelationship()));
    }
}