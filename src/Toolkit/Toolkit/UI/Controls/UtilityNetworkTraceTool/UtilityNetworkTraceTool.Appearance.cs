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
using Esri.ArcGISRuntime.UtilityNetworks;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ToggleButton = Windows.UI.Xaml.Controls.ToggleSwitch;
#else
using System.Windows;
using System.Windows.Controls;
using ToggleButton = System.Windows.Controls.Primitives.ToggleButton;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [TemplatePart(Name = "PART_UtilityNetworksPicker", Type = typeof(ComboBox))]
    [TemplatePart(Name = "PART_TraceTypesPicker", Type = typeof(ComboBox))]
    [TemplatePart(Name = "PART_IsAddStartingPointToggle", Type = typeof(ToggleButton))]
    [TemplatePart(Name = "PART_ResetButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_TraceButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_BusyIndicator", Type = typeof(ProgressBar))]
    [TemplatePart(Name = "PART_StatusLabel", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_StartingPointsList", Type = typeof(ListView))]
    [TemplatePart(Name = "PART_FunctionResultsList", Type = typeof(ItemsControl))]
    public partial class UtilityNetworkTraceTool
    {
        private ComboBox? _utilityNetworksPicker;
        private ComboBox? _traceTypesPicker;
        private ToggleButton? _addStartingPointToggle;
        private Button? _traceButton;
        private ProgressBar? _busyIndicator;
        private TextBlock? _statusLabel;
        private ListView? _startingPointsList;
        private ItemsControl? _functionResultsList;

        /// <inheritdoc />
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_UtilityNetworksPicker") is ComboBox utilityNetworkPicker)
            {
                _utilityNetworksPicker = utilityNetworkPicker;
                _utilityNetworksPicker.ItemsSource = _controller.UtilityNetworks;
                _utilityNetworksPicker.SelectionChanged += (s, e) =>
                {
                    _controller.UpdateSelectedUtilityNetwork(((ComboBox)s).SelectedItem as UtilityNetwork);
                };
            }

            if (GetTemplateChild("PART_TraceTypesPicker") is ComboBox traceTypesPicker)
            {
                _traceTypesPicker = traceTypesPicker;
                _traceTypesPicker.ItemsSource = _controller.TraceTypes;
                _traceTypesPicker.SelectionChanged += (s, e) =>
                {
                    _controller.UpdateSelectedTraceType(((ComboBox)s).SelectedItem as UtilityNamedTraceConfiguration);
                };
            }

            if (GetTemplateChild("PART_AddStartingPointToggle") is ToggleButton addStartingPointToggle)
            {
                _addStartingPointToggle = addStartingPointToggle;
#if NETFX_CORE
                _addStartingPointToggle.IsOn = IsAddingStartingPoints;
                _addStartingPointToggle.Toggled += (s, e) =>
                {
                    _controller.IsAddingStartingPoints = ((ToggleButton)s).IsOn;
                };
#else
                _addStartingPointToggle.IsChecked = IsAddingStartingPoints;
                _addStartingPointToggle.Click += (s, e) =>
                {
                    var isToggleChecked = ((ToggleButton)s).IsChecked == true;
                    _controller.IsAddingStartingPoints = isToggleChecked;
                };
#endif
            }

            if (GetTemplateChild("PART_ResetButton") is Button resetButton)
            {
                resetButton.Click += (s, e) => _controller.Reset();
            }

            if (GetTemplateChild("PART_TraceButton") is Button traceButton)
            {
                _traceButton = traceButton;
                traceButton.Click += (s, e) => _ = _controller.TraceAsync();
            }

            if (GetTemplateChild("PART_BusyIndicator") is ProgressBar busyIndicator)
            {
                _busyIndicator = busyIndicator;
            }

            if (GetTemplateChild("PART_StatusLabel") is TextBlock statusLabel)
            {
                _statusLabel = statusLabel;
            }

            if (GetTemplateChild("PART_StartingPointsList") is ListView startingPointsList)
            {
                _startingPointsList = startingPointsList;
                _startingPointsList.ItemsSource = _controller.ControllerStartingPoints;
                _startingPointsList.SelectionChanged += (s, e) =>
                {
                    _controller.UpdateSelectedStartingPoint(((ListView)s).SelectedItem as StartingPointModel);
                };
            }

            if (GetTemplateChild("PART_FunctionResultsList") is ListView functionResultList)
            {
                _functionResultsList = functionResultList;
                _functionResultsList.ItemsSource = _controller.FunctionResults;
            }
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
        /// Gets or sets the <see cref="Style"/> that is applied to the container element generated for each
        /// <see cref="UtilityNetwork"/> item.
        /// </summary>
        /// <value>A <see cref="Style"/> for a <see cref="UtilityNetwork"/> item.</value>
        public Style? UtilityNetworkItemContainerStyle
        {
            get => GetValue(UtilityNetworkItemContainerStyleProperty) as Style;
            set => SetValue(UtilityNetworkItemContainerStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="UtilityNetworkItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UtilityNetworkItemContainerStyleProperty =
            DependencyProperty.Register(nameof(UtilityNetworkItemContainerStyle), typeof(Style),
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
        /// Gets or sets the <see cref="Style"/> that is applied to the container element generated for each
        /// pre-configured trace type, which is a <see cref="UtilityNamedTraceConfiguration"/> item.
        /// </summary>
        /// <value>A <see cref="Style"/> for a <see cref="UtilityNamedTraceConfiguration"/> item.</value>
        public Style? TraceTypeItemContainerStyle
        {
            get => GetValue(TraceTypeItemContainerStyleProperty) as Style;
            set => SetValue(TraceTypeItemContainerStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TraceTypeItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TraceTypeItemContainerStyleProperty =
            DependencyProperty.Register(nameof(TraceTypeItemContainerStyle), typeof(Style),
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
        /// Gets or sets the <see cref="Style"/> that is applied to the container element generated for each
        /// starting point, which is a <see cref="UtilityElement"/> item.
        /// </summary>
        /// <value>A <see cref="Style"/> for a <see cref="UtilityElement"/> item.</value>
        public Style? StartingPointItemContainerStyle
        {
            get => GetValue(StartingPointItemContainerStyleProperty) as Style;
            set => SetValue(StartingPointItemContainerStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StartingPointItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingPointItemContainerStyleProperty =
            DependencyProperty.Register(nameof(StartingPointItemContainerStyle), typeof(Style),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to display a function trace result, which is
        /// a <see cref="UtilityTraceFunctionOutput"/> item.
        /// </summary>
        /// <value>A <see cref="DataTemplate"/> for a <see cref="UtilityTraceFunctionOutput"/> item.</value>
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

        /// <summary>
        /// Gets or sets the <see cref="Style"/> that is applied to the container element generated for each
        /// function trace result, which is a <see cref="UtilityTraceFunctionOutput"/> item.
        /// </summary>
        /// <value>A <see cref="Style"/> for a <see cref="UtilityTraceFunctionOutput"/> item.</value>
        public Style? ResultItemContainerStyle
        {
            get => GetValue(ResultItemContainerStyleProperty) as Style;
            set => SetValue(ResultItemContainerStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ResultItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultItemContainerStyleProperty =
            DependencyProperty.Register(nameof(ResultItemContainerStyle), typeof(Style),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="Style"/> that is applied to the add starting points <see cref="ToggleButton"/>.
        /// </summary>
        /// <value>A <see cref="Style"/> for the trace <see cref="ToggleButton"/>.</value>
        public Style? AddStartingPointToggleButtonStyle
        {
            get => GetValue(AddStartingPointToggleButtonStyleProperty) as Style;
            set => SetValue(AddStartingPointToggleButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AddStartingPointToggleButtonStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AddStartingPointToggleButtonStyleProperty =
            DependencyProperty.Register(nameof(AddStartingPointToggleButtonStyle),
                typeof(Style), typeof(UtilityNetworkTraceTool), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="Style"/> that is applied to the trace <see cref="Button"/>.
        /// </summary>
        /// <value>A <see cref="Style"/> for the trace <see cref="Button"/>.</value>
        public Style? TraceButtonStyle
        {
            get => GetValue(TraceButtonStyleProperty) as Style;
            set => SetValue(TraceButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TraceButtonStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TraceButtonStyleProperty =
            DependencyProperty.Register(nameof(TraceButtonStyle), typeof(Style),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="Style"/> that is applied to the reset <see cref="Button"/>.
        /// </summary>
        /// <value>A <see cref="Style"/> for the reset <see cref="Button"/>.</value>
        public Style? ResetButtonStyle
        {
            get => GetValue(ResetButtonStyleProperty) as Style;
            set => SetValue(ResetButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ResetButtonStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResetButtonStyleProperty =
            DependencyProperty.Register(nameof(ResetButtonStyle), typeof(Style),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="Style"/> that is applied to the busy indicator <see cref="ProgressBar"/>.
        /// </summary>
        /// <value>A <see cref="Style"/> for the busy indicator <see cref="ProgressBar"/>.</value>
        public Style? BusyProgressBarStyle
        {
            get => GetValue(BusyProgressBarStyleProperty) as Style;
            set => SetValue(BusyProgressBarStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BusyProgressBarStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BusyProgressBarStyleProperty =
            DependencyProperty.Register(nameof(BusyProgressBarStyle), typeof(Style),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="Style"/> that is applied to the status <see cref="TextBlock"/>.
        /// </summary>
        /// <value>A <see cref="Style"/> for the status <see cref="TextBlock"/>.</value>
        public Style? StatusTextBlockStyle
        {
            get => GetValue(StatusTextBlockStyleProperty) as Style;
            set => SetValue(StatusTextBlockStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StatusTextBlockStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StatusTextBlockStyleProperty =
            DependencyProperty.Register(nameof(StatusTextBlockStyle), typeof(Style),
                typeof(UtilityNetworkTraceTool), new PropertyMetadata(null));
    }
}
#endif
