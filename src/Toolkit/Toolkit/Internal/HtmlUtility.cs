#if WPF
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Linq;
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
    Text
}

/// <summary>
/// Represents a semantic node in a parsed HTML document.
/// </summary>
internal class MarkupNode
{
    public HtmlToken? Token { get; set; }
    public MarkupType Type { get; set; }
    public bool? IsBold { get; set; }
    public bool? IsItalic { get; set; }
    public bool? IsUnderline { get; set; }
    public double? FontSize { get; set; } // em
    public Color? FontColor { get; set; }
    public Color? BackColor { get; set; }
    public HtmlAlignment? Alignment { get; set; }
    public IList<MarkupNode> Children { get; } = new List<MarkupNode>();
    public string? Content { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder("MarkupNode{");
        sb.Append(Type);
        if (IsBold.HasValue)
            sb.Append(" bold=" + IsBold.Value);
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
            sb.Append(" text=" + Content);
        if (Children.Any())
            sb.Append(" children=" + Children.Count);
        sb.Append('}');
        return sb.ToString();
    }
}

/// <summary>
/// Provides methods for parsing HTML documents and attributes.
/// </summary>
internal class HtmlUtility
{
    /// <summary>
    /// Parses a string with HTML attribute declarations into a dictionary.
    /// If the same key is declared multiple times, the last value is used.
    /// </summary>
    /// <param name="attrString">Raw attribute declarations.</param>
    /// <returns>A dictionary of attribute-names to unescaped attribute-values.</returns>
    internal static Dictionary<string, string> ParseAttributes(string? attrString)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(attrString))
            return result;

        var attributeRegex = new Regex(@"([\w-]+)\s*(=\s*('([^']+)'|""([^""]+)""|([^'""\s]+)))?\s*");
        var matches = attributeRegex.Matches(attrString);

        foreach (Match match in matches)
        {
            string attrName = match.Groups[1].Value;
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
        var regex = new Regex(@"&(?:#(?<numeric>\d+)|#x(?<hex>[0-9a-fA-F]+)|(?<named>\w+));");
        var result = regex.Replace(input, m =>
        {
            if (m.Groups["numeric"].Success) // Decimal character reference
            {
                var code = int.Parse(m.Groups[1].Value);
                return char.ConvertFromUtf32(code);
            }
            else if (m.Groups["hex"].Success) // Hexadecimal character reference
            {
                var code = int.Parse(m.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
                return char.ConvertFromUtf32(code);
            }
            else // Named character reference
            {
                var entity = m.Groups["named"].Value;
                return entity switch
                {
                    // XML predefined entities
                    "quot" => "\"",
                    "amp" => "&",
                    "apos" => "'",
                    "lt" => "<",
                    "gt" => ">",
                    // Common HTML entities used for typography
                    "nbsp" => " ",
                    "copy" => "\xA9",
                    "deg" => "\xB0",
                    "para" => "\xB6",
                    "pound" => "\xA3",
                    "mdash" => "\u2014",
                    "euro" => "\u20AC",
                    "trade" => "\u2122",
                    "ne" => "\u2260",
                    // Add more named character references if necessary
                    // https://html.spec.whatwg.org/multipage/named-characters.html#named-character-references
                    _ => m.Value,
                };
            }
        });

        return result;
    }

    /// <summary>
    /// Parses a string with CSS declarations into a dictionary.
    /// If the same key is declared multiple times, the last value is used.
    /// </summary>
    /// <param name="styleAttribute">Unescaped value of the "style" attribute.</param>
    /// <returns>A dictionary of property-names to property-values.</returns>
    internal static IDictionary<string, string> ParseStyleAttribute(string styleAttribute)
    {
        var styles = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(styleAttribute))
            return styles;

        foreach (var declaration in styleAttribute.Split(';'))
        {
            var parts = declaration.Split(':');
            if (parts.Length == 2)
                styles[parts[0].Trim().ToLower()] = parts[1].Trim(); // store all keys in lowercase
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
        var root = new MarkupNode { Type = MarkupType.Document };
        stack.Push(root);
        var tokenator = new HtmlTokenParser(snippet);

        bool isInParagraph = false;

        while (tokenator.NextToken(out HtmlToken? t))
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
            bool isBlock = IsBlockContent(name);

            if (tokenType == HtmlTokenType.CloseTag && isVoid) // Ignore unnecessary closing tags like </img>
                continue;

            if (tokenType == HtmlTokenType.OpenTag && isVoid) // Automatically close tags that cannot have any content (like <br>)
                tokenType = HtmlTokenType.SelfClosingTag;

            if (isInParagraph && isBlock && tokenType != HtmlTokenType.CloseTag) // Start of any block content implicitly closes the nearest paragraph
            {
                while (stack.Peek().Token?.Name != "p")
                    stack.Pop();
                stack.Pop();
                parent = stack.Peek();
                isInParagraph = false;
            }
            // TODO handle unclosed elements other than "p"
            // static bool IsClosingOptional(string tagName) => tagName is "p" or "dt" or "dd" or "li" or "th" or "tbody" or "tr" or "td";

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
                    newNode.Type = MarkupType.Span;
                    if (name == "b" || name == "strong")
                        newNode.IsBold = true;
                    else if (name == "i" || name == "em")
                        newNode.IsItalic = true;
                    else if (name == "u")
                        newNode.IsUnderline = true;
                    else if (name == "font")
                    {
                        if (attr.TryGetValue("color", out var fontColor))
                            newNode.FontColor = ColorTranslator.FromHtml(fontColor);
                        if (attr.TryGetValue("size", out var fontSizeStr) && Int32.TryParse(fontSizeStr, out var fontSize))
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
                        newNode.BackColor = ColorTranslator.FromHtml(backColor);
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
                case "source": // ignore; we don't support embedded audio or video.
                case "thead": // ignore optional groupings; they carry no useful attributes
                case "tbody": // ignore optional groupings; they carry no useful attributes
                    continue;
                case "video": // just use fallback content
                case "audio": // just use fallback content
                case "figure": // TODO add a default margin
                case "figcaption":
                case "p":
                case "div":
                default:
                    newNode.Type = MarkupType.Block;
                    break;
            }

            if (name is "div" or "td" or "th" or "tr")
            {
                if (!attr.TryGetValue("align", out var alignStr) && Enum.TryParse<HtmlAlignment>(alignStr, true, out var align))
                    newNode.Alignment = align;
            }

            if (attr.TryGetValue("style", out var styleString))
            {
                var styles = ParseStyleAttribute(styleString);
                if (styles.TryGetValue("color", out var colorString))
                    newNode.FontColor = ParseCssColor(colorString);
                if (styles.TryGetValue("font-size", out var fontSizeString) && TryParseCssFontSize(fontSizeString, out var fontSize))
                    newNode.FontSize = fontSize;
                if (styles.TryGetValue("text-align", out var alignStr) && Enum.TryParse<HtmlAlignment>(alignStr, true, out var align))
                    newNode.Alignment = align;
                if (styles.TryGetValue("background-color", out var backColorString))
                    newNode.BackColor = ParseCssColor(backColorString);
                else if (styles.TryGetValue("background", out var backString))
                    newNode.BackColor = ParseCssColor(backString); // fallback if background-color is not available
                // TODO padding?
                // TODO margin?
            }

            if (tokenType == HtmlTokenType.OpenTag)
            {
                parent.Children.Add(newNode);
                stack.Push(newNode);
                if (t.Name == "p")
                    isInParagraph = true;
            }
            else if (tokenType == HtmlTokenType.CloseTag)
            {
                while (stack.Any() && stack.Peek().Token?.Name != t.Name)
                {
                    // The close tags must be out-of-order! Skip towards the root until we find a matching parent.
                    var popped = stack.Pop();
                    if (popped.Token?.Name == "p")
                        isInParagraph = false;
                }
                if (!stack.Any())
                {
                    // We walked all the way up to root, no more nodes left to close. Give up and return what we have so far.
                    RemoveTrailingBreaks(root);
                    return root;
                }
                var openTag = stack.Pop();
                if (openTag.Token?.Name == "p")
                    isInParagraph = false;
            }
            else
            {
                parent.Children.Add(newNode);
            }
        }
        RemoveTrailingBreaks(root);
        return root;
    }

    private static void RemoveTrailingBreaks(MarkupNode node, bool isLastChildInBlock = false)
    {
        // Per HTML spec, last <br> inside a non-empty block element should be ignored
        // See https://stackoverflow.com/a/62523690
        if (node.Children.Count == 0)
            return;
        var lastChild = node.Children.Last();

        bool thisIsBlock = node.Type is MarkupType.Document or MarkupType.ListItem or MarkupType.TableCell or MarkupType.Block;
        if ((isLastChildInBlock || thisIsBlock)
            && node.Children.Count > 1
            && lastChild.Type == MarkupType.Break)
        {
            node.Children.RemoveAt(node.Children.Count - 1);
        }
        foreach (var child in node.Children)
        {
            bool isLast = ReferenceEquals(child, lastChild);
            RemoveTrailingBreaks(child, isLast && (thisIsBlock || isLastChildInBlock));
        }
    }

    // True if tags of given tokenType cannot have any children/content.
    private static bool IsVoidElement(string tagName) => tagName is "br" or "hr" or "img" or "source";

    private static bool CanContainText(string? parentTag) => parentTag is not ("table" or "dl" or "tr" or "ol" or "ul");

    private static bool IsBlockContent(string tagName) => tagName is "div" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or "figure" or "ol" or "ul" or "dl" or "hr" or "table" or "p";

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

    // Parses CSS font-size values into em units.
    private static bool TryParseCssFontSize(string fontSizeString, out double emValue)
    {
        emValue = double.NaN;
        if (string.IsNullOrEmpty(fontSizeString))
            return false;

        fontSizeString = fontSizeString.Trim().ToLowerInvariant();
        if (fontSizeString.EndsWith("px"))
        {
            if (double.TryParse(fontSizeString.Substring(0, fontSizeString.Length - 2), out var pxValue))
            {
                emValue = pxValue / 16d; // 1em == 16px (approx)
                return true;
            }
        }
        emValue = fontSizeString switch
        {
            "xx-small" => 0.5,
            "x-small" => ParseHtmlFontSize(1),
            "small" => ParseHtmlFontSize(2),
            "medium" => ParseHtmlFontSize(3),
            "large " => ParseHtmlFontSize(4),
            "x-large" => ParseHtmlFontSize(5),
            "xx-large" => ParseHtmlFontSize(6),
            "xxx-large" => ParseHtmlFontSize(7),
            _ => double.NaN
        };

        return !double.IsNaN(emValue);
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
        else if (cssColor.StartsWith("rgb"))
            return null; // TODO: support CSS colors with rgb/rgba syntax
        else
            return ParseNamedCssColor(cssColor);
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
}

/// <summary>
/// Splits given HTML snippet string into tokens.
/// </summary>
internal class HtmlTokenParser
{
    private readonly string _html;
    private int _idx = 0;

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
            return false;

        var nextTokenIdx = _html.IndexOf('<', _idx) + 1;
        if (nextTokenIdx > _idx + 1)
        {
            // TODO collapse whitespace
            var text = ProcessText(_html.Substring(_idx, nextTokenIdx - _idx - 1));
            token = new HtmlToken(text, null, HtmlTokenType.PlainText);
            _idx = nextTokenIdx - 1;
        }
        else if (nextTokenIdx < 1)
        {
            // no more tokens
            if (_idx < _html.Length)
            {
                // TODO collapse whitespace
                var text = ProcessText(_html.Substring(_idx));
                token = new HtmlToken(text, null, HtmlTokenType.PlainText);
                _idx = _html.Length;
            }
        }
        else
        {
            var endTokenIdx = _html.IndexOf('>', nextTokenIdx);
            if (endTokenIdx == -1)
                return false; // Stop parsing if we encountered a syntax error
            HtmlTokenType type = HtmlTokenType.OpenTag;
            string? attributes = null;
            string name;

            if (_html[nextTokenIdx] == '/')
            {
                nextTokenIdx++;
                type = HtmlTokenType.CloseTag;
            }
            else if (_html[endTokenIdx - 1] == '/')
            {
                endTokenIdx--; // trim the slash
                type = HtmlTokenType.SelfClosingTag;
            }

            var space = _html.IndexOfAny(new[] { ' ', '\t', '\r', '\n', '\v' }, nextTokenIdx, endTokenIdx - nextTokenIdx) + 1;
            if (space > nextTokenIdx)
            {
                attributes = _html.Substring(space, endTokenIdx - space).Trim();
                if (string.IsNullOrEmpty(attributes))
                    attributes = null;
                name = _html.Substring(nextTokenIdx, space - nextTokenIdx - 1);
            }
            else
            {
                name = _html.Substring(nextTokenIdx, endTokenIdx - nextTokenIdx);
            }
            _idx = endTokenIdx;
            if (type == HtmlTokenType.SelfClosingTag)
                _idx++; // consume the slash
            token = new HtmlToken(name.Trim().ToLowerInvariant(), attributes, type);
        }
        return token != null;
    }

    private static string ProcessText(string rawText)
    {
        // replace HTML entities with their equivalent symbols
        var unescaped = HtmlUtility.UnescapeHtml(rawText);
        // trim newlines and collapse remaining whitespace
        return Regex.Replace(unescaped.Trim('\r', '\n'), @"\s+", " ");
    }
}

internal class HtmlToken
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

    // Equatable for testing purposes
    public override bool Equals(object? obj)
    {
        return obj is HtmlToken token &&
               Name == token.Name &&
               Attributes == token.Attributes &&
               Type == token.Type;
    }

    public override int GetHashCode()
    {
        int hashCode = 1279282087;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
        if (Attributes != null)
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Attributes);
        hashCode = hashCode * -1521134295 + Type.GetHashCode();
        return hashCode;
    }

    public override string ToString()
    {
        var attrString = Attributes == null ?
            "null" :
            '"' + Attributes + '"';
        return $"HtmlToken({Name}, {attrString}, {Type})";
    }
}

internal enum HtmlTokenType
{
    PlainText,
    OpenTag,
    CloseTag,
    SelfClosingTag,
}

internal enum HtmlAlignment
{
    Left,
    Right,
    Center,
    Justify,
}
#endif