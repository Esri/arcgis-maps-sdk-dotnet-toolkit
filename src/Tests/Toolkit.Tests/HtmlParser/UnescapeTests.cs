using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Toolkit.Tests.HtmlParser;

[TestClass]
public class UnescapeHtmlTests
{
    [TestMethod]
    public void TestNamedCharacterReferences()
    {
        string input = "Hello &quot;world&quot; &amp; &lt;everyone&gt;";
        string expected = "Hello \"world\" & <everyone>";
        string result = HtmlUtility.UnescapeHtml(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDecimalCharacterReferences()
    {
        string input = "Hello &#34;world&#34; &#38; &#60;everyone&#62;";
        string expected = "Hello \"world\" & <everyone>";
        string result = HtmlUtility.UnescapeHtml(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestHexadecimalCharacterReferences()
    {
        string input = "Hello &#x22;world&#x22; &#x26; &#x3C;everyone&#x3E;";
        string expected = "Hello \"world\" & <everyone>";
        string result = HtmlUtility.UnescapeHtml(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMixedCharacterReferences()
    {
        string input = "Hello &quot;world&quot; &#38; &#x3C;everyone&gt;";
        string expected = "Hello \"world\" & <everyone>";
        string result = HtmlUtility.UnescapeHtml(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestUnicode()
    {
        string input = "1.00 &#x20AC; = 39.73 &#8372;"; // beyond ASCII
        string expected = "1.00 € = 39.73 ₴";
        string result = HtmlUtility.UnescapeHtml(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNoCharacterReferences()
    {
        string input = "Hello world & everyone";
        string expected = "Hello world & everyone";
        string result = HtmlUtility.UnescapeHtml(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEmojiNumericCharacterReference()
    {
        string input = "&#128512;"; // beyond the Basic Multilingual Plane / beyond 16-bit
        string expected = "😀";
        string result = HtmlUtility.UnescapeHtml(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNonBreakingSpace()
    {
        string input = "100&nbsp;km";
        string expected = "100\u00A0km"; // non-breaking space is significant for text layout, so it is preserved and not normalized to a regular space
        string result = HtmlUtility.UnescapeHtml(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestOutOfRangeCode()
    {
        string input = "Hello &#x110000;"; // out of range for Unicode
        string result = HtmlUtility.UnescapeHtml(input);
        Assert.AreEqual(input, result); // unchanged
    }
}
