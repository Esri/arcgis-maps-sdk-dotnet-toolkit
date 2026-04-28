using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared.ScaleLine;

[TestClass]
public class ScaleLineTests : AppiumTestBase
{
    private const string ScaleLinePage = "ScaleLines";

    /// <summary>
    /// Basic test to show how platform-specific expected dimensions can be handled.
    /// </summary>
    [TestMethod]
    [DataRow(ScaleLineType.Advanced)]
    [DataRow(ScaleLineType.Simple)]
    public async Task ScaleLine_Renders(ScaleLineType scaleLineType)
    {
        OpenSample(ScaleLinePage);
        UpdateViewpoint(50000000, 0);

        // Check initial render
        var actualValues = GetScaleLineInfo(scaleLineType);
        var expectedValues = new ScaleLineInfo
        {
            MetricValue = 2000,
            MetricUnits = "km",
            MetricLineLengthPixels = PlatformSpecificPixelValue(151, 151, 151, 117),
            USValue = 1000,
            USUnits = "mi",
            USLineLengthPixels = PlatformSpecificPixelValue(122, 122, 122, 94)
        };

        Assert.AreEqual(expectedValues.MetricValue, actualValues.MetricValue);
        Assert.AreEqual(expectedValues.MetricUnits, actualValues.MetricUnits);
        Assert.AreEqual(expectedValues.USValue, actualValues.USValue);
        Assert.AreEqual(expectedValues.USUnits, actualValues.USUnits);

        Assert.AreEqual(expectedValues.MetricLineLengthPixels, GetNormalizedPixelValue(actualValues.MetricLineLengthPixels), 3.0);
        Assert.AreEqual(expectedValues.USLineLengthPixels, GetNormalizedPixelValue(actualValues.USLineLengthPixels), 3.0);
    }

    private ScaleLineInfo GetScaleLineInfo(ScaleLineType type)
    {
        var scaleLineElement = FindElement(type == ScaleLineType.Advanced ? "AdvancedScaleLine" : "SimpleScaleLine");

        var metricValueElement = FindElement(scaleLineElement, "MetricValue");
        var metricValue = int.Parse(GetElementText(metricValueElement));
        var metricUnitElement = FindElement(scaleLineElement, "MetricUnit");
        var metricUnit = GetElementText(metricUnitElement);
        var metricLineLength = metricValueElement.Location.X - scaleLineElement.Rect.Left;

        var usValueElement = FindElement(scaleLineElement, "UsValue");
        var usValue = int.Parse(GetElementText(usValueElement));
        var usUnitElement = FindElement(scaleLineElement, "UsUnit");
        var usUnit = GetElementText(usUnitElement);
        var usLineLength = usValueElement.Location.X - scaleLineElement.Rect.Left;

        return new ScaleLineInfo
        {
            MetricValue = metricValue,
            MetricUnits = metricUnit,
            MetricLineLengthPixels = metricLineLength,
            USValue = usValue,
            USUnits = usUnit,
            USLineLengthPixels = usLineLength
        };
    }

    private void UpdateViewpoint(int scale, int latitude)
    {
        var scaleInputElement = FindElement("ScaleTextBox");
        SubmitText(scaleInputElement, scale.ToString());

        var latitudeInputElement = FindElement("LatitudeTextBox");
        SubmitText(latitudeInputElement, latitude.ToString());

        var updateButtonElement = FindElement("UpdateViewpoint");
        Click(updateButtonElement);
    }

    public enum ScaleLineType
    {
        Advanced,
        Simple
    }

    private class ScaleLineInfo
    {
        public int MetricValue;
        public string MetricUnits = "";
        public int MetricLineLengthPixels;
        public int USValue;
        public string USUnits = "";
        public int USLineLengthPixels;

        public double MetricScale
        {
            get
            {
                return MetricLineLengthPixels / (double)MetricValue;
            }
        }

        public double USScale
        {
            get
            {
                return USLineLengthPixels / (double)USValue;
            }
        }
    }
}