using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Toolkit.Tests.HtmlParser;

[TestClass]
public class UnescapeHtmlTests
{
    [TestMethod]
    [DataRow("Hello &quot;world&quot; &amp; &lt;everyone&gt;", "Hello \"world\" & <everyone>")]
    [DataRow("Hello &#34;world&#34; &#38; &#60;everyone&#62;", "Hello \"world\" & <everyone>")]
    [DataRow("Hello &#x22;world&#x22; &#x26; &#x3C;everyone&#x3E;", "Hello \"world\" & <everyone>")]
    [DataRow("1.00 &#x20AC; = 39.73 &#8372;", "1.00 € = 39.73 ₴")]
    [DataRow("&#128512;", "😀")] // beyond the Basic Multilingual Plane
    [DataRow("100&nbsp;km", "100\u00A0km")] // significant for layout, so not normalized to a regular space
    [DataRow("Hello world & everyone", "Hello world & everyone")]
    public void UnescapeHtml(string input, string expected)
    {
        Assert.AreEqual(expected, HtmlUtility.UnescapeHtml(input));
    }

    [TestMethod]
    public void OutOfRangeNumericReferenceIsKeptAsText()
    {
        // Browsers decode a code point above U+10FFFF to U+FFFD; WebUtility.HtmlDecode keeps the reference as text.
        // Either is acceptable. Decoding must not throw, because a throw drops the whole popup to plain text.
        string input = "Hello &#x110000;";
        Assert.AreEqual(input, HtmlUtility.UnescapeHtml(input));
    }
}
