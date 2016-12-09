// /*******************************************************************************
//  * Copyright 2012-2016 Esri
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
using System.Windows;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal static partial class DesignTime
    {
        private static bool? s_isInDesignMode;

        /// <summary>
        /// Gets a value indicating whether the process is in design mode (running in Blend
        /// or Visual Studio).
        /// </summary>
        public static bool IsDesignMode
        {
            get
            {
                if (!s_isInDesignMode.HasValue)
                {
#if NETFX_CORE
                    s_isInDesignMode = Windows.ApplicationModel.DesignMode.DesignModeEnabled;
#elif !XAMARIN      // Design-time detection is TODO
                    // http://geekswithblogs.net/lbugnion/archive/2009/09/05/detecting-design-time-mode-in-wpf-and-silverlight.aspx
                    var prop = DesignerProperties.IsInDesignModeProperty;
                    s_isInDesignMode
                        = (bool)DependencyPropertyDescriptor
                        .FromProperty(prop, typeof(FrameworkElement))
                        .Metadata.DefaultValue;
#elif XAMARIN
                    s_isInDesignMode = false;
#endif
                }

                return s_isInDesignMode.Value;
            }
#if __IOS__
            set
            {
                s_isInDesignMode = value;
            }
#endif
        }
    }
}
