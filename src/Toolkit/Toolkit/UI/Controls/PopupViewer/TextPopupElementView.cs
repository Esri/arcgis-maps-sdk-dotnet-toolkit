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

#if WPF || MAUI
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
#if WPF
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
#elif MAUI
using Esri.ArcGISRuntime.Toolkit.Maui;
using Microsoft.Maui.ApplicationModel;
#endif


#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    public partial class TextPopupElementView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextPopupElementView"/> class.
        /// </summary>
        public TextPopupElementView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(TextPopupElementView);
#endif
        }

        /// <inheritdoc />
#if WINDOWS_XAML || MAUI
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            OnElementPropertyChanged();
        }

        /// <summary>
        /// Gets or sets the TextPopupElement.
        /// </summary>
        public TextPopupElement? Element
        {
            get => GetValue(ElementProperty) as TextPopupElement;
            set => SetValue(ElementProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ElementProperty =
            PropertyHelper.CreateProperty<TextPopupElement, TextPopupElementView>(nameof(Element), null, (s, oldValue, newValue) => s.OnElementPropertyChanged());
    }
}
#endif