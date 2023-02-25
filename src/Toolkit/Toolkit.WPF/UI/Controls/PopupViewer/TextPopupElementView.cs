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
using System.Diagnostics.CodeAnalysis;
using System.Windows.Documents;

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

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            OnElementPropertyChanged();
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
            // Full list of supported tags and attributes here: https://doc.arcgis.com/en/arcgis-online/reference/supported-html.htm
            return;
            if (!string.IsNullOrEmpty(Element?.Text))
            {
                HtmlTokenParser parser = new HtmlTokenParser(Element.Text);
                FlowDocument doc = new FlowDocument();
                bool isBold = false;
                bool isItalic = false;
                bool isHyperLink = false;
                while (parser.NextToken(out HtmlToken? t))
                {
                    var token = t.Value;
                    if (token.Type == TokenType.Begin && token.Name == "p")
                        doc.Blocks.Add(new Paragraph(new Span()));
                    else if (doc.Blocks.Count == 0)
                        doc.Blocks.Add(new Paragraph(new Span()));

                    if (token.Name == "b" || token.Name == "strong")
                        isBold = token.Type == TokenType.Begin;
                    else if (token.Name == "i" || token.Name == "em")
                        isItalic = token.Type == TokenType.Begin;
                    else if (token.Name == "a")
                        isHyperLink = token.Type == TokenType.Begin;

                    if (token.Name == "br" && token.Type !=  TokenType.End)
                    {
                        ((Span)((Paragraph)doc.Blocks.Last()).Inlines.Last()).Inlines.Add(new LineBreak());
                    }
                    if (token.Type == TokenType.None)
                    {
                        ((Span)((Paragraph)doc.Blocks.Last()).Inlines.Last()).Inlines.Add(new TextBlock()
                        {
                            Text = token.Name,
                            FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                            FontStyle = isItalic ? FontStyles.Italic : FontStyles.Normal,
                        });
                    }
                }
                if (GetTemplateChild("TextArea") is RichTextBox rtb)
                {
                    rtb.Document = doc;
                }
            }
        }

        private class HtmlTokenParser
        {
            string _html;
            HtmlToken? currentToken;
            int _idx = 0;
            public HtmlTokenParser(string html)
            { 
                _html = html;
            }
            
            
            public bool NextToken([NotNullWhen(true)] out HtmlToken? token)
            {
                token = null;
                if (_idx < _html.Length && _html[_idx] == '>')
                    _idx++;
                if (_idx >= _html.Length)
                {
                    return false;
                }
                var nextTokenIdx = _html.Substring(_idx).IndexOf('<') + _idx + 1;
                if (nextTokenIdx > _idx + 1)
                {
                    token = new HtmlToken() { Name = _html.Substring(_idx, nextTokenIdx - _idx - 1), Type = TokenType.None };
                    _idx = nextTokenIdx - 1;
                }
                else if (nextTokenIdx < 1)
                {
                    //no more tokens
                    if (_idx < _html.Length)
                    {
                        token = new HtmlToken() { Name = _html.Substring(_idx), Type = TokenType.None };
                        _idx = _html.Length;
                    }
                }
                else
                {
                    var endTokenIdx = _html.Substring(nextTokenIdx).IndexOf('>') + nextTokenIdx;
                    //TODO: Handle endTokenIdx==-1
                    var t = new HtmlToken();
                    if (_html[nextTokenIdx] == '/')
                    {
                        nextTokenIdx++;
                        t.Type = TokenType.End;
                    }
                    else if (_html[endTokenIdx - 1] == '/')
                    {
                        t.Type = TokenType.BeginAndEnd;
                    }
                    else
                        t.Type = TokenType.Begin;
                    var space = _html.Substring(nextTokenIdx, endTokenIdx - nextTokenIdx).IndexOf(' ') + nextTokenIdx + 1;
                    if (space > nextTokenIdx)
                    {
                        t.Attributes = _html.Substring(space, endTokenIdx - space);
                        t.Name = _html.Substring(nextTokenIdx, space - nextTokenIdx - 1);
                    }
                    else
                    {
                        t.Name = _html.Substring(nextTokenIdx, endTokenIdx - nextTokenIdx);
                    }
                    _idx = endTokenIdx;
                    token = t;
                }
                currentToken = token;
                return token != null;
            }
        }
        private struct HtmlToken
        {
            public string Name { get; set; }
            public string Attributes { get; set; }
            public TokenType Type { get; set; }
        }
        private enum TokenType
        {
            None,
            Begin,
            End,
            BeginAndEnd,
        }
    }
}
