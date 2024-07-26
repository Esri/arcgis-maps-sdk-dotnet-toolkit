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

#if WINDOWS_XAML
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;

#if WINUI
using RichTextBox = Microsoft.UI.Xaml.Controls.RichTextBlock;
using Microsoft.UI.Xaml.Documents;
#elif WINDOWS_UWP
using RichTextBox = Windows.UI.Xaml.Controls.RichTextBlock;
using Windows.UI.Xaml.Documents;
#endif


namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a <see cref="TextPopupElement"/>.
    /// </summary>
    [TemplatePart(Name = TextAreaName, Type = typeof(RichTextBox))]
    public partial class TextPopupElementView : Control
    {
        private const string TextAreaName = "TextArea";

        private void OnElementPropertyChanged()
        {
            if (!string.IsNullOrEmpty(Element?.Text) && GetTemplateChild(TextAreaName) is RichTextBox rtb)
            {
                //TODO. See https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.documents?view=windows-app-sdk-1.5
                // Fallback if something went wrong with the parsing:
                // Just display the text without any markup;
                var plainText = Element.Text.ToPlainText();
                Paragraph p = new Paragraph();
                p.Inlines.Add(new Run() { Text = Element.Text.ToPlainText() });
                rtb.Blocks.Clear();
                rtb.Blocks.Add(p);
            }
        }
    }
}
#endif