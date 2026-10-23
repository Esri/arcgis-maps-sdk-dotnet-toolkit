using System.Drawing;
using System.Diagnostics;
using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Toolkit.Tests.HtmlParser;

[TestClass]
public class NodeTreeTests
{
    [TestMethod]
    public void TestSpanWithBreaks()
    {
        // The no-attribute span should be eliminated, with 4 children placed directly under the document root.
        var htmlSnippet = "<span>Bold text<br /><br>Another line</span>";
        MarkupNode result = BuildNodeTree(htmlSnippet);

        Assert.AreEqual(MarkupType.Document, result.Type);
        Assert.HasCount(4, result.Children);

        var textNode1 = result.Children[0];
        Assert.AreEqual(MarkupType.Text, textNode1.Type);
        Assert.AreEqual("Bold text", textNode1.Content);

        var breakNode1 = result.Children[1];
        Assert.AreEqual(MarkupType.Break, breakNode1.Type);
        var breakNode2 = result.Children[2];
        Assert.AreEqual(MarkupType.Break, breakNode2.Type);

        var textNode2 = result.Children[3];
        Assert.AreEqual(MarkupType.Text, textNode2.Type);
        Assert.AreEqual("Another line", textNode2.Content);
    }

    [TestMethod]
    public void TestTable()
    {
        var htmlSnippet = @"<table>
  <thead>
    <tr>
      <th>header 1</th>
      <th>header 2</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>cell 1</td>
      <td>cell 2</td>
    </tr>
  </tbody>
</table>";
        MarkupNode result = BuildNodeTree(htmlSnippet);

        Assert.AreEqual(MarkupType.Document, result.Type);
        Assert.HasCount(1, result.Children);

        var tableNode = result.Children[0];
        Assert.AreEqual(MarkupType.Table, tableNode.Type);
        Assert.HasCount(2, tableNode.Children);

        // Expect <thead> and whitespace to be omitted
        var headerRowNode = tableNode.Children[0];
        Assert.AreEqual(MarkupType.TableRow, headerRowNode.Type);
        Assert.HasCount(2, headerRowNode.Children);

        var thNode1 = headerRowNode.Children[0];
        Assert.AreEqual(MarkupType.TableCell, thNode1.Type);
        Assert.AreEqual("header 1", thNode1.Children.Single().Content);

        var thNode2 = headerRowNode.Children[1];
        Assert.AreEqual(MarkupType.TableCell, thNode2.Type);
        Assert.AreEqual("header 2", thNode2.Children.Single().Content);

        // Expect <tbody> and whitespace to be omitted
        var bodyRowNode = tableNode.Children[1];
        Assert.AreEqual(MarkupType.TableRow, bodyRowNode.Type);
        Assert.HasCount(2, bodyRowNode.Children);

        var tdNode1 = bodyRowNode.Children[0];
        Assert.AreEqual(MarkupType.TableCell, tdNode1.Type);
        Assert.AreEqual("cell 1", tdNode1.Children.Single().Content);

        var tdNode2 = bodyRowNode.Children[1];
        Assert.AreEqual(MarkupType.TableCell, tdNode2.Type);
        Assert.AreEqual("cell 2", tdNode2.Children.Single().Content);
    }

    [TestMethod]
    public void TestInlineStyleTags()
    {
        var htmlSnippet = "<b>Bold</b><strong>Strong</strong><i>Italic</i><em>Emphasis</em><u>Underline</u>";
        MarkupNode result = BuildNodeTree(htmlSnippet);

        Assert.AreEqual(MarkupType.Document, result.Type);
        Assert.HasCount(3, result.Children);

        // Expect <b> and <strong> to be merged together into one text node
        var bText = result.Children[0];
        Assert.AreEqual(MarkupType.Text, bText.Type);
        Assert.IsTrue(bText.IsBold);
        Assert.AreEqual("BoldStrong", bText.Content);

        // Expect <i> and <em> to be merged together into one text node
        var iText = result.Children[1];
        Assert.AreEqual(MarkupType.Text, iText.Type);
        Assert.IsTrue(iText.IsItalic);
        Assert.AreEqual("ItalicEmphasis", iText.Content);

        // Expect <u> to be merged with its text node
        var uText = result.Children[2];
        Assert.AreEqual(MarkupType.Text, uText.Type);
        Assert.IsTrue(uText.IsUnderline);
        Assert.AreEqual("Underline", uText.Content);
    }

    [TestMethod]
    public void TestFontTag()
    {
        var htmlSnippet = "<font color='#FF0000' size='2'>Colored and sized text</font>";
        MarkupNode result = BuildNodeTree(htmlSnippet);

        Assert.AreEqual(MarkupType.Document, result.Type);
        Assert.HasCount(1, result.Children);

        // Expect font tag to be merged with text node
        var textNode = result.Children[0];
        Assert.AreEqual(MarkupType.Text, textNode.Type);
        Assert.AreEqual(Color.FromArgb(255, 0, 0), textNode.FontColor);
        Assert.AreEqual(0.82, textNode.FontSize); // em
        Assert.AreEqual("Colored and sized text", textNode.Content);
    }

    [TestMethod]
    public void TestImplicitlyClosedPara()
    {
        var htmlSnippet = "<p>Para 1<p>Para 2";
        MarkupNode result = BuildNodeTree(htmlSnippet);

        Assert.AreEqual(MarkupType.Document, result.Type);
        Assert.HasCount(2, result.Children);

        var para1 = result.Children[0];
        Assert.AreEqual(MarkupType.Block, para1.Type);
        Assert.HasCount(1, para1.Children);

        var text1 = para1.Children[0];
        Assert.AreEqual(MarkupType.Text, text1.Type);
        Assert.AreEqual("Para 1", text1.Content);

        var para2 = result.Children[1];
        Assert.AreEqual(MarkupType.Block, para2.Type);
        Assert.HasCount(1, para2.Children);

        var text2 = para2.Children[0];
        Assert.AreEqual(MarkupType.Text, text2.Type);
        Assert.AreEqual("Para 2", text2.Content);
    }

    [TestMethod]
    public void TestParaClosesOnUl()
    {
        var html = "<p>Para<ul><li>Item</li></ul>";
        var doc = BuildNodeTree(html);

        Assert.AreEqual(MarkupType.Document, doc.Type);
        Assert.HasCount(2, doc.Children);

        var p = doc.Children[0];
        Assert.AreEqual(MarkupType.Block, p.Type);
        Assert.AreEqual("Para", p.Children.Single().Content);

        var ul = doc.Children[1];
        Assert.AreEqual(MarkupType.List, ul.Type);
        Assert.HasCount(1, ul.Children);
        Assert.AreEqual("Item", ul.Children[0].Children.Single().Content);
    }

    [TestMethod]
    public void TestUnclosedListItems()
    {
        var htmlSnippet = "<ul><li>Item 1<li>Item 2</ul>";
        MarkupNode result = BuildNodeTree(htmlSnippet);
        PrintNode(result, 0);
        Assert.AreEqual(MarkupType.Document, result.Type);
        Assert.HasCount(1, result.Children);
        var listNode = result.Children[0];
        Assert.AreEqual(MarkupType.List, listNode.Type);
        Assert.HasCount(2, listNode.Children);
        var item1 = listNode.Children[0];
        Assert.AreEqual(MarkupType.ListItem, item1.Type);
        Assert.HasCount(1, item1.Children);
        Assert.AreEqual("Item 1", item1.Children[0].Content);
        var item2 = listNode.Children[1];
        Assert.AreEqual(MarkupType.ListItem, item2.Type);
        Assert.HasCount(1, item2.Children);
        Assert.AreEqual("Item 2", item2.Children[0].Content);
    }

    [TestMethod]
    public void TestDefinitionList()
    {
        var html = "<dl><dt>T<dd>D1<dd>D2</dl>";
        var doc = BuildNodeTree(html);

        var dl = doc.Children.Single();
        Assert.AreEqual(MarkupType.List, dl.Type);

        Assert.HasCount(3, dl.Children);
        Assert.AreEqual("T", dl.Children[0].Children.Single().Content); // dt
        Assert.AreEqual("D1", dl.Children[1].Children.Single().Content); // dd
        Assert.AreEqual("D2", dl.Children[2].Children.Single().Content); // dd
    }

    [TestMethod]
    public void TestUnclosedTable()
    {
        var htmlSnippet = "<table><tr><td>x<td>y";
        MarkupNode result = BuildNodeTree(htmlSnippet);
        PrintNode(result, 0);
        Assert.AreEqual(MarkupType.Document, result.Type);
        Assert.HasCount(1, result.Children);
        var tableNode = result.Children[0];
        Assert.AreEqual(MarkupType.Table, tableNode.Type);
        Assert.HasCount(1, tableNode.Children);
        var rowNode = tableNode.Children[0];
        Assert.AreEqual(MarkupType.TableRow, rowNode.Type);
        Assert.HasCount(2, rowNode.Children);
        var cell1 = rowNode.Children[0];
        Assert.AreEqual(MarkupType.TableCell, cell1.Type);
        Assert.HasCount(1, cell1.Children);
        Assert.AreEqual("x", cell1.Children[0].Content);
        var cell2 = rowNode.Children[1];
        Assert.AreEqual(MarkupType.TableCell, cell2.Type);
        Assert.HasCount(1, cell2.Children);
        Assert.AreEqual("y", cell2.Children[0].Content);
    }

    [TestMethod]
    public void TestAnchorClosesOnAnchor()
    {
        var html = "<a href='x'>one<a href='y'>two</a>";
        var doc = BuildNodeTree(html);

        // Expect two top-level Link nodes (no nesting)
        Assert.HasCount(2, doc.Children);
        var a1 = doc.Children[0];
        var a2 = doc.Children[1];

        Assert.AreEqual(MarkupType.Link, a1.Type);
        Assert.AreEqual("x", a1.Content);
        Assert.AreEqual("one", a1.Children.Single().Content);

        Assert.AreEqual(MarkupType.Link, a2.Type);
        Assert.AreEqual("y", a2.Content);
        Assert.AreEqual("two", a2.Children.Single().Content);
    }

    [TestMethod]
    public void RemoveTrailingBreaks()
    {
        // The second break after "Text" is removed.
        // But the very last <br> is preserved because it's part of an otherwise empty "inline group".
        var htmlSnippet = "<div>Text<br><br></div><br>";
        MarkupNode doc = BuildNodeTree(htmlSnippet);
        PrintNode(doc, 0);

        Assert.AreEqual(MarkupType.Document, doc.Type);
        Assert.HasCount(2, doc.Children);

        var para = doc.Children[0];
        Assert.AreEqual(MarkupType.Block, para.Type);
        Assert.HasCount(2, para.Children);

        var text = para.Children[0];
        Assert.AreEqual(MarkupType.Text, text.Type);
        Assert.AreEqual("Text", text.Content);

        var br = para.Children[1];
        Assert.AreEqual(MarkupType.Break, br.Type);

        var br2 = doc.Children[1];
        Assert.AreEqual(MarkupType.Break, br2.Type);
    }

    [TestMethod]
    public void RemoveTrailingBreaks_PreserveInEmptyBlock()
    {
        var htmlSnippet = "Before<div><br></div>After";
        MarkupNode doc = BuildNodeTree(htmlSnippet);
        PrintNode(doc, 0);

        Assert.AreEqual(MarkupType.Document, doc.Type);
        Assert.HasCount(3, doc.Children);

        var textBefore = doc.Children[0];
        Assert.AreEqual(MarkupType.Text, textBefore.Type);
        Assert.AreEqual("Before", textBefore.Content);

        // The inner <br> is preserved because it's in an empty block.
        var para = doc.Children[1];
        Assert.AreEqual(MarkupType.Block, para.Type);
        Assert.HasCount(1, para.Children);

        var br = para.Children[0];
        Assert.AreEqual(MarkupType.Break, br.Type);

        var textAfter = doc.Children[2];
        Assert.AreEqual(MarkupType.Text, textAfter.Type);
        Assert.AreEqual("After", textAfter.Content);
    }

    [TestMethod]
    public void RemoveTrailingBreaks_PreserveBetweenInlines()
    {
        var htmlSnippet = "<b>Bold<br></b>Normal<br>";
        MarkupNode doc = BuildNodeTree(htmlSnippet);

        Assert.AreEqual(MarkupType.Document, doc.Type);
        Assert.HasCount(2, doc.Children);

        var b = doc.Children[0];
        Assert.AreEqual(MarkupType.Span, b.Type);
        Assert.HasCount(2, b.Children);

        var boldText = b.Children[0];
        Assert.AreEqual(MarkupType.Text, boldText.Type);
        Assert.AreEqual("Bold", boldText.Content);

        var br = b.Children[1];
        Assert.AreEqual(MarkupType.Break, br.Type);

        var normalText = doc.Children[1];
        Assert.AreEqual(MarkupType.Text, normalText.Type);
        Assert.AreEqual("Normal", normalText.Content);
    }

    [TestMethod]
    public void RemoveTrailingBreaks_NestedInSpan()
    {
        // Last br in a block is still removed even if it's wrapped in an inline element
        var htmlSnippet = "<h1><i>Heading<br></i></h1>";
        MarkupNode doc = BuildNodeTree(htmlSnippet);
        PrintNode(doc, 0);

        Assert.AreEqual(MarkupType.Document, doc.Type);
        Assert.HasCount(1, doc.Children);

        var header = doc.Children[0];
        Assert.AreEqual(MarkupType.Block, header.Type);
        Assert.HasCount(1, header.Children);

        // Expect <br> to be eliminated, and <i> merged with text node
        var text = header.Children[0];
        Assert.AreEqual(MarkupType.Text, text.Type);
        Assert.AreEqual("Heading", text.Content);
    }

    [TestMethod]
    public void TestLinkWithImage()
    {
        var htmlSnippet = "<a href='https://www.example.com'><img src='image.jpg' alt='Example image' /></a>";
        MarkupNode result = BuildNodeTree(htmlSnippet);

        Assert.AreEqual(MarkupType.Document, result.Type);
        Assert.HasCount(1, result.Children);

        var linkNode = result.Children[0];
        Assert.AreEqual(MarkupType.Link, linkNode.Type);
        Assert.AreEqual("https://www.example.com", linkNode.Content);
        Assert.HasCount(1, linkNode.Children);

        var imageNode = linkNode.Children[0];
        Assert.AreEqual(MarkupType.Image, imageNode.Type);
        Assert.AreEqual("image.jpg", imageNode.Content);
    }

    [TestMethod]
    public void TestStyles()
    {
        var htmlSnippet = "<span style='color:#18212b;font-size:14px;background:#fff'>Test</span>";
        MarkupNode result = BuildNodeTree(htmlSnippet);

        Assert.HasCount(1, result.Children);
        // Expect <span> to be merged with text node
        var spanNode = result.Children[0];
        Assert.AreEqual(Color.FromArgb(24, 33, 43), spanNode.FontColor);
        Assert.AreEqual(0.875, spanNode.FontSize); // em relative to 16px
        Assert.AreEqual("Test", spanNode.Content);
    }

    [TestMethod]
    public void TestFontWeight()
    {
        // The <b> tag should be overridden by font-weight
        var htmlSnippet = "<b><span style='font-weight:normal'>Test</span></b>";
        MarkupNode result = BuildNodeTree(htmlSnippet);

        Assert.HasCount(1, result.Children);
        // Expect <b> and <span> to be merged into the text node
        var textNode = result.Children[0];
        Assert.AreEqual(MarkupType.Text, textNode.Type);
        Assert.IsFalse(textNode.IsBold);
        Assert.AreEqual("Test", textNode.Content);
    }

    [TestMethod]
    public void TestList()
    {
        var htmlSnippet = @"<ul>
            <li>Item 1</li>
            <li><b>Item 2</b></li>
        </ul>";
        MarkupNode result = BuildNodeTree(htmlSnippet);

        Assert.AreEqual(MarkupType.Document, result.Type);
        Assert.HasCount(1, result.Children);

        var listNode = result.Children[0];
        Assert.AreEqual(MarkupType.List, listNode.Type);
        // Expect whitespace between <ul> and <li> to be removed (it's not an "inline context").
        Assert.HasCount(2, listNode.Children);

        var listItem1 = listNode.Children[0];
        Assert.AreEqual(MarkupType.ListItem, listItem1.Type);
        Assert.HasCount(1, listItem1.Children);

        var textNode1 = listItem1.Children[0];
        Assert.AreEqual(MarkupType.Text, textNode1.Type);
        Assert.AreEqual("Item 1", textNode1.Content);

        var listItem2 = listNode.Children[1];
        Assert.AreEqual(MarkupType.ListItem, listItem2.Type);
        Assert.HasCount(1, listItem2.Children);

        // Expect <b> to be merged with text node.
        var textNode2 = listItem2.Children[0];
        Assert.IsTrue(textNode2.IsBold);
        Assert.AreEqual(MarkupType.Text, textNode2.Type);
        Assert.AreEqual("Item 2", textNode2.Content);
    }

    [TestMethod]
    public void SkipStyleTag()
    {
        // Assert that a <style> tag in the middle of the document is ignored
        var htmlSnippet = "<p>Text</p><style>body { color: red; }</style><p>More text</p>";
        MarkupNode result = BuildNodeTree(htmlSnippet);
        Assert.AreEqual(MarkupType.Document, result.Type);
        Assert.HasCount(2, result.Children);
        Assert.AreEqual(MarkupType.Block, result.Children[0].Type);
        Assert.AreEqual(MarkupType.Block, result.Children[1].Type);
        Assert.AreEqual("Text", result.Children[0].Children[0].Content);
        Assert.AreEqual("More text", result.Children[1].Children[0].Content);
    }

    [TestMethod]
    public void SkipScriptTag()
    {
        // Assert that a <script> tag in the middle of the document is ignored
        var htmlSnippet = "<p>Text</p><script>alert('Hello');</script><p>More text</p>";
        MarkupNode result = BuildNodeTree(htmlSnippet);
        Assert.AreEqual(MarkupType.Document, result.Type);
        Assert.HasCount(2, result.Children);
        Assert.AreEqual(MarkupType.Block, result.Children[0].Type);
        Assert.AreEqual(MarkupType.Block, result.Children[1].Type);
        Assert.AreEqual("Text", result.Children[0].Children[0].Content);
        Assert.AreEqual("More text", result.Children[1].Children[0].Content);
    }

    [TestMethod]
    public void ParsePastExtraCloseTag()
    {
        // Assert that if an extra closing tag appears that does not match any preceding open tag, we can still keep parsing past it -- we just assume that the tag takes us back to the document root level.
        var htmlSnippet = "<p>Text</p></div><p>More Text</p>";
        MarkupNode result = BuildNodeTree(htmlSnippet);
        Assert.AreEqual(MarkupType.Document, result.Type);
        Assert.HasCount(2, result.Children);
        Assert.AreEqual(MarkupType.Block, result.Children[0].Type);
        Assert.AreEqual(MarkupType.Block, result.Children[1].Type);
        Assert.AreEqual("Text", result.Children[0].Children[0].Content);
        Assert.AreEqual("More Text", result.Children[1].Children[0].Content);
    }

    [TestMethod]
    public void ParsePastMissingCloseTag()
    {
        // Assert that we can handle a mismatched close tag that follows a missing close tag
        var htmlSnippet = "<div><strong>Text</div> And more";
        MarkupNode result = BuildNodeTree(htmlSnippet);
        Assert.AreEqual(MarkupType.Document, result.Type);
        Assert.HasCount(2, result.Children);
        Assert.AreEqual(MarkupType.Block, result.Children[0].Type);
        Assert.HasCount(1, result.Children[0].Children);
        Assert.AreEqual("Text", result.Children[0].Children[0].Content);
        Assert.IsTrue(result.Children[0].Children[0].IsBold);
        Assert.AreEqual(MarkupType.Text, result.Children[1].Type);
        Assert.AreEqual("And more", result.Children[1].Content);
    }

    [TestMethod]
    public void Simplify_MergeBlocks()
    {
        var htmlSnippet = "<div align=center><p align=right>Hi</p></div>";
        MarkupNode result = BuildNodeTree(htmlSnippet);

        Assert.HasCount(1, result.Children);
        // Expect <div> and <p> to be merged, with inner alignment taking precedence
        var divBlock = result.Children[0];
        Assert.AreEqual(MarkupType.Block, divBlock.Type);
        Assert.AreEqual(HtmlAlignment.Right, divBlock.Alignment);
        Assert.HasCount(1, divBlock.Children);

        var textNode = divBlock.Children[0];
        Assert.AreEqual(MarkupType.Text, textNode.Type);
        Assert.AreEqual("Hi", textNode.Content);
    }

    [TestMethod]
    public void Simplify_MergeInlines()
    {
        var htmlSnippet = "<i><strong>Hello</strong></i>";
        MarkupNode doc = BuildNodeTree(htmlSnippet);

        Assert.AreEqual(MarkupType.Document, doc.Type);
        Assert.HasCount(1, doc.Children);

        var textNode = doc.Children[0];
        Assert.AreEqual(MarkupType.Text, textNode.Type);
        Assert.AreEqual("Hello", textNode.Content);
        Assert.IsTrue(textNode.IsBold.HasValue && textNode.IsBold.Value);
        Assert.IsTrue(textNode.IsItalic.HasValue && textNode.IsItalic.Value);
    }

    [TestMethod]
    public void Simplify_PreservesLinks()
    {
        // Can't merge anchor with its child span because they both have "content"
        var htmlSnippet = "<a href=\"x\"><b>text</b></a>";
        MarkupNode doc = BuildNodeTree(htmlSnippet);

        Assert.AreEqual(MarkupType.Document, doc.Type);
        var linkNode = doc.Children.Single();
        Assert.AreEqual(MarkupType.Link, linkNode.Type);
        Assert.AreEqual("x", linkNode.Content);
        var textNode = linkNode.Children.Single();
        Assert.AreEqual(MarkupType.Text, textNode.Type);
        Assert.AreEqual("text", textNode.Content);
    }

    [TestMethod]
    public void Simplify_RemoveDisplayNone()
    {
        var htmlSnippet = "Visible text <span style='display:none;'>Hidden text</span> More visible text";
        MarkupNode doc = BuildNodeTree(htmlSnippet);

        Assert.AreEqual(MarkupType.Document, doc.Type);
        Assert.HasCount(1, doc.Children);

        var visibleText = doc.Children[0];
        Assert.AreEqual(MarkupType.Text, visibleText.Type);
        Assert.AreEqual("Visible text  More visible text", visibleText.Content);
    }

    [TestMethod]
    public void Simplify_RemoveEmptySpan()
    {
        var htmlSnippet = "<p>Hello<b></b> world!</p>";
        MarkupNode doc = BuildNodeTree(htmlSnippet);

        Assert.AreEqual(MarkupType.Document, doc.Type);
        Assert.HasCount(1, doc.Children);

        var para = doc.Children[0];
        Assert.AreEqual(MarkupType.Block, para.Type);
        Assert.HasCount(1, para.Children);

        var textNode = para.Children[0];
        Assert.AreEqual(MarkupType.Text, textNode.Type);
        Assert.AreEqual("Hello world!", textNode.Content);
    }

    [TestMethod]
    public void Simplify_MergesAttributes()
    {
        // Make sure that ALL attributes are copied from the parent, and merged into the child node when simplifying.
        var htmlSnippet = "<div align='center'><div style='background: #123456;'><b><code><i><u><span style='font-size: 32px; color: #654321;'>Test</span></u></i></code></b></div></div>";
        var root = BuildNodeTree(htmlSnippet);

        Assert.AreEqual(MarkupType.Document, root.Type);
        Assert.HasCount(1, root.Children);

        var blockNode = root.Children[0];
        Assert.AreEqual(MarkupType.Block, blockNode.Type);
        Assert.AreEqual(HtmlAlignment.Center, blockNode.Alignment);
        Assert.AreEqual(Color.FromArgb(18, 52, 86), blockNode.BackColor);
        Assert.HasCount(1, blockNode.Children);

        var textNode = blockNode.Children[0];
        Assert.AreEqual(MarkupType.Text, textNode.Type);
        Assert.IsTrue(textNode.IsBold);
        Assert.IsTrue(textNode.IsItalic);
        Assert.IsTrue(textNode.IsMonospace);
        Assert.IsTrue(textNode.IsUnderline);
        Assert.AreEqual(2, textNode.FontSize); // 32px relative to 16px base font size
        Assert.AreEqual(Color.FromArgb(101, 67, 33), textNode.FontColor);
        Assert.AreEqual("Test", textNode.Content);
    }

    static MarkupNode BuildNodeTree(string html)
    {
        return HtmlUtility.BuildDocumentTree(html);
    }

    private void PrintNode(MarkupNode node, int indent = 0)
    {
        Trace.WriteLine(new string(' ', indent) + node);
        foreach (var child in node.Children)
            PrintNode(child, indent + 2);
    }
}