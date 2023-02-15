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

using Esri.ArcGISRuntime.Mapping.Popups;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a <see cref="TextPopupElement"/>.
    /// </summary>
    public class TextPopupElementView : Control
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextPopupElementView"/> class.
        /// </summary>
        public TextPopupElementView()
        {
            DefaultStyleKey = typeof(TextPopupElementView);
        }

        /// <summary>
        /// Gets or sets the TextPopupElement.
        /// </summary>
        public TextPopupElement? Element
        {
            get { return GetValue(ElementProperty) as TextPopupElement; }
            set { SetValue(ElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(TextPopupElement), typeof(TextPopupElementView), new PropertyMetadata(null, (s, e) => ((TextPopupElementView)s).OnElementPropertyChanged()));

        private void OnElementPropertyChanged()
        {
            // TODO: Convert to pretty html
            // var rt = GetTemplateChild("TextViewArea") as RichTextBox;
            // var html = Element?.Text;
        }
    }
}