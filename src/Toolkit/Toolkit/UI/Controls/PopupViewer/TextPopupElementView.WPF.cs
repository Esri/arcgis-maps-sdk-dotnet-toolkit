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
using Esri.ArcGISRuntime.UI;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;


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
            // Full list of supported tags and attributes here: https://doc.arcgis.com/en/arcgis-online/reference/supported-html.htm
            if (!string.IsNullOrEmpty(Element?.Text) && GetTemplateChild(TextAreaName) is RichTextBox rtb)
            {
                var doc = new FlowDocument { FontSize = 14d }; // match the default "content" font size on AGOL
                try
                {
                    var htmlRoot = HtmlUtility.BuildDocumentTree(Element.Text);
                    var blocks = VisitAndAddBlocks(htmlRoot.Children).ToList();
                    doc.Blocks.AddRange(blocks);
                }
                catch
                {
                    // Fallback if something went wrong with the parsing:
                    // Just display the text without any markup;
                    var plainText = Element.Text.ToPlainText();
                    doc.Blocks.Add(new Paragraph(new Run(plainText)));
                }
                rtb.Document = doc;
            }
        }

        private static IEnumerable<Block> VisitAndAddBlocks(IEnumerable<MarkupNode> nodes)
        {
            Paragraph? inlineHolder = null;
            foreach (var node in nodes)
            {
                if (MapsToBlock(node))
                {
                    if (inlineHolder != null)
                    {
                        yield return inlineHolder;
                        inlineHolder = null;
                    }
                    yield return VisitBlock(node);
                }
                else
                {
                    inlineHolder ??= new Paragraph { Margin = new Thickness(0) };
                    inlineHolder.Inlines.Add(VisitInline(node));
                }
            }
            if (inlineHolder != null)
                yield return inlineHolder;
        }

        private static IEnumerable<Inline> VisitAndAddInlines(IEnumerable<MarkupNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (MapsToBlock(node))
                {
                    // Blocks have to be wrapped in an AnchoredBlock (such as Floater) to appear among inlines
                    var blockHolder = new Floater();
                    blockHolder.Blocks.Add(VisitBlock(node));
                    yield return blockHolder;
                }
                yield return VisitInline(node);
            }
        }

        private static Block VisitBlock(MarkupNode node)
        {
            switch (node.Type)
            {
                case MarkupType.List:
                    var list = new List();
                    if (node.Token?.Name == "ol")
                        list.MarkerStyle = TextMarkerStyle.Decimal;
                    else
                        list.MarkerStyle = TextMarkerStyle.Disc;
                    foreach (var itemNode in node.Children)
                    {
                        if (itemNode.Type == MarkupType.ListItem)
                        {
                            var listItem = new ListItem();
                            listItem.Blocks.AddRange(VisitAndAddBlocks(itemNode.Children));
                            list.ListItems.Add(listItem);
                        }
                        // else ignore a misplaced non-list-item node
                    }
                    return list;

                case MarkupType.Block:
                    if (HasAnyBlocks(node))
                    {
                        var section = new Section();
                        ApplyStyle(section, node);
                        section.Blocks.AddRange(VisitAndAddBlocks(node.Children));
                        return section;
                    }
                    else
                    {
                        var para = new Paragraph();
                        // In HTML, <p> has default margin but other blocks (like <div>) do not.
                        // In WPF, all of these map to Paragraphs that *does* have a default margin.
                        if (node.Token?.Name != "p")
                            para.Margin = new Thickness(0);
                        ApplyStyle(para, node);
                        para.Inlines.AddRange(VisitAndAddInlines(node.Children));
                        return para;
                    }

                case MarkupType.Divider:
                    return new BlockUIContainer(new Separator());

                case MarkupType.Table:
                    var table = new Table { Margin = new Thickness(0) };
                    var columnCount = node.Children.Max(rowNode => rowNode.Children.Count);
                    for (int i = 0; i < columnCount; i++)
                        table.Columns.Add(new TableColumn());
                    var rowGroup = new TableRowGroup();
                    foreach (var rowNode in node.Children)
                    {
                        var row = new TableRow();
                        ApplyStyle(row, rowNode);
                        foreach (var cellNode in rowNode.Children)
                        {
                            var cell = new TableCell();
                            ApplyStyle(cell, cellNode);

                            // Apply colspan and rowspan, for non-uniform tables
                            var attr = HtmlUtility.ParseAttributes(cellNode.Token?.Attributes);
                            if (attr.TryGetValue("colspan", out var colSpanStr) && byte.TryParse(colSpanStr, out var colSpan))
                                cell.ColumnSpan = colSpan;
                            if (attr.TryGetValue("rowspan", out var rowSpanStr) && byte.TryParse(rowSpanStr, out var rowSpan))
                                cell.RowSpan = rowSpan;

                            cell.Blocks.AddRange(VisitAndAddBlocks(cellNode.Children));
                            row.Cells.Add(cell);
                        }
                        rowGroup.Rows.Add(row);
                    }
                    table.RowGroups.Add(rowGroup);
                    return table;

                default:
                    return new Section(); // placeholder for unsupported things
            }
        }

        private static Inline VisitInline(MarkupNode node)
        {
            switch (node.Type)
            {
                case MarkupType.Link:
                    var link = new Hyperlink();
                    if (Uri.TryCreate(node.Content, UriKind.Absolute, out var linkUri))
                    {
                        link.NavigateUri = linkUri;
                        link.RequestNavigate += NavigateToUri;
                    } // else If we can't create a URL, we can't make a link clickable
                    link.Inlines.AddRange(VisitAndAddInlines(node.Children));
                    ApplyStyle(link, node);
                    return link;

                case MarkupType.Image:
                    if (PopupMediaView.TryCreateImageSource(node.Content, out var imageSource))
                    {
                        var imageElement = new Image { Source = imageSource };
                        return new InlineUIContainer(imageElement);
                    }
                    return new Run(); // TODO find a better placeholder when img src is invalid

                case MarkupType.Span:
                    var span = new Span();
                    ApplyStyle(span, node);
                    span.Inlines.AddRange(VisitAndAddInlines(node.Children));
                    return span;

                case MarkupType.Sub:
                    var sub = new Span();
                    ApplyStyle(sub, node);
                    Typography.SetVariants(sub, FontVariants.Subscript);
                    sub.Inlines.AddRange(VisitAndAddInlines(node.Children));
                    return sub;

                case MarkupType.Sup:
                    var sup = new Span();
                    ApplyStyle(sup, node);
                    Typography.SetVariants(sup, FontVariants.Superscript);
                    sup.Inlines.AddRange(VisitAndAddInlines(node.Children));
                    return sup;

                case MarkupType.Break:
                    return new LineBreak();

                case MarkupType.Text:
                    var run = new Run(node.Content);
                    ApplyStyle(run, node);
                    return run;

                default:
                    return new Run(); // placeholder for unsupported types
            }
        }

        private static bool HasAnyBlocks(MarkupNode node)
        {
            return node.Children.Any(c => MapsToBlock(c) || HasAnyBlocks(c));
        }

        private static bool MapsToBlock(MarkupNode node)
        {
            return node.Type is MarkupType.List or MarkupType.Table or MarkupType.Block or MarkupType.Divider;
        }

        private static void ApplyStyle(TextElement el, MarkupNode node)
        {
            if (node.IsBold == true)
                el.FontWeight = FontWeights.Bold;
            if (node.IsItalic == true)
                el.FontStyle = FontStyles.Italic;
            if (node.FontColor.HasValue)
                el.Foreground = new SolidColorBrush(ConvertColor(node.FontColor.Value));
            if (node.BackColor.HasValue)
                el.Background = new SolidColorBrush(ConvertColor(node.BackColor.Value));
            if (node.FontSize.HasValue)
                el.FontSize = 16d * node.FontSize.Value; // based on AGOL's default font size
            if (node.Alignment.HasValue)
            {
                // Unfortunately the TextAlignment property is separately defined for these FlowDocument elements
                if (el is Block blockEl)
                    blockEl.TextAlignment = ConvertAlignment(node.Alignment);
                else if (el is TableCell cellEl)
                    cellEl.TextAlignment = ConvertAlignment(node.Alignment);
                else if (el is ListItem itemEl)
                    itemEl.TextAlignment = ConvertAlignment(node.Alignment);
            }
            if (node.IsUnderline == true)
            {
                if (el is Inline inlineEl)
                    inlineEl.TextDecorations.Add(TextDecorations.Underline);
                if (el is Paragraph paraEl)
                    paraEl.TextDecorations.Add(TextDecorations.Underline);
                // TODO underline inheritance from non-para blocks?
            }
        }

        private static System.Windows.Media.Color ConvertColor(System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private static TextAlignment ConvertAlignment(HtmlAlignment? alignment) => alignment switch
        {
            HtmlAlignment.Left => TextAlignment.Left,
            HtmlAlignment.Center => TextAlignment.Center,
            HtmlAlignment.Right => TextAlignment.Right,
            _ => TextAlignment.Left,
        };

        private static void NavigateToUri(object sender, RequestNavigateEventArgs ea)
        {
            PopupViewer.GetPopupViewerParent(sender as DependencyObject)?.OnHyperlinkClicked(ea.Uri);
        }
    }
}
#endif