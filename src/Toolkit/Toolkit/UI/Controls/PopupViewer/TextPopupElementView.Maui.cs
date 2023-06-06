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

#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.Popups;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.PopupViewer"/> control,
    /// used for rendering a <see cref="TextPopupElement"/>.
    /// </summary>
    public partial class TextPopupElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        /// <summary>
        /// Template name of the <see cref="Label"/> text area.
        /// </summary>
        public const string TextAreaName = "TextArea";

        static TextPopupElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            Label textArea = new Label();
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(textArea, nameScope);
            nameScope.RegisterName(TextAreaName, textArea);
            return textArea;
        }

        private void OnElementPropertyChanged()
        {
            var label = GetTemplateChild(TextAreaName) as Label;
            if (label is null) return;
            var text = Element?.Text;
            
#if !WINDOWS
            label.TextType = TextType.Html;
#else
            if (text != null)
                text = Toolkit.Internal.StringExtensions.ToPlainText(text);
#endif
            label.Text = text;
        }
    }
}
#endif