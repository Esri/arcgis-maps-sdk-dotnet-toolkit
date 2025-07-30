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

using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UtilityNetworks;
using Esri.ArcGISRuntime.Data;

#if MAUI
using TextBlock = Microsoft.Maui.Controls.Label;
#else
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Supporting control for the <see cref="PopupViewer"/> control,
    /// used for rendering a <see cref="UtilityAssociationResult"/>.
    /// </summary>
    public partial class UtilityAssociationResultPopupView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityAssociationResultPopupView"/> class.
        /// </summary>
        public UtilityAssociationResultPopupView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(UtilityAssociationResultPopupView);
#endif
        }

        /// <inheritdoc/>
#if WINDOWS_XAML || MAUI
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            UpdateView();
        }

        /// <summary>
        /// Gets or sets the AssociationResult.
        /// </summary>
        public UtilityAssociationResult? AssociationResult
        {
            get => GetValue(AssociationResultProperty) as UtilityAssociationResult;
            set => SetValue(AssociationResultProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AssociationResult"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AssociationResultProperty =
            PropertyHelper.CreateProperty<UtilityAssociationResult, UtilityAssociationResultPopupView>(nameof(AssociationResult), null, (s, oldValue, newValue) => s.OnAssociationResultPropertyChanged());

        private void OnAssociationResultPropertyChanged()
        {
            UpdateView();
        }

        private void UpdateView()
        {
            var title = GetTemplateChild("Title") as TextBlock;
#if WINDOWS_XAML
            if (GetTemplateChild("Icon") is FontIcon icon)
            {
                icon.Glyph = GetIconGlyph();
            }
#elif WPF
            if (GetTemplateChild("Icon") is TextBlock icon)
            {
                icon.Text = GetIconGlyph();
            }
#elif MAUI
            if (GetTemplateChild("Icon") is Image icon)
            {
                var glyph = GetIconGlyph();
                icon.Source = glyph is null ? null : new FontImageSource
                {
                    Glyph = glyph,
                    Color = Colors.Gray,
                    FontFamily = ToolkitIcons.FontFamilyName,
                    Size = 18
                };
            }
#endif

            if (GetTemplateChild("FractionAlong") is TextBlock fractionAlong)
            {
                fractionAlong.Text = AssociationResult != null &&
                AssociationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityMidspan &&
                AssociationResult.Association.FractionAlongEdge > 0
                    ? $"{Math.Round(AssociationResult.Association.FractionAlongEdge * 100)} %"
                    : string.Empty; ;
#if MAUI
                fractionAlong.IsVisible = fractionAlong.Text?.Length > 0;
#else
                fractionAlong.Visibility = fractionAlong.Text?.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
#endif
            }

            if (GetTemplateChild("ConnectionInfo") is TextBlock connectionInfo)
            {
                connectionInfo.Text = GetAssociationProperty(AssociationResult);
#if MAUI
                connectionInfo.IsVisible = connectionInfo.Text?.Length > 0;
#else
                connectionInfo.Visibility = connectionInfo.Text?.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
#endif
            }
        }

        private string GetAssociationProperty(UtilityAssociationResult? associationResult)
        {
            if (associationResult is null ||
                associationResult.AssociatedFeature is not ArcGISFeature feature ||
                feature.FeatureTable is not ArcGISFeatureTable table ||
                feature.GetAttributeValue(table.GlobalIdField) is not Guid associatedFeatureGlobalId)
            {
                return string.Empty;
            }

            if (associationResult.Association.AssociationType == UtilityAssociationType.Containment &&
                associationResult.Association.ToElement.GlobalId.Equals(associatedFeatureGlobalId))
            {
                var contentVisibility = associationResult.Association.IsContainmentVisible ?
                    Properties.Resources.GetString("PopupViewerUtilityAssociationsVisibleContent") :
                    Properties.Resources.GetString("PopupViewerUtilityAssociationsNonVisibleContent");
                return contentVisibility ?? string.Empty;
            }

            if (associationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityFromSide ||
                associationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityMidspan ||
                associationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityToSide ||
                associationResult.Association.AssociationType == UtilityAssociationType.Connectivity)
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

            return string.Empty;
        }

        private string GetIconGlyph()
        {
            if (AssociationResult?.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityFromSide)
                return ToolkitIcons.ConnectionEndLeft;

            if (AssociationResult?.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityToSide)
                return ToolkitIcons.ConnectionEndRight;

            if (AssociationResult?.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityMidspan)
                return ToolkitIcons.ConnectionMiddle;

            if (AssociationResult?.Association.AssociationType == UtilityAssociationType.Connectivity)
                return ToolkitIcons.ConnectionToConnection;

            return string.Empty;
        }
    }
}