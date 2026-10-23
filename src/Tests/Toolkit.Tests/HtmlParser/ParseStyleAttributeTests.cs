using System.Drawing;
using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Toolkit.Tests.HtmlParser;

[TestClass]
public class ParseStyleAttributeTests
{
    [TestMethod]
    public void ParseStyle_EmptyString()
    {
        var styleAttribute = "";
        var styles = HtmlUtility.ParseStyleAttribute(styleAttribute);
        Assert.IsNotNull(styles);
        Assert.IsEmpty(styles);
    }

    [TestMethod]
    public void ParseStyle_SingleDeclaration()
    {
        var styleAttribute = "color: red;";
        var styles = HtmlUtility.ParseStyleAttribute(styleAttribute);
        Assert.IsNotNull(styles);
        Assert.HasCount(1, styles);
        Assert.IsTrue(styles.ContainsKey("color"));
        Assert.AreEqual("red", styles["color"]);
    }

    [TestMethod]
    public void ParseStyle_MultipleDeclarations()
    {
        var styleAttribute = "color: red; font-size: 16px; background-color: #fff;";
        var styles = HtmlUtility.ParseStyleAttribute(styleAttribute);
        Assert.IsNotNull(styles);
        Assert.HasCount(3, styles);
        Assert.IsTrue(styles.ContainsKey("color"));
        Assert.AreEqual("red", styles["color"]);
        Assert.IsTrue(styles.ContainsKey("font-size"));
        Assert.AreEqual("16px", styles["font-size"]);
        Assert.IsTrue(styles.ContainsKey("background-color"));
        Assert.AreEqual("#fff", styles["background-color"]);
    }

    [TestMethod]
    public void ParseStyle_DuplicateDeclarations()
    {
        var styleAttribute = "color: red; color: blue; font-weight: bold;";
        var styles = HtmlUtility.ParseStyleAttribute(styleAttribute);
        Assert.IsNotNull(styles);
        Assert.HasCount(2, styles);
        Assert.IsTrue(styles.ContainsKey("color"));
        Assert.AreEqual("blue", styles["color"]);
        Assert.IsTrue(styles.ContainsKey("font-weight"));
        Assert.AreEqual("bold", styles["font-weight"]);
    }

    [TestMethod]
    public void ParseStyle_MalformedDeclarations()
    {
        var styleAttribute = "color: red; font-size; background-color: #fff;";
        var styles = HtmlUtility.ParseStyleAttribute(styleAttribute);
        Assert.IsNotNull(styles);
        Assert.HasCount(2, styles);
        Assert.IsTrue(styles.ContainsKey("color"));
        Assert.AreEqual("red", styles["color"]);
        Assert.IsTrue(styles.ContainsKey("background-color"));
        Assert.AreEqual("#fff", styles["background-color"]);
    }

    [TestMethod]
    public void ParseStyle_CaseInsensitiveKeys()
    {
        var styleAttribute = "Color: red; FONT-size: 16px; BACKGROUND-color: #fff;";
        var styles = HtmlUtility.ParseStyleAttribute(styleAttribute);
        Assert.IsNotNull(styles);
        Assert.HasCount(3, styles);
        Assert.IsTrue(styles.ContainsKey("color"));
        Assert.AreEqual("red", styles["color"]);
        Assert.IsTrue(styles.ContainsKey("font-size"));
        Assert.AreEqual("16px", styles["font-size"]);
        Assert.IsTrue(styles.ContainsKey("background-color"));
        Assert.AreEqual("#fff", styles["background-color"]);
    }

    [TestMethod]
    public void ParseStyle_WhitespaceBeforeColon()
    {
        var styleAttribute = "color : red; font-size : 16px; background-color : #fff;";
        var styles = HtmlUtility.ParseStyleAttribute(styleAttribute);
        Assert.IsNotNull(styles);
        Assert.HasCount(3, styles);
        Assert.IsTrue(styles.ContainsKey("color"));
        Assert.AreEqual("red", styles["color"]);
        Assert.IsTrue(styles.ContainsKey("font-size"));
        Assert.AreEqual("16px", styles["font-size"]);
        Assert.IsTrue(styles.ContainsKey("background-color"));
        Assert.AreEqual("#fff", styles["background-color"]);
    }

    [TestMethod]
    public void ParseStyle_WhitespaceAfterColon()
    {
        var styleAttribute = "color:   red; font-size:   16px; background-color:   #fff;";
        var styles = HtmlUtility.ParseStyleAttribute(styleAttribute);
        Assert.IsNotNull(styles);
        Assert.HasCount(3, styles);
        Assert.IsTrue(styles.ContainsKey("color"));
        Assert.AreEqual("red", styles["color"]);
        Assert.IsTrue(styles.ContainsKey("font-size"));
        Assert.AreEqual("16px", styles["font-size"]);
        Assert.IsTrue(styles.ContainsKey("background-color"));
        Assert.AreEqual("#fff", styles["background-color"]);
    }

    [TestMethod]
    public void ParseStyle_SloppyValues()
    {
        var styleAttribute = "color:;font-size:4 "; // empty value, extra trailing space, missing last semicolon
        var styles = HtmlUtility.ParseStyleAttribute(styleAttribute);
        Assert.IsNotNull(styles);
        Assert.HasCount(1, styles);
        // "color" should be ignored due to empty value
        Assert.IsTrue(styles.ContainsKey("font-size"));
        Assert.AreEqual("4", styles["font-size"]);
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
    public void ParseStyle_ColonInValue()
    {
        var styleAttribute = "background-image: url(https://example.com/image.png); color:#333;";
        var styles = HtmlUtility.ParseStyleAttribute(styleAttribute);
        Assert.IsNotNull(styles);
        Assert.HasCount(2, styles);
        Assert.IsTrue(styles.ContainsKey("background-image"));
        Assert.AreEqual("url(https://example.com/image.png)", styles["background-image"]);
        Assert.IsTrue(styles.ContainsKey("color"));
        Assert.AreEqual("#333", styles["color"]);
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
