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
using System.Windows;

namespace Esri.ArcGISRuntime.Toolkit.Preview.UI.Controls
{
    internal static partial class DesignTime
    {
        private static bool? _isInDesignMode;

        internal static bool IsDesignMode
        {
            get
            {
                if (!_isInDesignMode.HasValue)
                {
#if NETFX_CORE
                    _isInDesignMode = Windows.ApplicationModel.DesignMode.DesignModeEnabled;
#elif !XAMARIN
                    var prop = DesignerProperties.IsInDesignModeProperty;
                    _isInDesignMode
                        = (bool)DependencyPropertyDescriptor
                        .FromProperty(prop, typeof(FrameworkElement))
                        .Metadata.DefaultValue;
#elif __ANDROID__
                    // Assume we're in design-time if there is no application context
                    _isInDesignMode = Android.App.Application.Context == null;
#else
                    _isInDesignMode = false;
#endif
                }

                return _isInDesignMode.Value;
            }

#if __IOS__
            set
            {
                _isInDesignMode = value;
            }
#endif
        }
    }
}
