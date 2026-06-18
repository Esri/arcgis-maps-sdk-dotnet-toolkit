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

using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UtilityNetworks;
using System.Text;
using Esri.ArcGISRuntime.Data;

#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui;
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
    /// Supporting control for the <see cref="FeatureFormView"/> control,
    /// used for rendering a <see cref="UtilityAssociationResult"/>.
    /// </summary>
    public partial class UtilityAssociationResultDetailsView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityAssociationResultDetailsView"/> class.
        /// </summary>
        public UtilityAssociationResultDetailsView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
            this.ParentChanged += (s, e) => UpdateView();
#else
            DefaultStyleKey = typeof(UtilityAssociationResultDetailsView);
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
            if (GetTemplateChild("RemoveAssociationButton") is Button button)
            {
#if MAUI
                button.Clicked += (s, e) => RemoveAssociation();
#else
                button.Click += (s, e) => RemoveAssociation();
#endif
            }
            UpdateView();
        }

        private async void RemoveAssociation()
        {
            if (AssociationResult?.Association is null)
            {
                return;
            }

            var formview = FeatureFormView.GetFeatureFormViewParent(this);
            if (await UtilityAssociationResultView.RemoveAssociation(AssociationResult?.Association, formview))
                formview?.GoBackAsync();
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
            PropertyHelper.CreateProperty<UtilityAssociationResult, UtilityAssociationResultDetailsView>(nameof(AssociationResult), null, (s, oldValue, newValue) => s.OnAssociationResultPropertyChanged());

        private void OnAssociationResultPropertyChanged()
        {
            UpdateView();
        }

        private void UpdateView()
        {
            var title = GetTemplateChild("Title") as TextBlock;
            var ffv = FeatureFormView.GetFeatureFormViewParent(this);

            if (GetTemplateChild("RemoveAssociationButton") is Button button)
            {
                var form = ffv?.CurrentFeatureForm;
                var result = ffv?.GetNavigationStack().OfType<UtilityAssociationsFilterResult>().LastOrDefault();
                var a = form?.Elements.OfType<UtilityAssociationsFormElement>().Where(e => e.AssociationsFilterResults.Contains(result))?.FirstOrDefault();
#if MAUI
                button.IsVisible = a?.IsEditable == true;
#else
                button.Visibility = a?.IsEditable == true ? Visibility.Visible : Visibility.Collapsed;
#endif
            }

            // If association Type is (Connectivity or JunctionEdgeObjectConnectivityMidspan or JunctionEdgeObjectConnectivityFromSide or JunctionEdgeObjectConnectivityToSide) and
            // the terminal on the UN Element(feature) is not null, this terminal will also be displayed
            // If Association Type isJunctionEdgeObjectConnectivityMidspan or JunctionEdgeObjectConnectivityFromSide or JunctionEdgeObjectConnectivityToSide,
            // then we also want to display theFractionAlongEdgevalue of the association
            // Following the above two rules, if the element we are showing(other side of association) is
            // UtilityElement.UtilityNetworkSource.UtilityNetworkSourceType.Edgewe showFractionAlongEdge;
            // if it isUtilityElement.UtilityNetworkSource.UtilityNetworkSourceType.Junction we show the terminal.

            Guid? associatedFeatureGlobalId = null;
            if (AssociationResult is not null &&
                AssociationResult.AssociatedFeature is ArcGISFeature feature &&
                feature.FeatureTable is ArcGISFeatureTable table &&
                feature.GetAttributeValue(table.GlobalIdField) is Guid guid)
            {
                associatedFeatureGlobalId = guid;
            }

            double fromFractionAlongEdge = double.NaN;
            double toFractionAlongEdge = double.NaN;
            string? fromTitle = ffv?.CurrentFeatureForm?.Title;
            string? toTitle = AssociationResult?.AssociatedFeature is null ? "" : new FeatureForm(AssociationResult?.AssociatedFeature!)?.Title;
            string? fromTerminal = null;
            string? toTerminal = null;

            if (AssociationResult is not null)
            {
                // If association Type is (Connectivity or JunctionEdgeObjectConnectivityMidspan or JunctionEdgeObjectConnectivityFromSide or JunctionEdgeObjectConnectivityToSide) and the terminal on the UN Element(feature) is not null, this terminal will also be displayed
                if (AssociationResult.Association.AssociationType == UtilityAssociationType.Connectivity ||
                   AssociationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityMidspan ||
                   AssociationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityFromSide ||
                   AssociationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityToSide)
                {
                    fromTerminal = AssociationResult.Association.FromElement.Terminal?.Name;
                }

                if (AssociationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityMidspan)
                {
                    fromFractionAlongEdge = AssociationResult.Association.FractionAlongEdge;
                }

                if(AssociationResult.Association.ToElement.NetworkSource.SourceType == UtilityNetworkSourceType.Edge)
                {
                    toFractionAlongEdge = 1 - AssociationResult.Association.FractionAlongEdge;
                }
                else if (AssociationResult.Association.ToElement.NetworkSource.SourceType == UtilityNetworkSourceType.Junction)
                {
                    toTerminal = AssociationResult?.Association?.ToElement?.Terminal?.Name;
                }
            }

            if (GetTemplateChild("FromElementText") is TextBlock fromElementText)
            {
                fromElementText.Text = fromTitle;
            }

            if (GetTemplateChild("ToElementText") is TextBlock toElementText)
            {
                toElementText.Text = toTitle;
            }
            
            if (GetTemplateChild("FromTerminalText") is TextBlock fromTerminalText)
            {
                fromTerminalText.Text = fromTerminal;
#if MAUI
                fromTerminalText.IsVisible = !string.IsNullOrEmpty(fromTerminalText.Text);
#else
                fromTerminalText.Visibility = string.IsNullOrEmpty(fromTerminalText.Text) ? Visibility.Collapsed : Visibility.Visible;
#endif
            }

            if (GetTemplateChild("ToTerminalText") is TextBlock toTerminalText)
            {
                toTerminalText.Text = toTerminal;
#if MAUI
                toTerminalText.IsVisible = !string.IsNullOrEmpty(toTerminalText.Text);
#else
                toTerminalText.Visibility = string.IsNullOrEmpty(toTerminalText.Text) ? Visibility.Collapsed : Visibility.Visible;
#endif
            }

            if (GetTemplateChild("FromFraction") is Slider fromSlider)
            {
                bool showFromFraction = !double.IsNaN(fromFractionAlongEdge);
                fromSlider.Value = showFromFraction ? fromFractionAlongEdge : 0;
#if MAUI
                fromSlider.IsVisible = showFromFraction;
#else
                fromSlider.Visibility = showFromFraction ? Visibility.Visible : Visibility.Collapsed;
#endif
            }

            if (GetTemplateChild("ToFraction") is Slider toSlider)
            {
                bool showToFraction = !double.IsNaN(toFractionAlongEdge);
                toSlider.Value = showToFraction ? toFractionAlongEdge : 0;
#if MAUI
                toSlider.IsVisible = showToFraction;
#else
                toSlider.Visibility = showToFraction ? Visibility.Visible : Visibility.Collapsed;
#endif
            }
        }
    }
}