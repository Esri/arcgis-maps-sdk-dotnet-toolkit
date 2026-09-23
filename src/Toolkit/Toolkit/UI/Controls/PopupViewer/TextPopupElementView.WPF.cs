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

#if WPF
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System.Windows.Automation;
using System.Windows.Automation.Peers;


namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a <see cref="TextPopupElement"/>.
    /// </summary>
    [TemplatePart(Name = TextAreaName, Type = typeof(AccessibleRichTextBox))]
    public partial class TextPopupElementView : Control
    {
        private const string TextAreaName = "TextArea";

        /// <summary>
        /// Creates a new instance of the <see cref="TextPopupElementViewPeer"/> class.
        /// </summary>
        /// <returns></returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TextPopupElementViewPeer(this);
        }

        private void OnElementPropertyChanged()
        {
            // Full list of supported tags and attributes here: https://doc.arcgis.com/en/arcgis-online/reference/supported-html.htm
            if (!string.IsNullOrEmpty(Element?.Text) && GetTemplateChild(TextAreaName) is AccessibleRichTextBox rtb)
            {
                rtb.Document = HtmlToView.ToFlowDocument(Element.Text, (s,e) =>  PopupViewer.GetPopupViewerParent(s as DependencyObject)?.OnHyperlinkClicked(e.Uri));
                
                // RichTextBox's default automation peer only surfaces the current line to Narrator on focus
                // (the same "edit control" behavior as a plain multi-line TextBox), so the full text is set
                // explicitly as the accessible name to make sure all of it - not just the first line - is exposed.
                AutomationProperties.SetName(this, Element.Text.ToPlainText());
            }
        }
    }

    /// <summary>
    /// Automation peer for the <see cref="TextPopupElementView"/> control.
    /// </summary>
    public partial class TextPopupElementViewPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TextPopupElementViewPeer"/> class.
        /// </summary>
        /// <param name="owner"></param>
        public TextPopupElementViewPeer(TextPopupElementView owner) : base(owner)
        {
        }

        /// <inheritdoc />
        protected override string GetClassNameCore() => nameof(TextPopupElementView);

        /// <summary>
        /// Indicates that this control is a content element for accessibility purposes.
        /// </summary>
        /// <returns></returns>
        protected override bool IsContentElementCore()
        {
            return true;
        }

        /// <summary>
        /// Indicates that this control is a control element for accessibility purposes.
        /// </summary>
        /// <returns></returns>
        protected override bool IsControlElementCore()
        {
            return true;
        }
    }
}
#endif