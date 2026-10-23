using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Toolkit.Tests.HtmlParser;

[TestClass]
public class ParseAttributesTests
{
    [TestMethod]
    public void TestEmptyInput()
    {
        string input = "";
        var expected = new Dictionary<string, string>();
        var result = HtmlUtility.ParseAttributes(input);
        CollectionAssert.AreEquivalent(expected, result);
    }

    [TestMethod]
    public void ParseMultiple()
    {
        string input = "color='#000000' size='3'";
        var expected = new Dictionary<string, string> { { "color", "#000000" }, { "size", "3" } };
        var result = HtmlUtility.ParseAttributes(input);
        CollectionAssert.AreEquivalent(expected, result);
    }

    [TestMethod]
    public void ParseLink()
    {
        string input = "href='https://example.com/wiki/List_of_mountain_peaks' rel='nofollow ugc'";
        var expected = new Dictionary<string, string> { { "href", "https://example.com/wiki/List_of_mountain_peaks" }, { "rel", "nofollow ugc" } };
        var result = HtmlUtility.ParseAttributes(input);
        CollectionAssert.AreEquivalent(expected, result);
    }

    [TestMethod]
    public void ParseEscapedValues()
    {
        string input = "title=\"Some &lt;text&gt;\"";
        var expected = new Dictionary<string, string> { { "title", "Some <text>" } };
        var result = HtmlUtility.ParseAttributes(input);
        CollectionAssert.AreEquivalent(expected, result);
    }

    [TestMethod]
    public void ParseNoValueAttribute()
    {
        string input = "disabled";
        var expected = new Dictionary<string, string> { { "disabled", "" } };
        var result = HtmlUtility.ParseAttributes(input);
        CollectionAssert.AreEquivalent(expected, result);
    }

    [TestMethod]
    public void TestExtraWhitespace()
    {
        string input = "  color  =  '#000000'   size  =  '3'  ";
        var expected = new Dictionary<string, string> { { "color", "#000000" }, { "size", "3" } };
        var result = HtmlUtility.ParseAttributes(input);
        CollectionAssert.AreEquivalent(expected, result);
    }

    [TestMethod]
    public void TestMixedQuotes()
    {
        string input = "color='#000000' size=\"3\"";
        var expected = new Dictionary<string, string> { { "color", "#000000" }, { "size", "3" } };
        var result = HtmlUtility.ParseAttributes(input);
        CollectionAssert.AreEquivalent(expected, result);
    }

    [TestMethod]
    public void TestEscapedQuotes()
    {
        string input = "text='I&apos;m &quot;sure&quot; this works'";
        var expected = new Dictionary<string, string> { { "text", "I'm \"sure\" this works" } };
        var result = HtmlUtility.ParseAttributes(input);
        CollectionAssert.AreEquivalent(expected, result);
    }

    [TestMethod]
    public void TestAttributeWithSpaces()
    {
        string input = "data-value='Hello &amp; good day'";
        var expected = new Dictionary<string, string> { { "data-value", "Hello & good day" } };
        var result = HtmlUtility.ParseAttributes(input);
        CollectionAssert.AreEquivalent(expected, result);
    }

    [TestMethod]
    public void TestMultipleAttributes()
    {
        string input = "style='color: red' title='Hello &amp; World' data-info='Some &lt;info&gt; here' disabled";
        var expected = new Dictionary<string, string> { { "style", "color: red" }, { "title", "Hello & World" }, { "data-info", "Some <info> here" }, { "disabled", "" } };
        var result = HtmlUtility.ParseAttributes(input);
        CollectionAssert.AreEquivalent(expected, result);
    }

    [TestMethod]
    public void TestUnquotedAttributeValues()
    {
        string input = "color=red size=3 disabled";
        var expected = new Dictionary<string, string> { { "color", "red" }, { "size", "3" }, { "disabled", "" } };
        var result = HtmlUtility.ParseAttributes(input);
        CollectionAssert.AreEquivalent(expected, result);
    }

    [TestMethod]
    public void TestMixedQuotedAndUnquotedAttributeValues()
    {
        string input = "color=red size='3' title=\"Some text\" disabled";
        var expected = new Dictionary<string, string> { { "color", "red" }, { "size", "3" }, { "title", "Some text" }, { "disabled", "" } };
        var result = HtmlUtility.ParseAttributes(input);
        CollectionAssert.AreEquivalent(expected, result);
    }

    [TestMethod]
    public void TestCaseInsensitiveKeys()
    {
        string input = "Color='#000000' SIZE='3'";
        var expected = new Dictionary<string, string> { { "color", "#000000" }, { "size", "3" } };
        var result = HtmlUtility.ParseAttributes(input);
        CollectionAssert.AreEquivalent(expected, result);
    }
}