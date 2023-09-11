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
using Esri.ArcGISRuntime.UI;
using Grid = Microsoft.Maui.Controls.Grid;
using RuntimeImageExtensions = Esri.ArcGISRuntime.Maui.RuntimeImageExtensions;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.PopupViewer"/> control,
    /// used for rendering a <see cref="TextPopupElement"/>.
    /// </summary>
    public partial class TextPopupElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private static Thickness ParagraphMargin = new(0, 0, 0, 16);

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
                var blocks = VisitChildren(htmlRoot);
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

        private static IEnumerable<View> VisitChildren(MarkupNode parent)
        {
            // Create views for all the children of a given node.
            // Nodes with blocks are converted individually, but consecutive inline-only nodes are grouped into labels.
            List<MarkupNode>? inlineNodes = null;
            foreach (var node in parent.Children)
            {
                node.InheritAttributes(parent);
                if (MapsToBlock(node) || HasAnyBlocks(node))
                {
                    if (inlineNodes != null)
                    {
                        var label = CreateFormattedText(inlineNodes);
                        ApplyStyle(label, parent);
                        inlineNodes = null;
                        yield return label;
                    }
                    yield return CreateBlock(node);
                }
                else
                {
                    inlineNodes ??= new List<MarkupNode>();
                    inlineNodes.Add(node);
                }
            }
            if (inlineNodes != null)
            {
                var label = CreateFormattedText(inlineNodes);
                ApplyStyle(label, parent);
                yield return label;
            }
        }

        private static View CreateBlock(MarkupNode node)
        {
            // Create a view for a single block node.
            switch (node.Type)
            {
                case MarkupType.List:
                    // Lists (li and ol) are laid out in a grid, with a narrow column of markers and a wide one for the content.
                    // +-----+----------------------+
                    // | 1.  | First item content   |
                    // +-----+----------------------+
                    // | 2.  | Second item content  |
                    // +-----+----------------------+
                    // | ...                        |
                    // +-----+----------------------+
                    // | 3.  | Last item content    |
                    // +-----+----------------------+
                    bool isOrdered = node.Token?.Name == "ol";
                    var listGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
                    listGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // bullets
                    listGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star }); // contents

                    var childItems = node.Children.Where(n => n.Type == MarkupType.ListItem).ToList(); // ignore a misplaced non-list-item node

                    for (int row = 0; row < childItems.Count; row++)
                    {
                        listGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        var markerText = isOrdered ? $"{row + 1}." : "\u2022";
                        listGrid.Add(new Label { Text = markerText, HorizontalTextAlignment = TextAlignment.End, Margin = new Thickness(5, 0) }, 0, row);
                        var item = CreateBlock(childItems[row]);
                        listGrid.Add(item, 1, row);
                    }
                    return listGrid;

                case MarkupType.Block:
                case MarkupType.ListItem:
                case MarkupType.TableCell:
                    bool isPara = node.Token?.Name == "p";
                    View view;
                    if (HasAnyBlocks(node))
                    {
                        var container = new StackLayout();
                        if (isPara)
                            container.Margin = ParagraphMargin;
                        if (node.BackColor.HasValue)
                            container.BackgroundColor = ConvertColor(node.BackColor.Value);

                        var blocks = VisitChildren(node);
                        foreach (var block in blocks)
                        {
                            container.Children.Add(block);
                        }
                        view = container;
                    }
                    else
                    {
                        var label = CreateFormattedText(node.Children);
                        if (isPara)
                            label.Margin = ParagraphMargin;
                        ApplyStyle(label, node);
                        view = label;
                    }
                    if (node.Type == MarkupType.TableCell)
                        return VerticallyAlignTableCell(node, view);
                    return view;

                case MarkupType.Divider:
                    return new BoxView { HeightRequest = 1, Color = Colors.Gray }; // TODO: Do we need to set a color?

                case MarkupType.Table:
                    return ConvertTableToGrid(node);

                case MarkupType.Image:
                    var imageElement = new Image();
                    if (Uri.TryCreate(node.Content, UriKind.Absolute, out var imgUri))
                    {
                        imageElement.Loaded += OnImageElementLoaded;

                        async void OnImageElementLoaded(object? sender, EventArgs e)
                        {
                            imageElement.Loaded -= OnImageElementLoaded;
                            var taggedUri = imgUri;
                            var ri = new RuntimeImage(taggedUri); // Use Runtime's caching and authentication
                            try
                            {
                                imageElement.Source = await RuntimeImageExtensions.ToImageSourceAsync(ri);
                            }
                            catch
                            {
                                // Don't let one bad image take down the whole app. Better to ignore a failed image load.
                            }
                        }
                    }
                    return imageElement;

                default:
                    return new Border(); // placeholder for unsupported things
            }

            static View VerticallyAlignTableCell(MarkupNode node, View cellContent)
            {
                // In HTML, table cells are vertically centered by default.
                cellContent.VerticalOptions = LayoutOptions.Center;
                if (node.BackColor.HasValue)
                {
                    // If a table-cell has a background color, we need to wrap the content in a space-filling Grid,
                    // otherwise the background color will only show directly behind the text and look patchy.
                    var grid = new Grid { cellContent };
                    grid.BackgroundColor = ConvertColor(node.BackColor.Value);
                    return grid;
                }
                else
                {
                    return cellContent;
                }
            }
        }

        private static Label CreateFormattedText(IEnumerable<MarkupNode> nodes)
        {
            // Flattens given tree of inline nodes into a single label.
            var str = new FormattedString();
            foreach (var node in nodes)
            {
                foreach (var span in VisitInline(node))
                {
                    str.Spans.Add(span);
                }
            }
            return new Label { FormattedText = str, LineBreakMode = LineBreakMode.WordWrap };
        }

        private static IEnumerable<Span> VisitInline(MarkupNode node)
        {
            // Converts a single inline node into a sequence of spans.
            // The whole tree is expected to only contain inline nodes. Other nodes are handled by VisitBlock.
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

        private static Grid ConvertTableToGrid(MarkupNode table)
        {
            // Determines the dimensions of a grid necessary to hold a given table.
            // Utilizes a dynamically-sized 2D bitmap (`gridMap`) to mark occupied cells while iterating over the table.
            // Expands the grid as necessary based on cell spans and avoids collisions by checking the bitmap.
            List<List<bool>> gridMap = new List<List<bool>>();

            int maxRowUsed = -1;
            int maxColUsed = -1;

            var gridView = new Grid();

            int curRow = 0;
            foreach (MarkupNode tr in table.Children)
            {
                tr.InheritAttributes(table);
                int curCol = 0;
                foreach (MarkupNode td in tr.Children)
                {
                    // Find the next available cell in this row
                    EnsureColumnExists(curRow, curCol);
                    while (gridMap[curRow][curCol])
                    {
                        curCol++;
                        EnsureColumnExists(curRow, curCol);
                    }

                    int rowSpan = 1;
                    int colSpan = 1;

                    // Create a View for the current table-cell, and add it to the grid.
                    td.InheritAttributes(tr);
                    var cellView = CreateBlock(td);
                    var attr = HtmlUtility.ParseAttributes(td.Token?.Attributes);
                    if (attr.TryGetValue("colspan", out var colSpanStr) && ushort.TryParse(colSpanStr, out var colSpanFromAttr))
                    {
                        colSpan = colSpanFromAttr;
                        Grid.SetColumnSpan(cellView, colSpan);
                    }
                    if (attr.TryGetValue("rowspan", out var rowSpanStr) && ushort.TryParse(rowSpanStr, out var rowSpanFromAttr))
                    {
                        rowSpan = rowSpanFromAttr;
                        Grid.SetRowSpan(cellView, colSpan);
                    }
                    gridView.Add(cellView, curCol, curRow);

                    // Mark grid-cells occupied by the current table-cell
                    for (int i = 0; i < rowSpan; i++)
                    {
                        for (int j = 0; j < colSpan; j++)
                        {
                            EnsureColumnExists(curRow + i, curCol + j);

                            gridMap[curRow + i][curCol + j] = true;

                            maxRowUsed = Math.Max(maxRowUsed, curRow + i);
                            maxColUsed = Math.Max(maxColUsed, curCol + j);
                        }
                    }
                    curCol += colSpan;
                }
                curRow++;
            }

            // Now we know exactly how many rows and columns were necessary to hold the table. Allocate them!
            for (int i = 0; i <= maxRowUsed; i++)
                gridView.RowDefinitions.Add(new RowDefinition());
            for (int i = 0; i <= maxColUsed; i++)
                gridView.ColumnDefinitions.Add(new ColumnDefinition());

            return gridView;

            // Expand the gridMap as needed to make sure that given row/column exists
            void EnsureColumnExists(int row, int col)
            {
                while (gridMap.Count <= row)
                    gridMap.Add(new List<bool>());
                while (gridMap[row].Count <= col)
                    gridMap[row].Add(false);
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

            if (node.Alignment.HasValue)
                el.HorizontalTextAlignment = ConvertAlignment(node.Alignment);
        }

        private static TextAlignment ConvertAlignment(HtmlAlignment? alignment) => alignment switch
        {
            HtmlAlignment.Left => TextAlignment.Start,
            HtmlAlignment.Center => TextAlignment.Center,
            HtmlAlignment.Right => TextAlignment.End,
            _ => TextAlignment.Start,
        };

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