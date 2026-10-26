using System.Drawing;
using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Toolkit.Tests.HtmlParser;

[TestClass]
public class ParseStyleAttributeTests
{
    [TestMethod]
    [DataRow("")]
    [DataRow("color: red; font-size: 16px; background-color: #fff;", "color=red", "font-size=16px", "background-color=#fff")]
    [DataRow("color: red; color: blue; font-weight: bold;", "color=blue", "font-weight=bold")]
    [DataRow("color: red; font-size; background-color: #fff;", "color=red", "background-color=#fff")]
    [DataRow("Color: red; FONT-size: 16px", "color=red", "font-size=16px")]
    [DataRow("color :   red ;  font-size :16px", "color=red", "font-size=16px")]
    [DataRow("color:;font-size:4 ", "font-size=4")] // empty value, trailing space, no last semicolon
    [DataRow("background-image: url(https://example.com/image.png); color:#333;", "background-image=url(https://example.com/image.png)", "color=#333")]
    public void ParseStyleAttribute(string style, params string[] expected)
    {
        var actual = HtmlUtility.ParseStyleAttribute(style).Select(kv => $"{kv.Key}={kv.Value}").ToList();
        CollectionAssert.AreEquivalent(expected, actual);
    }

    [TestMethod]
    [DataRow("8px", 0.5)]
    [DataRow("16px", 1.0)]
    [DataRow("12pt", 1.0)]
    [DataRow("24.0pt", 2.0)]
    [DataRow("medium", 1.0)]
    [DataRow("1.5em", 1.5)]
    [DataRow("2rem", 2.0)]
    [DataRow("50%", 0.5)]
    public void TryParseCssFontSize(string cssFontSize, double expectedSizeEm)
    {
        var result = HtmlUtility.TryParseCssFontSize(cssFontSize, out var parsedSizeEm);
        Assert.IsTrue(result);
        Assert.AreEqual(expectedSizeEm, parsedSizeEm);
    }

    [TestMethod]
    public void TryParseCssFontSize_NonEnglishLocale()
    {
        // Check if we use invariant culture for parsing
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
            var cssFontSize = "15.0pt"; // French uses comma as decimal separator
            var result = HtmlUtility.TryParseCssFontSize(cssFontSize, out var parsedSizeEm);
            Assert.IsTrue(result);
            Assert.AreEqual(1.25, parsedSizeEm); // Should still parse correctly as invariant culture is used
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    [DataRow("red", 255, 0, 0, 255)]
    [DataRow("green", 0, 128, 0, 255)]
    [DataRow("blue", 0, 0, 255, 255)]
    [DataRow("#123", 17, 34, 51, 255)]
    [DataRow("#f00", 255, 0, 0, 255)]
    [DataRow("#0f08", 0, 255, 0, 136)]
    [DataRow("#ff0000", 255, 0, 0, 255)]
    [DataRow("#00ff00aa", 0, 255, 0, 170)]
    [DataRow("rgb(123,45,67)", 123, 45, 67, 255)]
    [DataRow("rgba(0,255,0,0.5)", 0, 255, 0, 128)]
    [DataRow("rgb(1 2 3 / 0.5)", 1, 2, 3, 128)]
    public void ParseColor_Valid(
        string cssColor, int red, int green, int blue, int alpha)
    {
        var expectedColor = Color.FromArgb(alpha, red, green, blue);
        var parsedColor = HtmlUtility.ParseCssColor(cssColor);
        Assert.AreEqual(expectedColor, parsedColor);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("#")]
    [DataRow("notacolor")]
    [DataRow("123456")]
    [DataRow("#123456789")]
    public void ParseColor_Invalid(string? cssColor)
    {
        Color? parsedColor = HtmlUtility.ParseCssColor(cssColor);
        Assert.IsNull(parsedColor);
    }

    // Expected values are what Edge renders for <font color>.
    [TestMethod]
    [DataRow("#ff0000", 255, 0, 0)]
    [DataRow("ff0000", 255, 0, 0)]
    [DataRow("##2F84B1", 2, 132, 16)]
    [DataRow(" DarkGrey ", 169, 169, 169)]
    [DataRow("#f00", 255, 0, 0)]
    [DataRow("f00", 15, 0, 0)] // three digits expand only after '#'
    [DataRow("#ff000080", 255, 0, 128)] // no alpha: the digits split as ff0, 000, 800
    [DataRow("001002003", 1, 2, 3)] // leading zeros that all components share are dropped
    [DataRow("#123456789abcdef0123456789", 35, 188, 69)] // components keep their last eight digits
    [DataRow("rgb(1,2,3)", 0, 16, 48)] // CSS syntax does not apply
    public void ParseLegacyColor(string value, int red, int green, int blue)
    {
        Assert.AreEqual(Color.FromArgb(red, green, blue).ToArgb(), HtmlUtility.ParseLegacyColor(value)?.ToArgb());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("Transparent")]
    public void ParseLegacyColor_Invalid(string? value)
    {
        Assert.IsNull(HtmlUtility.ParseLegacyColor(value));
    }

    [TestMethod]
    public void ParseColor_NamedColors()
    {
        // System.Drawing knows every CSS color name, but only the "gray" spelling of the grays.
        foreach (var known in Enum.GetValues<KnownColor>().Select(Color.FromKnownColor).Where(c => !c.IsSystemColor))
        {
            var name = known.Name.ToLowerInvariant();
            foreach (var spelling in new[] { name, name.Replace("gray", "grey") }.Distinct())
                Assert.AreEqual(known.ToArgb(), HtmlUtility.ParseCssColor(spelling)?.ToArgb(), spelling);
        }
    }

    [TestMethod]
    [DataRow("normal", false)]
    [DataRow("bold", true)]
    [DataRow("Bolder", true)]
    [DataRow("123", false)]
    [DataRow("900", true)]
    [DataRow("lighter", false)]
    public void ParseCssFontWeight_ValidInputs(string rawValue, bool? expectedResult)
    {
        Assert.AreEqual(expectedResult, HtmlUtility.ParseCssFontWeight(rawValue));
    }

    [TestMethod]
    [DataRow("banana")]
    [DataRow("-1")]
    [DataRow(" ")]
    [DataRow(null)]
    public void ParseCssFontWeight_InvalidInputs(string rawValue)
    {
        Assert.IsNull(HtmlUtility.ParseCssFontWeight(rawValue));
    }
}
