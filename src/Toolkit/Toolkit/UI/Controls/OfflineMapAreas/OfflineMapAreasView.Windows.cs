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

#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
#if WPF
using System.Windows.Controls.Primitives;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class OfflineMapAreasView : Control
    {
        /// <inheritdoc/>
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            if (GetTemplateChild("RefreshMapAreasButton") is ButtonBase refreshMapAreasButton)
            {
                refreshMapAreasButton.Click += (s, e) => _vm?.LoadModelsAsync();
            }

            if (GetTemplateChild("NoInternetRefreshButton") is ButtonBase noInternetRefreshButton)
            {
                noInternetRefreshButton.Click += (s, e) => _vm?.LoadModelsAsync();
            }

            if (GetTemplateChild("AddMapAreaButton") is ButtonBase addMapAreaButton)
            {
                addMapAreaButton.Click += (s, e) => InitAddOnDemandArea();
            }

            if (GetTemplateChild("AcceptAddOnDemandAreaButton") is ButtonBase acceptAddOnDemandAreaButton)
            {
                acceptAddOnDemandAreaButton.Click += (s, e) => AddOnDemandArea();
            }

            if (GetTemplateChild("CancelAddOnDemandAreaButton") is ButtonBase cancelAddOnDemandAreaButton)
            {
                cancelAddOnDemandAreaButton.Click += (s, e) => CloseAddOnDemandArea();
            }
        }

        // Template settings class.
        // See https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/template-settings-classes for more information about template settings and why we use them.

        /// <summary>
        /// Gets an object that provides calculated values that can be referenced as TemplateBinding sources when defining templates for a <see cref="OfflineMapAreasView" /> control.
        /// </summary>
        public OfflineMapAreasTemplateSettings TemplateSettings
        {
#if WPF
            get => (OfflineMapAreasTemplateSettings)GetValue(TemplateSettingsPropertyKey.DependencyProperty);
            private set => SetValue(TemplateSettingsPropertyKey, value);
#elif WINUI
            get => (OfflineMapAreasTemplateSettings)GetValue(TemplateSettingsProperty);
            private set => SetValue(TemplateSettingsProperty, value);
#endif
        }

#if WINUI
        /// <summary>Identifies the <see cref="TemplateSettings"/> dependency property.</summary>
        public static readonly DependencyProperty TemplateSettingsProperty =
            DependencyProperty.Register(nameof(TemplateSettings), typeof(OfflineMapAreasTemplateSettings), typeof(OfflineMapAreasView), new PropertyMetadata(null));
#elif WPF
        private static readonly DependencyPropertyKey TemplateSettingsPropertyKey =
                DependencyProperty.RegisterReadOnly(
                  name: nameof(TemplateSettings),
                  propertyType: typeof(OfflineMapAreasTemplateSettings),
                  ownerType: typeof(OfflineMapAreasView),
                  typeMetadata: new FrameworkPropertyMetadata());
#endif
    }
}
#endif
