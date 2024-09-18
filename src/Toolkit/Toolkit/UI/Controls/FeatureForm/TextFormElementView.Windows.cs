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
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
#if WPF
using System.Windows.Controls.Primitives;
using System.Windows.Input;
#endif
using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{

#if WPF
    [TemplatePart(Name = TextAreaName, Type = typeof(RichTextBox))]
#elif WINDOWS_XAML
    [TemplatePart(Name = TextAreaName, Type = typeof(ContentControl))]
#endif
    public partial class TextFormElementView : Control
    {
#if WPF
        private RichTextBox? _textContainer;
#else
        private ContentControl? _textContainer;
#endif


        /// <inheritdoc />
#if WPF
        public override void OnApplyTemplate()
#else
        protected override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
#if WPF
            _textContainer = GetTemplateChild(TextAreaName) as RichTextBox;
#else
            _textContainer = GetTemplateChild(TextAreaName) as ContentControl;
#endif
            UpdateText();
            UpdateVisibility();
        }

        private void UpdateText()
        {
            if (_textContainer is not null)
            {
                var text = Element?.Text ?? string.Empty;
#if WPF
                var doc = new System.Windows.Documents.FlowDocument { FontSize = 14d }; // match the default "content" font size on AGOL
                if (Element?.Format == FormTextFormat.Markdown)
                {
                    try
                    {
                        var pipeline = new Markdig.MarkdownPipelineBuilder().Build();
                        var result = Markdig.Markdown.ToHtml(text ?? string.Empty, pipeline);

                        var htmlRoot = HtmlUtility.BuildDocumentTree(result);
                        var blocks = TextPopupElementView.VisitAndAddBlocks(htmlRoot.Children).ToList();
                        doc.Blocks.AddRange(blocks);
                        _textContainer.Document = doc;
                        return;
                    }
                    catch
                    {
                        text = RemoveMarkdown(text); // Fallback
                    }
                }
                doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(text)));
                _textContainer.Document = doc;
#elif WINDOWS_XAML
                if (Element?.Format == FormTextFormat.Markdown)
                {
                    StackPanel container = new StackPanel();
                    try
                    {
                        var pipeline = new Markdig.MarkdownPipelineBuilder().Build();
                        var result = Markdig.Markdown.ToHtml(text ?? string.Empty, pipeline);
                        var htmlRoot = HtmlUtility.BuildDocumentTree(result);
                        var blocks = TextPopupElementView.VisitChildren(htmlRoot);
                        foreach (var block in blocks)
                            container.Children.Add(block);
                        _textContainer.Content = container;
                        return;
                    }
                    catch
                    {
                        text = RemoveMarkdown(text); // Fallback
                    }
                }
                _textContainer.Content = new TextBlock() { Text = text };
#endif
            }
        }
    }
}
#endif