using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Toolkit.Tests.HtmlParser;

[TestClass]
public class ParseAttributesTests
{
    [TestMethod]
    [DataRow("")]
    [DataRow("color='#000000' size='3'", "color=#000000", "size=3")]
    [DataRow("  color  =  '#000000'   size  =  '3'  ", "color=#000000", "size=3")]
    [DataRow("title=\"Some &lt;text&gt;\"", "title=Some <text>")]
    [DataRow("text='I&apos;m &quot;sure&quot; this works'", "text=I'm \"sure\" this works")]
    [DataRow("data-value='Hello &amp; good day'", "data-value=Hello & good day")]
    [DataRow("disabled", "disabled=")]
    [DataRow("color=red size=3 disabled", "color=red", "size=3", "disabled=")]
    [DataRow("color=red size='3' title=\"Some text\" disabled", "color=red", "size=3", "title=Some text", "disabled=")]
    [DataRow("Color='#000000' SIZE='3'", "color=#000000", "size=3")]
    public void ParseAttributes(string attributes, params string[] expected)
    {
        var actual = HtmlUtility.ParseAttributes(attributes).Select(kv => $"{kv.Key}={kv.Value}").ToList();
        CollectionAssert.AreEquivalent(expected, actual);
    }
}
