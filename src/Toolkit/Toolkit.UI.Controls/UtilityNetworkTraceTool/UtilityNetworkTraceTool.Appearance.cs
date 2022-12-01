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
using System;
using System.Linq;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UtilityNetworks;
#if NETFX_CORE
using ToggleButton = Windows.UI.Xaml.Controls.ToggleSwitch;
#elif WINUI
using ToggleButton = Microsoft.UI.Xaml.Controls.ToggleSwitch;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [TemplatePart(Name = "PART_IneligibleScrim", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_LoadingScrim", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_UtilityNetworkSelectorContainer", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_UtilityNetworksSelector", Type = typeof(Selector))]
    [TemplatePart(Name = "PART_TraceConfigurationsContainer", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_TraceConfigurationsSelector", Type = typeof(Selector))]
    [TemplatePart(Name = "PART_StartingPointsSectionContainer", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_StartingPointsList", Type = typeof(ListView))]
    [TemplatePart(Name = "PART_IsAddingStartingPointsIndicator", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_AddRemoveStartingPointButtonsContainer", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_ResetStartingPointsButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_AddStartingPointsButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_CancelAddStartingPointsButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_AdvancedOptionsSectionContainer", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_ResultNameTextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_ResultColorPalette", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_NoNamedTraceConfigurationsWarningContainer", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_NeedMoreStartingPointsWarningContainer", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_DuplicateTraceWarningContainer", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_ExtraStartingPointsWarningContainer", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_TraceInProgressIndicator", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_RunTraceButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_CancelTraceButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_IdentifyInProgressIndicator", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_CancelIdentifyButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_ResultsTabItem", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_ResultsItemControl", Type = typeof(ItemsControl))]
    [TemplatePart(Name = "PART_DeleteAllResultsButton", Type = typeof(ButtonBase))]
#if !WINDOWS_XAML
    [TemplatePart(Name = "PART_TabsControl", Type = typeof(TabControl))]
#else
    [TemplatePart(Name = "PART_TabsControl", Type = typeof(Pivot))]
#endif
    public partial class UtilityNetworkTraceTool
    {
        private UIElement? _loadingScrim;
        private UIElement? _ineligibleScrim;
        private UIElement? _part_utilityNetworkSelectorContainer;
        private Selector? _part_utilityNetworkSelector;
        private UIElement? _part_traceConfigurationsContainer;
        private Selector? _part_traceConfigurationsSelector;
        private UIElement? _part_startingPointsSectionContainer;
        private ListView? _part_startingPointsList;
        private UIElement? _part_isAddingStartingPointsIndicator;
        private UIElement? _part_addRemoveStartingPointsButtonContainer;
        private ButtonBase? _part_resetStartingPointsButton;
        private ButtonBase? _part_addStartingPointsButton;
        private ButtonBase? _part_cancelAddStartingPointsButton;
        private UIElement? _part_advancedOptionsSectionContainer;
        private TextBox? _part_resultNamedTextBox;
        private ToolkitColorPalette? _part_resultColorPalette;
        private UIElement? _part_noNamedTraceConfigurationsWarningContainer;
        private UIElement? _part_needMoreStartingPointsWarningContainer;
        private UIElement? _part_duplicateTraceWarningContainer;
        private UIElement? _part_extraStartingPointsWarningContainer;
        private UIElement? _part_traceInProgressIndicator;
        private ButtonBase? _part_runTraceButton;
        private ButtonBase? _part_cancelTraceButton;
        private UIElement? _part_identifyInProgressIndicator;
        private ButtonBase? _part_cancelIdentifyButton;
        private UIElement? _part_resultsTabItem;
        private ItemsControl? _part_resultsItemControl;
        private ButtonBase? _part_deleteAllResultsButton;
#if !WINDOWS_XAML
        private TabControl? _tabControl;
#else
        private Pivot? _pivotControl;
#endif

        /// <inheritdoc />
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            if (GetTemplateChild("PART_UtilityNetworkSelectorContainer") is UIElement unSelectorContainer)
            {
                _part_utilityNetworkSelectorContainer = unSelectorContainer;
                _part_utilityNetworkSelectorContainer.Visibility = _controller.UtilityNetworks.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (GetTemplateChild("PART_UtilityNetworkSelector") is Selector unSelector)
            {
                _part_utilityNetworkSelector = unSelector;
                _part_utilityNetworkSelector.ItemsSource = _controller.UtilityNetworks;
                _part_utilityNetworkSelector.SelectedItem = _controller.SelectedUtilityNetwork;
                _part_utilityNetworkSelector.SelectionChanged += Part_utilityNetworkSelector_SelectionChanged;
            }

            if (GetTemplateChild("PART_TraceConfigurationsContainer") is UIElement traceConfigContainer)
            {
                _part_traceConfigurationsContainer = traceConfigContainer;
                _part_traceConfigurationsContainer.Visibility = _controller.TraceTypes.Any() ? Visibility.Visible : Visibility.Collapsed;
            }

            if (GetTemplateChild("PART_TraceConfigurationsSelector") is Selector traceConfigSelector)
            {
                _part_traceConfigurationsSelector = traceConfigSelector;
                _part_traceConfigurationsSelector.ItemsSource = _controller.TraceTypes;
                _part_traceConfigurationsSelector.SelectedItem = _controller.SelectedTraceType;
                _part_traceConfigurationsSelector.SelectionChanged += Part_traceConfigurationsSelector_SelectionChanged;
            }

            if (GetTemplateChild("PART_StartingPointsSectionContainer") is UIElement startingPointsSectionContainer)
            {
                _part_startingPointsSectionContainer = startingPointsSectionContainer;
                _part_startingPointsSectionContainer.Visibility = _controller.SelectedTraceType == null ? Visibility.Collapsed : Visibility.Visible;
            }

            if (GetTemplateChild("PART_StartingPointsList") is ListView startingPointsList)
            {
                _part_startingPointsList = startingPointsList;
                _part_startingPointsList.ItemsSource = _controller.StartingPoints;
                _part_startingPointsList.SelectedItem = _controller.SelectedStartingPoint;
                _part_startingPointsList.Visibility = _controller.IsAddingStartingPoints ? Visibility.Collapsed : Visibility.Visible;
                _part_startingPointsList.SelectionChanged += Part_startingPointsList_SelectionChanged;
                if (_part_startingPointsList is StartingPointListView specialized)
                {
                    specialized.ZoomToCommand = new DelegateCommand((parameter) => HandleZoomToStartingPointCommand(parameter));
                }
            }

            if (GetTemplateChild("PART_IsAddingStartingPointsIndicator") is UIElement addingStartingPointsIndicator)
            {
                _part_isAddingStartingPointsIndicator = addingStartingPointsIndicator;
                _part_isAddingStartingPointsIndicator.Visibility = _controller.IsAddingStartingPoints ? Visibility.Visible : Visibility.Collapsed;
            }

            if (GetTemplateChild("PART_AddRemoveStartingPointButtonsContainer") is UIElement addremoveStartingPointButtonsContainer)
            {
                _part_addRemoveStartingPointsButtonContainer = addremoveStartingPointButtonsContainer;
                _part_addRemoveStartingPointsButtonContainer.Visibility = _controller.IsAddingStartingPoints ? Visibility.Collapsed : Visibility.Visible;
            }

            if (GetTemplateChild("PART_ResetStartingPointsButton") is ButtonBase resetStartingPointsButton)
            {
                _part_resetStartingPointsButton = resetStartingPointsButton;
                _part_resetStartingPointsButton.Visibility = _controller.StartingPoints.Any() ? Visibility.Visible : Visibility.Collapsed;
                _part_resetStartingPointsButton.Click += Part_resetStartingPointsButton_Click;
            }

            if (GetTemplateChild("PART_AddStartingPointsButton") is ButtonBase addStartingPointsButton)
            {
                _part_addStartingPointsButton = addStartingPointsButton;
                _part_addStartingPointsButton.Click += Part_addStartingPointsButton_Click;
            }

            if (GetTemplateChild("PART_CancelAddStartingPointsButton") is ButtonBase cancelAddStartingPointsButton)
            {
                _part_cancelAddStartingPointsButton = cancelAddStartingPointsButton;
                _part_cancelAddStartingPointsButton.Visibility = _controller.IsAddingStartingPoints ? Visibility.Visible : Visibility.Collapsed;
                _part_cancelAddStartingPointsButton.Click += Part_cancelAddStartingPointsButton_Click;
            }

            if (GetTemplateChild("PART_AdvancedOptionsSectionContainer") is UIElement advancedOptionsSectionContainer)
            {
                _part_advancedOptionsSectionContainer = advancedOptionsSectionContainer;
                _part_advancedOptionsSectionContainer.Visibility = _controller.SelectedTraceType == null ? Visibility.Collapsed : Visibility.Visible;
            }

            if (GetTemplateChild("PART_ResultNameTextBox") is TextBox resultNameTextBox)
            {
                _part_resultNamedTextBox = resultNameTextBox;
                _part_resultNamedTextBox.Text = _controller.TraceName ?? "";
                _part_resultNamedTextBox.TextChanged += Part_resultNamedTextBox_TextChanged;
            }

            if (GetTemplateChild("PART_ResultColorPalette") is ToolkitColorPalette palette)
            {
                _part_resultColorPalette = palette;
                _part_resultColorPalette.SelectedColor = _controller.ResultColor;
                _part_resultColorPalette.SelectionChanged += Part_resultColorPalette_SelectionChanged;
            }

            if (GetTemplateChild("PART_NoNamedTraceConfigurationsWarningContainer") is UIElement noConfigsWarning)
            {
                _part_noNamedTraceConfigurationsWarningContainer = noConfigsWarning;
                _part_noNamedTraceConfigurationsWarningContainer.Visibility = _controller.TraceTypes.Any() ? Visibility.Collapsed : Visibility.Visible;
            }

            if (GetTemplateChild("PART_NeedMoreStartingPointsWarningContainer") is UIElement needMorePointsWarning)
            {
                _part_needMoreStartingPointsWarningContainer = needMorePointsWarning;
                _part_needMoreStartingPointsWarningContainer.Visibility = _controller.InsufficientStartingPointsWarning ? Visibility.Visible : Visibility.Collapsed;
            }

            if (GetTemplateChild("PART_DuplicateTraceWarningContainer") is UIElement duplicateTraceWarning)
            {
                _part_duplicateTraceWarningContainer = duplicateTraceWarning;
                _part_duplicateTraceWarningContainer.Visibility = _controller.DuplicatedTraceWarning ? Visibility.Visible : Visibility.Collapsed;
            }

            if (GetTemplateChild("PART_ExtraStartingPointsWarningContainer") is UIElement extraPointsWarning)
            {
                _part_extraStartingPointsWarningContainer = extraPointsWarning;
                _part_extraStartingPointsWarningContainer.Visibility = _controller.TooManyStartingPointsWarning ? Visibility.Visible : Visibility.Collapsed;
            }

            if (GetTemplateChild("PART_TraceInProgressIndicator") is UIElement traceInProgressIndicator)
            {
                _part_traceInProgressIndicator = traceInProgressIndicator;
                _part_traceInProgressIndicator.Visibility = _controller.IsRunningTrace ? Visibility.Visible : Visibility.Collapsed;
            }

            if (GetTemplateChild("PART_RunTraceButton") is ButtonBase runTraceButton)
            {
                _part_runTraceButton = runTraceButton;
                _part_runTraceButton.IsEnabled = _controller.EnableTrace;
                _part_runTraceButton.Click += Part_runTraceButton_Click;
            }

            if (GetTemplateChild("PART_CancelTraceButton") is ButtonBase cancelTraceButton)
            {
                _part_cancelTraceButton = cancelTraceButton;
                _part_cancelTraceButton.Click += Part_cancelTraceButton_Click;
            }

            if (GetTemplateChild("PART_IdentifyInProgressIndicator") is UIElement identifyInProgressIndicator)
            {
                _part_identifyInProgressIndicator = identifyInProgressIndicator;
                _part_identifyInProgressIndicator.Visibility = Visibility.Collapsed;
            }

            if (GetTemplateChild("PART_CancelIdentifyButton") is ButtonBase cancelIdentifyButton)
            {
                _part_cancelIdentifyButton = cancelIdentifyButton;
            }

            if (GetTemplateChild("PART_ResultsTabItem") is UIElement resultsSection)
            {
                _part_resultsTabItem = resultsSection;
                _part_resultsTabItem.Visibility = _controller.Results.Any() ? Visibility.Visible : Visibility.Collapsed;
            }

            if (GetTemplateChild("PART_ResultsItemControl") is ItemsControl resultsViewer)
            {
                _part_resultsItemControl = resultsViewer;
                #if !WPF
                _part_resultsItemControl.ItemsSource = _controller.Results;
                #endif
            }

            if (GetTemplateChild("PART_DeleteAllResultsButton") is ButtonBase deleteAllResultsButton)
            {
                _part_deleteAllResultsButton = deleteAllResultsButton;
                _part_deleteAllResultsButton.Click += Part_deleteAllResultsButton_Click;
            }

#if !WINDOWS_XAML
            if (GetTemplateChild("PART_TabsControl") is TabControl tabcontrol)
            {
                _tabControl = tabcontrol;
            }
#else
            if (GetTemplateChild("PART_TabsControl") is Pivot tabcontrol)
            {
                _pivotControl = tabcontrol;
            }
#endif

            if (GetTemplateChild("PART_IneligibleScrim") is UIElement ineligibleScrim)
            {
                _ineligibleScrim = ineligibleScrim;
                _ineligibleScrim.Visibility = (!_controller.IsReadyToConfigure && !_controller.IsLoadingNetwork) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (GetTemplateChild("PART_LoadingScrim") is UIElement loadingScrim)
            {
                _loadingScrim = loadingScrim;
                _loadingScrim.Visibility = _controller.IsLoadingNetwork ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void Part_resultNamedTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_part_resultNamedTextBox?.Text is string newName)
            {
                _controller.TraceName = newName;
            }
        }

        private void Part_resultColorPalette_SelectionChanged(object? sender, EventArgs e)
        {
            if (sender is ToolkitColorPalette palette)
            {
                _controller.ResultColor = palette.SelectedColor;
            }
        }

        private void Part_resetStartingPointsButton_Click(object? sender, RoutedEventArgs e)
        {
            _controller.StartingPoints.Clear();
        }

        private void Part_deleteAllResultsButton_Click(object? sender, RoutedEventArgs e)
        {
            foreach (var result in _controller.Results)
            {
                result.AreFeaturesSelected = false;
            }

            _controller.Results.Clear();
        }

        private void Part_cancelTraceButton_Click(object? sender, RoutedEventArgs e)
        {
            _identifyLayersCts?.Cancel();
            _controller._getFeaturesForElementsCts?.Cancel();
            _controller._traceCts?.Cancel();
        }

        private async void Part_runTraceButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                await _controller.TraceAsync();
#if !WINDOWS_XAML
                _tabControl?.SetValue(TabControl.SelectedIndexProperty, 1);
#else
                _pivotControl?.SetValue(Pivot.SelectedIndexProperty, 1);
#endif
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        private void Part_cancelAddStartingPointsButton_Click(object? sender, RoutedEventArgs e)
        {
            _controller.IsAddingStartingPoints = false;
        }

        private void Part_addStartingPointsButton_Click(object? sender, RoutedEventArgs e)
        {
            _controller.IsAddingStartingPoints = true;
        }

        private void Part_startingPointsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            _controller.SelectedStartingPoint = (sender as Selector)?.SelectedItem as StartingPointModel;
        }

        private void Part_traceConfigurationsSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            _controller.SelectedTraceType = (sender as Selector)?.SelectedItem as UtilityNamedTraceConfiguration;
        }

        private void Part_utilityNetworkSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            _controller.SelectedUtilityNetwork = (sender as Selector)?.SelectedItem as UtilityNetwork;
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to display a <see cref="UtilityNetwork"/>.
        /// </summary>
        /// <value>A <see cref="DataTemplate"/> for a <see cref="UtilityNetwork"/> item.</value>
        public DataTemplate? UtilityNetworkItemTemplate
        {
            get => GetValue(UtilityNetworkItemTemplateProperty) as DataTemplate;
            set => SetValue(UtilityNetworkItemTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="UtilityNetworkItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UtilityNetworkItemTemplateProperty =
            DependencyProperty.Register(nameof(UtilityNetworkItemTemplate), typeof(DataTemplate),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to display a pre-configured trace type, which is
        /// a <see cref="UtilityNamedTraceConfiguration"/> item.
        /// </summary>
        /// <value>A <see cref="DataTemplate"/> for a <see cref="UtilityNamedTraceConfiguration"/> item.</value>
        public DataTemplate? TraceTypeItemTemplate
        {
            get => GetValue(TraceTypeItemTemplateProperty) as DataTemplate;
            set => SetValue(TraceTypeItemTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TraceTypeItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TraceTypeItemTemplateProperty =
            DependencyProperty.Register(nameof(TraceTypeItemTemplate), typeof(DataTemplate),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to display a starting point, which is
        /// a <see cref="UtilityElement"/> item.
        /// </summary>
        /// <value>
        /// A <see cref="DataTemplate"/> for a <see cref="UtilityElement"/> item.
        /// </value>
        public DataTemplate? StartingPointItemTemplate
        {
            get => GetValue(StartingPointItemTemplateProperty) as DataTemplate;
            set => SetValue(StartingPointItemTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StartingPointItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingPointItemTemplateProperty =
            DependencyProperty.Register(nameof(StartingPointItemTemplate), typeof(DataTemplate),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to display a trace result.
        /// </summary>
        public DataTemplate? ResultItemTemplate
        {
            get => GetValue(ResultItemTemplateProperty) as DataTemplate;
            set => SetValue(ResultItemTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ResultItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultItemTemplateProperty =
            DependencyProperty.Register(nameof(ResultItemTemplate), typeof(DataTemplate), typeof(UtilityNetworkTraceTool), new PropertyMetadata(null));
    }
}
#endif
