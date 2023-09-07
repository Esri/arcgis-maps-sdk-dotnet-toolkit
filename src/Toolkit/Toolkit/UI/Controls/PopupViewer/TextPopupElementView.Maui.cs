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
using Esri.ArcGISRuntime.Toolkit.Internal;
using Microsoft.Maui.ApplicationModel;

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
        /// Template name of the <see cref="StackLayout"/> text area.
        /// </summary>
        public const string TextAreaName = "TextArea";

        static TextPopupElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            var textArea = new StackLayout();
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(textArea, nameScope);
            nameScope.RegisterName(TextAreaName, textArea);
            return textArea;
        }

        private void OnElementPropertyChanged()
        {
            var container = GetTemplateChild(TextAreaName) as StackLayout;
            var text = Element?.Text;
            if (container is null || string.IsNullOrEmpty(text))
                return;

            try
            {
                container.Children.Clear();
                var htmlRoot = HtmlUtility.BuildDocumentTree(text);
                var blocks = VisitAndAddBlocks(htmlRoot);
                foreach (var block in blocks)
                    container.Children.Add(block);
            }
            catch
            {
                container.Children.Clear();
                // Fallback if something went wrong with the parsing:
                // Just display the text without any markup;
                var label = new Label();
#if !WINDOWS
                label.TextType = TextType.Html;
#else
                if (text != null)
                    text = Toolkit.Internal.StringExtensions.ToPlainText(text);
#endif
                label.Text = text;
                container.Children.Add(label);
            }
        }

        private static IEnumerable<View> VisitAndAddBlocks(MarkupNode parent)
        {
            List<MarkupNode>? inlineNodes = null;
            foreach (var node in parent.Children)
            {
                if (MapsToBlock(node) || HasAnyBlocks(node))
                {
                    if (inlineNodes != null)
                    {
                        var label = VisitAndAddInlines(inlineNodes);
                        ApplyStyle(label, parent);
                        inlineNodes = null;
                    }
                    yield return VisitBlock(node);
                }
                else
                {
                    inlineNodes ??= new List<MarkupNode>();
                    inlineNodes.Add(node);
                }
            }
            if (inlineNodes != null)
            {
                var label = VisitAndAddInlines(inlineNodes);
                ApplyStyle(label, parent);
                yield return label;
            }
        }

        private static View VisitBlock(MarkupNode node)
        {
            switch (node.Type)
            {
                case MarkupType.List:
                // TODO: Implement list as a StackLayout-of-items

                //var list = new List();
                //if (node.Token?.Name == "ol")
                //    list.MarkerStyle = TextMarkerStyle.Decimal;
                //else
                //    list.MarkerStyle = TextMarkerStyle.Disc;
                //foreach (var itemNode in node.Children)
                //{
                //    if (itemNode.Type == MarkupType.ListItem)
                //    {
                //        var listItem = new ListItem();
                //        listItem.Blocks.AddRange(VisitAndAddBlocks(itemNode.Children));
                //        list.ListItems.Add(listItem);
                //    }
                //    // else ignore a misplaced non-list-item node
                //}
                //return list;

                case MarkupType.Block:
                    if (HasAnyBlocks(node))
                    {
                        var container = new StackLayout();
                        var blocks = VisitAndAddBlocks(node);
                        foreach (var block in blocks)
                            container.Children.Add(block);
                        return container;
                    }
                    else
                    {
                        var label = VisitAndAddInlines(node.Children);
                        ApplyStyle(label, node);
                        return label;
                    }

                case MarkupType.Divider:
                    return new BoxView { HeightRequest = 1, Color = Colors.Gray }; // TODO: Do we need to set a color?

                case MarkupType.Table:
                // TODO: Implement table as a boxy layout
                //var table = new Table();
                //var columnCount = node.Children.Max(rowNode => rowNode.Children.Count);
                //for (int i = 0; i < columnCount; i++)
                //    table.Columns.Add(new TableColumn());
                //var rowGroup = new TableRowGroup();
                //foreach (var rowNode in node.Children)
                //{
                //    var row = new TableRow();
                //    ApplyStyle(row, rowNode);
                //    foreach (var cellNode in rowNode.Children)
                //    {
                //        var cell = new TableCell();
                //        ApplyStyle(cell, cellNode);

                //        // Apply colspan and rowspan, for non-uniform tables
                //        var attr = HtmlUtility.ParseAttributes(cellNode.Token?.Attributes);
                //        if (attr.TryGetValue("colspan", out var colSpanStr) && byte.TryParse(colSpanStr, out var colSpan))
                //            cell.ColumnSpan = colSpan;
                //        if (attr.TryGetValue("rowspan", out var rowSpanStr) && byte.TryParse(rowSpanStr, out var rowSpan))
                //            cell.RowSpan = rowSpan;

                //        cell.Blocks.AddRange(VisitAndAddBlocks(cellNode.Children));
                //        row.Cells.Add(cell);
                //    }
                //    rowGroup.Rows.Add(row);
                //}
                //table.RowGroups.Add(rowGroup);
                //return table;

                case MarkupType.Image:
                // TODO: Implement image

                case MarkupType.Link:
                // TODO: Implement link-around-blocks

                default:
                    return new Border(); // placeholder for unsupported things
            }
        }

        private static Label VisitAndAddInlines(IEnumerable<MarkupNode> nodes)
        {
            // Flattens given tree of inline nodes into a single FormattedText
            var str = new FormattedString();
            foreach (var node in nodes)
            {
                foreach (var span in VisitInline(node))
                {
                    str.Spans.Add(span);
                }
            }
            return new Label{FormattedText = str, LineBreakMode = LineBreakMode.WordWrap };
        }

        private static IEnumerable<Span> VisitInline(MarkupNode node)
        {
            switch (node.Type)
            {
                case MarkupType.Link:
                    if (Uri.TryCreate(node.Content, UriKind.Absolute, out var linkUri))
                    {
                        // The gesture recognizer will be shared by all the individual spans
                        var tapRecognizer = new TapGestureRecognizer();
                        tapRecognizer.Tapped += (s, e) =>
                        {
                            try
                            {
                                Browser.OpenAsync(node.Content, BrowserLaunchMode.SystemPreferred);
                            }
                            catch { }
                        };
                        foreach (var subNode in node.Children)
                        {
                            subNode.InheritAttributes(node);
                            foreach (var subSpan in VisitInline(subNode))
                            {
                                ApplyStyle(subSpan, node);
                                if (node.IsUnderline != false) // Add underline to links by default (unless specifically disabled)
                                    subSpan.TextDecorations = TextDecorations.Underline;

                                if (subSpan.GestureRecognizers.Count == 0)
                                    subSpan.GestureRecognizers.Add(tapRecognizer);
                                yield return subSpan;
                            }
                        }
                    }
                    else
                    {
                        // Fallback: treat it as a regular span
                        goto case MarkupType.Span;
                    }
                    break;

                case MarkupType.Span:
                case MarkupType.Sub: // Font variants are not supported on MAUI
                case MarkupType.Sup: // Font variants are not supported on MAUI
                    foreach (var subNode in node.Children)
                    {
                        subNode.InheritAttributes(node);
                        foreach (var subSpan in VisitInline(subNode))
                        {
                            yield return subSpan;
                        }
                    }
                    break;

                case MarkupType.Break:
                    yield return new Span { Text = Environment.NewLine };
                    break;

                case MarkupType.Text:
                    var textSpan = new Span { Text = node.Content };
                    ApplyStyle(textSpan, node);
                    yield return textSpan;
                    break;
            }
        }



        private static void ApplyStyle(Span el, MarkupNode node)
        {
            if (node.IsBold == true)
                el.FontAttributes |= FontAttributes.Bold;
            else if (node.IsBold == false)
                el.FontAttributes &= ~FontAttributes.Bold;

            if (node.IsItalic == true)
                el.FontAttributes |= FontAttributes.Italic;
            else if (node.IsItalic == false)
                el.FontAttributes &= ~FontAttributes.Italic;

            if (node.IsUnderline == true)
                el.TextDecorations |= TextDecorations.Underline;
            else if (node.IsUnderline == false)
                el.TextDecorations &= ~TextDecorations.Underline;

            if (node.FontColor.HasValue)
                el.TextColor = ConvertColor(node.FontColor.Value);
            if (node.BackColor.HasValue)
                el.BackgroundColor = ConvertColor(node.BackColor.Value);
            if (node.FontSize.HasValue)
                el.FontSize = 16d * node.FontSize.Value; // based on AGOL's default font size
        }

        private static void ApplyStyle(Label el, MarkupNode node)
        {
            if (node.IsBold == true)
                el.FontAttributes |= FontAttributes.Bold;
            else if (node.IsBold == false)
                el.FontAttributes &= ~FontAttributes.Bold;

            if (node.IsItalic == true)
                el.FontAttributes |= FontAttributes.Italic;
            else if (node.IsItalic == false)
                el.FontAttributes &= ~FontAttributes.Italic;

            if (node.IsUnderline == true)
                el.TextDecorations |= TextDecorations.Underline;
            else if (node.IsUnderline == false)
                el.TextDecorations &= ~TextDecorations.Underline;

            if (node.FontColor.HasValue)
                el.TextColor = ConvertColor(node.FontColor.Value);
            if (node.BackColor.HasValue)
                el.BackgroundColor = ConvertColor(node.BackColor.Value);
            if (node.FontSize.HasValue)
                el.FontSize = 16d * node.FontSize.Value; // based on AGOL's default font size
        }

        private static Color ConvertColor(System.Drawing.Color color)
        {
            return Color.FromRgba(color.R, color.G, color.B, color.A);
        }

        private static bool HasAnyBlocks(MarkupNode node)
        {
            return node.Children.Any(c => MapsToBlock(c) || HasAnyBlocks(c));
        }

        private static bool MapsToBlock(MarkupNode node)
        {
            return node.Type is MarkupType.List or MarkupType.Table or MarkupType.Block or MarkupType.Divider or MarkupType.Image;
        }
    }
}
#endif