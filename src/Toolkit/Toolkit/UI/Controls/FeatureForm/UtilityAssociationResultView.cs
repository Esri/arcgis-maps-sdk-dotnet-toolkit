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
    public partial class UtilityAssociationResultView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityAssociationResultView"/> class.
        /// </summary>
        public UtilityAssociationResultView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(UtilityAssociationResultView);
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
            PropertyHelper.CreateProperty<UtilityAssociationResult, UtilityAssociationResultView>(nameof(AssociationResult), null, (s, oldValue, newValue) => s.OnAssociationResultPropertyChanged());

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
                    FontFamily = "toolkit-icons",
                    Size = 18
                };
            }
#endif
            // One of the elements is this associated feature. We want to display info about the other element
            // Use the guids to figure out whether this is the To or From element, and select the other one to
            // display information on.
            var guidField = (AssociationResult?.AssociatedFeature.FeatureTable as Data.ArcGISFeatureTable)?.GlobalIdField;
            Guid? guid = null;
            if (guidField is not null && AssociationResult?.AssociatedFeature.Attributes.ContainsKey(guidField) == true)
            {
                guid = (Guid?)AssociationResult?.AssociatedFeature.Attributes[guidField];
            }
            UtilityElement? otherElement = null;
            if (AssociationResult?.Association.FromElement.GlobalId == guid)
                otherElement = AssociationResult?.Association.ToElement;
            if (AssociationResult?.Association.ToElement.GlobalId == guid)
                otherElement = AssociationResult?.Association.FromElement;

            if (GetTemplateChild("ConnectionInfo") is TextBlock connectionInfo)
            {
                StringBuilder sb = new StringBuilder();
                bool showFraction = AssociationResult?.Association.AssociationType == UtilityAssociationType.Connectivity;
                if (showFraction)
                {
                    var fraction = AssociationResult?.Association.FractionAlongEdge ?? 0;
                    if (fraction == 0)
                        fraction = otherElement?.FractionAlongEdge ?? 0;
                    if (sb.Length > 0)
                        sb.Append(" ");
                    sb.Append(string.Format(Properties.Resources.GetString("FeatureFormUtilityElementFractionAlongEdge")!, fraction.ToString("P0")));
                }

                string? terminalName = otherElement?.Terminal?.Name;
                if (!string.IsNullOrEmpty(terminalName))
                {
                    if (sb.Length > 0)
                        sb.Append(" ");
                    sb.Append(string.IsNullOrEmpty(terminalName) ? "" : string.Format(Properties.Resources.GetString("FeatureFormUtilityElementTerminalName")!, terminalName));
                }

                bool showIscontentVisible = AssociationResult?.Association.AssociationType == UtilityAssociationType.Containment && otherElement == AssociationResult?.Association.ToElement;
                if (showIscontentVisible)
                {
                    if (sb.Length > 0)
                        sb.Append(" ");
                    sb.Append(AssociationResult?.Association.IsContainmentVisible == true ? Properties.Resources.GetString("FeatureFormUtilityElementIsContentVisible") : Properties.Resources.GetString("FeatureFormUtilityElementIsContentNotVisible"));
                }
                connectionInfo.Text = sb.ToString().Trim();
#if MAUI
                connectionInfo.IsVisible = connectionInfo.Text.Length > 0;
#else
                connectionInfo.Visibility = connectionInfo.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
#endif
            }
        }

        private string GetIconGlyph()
        {
            if (AssociationResult is not null)
            {
                if (AssociationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityFromSide)
                    return ((char)0xE7D7).ToString();
                if (AssociationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityToSide)
                    return ((char)0xE7D8).ToString();
                if (AssociationResult.Association.AssociationType == UtilityAssociationType.JunctionEdgeObjectConnectivityMidspan)
                    return ((char)0xE7D9).ToString();
            }
            return "";
        }
    }
}