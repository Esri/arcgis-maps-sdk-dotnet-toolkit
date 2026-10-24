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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Color = System.Drawing.Color;

namespace Esri.ArcGISRuntime.Toolkit.Internal;

internal enum MarkupType
{
    Document,
    Link, // a
    Image, // img
    List, // ul, ol, dl
    ListItem, // li, dt, dd
    Table, // table
    TableRow, // tr
    TableCell, // td, th
    Span, // span, font, b, strong, i, em, u, abbr
    Block, // div, p, figure, figcaption, video, audio, h1-h6
    Sub, // sub
    Sup, // sup
    Divider, // hr
    Break, // br
    Text,
    Source,
    Audio,
    Video,
}

/// <summary>
/// Represents a semantic node in a parsed HTML document.
/// </summary>
internal class MarkupNode
{
    public HtmlToken? Token { get; set; }
    public MarkupType Type { get; set; }
    public bool? IsBold { get; set; }
    public bool? IsMonospace { get; set; }
    public bool? IsItalic { get; set; }
    public bool? IsUnderline { get; set; }
    public double? FontSize { get; set; } // em
    public Color? FontColor { get; set; }
    public Color? BackColor { get; set; }
    public HtmlAlignment? Alignment { get; set; }
    public List<MarkupNode> Children { get; } = new List<MarkupNode>();
    public string? Content { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder("MarkupNode{");
        sb.Append(Type);
        if (IsBold.HasValue)
            sb.Append(" bold=" + IsBold.Value);
        if (IsMonospace.HasValue)
            sb.Append(" monospace=" + IsMonospace.Value);
        if (IsItalic.HasValue)
            sb.Append(" italic=" + IsItalic.Value);
        if (IsUnderline.HasValue)
            sb.Append(" underline=" + IsUnderline.Value);
        if (FontSize.HasValue)
            sb.Append(" size=" + FontSize.Value);
        if (FontColor.HasValue)
            sb.Append(" color=" + FontColor.Value);
        if (BackColor.HasValue)
            sb.Append(" bg=" + BackColor.Value);
        if (Alignment.HasValue)
            sb.Append(" align=" + Alignment.Value);
        if (Content != null)
        {
            if (Type == MarkupType.Link)
                sb.Append(" href=" + Content);
            else if (Type == MarkupType.Image)
                sb.Append(" src=" + Content);
            else
                sb.Append(" text=\"" + Content + "\"");
        }
        if (Children.Any())
            sb.Append(" children=" + Children.Count);
        sb.Append('}');
        return sb.ToString();
    }

    public void InheritAttributes(MarkupNode parent)
    {
        // Copy style attributes from the parent node, unless overridden on this node
        IsBold ??= parent.IsBold;
        IsMonospace ??= parent.IsMonospace;
        IsItalic ??= parent.IsItalic;
        IsUnderline ??= parent.IsUnderline;
        FontSize ??= parent.FontSize;
        FontColor ??= parent.FontColor;
        BackColor ??= parent.BackColor;
        Alignment ??= parent.Alignment;
    }

    public bool HasAnyTextAttributes => IsBold.HasValue || IsMonospace.HasValue || IsItalic.HasValue || IsUnderline.HasValue || FontSize.HasValue || FontColor.HasValue || BackColor.HasValue;

    public bool HasSameTextAttributes(MarkupNode other)
    {
        return IsBold == other.IsBold &&
               IsMonospace == other.IsMonospace &&
               IsItalic == other.IsItalic &&
               IsUnderline == other.IsUnderline &&
               FontSize == other.FontSize &&
               FontColor == other.FontColor &&
               BackColor == other.BackColor;
    }
}

/// <summary>
/// Provides methods for parsing HTML documents and attributes.
/// </summary>
internal class HtmlUtility
{
    private static readonly Regex HtmlTagExpression = new Regex("<[^>]+>", RegexOptions.Compiled);

    /// <summary>
    /// Strips HTML from a string
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string? StripHtml(string? value)
            => string.IsNullOrWhiteSpace(value) ? value : HtmlTagExpression.Replace(value, string.Empty);


    private static readonly Regex AttributeRegex = new Regex(@"([\w-]+)\s*(=\s*('([^']+)'|""([^""]+)""|([^'""\s]+)))?\s*", RegexOptions.Compiled);

    /// <summary>
    /// Parses a string with HTML attribute declarations into a dictionary.
    /// If the same key is declared multiple times, the last value is used.
    /// </summary>
    /// <param name="attrString">Raw attribute declarations.</param>
    /// <returns>A dictionary of attribute-names to unescaped attribute-values.</returns>
    internal static Dictionary<string, string> ParseAttributes(string? attrString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(attrString))
            return result;

        var matches = AttributeRegex.Matches(attrString);

        foreach (Match match in matches)
        {
            string attrName = match.Groups[1].Value.ToLowerInvariant();
            string attrValue = match.Groups[4].Success ? match.Groups[4].Value :
                               match.Groups[5].Success ? match.Groups[5].Value :
                               match.Groups[6].Success ? match.Groups[6].Value : "";
            attrValue = UnescapeHtml(attrValue);
            result[attrName] = attrValue;
        }

        return result;
    }

    /// <summary>
    /// Decodes HTML character references in a string.
    /// </summary>
    /// <param name="input">Raw string literal or attribute value from HTML.</param>
    /// <returns>Unescaped string.</returns>
    internal static string UnescapeHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        return WebUtility.HtmlDecode(input);
    }

    /// <summary>
    /// Parses a string with CSS declarations into a dictionary.
    /// If the same key is declared multiple times, the last value is used.
    /// Keys are case-insensitive, values are trimmed of whitespace.
    /// </summary>
    /// <param name="styleAttribute">Unescaped value of the "style" attribute.</param>
    /// <returns>A dictionary of property-names to property-values.</returns>
    internal static IDictionary<string, string> ParseStyleAttribute(string styleAttribute)
    {
        var styles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(styleAttribute))
            return styles;

        // TODO: handle data URLs, which are special-cased to allow semicolons in values
        // e.g. "background-image: url(data:image/png;base64,...)"
        foreach (var raw in styleAttribute.Split(';'))
        {
            var declaration = raw.Trim();
            if (declaration.Length == 0) continue;

            int colon = declaration.IndexOf(':');
            if (colon <= 0 || colon == declaration.Length - 1) continue; // malformed

            string key = declaration.Substring(0, colon).Trim().ToLowerInvariant();
            string value = declaration.Substring(colon + 1).Trim();

            styles[key] = value;
        }
        return styles;
    }

    /// <summary>
    /// Parses the given HTML snippet into a semantic document tree.
    /// If given snippet does not contain any HTML, a document node with a single "Text" child is returned.
    /// </summary>
    /// <param name="snippet">HTML snippet or plain text.</param>
    /// <returns>Root node of the parsed document.</returns>
    internal static MarkupNode BuildDocumentTree(string snippet)
    {
        var stack = new Stack<MarkupNode>();
        var hiddenNodes = new HashSet<MarkupNode>(); // We have to parse hidden nodes and their children, but don't add them to the document tree
        var root = new MarkupNode { Type = MarkupType.Document };
        stack.Push(root);
        var tokenator = new HtmlTokenParser(snippet);

        while (tokenator.NextToken(out HtmlToken t))
        {
            var parent = stack.Peek();
            var name = t.Name;
            if (t.Type == HtmlTokenType.PlainText)
            {
                if (!string.IsNullOrEmpty(name) && CanContainText(parent.Token?.Name))
                    parent.Children.Add(new MarkupNode { Type = MarkupType.Text, Content = name });
                // else: ignore empty text, and text in contexts where no text is allowed.
                continue;
            }
            var tokenType = t.Type;
            bool isVoid = IsVoidElement(name);

            if (tokenType == HtmlTokenType.CloseTag && isVoid) // Ignore unnecessary closing tags like </img>
                continue;

            if (tokenType == HtmlTokenType.OpenTag && isVoid) // Automatically close tags that cannot have any content (like <br>)
                tokenType = HtmlTokenType.SelfClosingTag;

            // handle implicit closing for optional end tags
            if (tokenType == HtmlTokenType.OpenTag || tokenType == HtmlTokenType.SelfClosingTag)
            {
                // Pop everything that must close when <name> appears.
                // Open inline elements, such as the spans that editors wrap around pasted content,
                // do not keep a p, li, or td open, so look past them as browsers do.
                while (FindImplicitlyClosed(stack, name) is int depth)
                {
                    for (int i = 0; i <= depth; i++)
                        stack.Pop();
                    parent = stack.Peek(); // Update parent to the new top of the stack
                }
            }

            if (name is "script" or "style")
            {
                // Skip unsupported tags
                if (tokenType == HtmlTokenType.OpenTag)
                {
                    while (tokenator.NextToken(out t) && t.Type != HtmlTokenType.CloseTag) { }
                }
                continue;
            }

            var newNode = new MarkupNode { Token = t };
            var attr = ParseAttributes(t.Attributes);

            switch (name)
            {
                case "a":
                    if (attr.TryGetValue("href", out var href))
                    {
                        newNode.Type = MarkupType.Link;
                        newNode.Content = href;
                    }
                    else
                    {
                        // Fallback: anchors with no links are just blocks.
                        newNode.Type = MarkupType.Block;
                    }
                    break;
                case "img":
                    newNode.Type = MarkupType.Image;
                    if (!attr.TryGetValue("src", out var src))
                        continue; // ignore img with no source
                    newNode.Content = src;
                    // TODO width/height overrides
                    // TODO border
                    break;
                case "h1":
                case "h2":
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                    newNode.Type = MarkupType.Block;
                    newNode.FontSize = name switch
                    {
                        "h1" => 2.0,
                        "h2" => 1.5,
                        "h3" => 1.3,
                        "h4" => 1.0,
                        "h5" => 0.8,
                        _ => 0.7, // h6
                    };
                    break;
                case "span":
                case "font":
                case "b":
                case "strong":
                case "i":
                case "em":
                case "u":
                case "abbr":
                case "code":
                    newNode.Type = MarkupType.Span;
                    if (name == "b" || name == "strong")
                        newNode.IsBold = true;
                    else if (name == "i" || name == "em")
                        newNode.IsItalic = true;
                    else if (name == "u")
                        newNode.IsUnderline = true;
                    else if (name == "code")
                        newNode.IsMonospace = true;
                    else if (name == "font")
                    {
                        if (attr.TryGetValue("color", out var fontColor))
#if WINDOWS_UWP
                            newNode.FontColor = ParseCssColor(fontColor);
#else
                            newNode.FontColor = ColorTranslator.FromHtml(fontColor);
#endif
                        if (attr.TryGetValue("size", out var fontSizeStr) && Int32.TryParse(fontSizeStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fontSize))
                            newNode.FontSize = ParseHtmlFontSize(fontSize);
                    }
                    // TODO abbr title -> tooltip
                    break;
                case "table":
                    newNode.Type = MarkupType.Table;
                    // TODO width/height
                    // TODO cellpadding/cellspacing
                    // TODO border
                    break;
                case "dl":
                case "ol":
                case "ul":
                    newNode.Type = MarkupType.List;
                    break;
                case "dd":
                case "dt":
                case "li":
                    newNode.Type = MarkupType.ListItem;
                    break;
                case "tr":
                    newNode.Type = MarkupType.TableRow;
                    if (attr.TryGetValue("bgcolor", out var backColor))
#if WINDOWS_UWP
                        newNode.BackColor = ParseCssColor(backColor);
#else
                        newNode.BackColor = ColorTranslator.FromHtml(backColor);
#endif
                    // TODO valign
                    break;
                case "td":
                case "th":
                    newNode.Type = MarkupType.TableCell;
                    // TODO valign
                    // TODO colspan/rowspan
                    // TODO nowrap
                    break;
                case "br":
                    newNode.Type = MarkupType.Break;
                    break;
                case "hr":
                    newNode.Type = MarkupType.Divider;
                    break;
                case "sup":
                    newNode.Type = MarkupType.Sup;
                    break;
                case "sub":
                    newNode.Type = MarkupType.Sub;
                    break;
                case "source":
                    newNode.Type = MarkupType.Source;
                    if (attr.TryGetValue("src", out var sourceSrc))
                        newNode.Content = sourceSrc;
                    break;
                case "thead": // ignore optional groupings; they carry no useful attributes
                case "tbody":
                case "tfoot":
                    continue;
                case "video":
                    newNode.Type = MarkupType.Video;
                    if (attr.TryGetValue("src", out var videoSrc))
                        newNode.Content = videoSrc;
                    break;
                case "audio":
                    newNode.Type = MarkupType.Audio;
                    if (attr.TryGetValue("src", out var audioSrc))
                        newNode.Content = audioSrc;
                    break;
                case "figure": // TODO add a default margin
                case "figcaption":
                case "p":
                case "div":
                default:
                    newNode.Type = MarkupType.Block;
                    break;
            }

            if (name is "div" or "p" or "td" or "th" or "tr")
            {
                if (attr.TryGetValue("align", out var alignStr) && Enum.TryParse<HtmlAlignment>(alignStr, true, out var align))
                    newNode.Alignment = align;
            }

            if (attr.TryGetValue("style", out var styleString))
            {
                var styles = ParseStyleAttribute(styleString);
                if (styles.TryGetValue("color", out var colorString))
                    newNode.FontColor = ParseCssColor(colorString);
                if (styles.TryGetValue("font-size", out var fontSizeString))
                {
                    var parentFontSize = stack.FirstOrDefault(ancestor => ancestor.FontSize.HasValue)?.FontSize ?? 1.0;
                    if (TryParseCssFontSize(fontSizeString, out var fontSize, parentFontSize))
                        newNode.FontSize = fontSize;
                }
                if (styles.TryGetValue("font-weight", out var fontWeightStr) && ParseCssFontWeight(fontWeightStr) is bool isBold)
                    newNode.IsBold = isBold;
                if (styles.TryGetValue("text-align", out var alignStr) && Enum.TryParse<HtmlAlignment>(alignStr, true, out var align))
                    newNode.Alignment = align;
                if (styles.TryGetValue("background-color", out var backColorString))
                    newNode.BackColor = ParseCssColor(backColorString);
                else if (styles.TryGetValue("background", out var backString))
                    newNode.BackColor = ParseCssColor(backString); // fallback if background-color is not available
                if (styles.TryGetValue("display", out var display) && display == "none")
                    hiddenNodes.Add(newNode);
                // TODO padding?
                // TODO margin?
            }

            if (tokenType == HtmlTokenType.OpenTag)
            {
                if (!hiddenNodes.Contains(newNode))
                    parent.Children.Add(newNode);
                stack.Push(newNode);
            }
            else if (tokenType == HtmlTokenType.CloseTag)
            {
                // Pop until we find a matching opener, or until we reach the root
                while (stack.Count > 1 && stack.Peek().Token?.Name != name)
                    stack.Pop();
                if (stack.Count > 1)
                    stack.Pop();
            }
            else
            {
                if (!hiddenNodes.Contains(newNode))
                    parent.Children.Add(newNode);
            }
        }
        SimplifySubtree(root);
        return root;
    }

    #region Tree Simplification

    // Simplify the document tree to make it easier to map to GUI components.
    // This method tries to remove unnecessary nodes, merge nodes that can be combined, and trim collapsible whitespace.
    private static void SimplifySubtree(MarkupNode parent)
    {
        if (parent.Children.Count == 0)
            return;

        var optimizedChildren = new List<MarkupNode>();

        // Recursively optimize all internal nodes, depth-first.
        foreach (var child in parent.Children)
        {
            SimplifySubtree(child);
            switch (AnalyzeNode(child))
            {
                case NodeAction.None:
                    optimizedChildren.Add(child);
                    break;
                case NodeAction.MergeUp:
                    // Eliminate grandchild, apply its attributes up to the child
                    optimizedChildren.Add(Merge(child, child.Children[0], NodeAction.MergeUp));
                    break;
                case NodeAction.MergeDown:
                    // Eliminate child, apply its attributes down to its grandchildren, then "inline" grandchildren into child's place
                    foreach (var grandchild in child.Children)
                    {
                        optimizedChildren.Add(Merge(child, grandchild, NodeAction.MergeDown));
                    }
                    break;
                case NodeAction.Skip:
                    continue;
            }
        }

        // Remove redundant whitespace and coalesce adjacent text nodes.
        if (IsBlockContainer(parent) || IsInlineContainer(parent))
        {
            // Keep retained nodes in the prefix, leaving unread neighbors intact for whitespace checks.
            // Remove the unused tail once to avoid repeated list shifts.
            int writeIndex = 0;
            for (int readIndex = 0; readIndex < optimizedChildren.Count; readIndex++)
            {
                var child = optimizedChildren[readIndex];
                var prev = writeIndex > 0 ? optimizedChildren[writeIndex - 1] : null;
                if (child.Type == MarkupType.Text && (string.IsNullOrEmpty(child.Content) || child.Content == " "))
                {
                    // A whitespace node is considered redundant if it is adjacent to a blocky node, or immediately follows/precedes whitespace in another text node
                    var next = readIndex < optimizedChildren.Count - 1 ? optimizedChildren[readIndex + 1] : null;
                    if (prev != null && (!IsInlineContainer(prev) && prev.Type != MarkupType.Text || EndsWithSpace(prev)) ||
                        next != null && (!IsInlineContainer(next) && next.Type != MarkupType.Text || StartsWithSpace(next)))
                    {
                        continue;
                    }
                }
                else if (prev != null && EndsWithSpace(prev) && StartsWithSpace(child))
                {
                    // A space that follows another space collapses, also across inline element boundaries.
                    TrimLeadingSpace(child);
                }
                optimizedChildren[writeIndex++] = child;
            }
            optimizedChildren.RemoveRange(writeIndex, optimizedChildren.Count - writeIndex);

            // Coalesce each run of equally styled text into its first node, preserving inline boundaries.
            // Buffer the run so appending text does not repeatedly copy the accumulated prefix.
            writeIndex = 0;
            for (int readIndex = 0; readIndex < optimizedChildren.Count; readIndex++)
            {
                var child = optimizedChildren[readIndex];
                StringBuilder? content = null;
                while (child.Type == MarkupType.Text && readIndex + 1 < optimizedChildren.Count &&
                       optimizedChildren[readIndex + 1].Type == MarkupType.Text && child.HasSameTextAttributes(optimizedChildren[readIndex + 1]))
                {
                    content ??= new StringBuilder(child.Content);
                    content.Append(optimizedChildren[++readIndex].Content);
                }
                if (content != null)
                    child.Content = content.ToString();
                optimizedChildren[writeIndex++] = child;
            }
            optimizedChildren.RemoveRange(writeIndex, optimizedChildren.Count - writeIndex);
        }

        if (IsBlockContainer(parent) && optimizedChildren.Count > 0)
            optimizedChildren = FixWhitespace(optimizedChildren);

        parent.Children.Clear();
        parent.Children.AddRange(optimizedChildren);
    }

    // Trims leading whitespace, trailing whitespace, and the last trailing break from blocky elements.
    private static List<MarkupNode> FixWhitespace(List<MarkupNode> optimizedChildren)
    {
        var newChildren = new List<MarkupNode>();

        // Go over all children and find groups of inline nodes (i.e. IsInlineContainer, Text, or Break).
        // There might be one group, or many groups, or none. Each group should have at least 1 member.
        // Groups are separated by one or more block (non-inline) nodes.
        //
        // Example:
        //      <div>  foo foo <br> <table>...</table>  bar <br><br> </div>
        //           |---group-1---|                  |---group-2---|
        // Becomes:
        //      <div>foo foo<table>...</table>bar <br></div>
        var inlineGroup = new List<MarkupNode>();
        for (int i = 0; i < optimizedChildren.Count; i++)
        {
            var child = optimizedChildren[i];
            if (IsInlineContainer(child) || child.Type == MarkupType.Text || child.Type == MarkupType.Break)
            {
                inlineGroup.Add(child);
            }
            else
            {
                ProcessInlineGroup(); // a blocky child interrupts and ends the current inline group (if any)
                newChildren.Add(child);
            }
        }
        ProcessInlineGroup(); // process the trailing inline group (if any)

        void ProcessInlineGroup()
        {
            if (inlineGroup.Count > 0)
            {
                TrimLeadingWhitespace(inlineGroup);
                TrimTrailingWhitespace(inlineGroup);
                if ((HasContent(inlineGroup) || CountBreaks(inlineGroup) > 1) && TrimLastLineBreak(inlineGroup))
                {
                    // Remove trailing whitespace that was before <br> too
                    TrimTrailingWhitespace(inlineGroup);
                }
                newChildren.AddRange(inlineGroup);
                // End the current group
                inlineGroup.Clear();
            }
        }
        return newChildren;
    }

    private static void TrimLeadingWhitespace(List<MarkupNode> group)
    {
        while (group.Count > 0)
        {
            var firstChild = group[0];
            if (firstChild.Type != MarkupType.Text)
                break;
            firstChild.Content = firstChild.Content?.TrimStart(' ');
            if (!string.IsNullOrEmpty(firstChild.Content))
                break;
            group.RemoveAt(0);
        }
    }

    private static void TrimTrailingWhitespace(List<MarkupNode> group)
    {
        while (group.Count > 0)
        {
            var lastChild = group.Last();
            if (lastChild.Type is MarkupType.Span or MarkupType.Link)
            {
                TrimTrailingWhitespace(lastChild.Children);
                if (lastChild.Children.Count > 0)
                    break;
            }
            else if (lastChild.Type == MarkupType.Text)
            {
                lastChild.Content = lastChild.Content?.TrimEnd(' ');
                if (!string.IsNullOrEmpty(lastChild.Content))
                    break;
            }
            else
            {
                break;
            }
            group.RemoveAt(group.Count - 1);
        }
    }

    // Returns true if a group of nodes contain any content
    // (i.e. anything other than linebreaks and empty spans)
    private static bool HasContent(List<MarkupNode> group)
    {
        foreach (var child in group)
        {
            if (!IsInlineContainer(child) && child.Type != MarkupType.Break)
                return true;
            if (HasContent(child.Children))
                return true;
        }
        return false;
    }

    // Recursively counts the number of descendant nodes of Type Break
    private static int CountBreaks(List<MarkupNode> group)
    {
        int count = 0;
        foreach (var node in group)
        {
            if (node.Type == MarkupType.Break)
                count++;
            else
                count += CountBreaks(node.Children);
        }
        return count;
    }

    // Per HTML spec, last <br> inside a non-empty block element should be ignored
    // See https://stackoverflow.com/a/62523690
    private static bool TrimLastLineBreak(List<MarkupNode> group)
    {
        if (group.Count == 0)
            return false;
        var lastNode = group.Last();
        if (lastNode.Type == MarkupType.Break)
        {
            group.RemoveAt(group.Count - 1);
            return true;
        }
        else if (IsInlineContainer(lastNode))
        {
            // Recurse into inline containers (such as span).
            // From HTML's perspective, 
            return TrimLastLineBreak(lastNode.Children);
        }
        return false;
    }

    // True if the last leaf node in this subtree is a Text node that ends with a space.
    // Stops search and returns false if we encounter a non-inline node.
    private static bool EndsWithSpace(MarkupNode node)
    {
        if (node.Type == MarkupType.Text && node.Content != null)
            return node.Content.EndsWith(" ");
        if (IsInlineContainer(node))
            return node.Children.Count > 0 && EndsWithSpace(node.Children.Last());
        return false;
    }

    // True if the first leaf node in this subtree is a Text node that ends with a space.
    // Stops search and returns false if we encounter a non-inline node.
    private static bool StartsWithSpace(MarkupNode node)
    {
        if (node.Type == MarkupType.Text && node.Content != null)
            return node.Content.StartsWith(" ");
        if (IsInlineContainer(node))
            return node.Children.Count > 0 && StartsWithSpace(node.Children.First());
        return false;
    }

    // Removes the leading space that StartsWithSpace found in the first leaf Text node, and drops the leaf if it becomes empty.
    private static void TrimLeadingSpace(MarkupNode node)
    {
        if (node.Type == MarkupType.Text)
        {
            node.Content = node.Content!.Substring(1);
            return;
        }
        var first = node.Children[0];
        TrimLeadingSpace(first);
        if (first.Type == MarkupType.Text && first.Content!.Length == 0)
            node.Children.RemoveAt(0);
    }

    enum NodeAction { None, MergeUp, MergeDown, Skip }

    private static NodeAction AnalyzeNode(MarkupNode node)
    {
        // Remove insignificant empty nodes (e.g. <b></b>)
        if (node.Children.Count == 0 && (node.Type is MarkupType.Block || IsInlineContainer(node)))
            return NodeAction.Skip;

        // Span nodes without useful attributes can be "inlined" by merging span's children into span's parent.
        if (node.Type == MarkupType.Span && !node.HasAnyTextAttributes)
            return NodeAction.MergeDown;

        // The rest of the analysis only looks at single-child nodes
        if (node.Children.Count != 1)
            return NodeAction.None;
        var child = node.Children[0];

        // Merge an inline node with its single child,
        // e.g. <span><b>foo</b></span> ==> <b>foo</b>
        if (node.Type is MarkupType.Span)
            return NodeAction.MergeDown;

        // Merge a block node with its single blocky child,
        // e.g. <div><table>...</table></div> ==> <table>...</table>
        if (node.Type is MarkupType.Block && child.Type is (MarkupType.List or MarkupType.Table or MarkupType.Block or MarkupType.Divider))
            return NodeAction.MergeDown;

        // Elide a single span inside a block,
        // e.g. <div><span>foo</span></div> ==> <div>foo</div>
        if (node.Type is MarkupType.Block && child.Type is MarkupType.Span)
            return NodeAction.MergeUp;

        return NodeAction.None;
    }

    // Remove the parent and apply its attributes to the child.
    // In case of conflict, child attributes take precedence.
    // New node's type depends on whether we are merging "down" (eliminating a parent) or "up" (eliminating a child).
    private static MarkupNode Merge(MarkupNode parent, MarkupNode child, NodeAction action)
    {
        var newNode = new MarkupNode
        {
            Token = (action == NodeAction.MergeDown) ? child.Token : parent.Token,
            Type = (action == NodeAction.MergeDown) ? child.Type : parent.Type,
            IsBold = child.IsBold ?? parent.IsBold,
            IsItalic = child.IsItalic ?? parent.IsItalic,
            IsUnderline = child.IsUnderline ?? parent.IsUnderline,
            FontSize = child.FontSize ?? parent.FontSize,
            FontColor = child.FontColor ?? parent.FontColor,
            BackColor = child.BackColor ?? parent.BackColor,
            Alignment = child.Alignment ?? parent.Alignment,
            IsMonospace = child.IsMonospace ?? parent.IsMonospace,
            Content = child.Content,
        };
        newNode.Children.AddRange(child.Children);
        return newNode;
    }

    #endregion

    // True if tags of given tokenType cannot have any children/content.
    // https://html.spec.whatwg.org/multipage/syntax.html#void-elements
    private static bool IsVoidElement(string tagName) => tagName is "area" or "base" or "br" or "col" or "embed" or "hr" or "img" or "input" or "link" or "meta" or "source" or "track" or "wbr";

    private static bool CanContainText(string? parentTag) => parentTag is not ("table" or "dl" or "tr" or "ol" or "ul");

    private static bool IsInlineContainer(MarkupNode node) => node.Type is MarkupType.Span or MarkupType.Link or MarkupType.Sub or MarkupType.Sup;

    private static bool IsBlockContainer(MarkupNode node) => node.Type is MarkupType.Document or MarkupType.Block or MarkupType.ListItem or MarkupType.TableCell;

    // Approximation based on https://stackoverflow.com/a/819121
    private static double ParseHtmlFontSize(int fontSize) =>
        fontSize switch
        {
            1 => 0.63,
            2 => 0.82,
            3 => 1.0,
            4 => 1.13,
            5 => 1.5,
            6 => 2.0,
            _ => 3.0, // 7+
        };

    // Parses CSS font-size values relative to the 16px base font size.
    internal static bool TryParseCssFontSize(string fontSizeString, out double emValue, double parentFontSize = 1.0)
    {
        emValue = double.NaN;
        if (string.IsNullOrEmpty(fontSizeString))
            return false;

        var s = fontSizeString.Trim().ToLowerInvariant();

        switch (s)
        {
            case "xx-small": emValue = 0.5; return true;
            case "x-small": emValue = ParseHtmlFontSize(1); return true;
            case "small": emValue = ParseHtmlFontSize(2); return true;
            case "medium": emValue = ParseHtmlFontSize(3); return true;
            case "large": emValue = ParseHtmlFontSize(4); return true;
            case "x-large": emValue = ParseHtmlFontSize(5); return true;
            case "xx-large": emValue = ParseHtmlFontSize(6); return true;
            case "xxx-large": emValue = ParseHtmlFontSize(7); return true;
        }

        // Split number and unit
        int unitStart = s.Length;
        while (unitStart > 0 && (char.IsLetter(s[unitStart - 1]) || s[unitStart - 1] == '%'))
            unitStart--;

        var numberStr = s.Substring(0, unitStart).Trim();
        var unit = s.Substring(unitStart);

        if (!double.TryParse(numberStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            return false;

        // Unit conversion is pragmatic and approximate
        switch (unit)
        {
            case "px":  emValue = value / 16;   return true; // 1em == 16px
            case "pt":  emValue = value / 12;   return true; // 1em == 12pt
            case "em":  emValue = value * parentFontSize; return true;
            case "rem": emValue = value;        return true;
            case "%":   emValue = value / 100 * parentFontSize; return true;
            default:
                return false;
        }
    }

    // Parses the value of CSS font-weight into a boolean. Returns null for invalid values.
    internal static bool? ParseCssFontWeight(string rawValue)
    {
        if (string.IsNullOrEmpty(rawValue))
            return null;

        string normalizedValue = rawValue.Trim().ToLowerInvariant();
        switch (normalizedValue)
        {
            case "normal":
            case "lighter":
                return false;
            case "bold":
            case "bolder":
                return true;
            default:
                if (int.TryParse(normalizedValue, NumberStyles.None, CultureInfo.InvariantCulture, out int numericValue) && numericValue > 0)
                {
                    // Any numeric value greater than 400 represents a bolder-than-normal font weight
                    return numericValue > 400;
                }
                return null;
        }
    }

    /// <summary>
    /// Parses CSS color values into <see cref="Color"/> instances.
    /// </summary>
    /// <param name="cssColor">Value of a CSS color property.</param>
    /// <returns>Color or null if the given value could not be parsed.</returns>
    internal static Color? ParseCssColor(string? cssColor)
    {
        if (cssColor is null || string.IsNullOrWhiteSpace(cssColor))
            return null;

        cssColor = cssColor.Trim().ToLowerInvariant();

        if (cssColor[0] == '#')
            return ParseHexCssColor(cssColor);
        else if (cssColor.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            return ParseRgbColor(cssColor);
        else
            return ParseNamedCssColor(cssColor);
    }

    private static readonly Regex RgbRegex = new Regex(
        @"^rgba?\(\s*" +
          @"(\d{1,3})[,\s]\s*" +        // R
          @"(\d{1,3})[,\s]\s*" +        // G
          @"(\d{1,3})" +                // B
          @"(?:[,\s]\s*(?:\/\s*)?(\d*\.?\d+))?" + // optional alpha
        @"\s*\)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    );

    private static Color? ParseRgbColor(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var m = RgbRegex.Match(input.Trim());
        if (!m.Success)
            return null;

        // Helper to parse and clamp 0-255
        static int ParseClamp8(string v)
        {
            if (int.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out int i))
                return Math.Min(255, Math.Max(0, i));
            return 0;
        }

        int r = ParseClamp8(m.Groups[1].Value);
        int g = ParseClamp8(m.Groups[2].Value);
        int b = ParseClamp8(m.Groups[3].Value);

        double aFrac = 1.0;
        if (m.Groups[4].Success && double.TryParse(m.Groups[4].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out aFrac))
            aFrac = Math.Min(1.0, Math.Max(0.0, aFrac)); // Clamp to 0-1

        byte a = (byte)Math.Round(aFrac * 255);
        return Color.FromArgb(a, r, g, b);
    }

    private static Color? ParseHexCssColor(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor) || hexColor[0] != '#' || (hexColor.Length != 4 && hexColor.Length != 5 && hexColor.Length != 7 && hexColor.Length != 9))
            return null;

        int r, g, b, a = 255;
        try
        {
            if (hexColor.Length < 6)
            {
                // #RGB
                r = int.Parse(hexColor[1].ToString() + hexColor[1], NumberStyles.HexNumber);
                g = int.Parse(hexColor[2].ToString() + hexColor[2], NumberStyles.HexNumber);
                b = int.Parse(hexColor[3].ToString() + hexColor[3], NumberStyles.HexNumber);

                // #RGBA
                if (hexColor.Length == 5)
                    a = int.Parse(hexColor[4].ToString() + hexColor[4], NumberStyles.HexNumber);
            }
            else
            {
                // #RRGGBB
                r = int.Parse(hexColor.Substring(1, 2), NumberStyles.HexNumber);
                g = int.Parse(hexColor.Substring(3, 2), NumberStyles.HexNumber);
                b = int.Parse(hexColor.Substring(5, 2), NumberStyles.HexNumber);

                // #RRGGBBAA
                if (hexColor.Length == 9)
                    a = int.Parse(hexColor.Substring(7, 2), NumberStyles.HexNumber);
            }
            return Color.FromArgb(a, r, g, b);
        }
        catch
        {
            return null;
        }
    }

    private static Color? ParseNamedCssColor(string colorName)
    {
        return colorName.ToLowerInvariant() switch
        {
            // Current as of CSS4, list based on https://developer.mozilla.org/en-US/docs/Web/CSS/named-color
            "black" => Color.Black,
            "silver" => Color.FromArgb(192, 192, 192),
            "gray" => Color.FromArgb(128, 128, 128),
            "white" => Color.White,
            "maroon" => Color.FromArgb(128, 0, 0),
            "red" => Color.FromArgb(255, 0, 0),
            "purple" => Color.FromArgb(128, 0, 128),
            "fuchsia" => Color.FromArgb(255, 0, 255),
            "green" => Color.FromArgb(0, 128, 0),
            "lime" => Color.FromArgb(0, 255, 0),
            "olive" => Color.FromArgb(128, 128, 0),
            "yellow" => Color.FromArgb(255, 255, 0),
            "navy" => Color.FromArgb(0, 0, 128),
            "blue" => Color.FromArgb(0, 0, 255),
            "teal" => Color.FromArgb(0, 128, 128),
            "aqua" => Color.FromArgb(0, 255, 255),
            "orange" => Color.FromArgb(255, 165, 0),
            "aliceblue" => Color.FromArgb(240, 248, 255),
            "antiquewhite" => Color.FromArgb(250, 235, 215),
            "aquamarine" => Color.FromArgb(127, 255, 212),
            "azure" => Color.FromArgb(240, 255, 255),
            "beige" => Color.FromArgb(245, 245, 220),
            "bisque" => Color.FromArgb(255, 228, 196),
            "blanchedalmond" => Color.FromArgb(255, 235, 205),
            "blueviolet" => Color.FromArgb(138, 43, 226),
            "brown" => Color.FromArgb(165, 42, 42),
            "burlywood" => Color.FromArgb(222, 184, 135),
            "cadetblue" => Color.FromArgb(95, 158, 160),
            "chartreuse" => Color.FromArgb(127, 255, 0),
            "chocolate" => Color.FromArgb(210, 105, 30),
            "coral" => Color.FromArgb(255, 127, 80),
            "cornflowerblue" => Color.FromArgb(100, 149, 237),
            "cornsilk" => Color.FromArgb(255, 248, 220),
            "crimson" => Color.FromArgb(220, 20, 60),
            "cyan" => Color.FromArgb(0, 255, 255),
            "darkblue" => Color.FromArgb(0, 0, 139),
            "darkcyan" => Color.FromArgb(0, 139, 139),
            "darkgoldenrod" => Color.FromArgb(184, 134, 11),
            "darkgray" => Color.FromArgb(169, 169, 169),
            "darkgreen" => Color.FromArgb(0, 100, 0),
            "darkkhaki" => Color.FromArgb(189, 183, 107),
            "darkmagenta" => Color.FromArgb(139, 0, 139),
            "darkolivegreen" => Color.FromArgb(85, 107, 47),
            "darkorange" => Color.FromArgb(255, 140, 0),
            "darkorchid" => Color.FromArgb(153, 50, 204),
            "darkred" => Color.FromArgb(139, 0, 0),
            "darksalmon" => Color.FromArgb(233, 150, 122),
            "darkseagreen" => Color.FromArgb(143, 188, 143),
            "darkslateblue" => Color.FromArgb(72, 61, 139),
            "darkslategray" => Color.FromArgb(47, 79, 79),
            "darkturquoise" => Color.FromArgb(0, 206, 209),
            "darkviolet" => Color.FromArgb(148, 0, 211),
            "deeppink" => Color.FromArgb(255, 20, 147),
            "deepskyblue" => Color.FromArgb(0, 191, 255),
            "dimgray" => Color.FromArgb(105, 105, 105),
            "dimgrey" => Color.FromArgb(105, 105, 105),
            "dodgerblue" => Color.FromArgb(30, 144, 255),
            "firebrick" => Color.FromArgb(178, 34, 34),
            "floralwhite" => Color.FromArgb(255, 250, 240),
            "forestgreen" => Color.FromArgb(34, 139, 34),
            "gainsboro" => Color.FromArgb(220, 220, 220),
            "ghostwhite" => Color.FromArgb(248, 248, 255),
            "gold" => Color.FromArgb(255, 215, 0),
            "goldenrod" => Color.FromArgb(218, 165, 32),
            "greenyellow" => Color.FromArgb(173, 255, 47),
            "grey" => Color.FromArgb(128, 128, 128),
            "honeydew" => Color.FromArgb(240, 255, 240),
            "hotpink" => Color.FromArgb(255, 105, 180),
            "indianred" => Color.FromArgb(205, 92, 92),
            "indigo" => Color.FromArgb(75, 0, 130),
            "ivory" => Color.FromArgb(255, 255, 240),
            "khaki" => Color.FromArgb(240, 230, 140),
            "lavender" => Color.FromArgb(230, 230, 250),
            "lavenderblush" => Color.FromArgb(255, 240, 245),
            "lawngreen" => Color.FromArgb(124, 252, 0),
            "lemonchiffon" => Color.FromArgb(255, 250, 205),
            "lightblue" => Color.FromArgb(173, 216, 230),
            "lightcoral" => Color.FromArgb(240, 128, 128),
            "lightcyan" => Color.FromArgb(224, 255, 255),
            "lightgoldenrodyellow" => Color.FromArgb(250, 250, 210),
            "lightgray" => Color.FromArgb(211, 211, 211),
            "lightgreen" => Color.FromArgb(144, 238, 144),
            "lightgrey" => Color.FromArgb(211, 211, 211),
            "lightpink" => Color.FromArgb(255, 182, 193),
            "lightsalmon" => Color.FromArgb(250, 128, 114),
            "lightseagreen" => Color.FromArgb(32, 178, 170),
            "lightskyblue" => Color.FromArgb(135, 206, 250),
            "lightslategray" or "lightslategrey" => Color.FromArgb(119, 136, 153),
            "lightsteelblue" => Color.FromArgb(176, 196, 222),
            "lightyellow" => Color.FromArgb(255, 255, 224),
            "limegreen" => Color.FromArgb(50, 205, 50),
            "linen" => Color.FromArgb(250, 240, 230),
            "magenta" => Color.FromArgb(255, 0, 255),
            "mediumaquamarine" => Color.FromArgb(102, 205, 170),
            "mediumblue" => Color.FromArgb(0, 0, 205),
            "mediumorchid" => Color.FromArgb(186, 85, 211),
            "mediumpurple" => Color.FromArgb(147, 112, 219),
            "mediumseagreen" => Color.FromArgb(60, 179, 113),
            "mediumslateblue" => Color.FromArgb(123, 104, 238),
            "mediumspringgreen" => Color.FromArgb(0, 250, 154),
            "mediumturquoise" => Color.FromArgb(72, 209, 204),
            "mediumvioletred" => Color.FromArgb(199, 21, 133),
            "midnightblue" => Color.FromArgb(25, 25, 112),
            "mintcream" => Color.FromArgb(245, 255, 250),
            "mistyrose" => Color.FromArgb(255, 228, 225),
            "moccasin" => Color.FromArgb(255, 228, 181),
            "navajowhite" => Color.FromArgb(255, 222, 173),
            "oldlace" => Color.FromArgb(253, 245, 230),
            "olivedrab" => Color.FromArgb(107, 142, 35),
            "orangered" => Color.FromArgb(255, 69, 0),
            "orchid" => Color.FromArgb(218, 112, 214),
            "palegoldenrod" => Color.FromArgb(238, 232, 170),
            "palegreen" => Color.FromArgb(152, 251, 152),
            "paleturquoise" => Color.FromArgb(175, 238, 238),
            "palevioletred" => Color.FromArgb(219, 112, 147),
            "papayawhip" => Color.FromArgb(255, 239, 213),
            "peachpuff" => Color.FromArgb(255, 218, 185),
            "peru" => Color.FromArgb(205, 133, 63),
            "pink" => Color.FromArgb(255, 192, 203),
            "plum" => Color.FromArgb(221, 160, 221),
            "powderblue" => Color.FromArgb(176, 224, 230),
            "rosybrown" => Color.FromArgb(188, 143, 143),
            "royalblue" => Color.FromArgb(65, 105, 225),
            "saddlebrown" => Color.FromArgb(139, 69, 19),
            "salmon" => Color.FromArgb(250, 128, 114),
            "sandybrown" => Color.FromArgb(244, 164, 96),
            "seagreen" => Color.FromArgb(46, 139, 87),
            "seashell" => Color.FromArgb(255, 245, 238),
            "sienna" => Color.FromArgb(160, 82, 45),
            "skyblue" => Color.FromArgb(135, 206, 235),
            "slateblue" => Color.FromArgb(106, 90, 205),
            "slategray" or "slategrey" => Color.FromArgb(112, 128, 144),
            "snow" => Color.FromArgb(255, 250, 250),
            "springgreen" => Color.FromArgb(0, 255, 127),
            "steelblue" => Color.FromArgb(70, 130, 180),
            "tan" => Color.FromArgb(210, 180, 140),
            "thistle" => Color.FromArgb(216, 191, 216),
            "transparent" => Color.Transparent,
            "tomato" => Color.FromArgb(255, 99, 71),
            "turquoise" => Color.FromArgb(64, 224, 208),
            "violet" => Color.FromArgb(238, 130, 238),
            "wheat" => Color.FromArgb(245, 222, 179),
            "whitesmoke" => Color.FromArgb(245, 245, 245),
            "yellowgreen" => Color.FromArgb(154, 205, 50),
            "rebeccapurple" => Color.FromArgb(102, 51, 153),
            _ => null
        };
    }

    // Returns a list of tags that can implicitly close the given tag
    // https://html.spec.whatwg.org/multipage/syntax.html#syntax-tag-omission
    private static readonly Dictionary<string, string[]> _optionalClosers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["p"] = new[] { "address", "article", "aside", "blockquote", "details", "dialog",
                        "div", "dl", "fieldset", "figcaption", "figure", "footer", "form",
                        "h1", "h2", "h3", "h4", "h5", "h6", "header", "hgroup", "hr", "main",
                        "menu", "nav", "ol", "ul", "pre", "search", "section", "table", "p" },
        ["li"] = new[] { "li" },
        ["dt"] = new[] { "dt", "dd" },
        ["dd"] = new[] { "dt", "dd" },
        ["rt"] = new[] { "rt", "rp" },
        ["rp"] = new[] { "rt", "rp" },
        ["optgroup"] = new[] { "optgroup", "hr" },
        ["option"] = new[] { "option", "optgroup", "hr" },
        ["thead"] = new[] { "tbody", "tfoot" },
        ["tbody"] = new[] { "tbody", "tfoot" },
        ["tfoot"] = new[] { "tbody" },
        ["tr"] = new[] { "tr" },
        ["td"] = new[] { "td", "th" },
        ["th"] = new[] { "td", "th" },
        // Not in the spec, but de-facto browser behavior: anchors cannot be nested
        ["a"] = new[] { "a" },
    };

    // Returns the stack depth of the open element that <incoming> closes implicitly, or null if there is none.
    // Only open inline elements may lie above it.
    private static int? FindImplicitlyClosed(Stack<MarkupNode> stack, string incoming)
    {
        int depth = 0;
        foreach (var open in stack)
        {
            if (open.Token?.Name is string openName && ShouldImplicitlyClose(openName, incoming))
                return depth;
            if (!IsInlineContainer(open))
                return null;
            depth++;
        }
        return null;
    }

    /// <summary>Returns <c>true</c> if <paramref name="incoming"/> closes <paramref name="openTag"/> implicitly.</summary>
    private static bool ShouldImplicitlyClose(string openTag, string incoming)
    {
        return _optionalClosers.TryGetValue(openTag, out var triggers) &&
               triggers.Contains(incoming, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Splits given HTML snippet string into tokens.
/// </summary>
internal class HtmlTokenParser
{
    private readonly string _html;
    private int _idx = 0;

    // RawText parsing is needed to skip elements like script or style.
    private bool _rawParsingMode = false;
    // When in RawText mode, we need to remember which end-tag we are looking for (e.g. "script")
    private string? _expectedEndTag = null;

    public HtmlTokenParser(string html)
    {
        _html = html ?? throw new ArgumentNullException(nameof(html));
    }

    /// <summary>
    /// Attempts to retrieve the next token from the HTML stream.
    /// </summary>
    /// <param name="token">The output token, if one is found.</param>
    /// <returns>True if a token was found, false otherwise.</returns>
    public bool NextToken([NotNullWhen(true)] out HtmlToken token)
    {
        token = default;
        while (_idx < _html.Length) // Loop to skip comments or handle empty text results
        {
            // Comments start with <!-- and end with -->
            if (_html[_idx] == '<' && _idx + 3 < _html.Length && _html[_idx + 1] == '!' && _html[_idx + 2] == '-' && _html[_idx + 3] == '-')
            {
                int endCommentIdx = _html.IndexOf("-->", _idx + 4, StringComparison.Ordinal);
                if (endCommentIdx != -1)
                {
                    _idx = endCommentIdx + 3;
                    continue; // Skip comment and restart loop
                }
                else
                {
                    // Unclosed comment (i.e. the rest of HTML is commented out), stop parsing
                    _idx = _html.Length;
                    return false;
                }
            }

            if (_rawParsingMode && TryParseRawText(out token))
                return true;
            if (!_rawParsingMode && TryParseNormal(out token))
                return true;

            // If TryParse returned false, it either skipped something (like empty text after processing)
            // or reached the end. Loop will continue unless we actually reached the end.
        }
        return false; // End of string reached
    }

    // Parses the next token when in Normal mode (tags or text).
    private bool TryParseNormal([NotNullWhen(true)] out HtmlToken token)
    {
        token = default;
        if (_idx >= _html.Length) return false;

        // Check if we have a '<' that is actually the start/end of a tag.
        if (_html[_idx] == '<' && LooksLikeTag(_idx))
            return TryParseTag(out token);

        // Otherwise, scan forward to the next real tag-start.
        int endOfText;
        int searchPos = _idx;
        while (true)
        {
            int nextLt = _html.IndexOf('<', searchPos);
            if (nextLt == -1)
            {
                endOfText = _html.Length;
                break;
            }
            if (LooksLikeTag(nextLt))
            {
                endOfText = nextLt;
                break;
            }
            // stray '<', skip it
            searchPos = nextLt + 1;
        }

        // Extract and process the text run
        string rawText = _html.Substring(_idx, endOfText - _idx);
        _idx = endOfText;
        string processedText = ProcessNormalText(rawText);

        if (!string.IsNullOrEmpty(processedText))
        {
            token = new HtmlToken(processedText, null, HtmlTokenType.PlainText);
            return true;
        }

        // collapsed to empty text, go back to looping
        return false;
    }

    // Checks if a '<' at the given position is actually the start of a tag/comment,
    // or just a stray character that should be treated as text.
    private bool LooksLikeTag(int pos)
    {
        // must have at least one character after '<'
        if (pos + 1 >= _html.Length) return false;

        char after = _html[pos + 1];

        if (char.IsLetter(after) || after == '!')
            return true; // open tag: <letter or comment

        if (after == '/' && pos + 2 < _html.Length && char.IsLetter(_html[pos + 2]))
            return true; // close tag: </letter

        return false;
    }

    // Attempts to parse a tag (open, close, or self-closing) starting at the current index.
    // Also handles state transitions into RawText mode.
    private bool TryParseTag([NotNullWhen(true)] out HtmlToken token)
    {
        token = default;
        int startTagContentIdx = _idx + 1;
        if (startTagContentIdx >= _html.Length)
        {
            // Dangling '<' at end. Consume it and stop
            _idx++;
            return false;
        }

        // Find the end of the tag
        int endTagCloseIdx = FindTagEnd(startTagContentIdx);
        if (endTagCloseIdx == -1)
        {
            // Unclosed tag. Consume the rest of the string and stop
            _idx = _html.Length;
            return false;
        }

        HtmlTokenType type;
        bool isSelfClosing = false;
        int nameEndIdx = endTagCloseIdx; // End boundary for name/attributes

        // Check for closing tag: </tag>
        if (_html[startTagContentIdx] == '/')
        {
            startTagContentIdx++; // Skip '/'
            if (startTagContentIdx >= endTagCloseIdx) // Handles </>
            {
                _idx = endTagCloseIdx + 1; // Skip invalid tag
                return false; // Let main loop continue
            }
            type = HtmlTokenType.CloseTag;
        }
        // Check for self-closing tag: <tag/> or <tag />
        else if (_html[endTagCloseIdx - 1] == '/')
        {
            nameEndIdx--; // Exclude the '/' from name/attribute processing
            type = HtmlTokenType.SelfClosingTag;
            isSelfClosing = true;
            // Trim trailing whitespace before the '/' e.g. <br />
            while (nameEndIdx > startTagContentIdx && char.IsWhiteSpace(_html[nameEndIdx - 1]))
            {
                nameEndIdx--;
            }
        }
        else
        {
            type = HtmlTokenType.OpenTag;
        }

        // Extract tag name (first word)
        int nameLength = 0;
        while (startTagContentIdx + nameLength < nameEndIdx &&
               !char.IsWhiteSpace(_html[startTagContentIdx + nameLength]) &&
               _html[startTagContentIdx + nameLength] != '/') // Stop name at '/' for <tag/> case without space
        {
            nameLength++;
        }

        if (nameLength == 0) // Handles <> or < > or < /> etc.
        {
            _idx = endTagCloseIdx + 1; // Skip invalid tag
            return false; // Let main loop continue
        }

        string name = _html.Substring(startTagContentIdx, nameLength);

        // Extract attributes
        int attributeStartIdx = startTagContentIdx + nameLength;
        while (attributeStartIdx < nameEndIdx && char.IsWhiteSpace(_html[attributeStartIdx]))
        {
            attributeStartIdx++; // Skip whitespace after name
        }

        string? attributes = null;
        if (attributeStartIdx < nameEndIdx)
        {
            attributes = _html.Substring(attributeStartIdx, nameEndIdx - attributeStartIdx).Trim();
            if (string.IsNullOrEmpty(attributes))
                attributes = null;
        }

        _idx = endTagCloseIdx + 1; // Update index past the '>'

        string lowerName = name.ToLowerInvariant();
        token = new HtmlToken(lowerName, attributes, type);

        if (type == HtmlTokenType.OpenTag && !isSelfClosing) // Only non-self-closing tags can contain content
        {
            if (lowerName is "script" or "style")
            {
                _rawParsingMode = true;
                _expectedEndTag = lowerName;
                // TryParseRawText will reset the mode just before the end tag
            }
        }
        return true;
    }

    // Finds end of the tag using HTML's more-forgiving-than-XML rules.
    // Unescaped angle brackets are allowed as long as they are within quoted attribute values.
    // A quote opens a value only right after '='. Elsewhere it is literal, as in class=O'Brien.
    // See https://html.spec.whatwg.org/multipage/parsing.html#before-attribute-value-state
    private int FindTagEnd(int from)
    {
        char quote = '\0';
        bool afterEquals = false;
        for (int i = from; i < _html.Length; i++)
        {
            char c = _html[i];
            if (quote != '\0') { if (c == quote) quote = '\0'; }
            else if (c == '>') return i;
            else if (c == '=') afterEquals = true;
            else if (afterEquals && (c == '\'' || c == '"')) { quote = c; afterEquals = false; }
            else if (!char.IsWhiteSpace(c)) afterEquals = false;
        }
        // An unterminated quoted value would swallow the rest of the document, so end the tag at the first '>'.
        return _html.IndexOf('>', from);
    }

    // Parses content when in RawText mode, that treats content as literal characters until the specific end tag,
    // and does not interpret entities. Returns the content as a single PlainText token.
    // See https://www.w3.org/TR/2010/WD-html5-20101019/syntax.html#raw-text-elements
    private bool TryParseRawText([NotNullWhen(true)] out HtmlToken token)
    {
        Debug.Assert(_expectedEndTag != null, "Raw parsing mode should have an expected end tag.");

        string endTag = $"</{_expectedEndTag}>";
        // Find the end tag using case-insensitive search
        int endTagIdx = _html.IndexOf(endTag, _idx, StringComparison.OrdinalIgnoreCase);

        string rawText;
        if (endTagIdx == -1)
        {
            // End tag not found, consume the rest of the string
            rawText = _html.Substring(_idx);
            _idx = _html.Length;
            _rawParsingMode = false; // Reset state as we hit the end
            _expectedEndTag = null;
        }
        else
        {
            // End tag found, extract text before it
            rawText = _html.Substring(_idx, endTagIdx - _idx);
            _idx = endTagIdx; // Position index at the START of the end tag
            _rawParsingMode = false; // Reset state BEFORE parsing the end tag next
            _expectedEndTag = null;
        }

        // Do NOT process entities or whitespace for RAWTEXT.
        token = new HtmlToken(rawText, null, HtmlTokenType.PlainText);
        return true;
    }

    private static readonly Regex WhitespaceRegex = new Regex(@"[^\S\u00A0]+", RegexOptions.Compiled);

    private static string ProcessNormalText(string rawText)
    {
        // Replace HTML entities with their equivalent symbols.
        var unescaped = HtmlUtility.UnescapeHtml(rawText);
        // Collapse internal whitespace sequences to a single space, preserving nbsp
        return WhitespaceRegex.Replace(unescaped, " ");
    }
}

internal readonly record struct HtmlToken
{
    public HtmlToken(string name, string? attributes, HtmlTokenType type)
    {
        Name = name;
        Attributes = attributes;
        Type = type;
    }

    public string Name { get; }
    public string? Attributes { get; }
    public HtmlTokenType Type { get; }

    public override readonly string ToString()
    {
        var attrString = Attributes == null ?
            "null" :
            '"' + Attributes + '"';
        return $"HtmlToken({Name}, {attrString}, {Type})";
    }
}

internal enum HtmlTokenType : byte
{
    PlainText,
    OpenTag,
    CloseTag,
    SelfClosingTag,
}

internal enum HtmlAlignment : byte
{
    Left,
    Right,
    Center,
    Justify,
}
