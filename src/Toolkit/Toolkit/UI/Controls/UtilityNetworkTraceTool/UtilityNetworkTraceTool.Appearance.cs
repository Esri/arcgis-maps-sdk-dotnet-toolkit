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
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UtilityNetworks;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ToggleButton = Windows.UI.Xaml.Controls.ToggleSwitch;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ToggleButton = System.Windows.Controls.Primitives.ToggleButton;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [TemplatePart(Name = "PART_IneligibleScrim", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_LoadingScrim", Type = typeof(UIElement))]
    #if !WINDOWS_UWP
    [TemplatePart(Name = "PART_TabsControl", Type = typeof(TabControl))]
    #endif
    public partial class UtilityNetworkTraceTool
    {
        private UIElement? _loadingScrim;
        private UIElement? _ineligibleScrim;
        #if !WINDOWS_UWP
        private TabControl? _tabControl;
        #endif

        /// <inheritdoc />
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            #if !WINDOWS_UWP
            if (GetTemplateChild("PART_TabsControl") is TabControl tabcontrol)
            {
                _tabControl = tabcontrol;
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
