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

#if !__IOS__ && !__ANDROID__
#if NETFX_CORE
using Windows.UI.Xaml;
#else
using System.Windows;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class UtilityNetworkTraceTool
    {
        /// <summary>
        /// Gets or sets the label shown for the 'new trace' pane.
        /// </summary>
        public string? NewTraceLabel
        {
            get => GetValue(NewTraceLabelProperty) as string;
            set => SetValue(NewTraceLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="NewTraceLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty NewTraceLabelProperty =
            DependencyProperty.Register(nameof(NewTraceLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label shown for the utility network selection section.
        /// </summary>
        public string? UtilityNetworksSectionHeaderLabel
        {
            get => GetValue(UtilityNetworksSectionHeaderLabelProperty) as string;
            set => SetValue(UtilityNetworksSectionHeaderLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="UtilityNetworksSectionHeaderLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty UtilityNetworksSectionHeaderLabelProperty =
            DependencyProperty.Register(nameof(UtilityNetworksSectionHeaderLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label shown for the trace configuration selection section.
        /// </summary>
        public string? TraceConfigurationSectionHeaderLabel
        {
            get => GetValue(TraceConfigurationSectionHeaderLabelProperty) as string;
            set => SetValue(TraceConfigurationSectionHeaderLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TraceConfigurationSectionHeaderLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty TraceConfigurationSectionHeaderLabelProperty =
            DependencyProperty.Register(nameof(TraceConfigurationSectionHeaderLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label shown for the starting point selection section.
        /// </summary>
        public string? StartingPointSectionHeaderLabel
        {
            get => GetValue(StartingPointSectionHeaderLabelProperty) as string;
            set => SetValue(StartingPointSectionHeaderLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StartingPointSectionHeaderLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingPointSectionHeaderLabelProperty =
            DependencyProperty.Register(nameof(StartingPointSectionHeaderLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label shown for the advanced options section.
        /// </summary>
        public string? AdvancedOptionsSectionHeaderLabel
        {
            get => GetValue(AdvancedOptionsSectionHeaderLabelProperty) as string;
            set => SetValue(AdvancedOptionsSectionHeaderLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AdvancedOptionsSectionHeaderLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty AdvancedOptionsSectionHeaderLabelProperty =
            DependencyProperty.Register(nameof(AdvancedOptionsSectionHeaderLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label shown to indicate that the user should use the map to select a starting point.
        /// </summary>
        public string? AddStartingPointHelpTextLabel
        {
            get => GetValue(AddStartingPointHelpTextLabelProperty) as string;
            set => SetValue(AddStartingPointHelpTextLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AddStartingPointHelpTextLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty AddStartingPointHelpTextLabelProperty =
            DependencyProperty.Register(nameof(AddStartingPointHelpTextLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the text for the cancel button(s).
        /// </summary>
        public string? CancelButtonLabel
        {
            get => GetValue(CancelButtonLabelProperty) as string;
            set => SetValue(CancelButtonLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CancelButtonLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty CancelButtonLabelProperty =
            DependencyProperty.Register(nameof(CancelButtonLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the text for the 'run trace' button.
        /// </summary>
        public string? RunTraceButtonLabel
        {
            get => GetValue(RunTraceButtonLabelProperty) as string;
            set => SetValue(RunTraceButtonLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RunTraceButtonLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty RunTraceButtonLabelProperty =
            DependencyProperty.Register(nameof(RunTraceButtonLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the text for the label shown while the control is identifying starting points.
        /// </summary>
        public string? IdentifyingStartingPointsLabel
        {
            get => GetValue(IdentifyingStartingPointsLabelProperty) as string;
            set => SetValue(IdentifyingStartingPointsLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IdentifyingStartingPointsLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IdentifyingStartingPointsLabelProperty =
            DependencyProperty.Register(nameof(IdentifyingStartingPointsLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label to show for the results tab.
        /// </summary>
        public string? ResultsTabLabel
        {
            get => GetValue(ResultsTabLabelProperty) as string;
            set => SetValue(ResultsTabLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ResultsTabLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultsTabLabelProperty =
            DependencyProperty.Register(nameof(ResultsTabLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label to show while the control is loading.
        /// </summary>
        public string? LoadingLabel
        {
            get => GetValue(LoadingLabelProperty) as string;
            set => SetValue(LoadingLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="LoadingLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty LoadingLabelProperty =
            DependencyProperty.Register(nameof(LoadingLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the text for the 'discard' button for a result.
        /// </summary>
        public string? DiscardResultLabel
        {
            get => GetValue(DiscardResultLabelProperty) as string;
            set => SetValue(DiscardResultLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DiscardResultLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty DiscardResultLabelProperty =
            DependencyProperty.Register(nameof(DiscardResultLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the text for the 'clear all' button.
        /// </summary>
        public string? ClearAllLabel
        {
            get => GetValue(ClearAllLabelProperty) as string;
            set => SetValue(ClearAllLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ClearAllLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClearAllLabelProperty =
            DependencyProperty.Register(nameof(ClearAllLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label shown for the 'zoom to' button.
        /// </summary>
        public string? ZoomToLabel
        {
            get => GetValue(ZoomToLabelProperty) as string;
            set => SetValue(ZoomToLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ZoomToLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomToLabelProperty =
            DependencyProperty.Register(nameof(ZoomToLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label shown for the 'visualization options' result section.
        /// </summary>
        public string? VisualizationOptionsSectionHeaderLabel
        {
            get => GetValue(VisualizationOptionsSectionHeaderLabelProperty) as string;
            set => SetValue(VisualizationOptionsSectionHeaderLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="VisualizationOptionsSectionHeaderLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty VisualizationOptionsSectionHeaderLabelProperty =
            DependencyProperty.Register(nameof(VisualizationOptionsSectionHeaderLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label shown for the 'trace configuration details' result section.
        /// </summary>
        public string? TraceConfigurationDetailsSectionHeaderLabel
        {
            get => GetValue(TraceConfigurationDetailsSectionHeaderLabelProperty) as string;
            set => SetValue(TraceConfigurationDetailsSectionHeaderLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TraceConfigurationDetailsSectionHeaderLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty TraceConfigurationDetailsSectionHeaderLabelProperty =
            DependencyProperty.Register(nameof(TraceConfigurationDetailsSectionHeaderLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label for the 'feature results' results section.
        /// </summary>
        public string? FeatureResultsSectionHeaderLabel
        {
            get => GetValue(FeatureResultsSectionHeaderLabelProperty) as string;
            set => SetValue(FeatureResultsSectionHeaderLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FeatureResultsSectionHeaderLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty FeatureResultsSectionHeaderLabelProperty =
            DependencyProperty.Register(nameof(FeatureResultsSectionHeaderLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label for the 'function results' results section.
        /// </summary>
        public string? FunctionResultsSectionHeaderLabel
        {
            get => GetValue(FunctionResultsSectionHeaderLabelProperty) as string;
            set => SetValue(FunctionResultsSectionHeaderLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FunctionResultsSectionHeaderLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty FunctionResultsSectionHeaderLabelProperty =
            DependencyProperty.Register(nameof(FunctionResultsSectionHeaderLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the message shown when there are no utility networks.
        /// </summary>
        public string? NoNetworksFoundLabel
        {
            get => GetValue(NoNetworksFoundLabelProperty) as string;
            set => SetValue(NoNetworksFoundLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="NoNetworksFoundLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NoNetworksFoundLabelProperty =
            DependencyProperty.Register(nameof(NoNetworksFoundLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the warning label shown when there are more than the minimum required starting points.
        /// </summary>
        public string? ExcessStartingPointsWarningLabel
        {
            get => GetValue(ExcessStartingPointsWarningLabelProperty) as string;
            set => SetValue(ExcessStartingPointsWarningLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ExcessStartingPointsWarningLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExcessStartingPointsWarningLabelProperty =
            DependencyProperty.Register(nameof(ExcessStartingPointsWarningLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the warning label shown when the trace configuration duplicates an existing result.
        /// </summary>
        public string? DuplicateTraceWarningLabel
        {
            get => GetValue(DuplicateTraceWarningLabelProperty) as string;
            set => SetValue(DuplicateTraceWarningLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DuplicateTraceWarningLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DuplicateTraceWarningLabelProperty =
            DependencyProperty.Register(nameof(DuplicateTraceWarningLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label shown when there aren't enough starting points.
        /// </summary>
        public string? InsufficientStartingPointsWarningLabel
        {
            get => GetValue(InsufficientStartingPointsWarningLabelProperty) as string;
            set => SetValue(InsufficientStartingPointsWarningLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="InsufficientStartingPointsWarningLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InsufficientStartingPointsWarningLabelProperty =
            DependencyProperty.Register(nameof(InsufficientStartingPointsWarningLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the warning shown when there are no trace configurations available for the selected network.
        /// </summary>
        public string? NoTraceConfigurationsFoundLabel
        {
            get => GetValue(NoTraceConfigurationsFoundLabelProperty) as string;
            set => SetValue(NoTraceConfigurationsFoundLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="NoTraceConfigurationsFoundLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NoTraceConfigurationsFoundLabelProperty =
            DependencyProperty.Register(nameof(NoTraceConfigurationsFoundLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label used to identify the previewed symbology for the symbology color pickers.
        /// </summary>
        public string? PreviewLabel
        {
            get => GetValue(PreviewLabelProperty) as string;
            set => SetValue(PreviewLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PreviewLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PreviewLabelProperty =
            DependencyProperty.Register(nameof(PreviewLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label for the 'zoom automatically' setting checkbox.
        /// </summary>
        public string? ZoomToResultSettingLabel
        {
            get => GetValue(ZoomToResultSettingLabelProperty) as string;
            set => SetValue(ZoomToResultSettingLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ZoomToResultSettingLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomToResultSettingLabelProperty =
            DependencyProperty.Register(nameof(ZoomToResultSettingLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label shown for result color pickers.
        /// </summary>
        public string? ResultVisualizationColorLabel
        {
            get => GetValue(ResultVisualizationColorLabelProperty) as string;
            set => SetValue(ResultVisualizationColorLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref=" ResultVisualizationColorLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultVisualizationColorLabelProperty =
            DependencyProperty.Register(nameof(ResultVisualizationColorLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label shown for the result name setting.
        /// </summary>
        public string? ResultNameLabel
        {
            get => GetValue(ResultNameLabelProperty) as string;
            set => SetValue(ResultNameLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ResultNameLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultNameLabelProperty =
            DependencyProperty.Register(nameof(ResultNameLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the text for the 'add starting point' label.
        /// </summary>
        public string? AddStartingPointButtonLabel
        {
            get => GetValue(AddStartingPointButtonLabelProperty) as string;
            set => SetValue(AddStartingPointButtonLabelProperty, value);
        }

        /// <summary>
        /// Identifies teh <see cref="AddStartingPointButtonLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AddStartingPointButtonLabelProperty =
            DependencyProperty.Register(nameof(AddStartingPointButtonLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the text for the button for clearing starting points.
        /// </summary>
        public string? RemoveAllStartingPointsButtonLabel
        {
            get => GetValue(RemoveAllStartingPointsButtonLabelProperty) as string;
            set => SetValue(RemoveAllStartingPointsButtonLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RemoveAllStartingPointsButtonLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RemoveAllStartingPointsButtonLabelProperty =
            DependencyProperty.Register(nameof(RemoveAllStartingPointsButtonLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the section header for the warnings section of trace results.
        /// </summary>
        public string? ResultsWarningsSectionHeaderLabel
        {
            get => GetValue(ResultsWarningsSectionHeaderLabelProperty) as string;
            set => SetValue(ResultsWarningsSectionHeaderLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ResultsWarningsSectionHeaderLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultsWarningsSectionHeaderLabelProperty =
            DependencyProperty.Register(nameof(ResultsWarningsSectionHeaderLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label to show for the trace configuration description.
        /// </summary>
        public string? ResultDetailsSectionDescriptionLabel
        {
            get => GetValue(ResultDetailsSectionDescriptionLabelProperty) as string;
            set => SetValue(ResultDetailsSectionDescriptionLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ResultDetailsSectionDescriptionLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultDetailsSectionDescriptionLabelProperty =
            DependencyProperty.Register(nameof(ResultDetailsSectionDescriptionLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label to show for the trace configuration creator.
        /// </summary>
        public string? ResultDetailsSectionCreatorLabel
        {
            get => GetValue(ResultDetailsSectionCreatorLabelProperty) as string;
            set => SetValue(ResultDetailsSectionCreatorLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ResultDetailsSectionCreatorLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultDetailsSectionCreatorLabelProperty =
            DependencyProperty.Register(nameof(ResultDetailsSectionCreatorLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label to show for the trace configuration tags.
        /// </summary>
        public string? ResultDetailsSectionTagsLabel
        {
            get => GetValue(ResultDetailsSectionTagsLabelProperty) as string;
            set => SetValue(ResultDetailsSectionTagsLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ResultDetailsSectionTagsLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultDetailsSectionTagsLabelProperty =
            DependencyProperty.Register(nameof(ResultDetailsSectionTagsLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the text for the 'show graphics' checkbox for graphics results.
        /// </summary>
        public string? ResultsShowGraphicsCheckboxLabel
        {
            get => GetValue(ResultsShowGraphicsCheckboxLabelProperty) as string;
            set => SetValue(ResultsShowGraphicsCheckboxLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ResultsShowGraphicsCheckboxLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultsShowGraphicsCheckboxLabelProperty =
            DependencyProperty.Register(nameof(ResultsShowGraphicsCheckboxLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label to show for the 'select features' checkbox for feature results.
        /// </summary>
        public string? ResultsSelectFeaturesCheckboxLabel
        {
            get => GetValue(ResultsSelectFeaturesCheckboxLabelProperty) as string;
            set => SetValue(ResultsSelectFeaturesCheckboxLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ResultsSelectFeaturesCheckboxLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultsSelectFeaturesCheckboxLabelProperty =
            DependencyProperty.Register(nameof(ResultsSelectFeaturesCheckboxLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label to show when there are no results.
        /// </summary>
        public string? NoResultsFoundLabel
        {
            get => GetValue(NoResultsFoundLabelProperty) as string;
            set => SetValue(NoResultsFoundLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="NoResultsFoundLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty NoResultsFoundLabelProperty =
            DependencyProperty.Register(nameof(NoResultsFoundLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label to show in the terminal picker dropdown.
        /// </summary>
        public string? TerminalPickerLabel
        {
            get => GetValue(TerminalPickerLabelProperty) as string;
            set => SetValue(TerminalPickerLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TerminalPickerLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty TerminalPickerLabelProperty =
            DependencyProperty.Register(nameof(TerminalPickerLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label for the popup section of the starting point inspector.
        /// </summary>
        public string? InspectorPopupLabel
        {
            get => GetValue(InspectorPopupLabelProperty) as string;
            set => SetValue(InspectorPopupLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="InspectorPopupLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty InspectorPopupLabelProperty =
            DependencyProperty.Register(nameof(InspectorPopupLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);

        /// <summary>
        /// Gets or sets the label for the fraction along section of the starting point inspector.
        /// </summary>
        public string? InspectorFractionAlongLabel
        {
            get => GetValue(InspectorFractionAlongLabelProperty) as string;
            set => SetValue(InspectorFractionAlongLabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="InspectorFractionAlongLabel" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty InspectorFractionAlongLabelProperty =
            DependencyProperty.Register(nameof(InspectorFractionAlongLabel), typeof(string), typeof(UtilityNetworkTraceTool), null);
    }
}
#endif