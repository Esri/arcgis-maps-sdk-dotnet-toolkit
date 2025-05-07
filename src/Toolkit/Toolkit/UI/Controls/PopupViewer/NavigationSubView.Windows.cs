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

using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
#if WPF
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
#elif WINUI
using Microsoft.UI.Xaml.Media.Animation;
#elif WINDOWS_UWP
using Windows.UI.Xaml.Media.Animation;
#endif


namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class NavigationSubView : Control
    {
#if WINDOWS_XAML
        /// <summary>
        /// Gets or sets the vertical scrollbar visibility of the scrollviewer below the title.
        /// </summary>
        public TransitionCollection ContentTransitions
        {
            get => (TransitionCollection)GetValue(ContentTransitionsProperty);
            set => SetValue(ContentTransitionsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ContentTransitions"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentTransitionsProperty = PropertyHelper.CreateProperty<TransitionCollection , NavigationSubView>(nameof(ContentTransitions), null);

#endif
    }
}
#endif