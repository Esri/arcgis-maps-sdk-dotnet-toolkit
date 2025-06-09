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
using Esri.ArcGISRuntime.UtilityNetworks;
using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal
{
    internal class StringConverter : IValueConverter
    {
        public static StringConverter Instance { get; } = new StringConverter();
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not UtilityAssociationResult associationResult ||
                associationResult.AssociatedFeature is not ArcGISFeature feature ||
                feature.FeatureTable is not ArcGISFeatureTable table ||
                feature.GetAttributeValue(table.GlobalIdField) is not Guid associatedFeatureGlobalId)
            {
                return string.Empty;
            }

            if (associationResult.Association.AssociationType == UtilityAssociationType.Containment &&
                associationResult.Association.ToElement.GlobalId.Equals(associatedFeatureGlobalId))
            {
                return associationResult.Association.IsContainmentVisible ? "Visible content" : "Content";
            }

            if (associationResult.Association.AssociationType == UtilityAssociationType.Connectivity)
            {
                if (associationResult.Association.FromElement.GlobalId.Equals(associatedFeatureGlobalId) &&
                    associationResult.Association.FromElement.Terminal is UtilityTerminal fromTerminal)
                {
                    return fromTerminal.Name;
                }

                if (associationResult.Association.ToElement.GlobalId.Equals(associatedFeatureGlobalId) &&
                    associationResult.Association.ToElement.Terminal is UtilityTerminal toTerminal)
                {
                    return toTerminal.Name;
                }
            }

            if ((associationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityFromSide ||
                 associationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityMidspan ||
                 associationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityToSide) &&
                 associationResult.Association.FromElement.GlobalId.Equals(associatedFeatureGlobalId) &&
                 associationResult.Association.FromElement.Terminal is UtilityTerminal junctionTerminal)
            {
                return junctionTerminal.Name;
            }

            if (associationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityMidspan &&
                associationResult.Association.ToElement.GlobalId.Equals(associatedFeatureGlobalId) &&
                associationResult.Association.FractionAlongEdge > 0)
            {
                return $"{associationResult.Association.FractionAlongEdge * 100.0} %";
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
