using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared.ScaleLine;

[TestClass]
public class ScaleLineTests : AppiumTestBase
{
    private const string ScaleLinePage = "ScaleLines";

    [TestMethod]
    [DataRow(ScaleLineType.Advanced)]
    [DataRow(ScaleLineType.Simple)]
    public async Task ScaleLine_Renders(ScaleLineType scaleLineType)
    {
        OpenSample(ScaleLinePage);
        UpdateViewpoint(50000000, 0);

        // Check initial render
        var scaleLineInfo = GetScaleLineInfo(scaleLineType);
        var initialExpectedValues = new ScaleLineInfo
        {
            MetricValue = 2000,
            MetricUnits = "km",
            MetricLineLengthPixels = PlatformSpecificPixelValue(151, 151, 151, 117),
            USValue = 1000,
            USUnits = "mi",
            USLineLengthPixels = PlatformSpecificPixelValue(122, 122, 122, 94)
        };
        AssertScaleLineInfo(initialExpectedValues, scaleLineInfo);
    }

    [TestMethod]
    [DataRow(ScaleLineType.Advanced)]
    [DataRow(ScaleLineType.Simple)]
    public async Task ScaleLine_UpdatesWithScale(ScaleLineType scaleLineType)
    {
        OpenSample(ScaleLinePage);

        UpdateViewpoint(50000000, 0);
        var initialScaleLineInfo = GetScaleLineInfo(scaleLineType);

        UpdateViewpoint(5000000, 0);
        var finalScaleLineInfo = GetScaleLineInfo(scaleLineType);

        var scaleRatioMetric = finalScaleLineInfo.MetricScale / initialScaleLineInfo.MetricScale;
        var scaleRatioUS = finalScaleLineInfo.USScale / initialScaleLineInfo.USScale;

        // The viewpoint scale increased by 10 times (5000000 is the denominator), so the scale lines should reflect this
        Assert.AreEqual(10.0, scaleRatioMetric, 0.1);
        Assert.AreEqual(10.0, scaleRatioUS, 0.1);
    }


    [TestMethod]
    [DataRow(ScaleLineType.Advanced)]
    [DataRow(ScaleLineType.Simple)]
    public async Task ScaleLine_UpdatesWithLatitude(ScaleLineType scaleLineType)
    {
        OpenSample(ScaleLinePage);

        UpdateViewpoint(5000000, 0);
        var initialInfo = GetScaleLineInfo(scaleLineType);

        UpdateViewpoint(5000000, 60);
        var finalInfo = GetScaleLineInfo(scaleLineType);

        if (scaleLineType == ScaleLineType.Advanced)
        {
            // Moving away from the equator increases scale in the mercator projection
            Assert.IsGreaterThan(initialInfo.MetricScale, finalInfo.MetricScale);
            Assert.IsGreaterThan(initialInfo.USScale, finalInfo.USScale);
        }
        else if (scaleLineType == ScaleLineType.Simple)
        {
            // The simple scale line should not update
            Assert.AreEqual(initialInfo.MetricScale, finalInfo.MetricScale, 0.01);
            Assert.AreEqual(initialInfo.USScale, finalInfo.USScale, 0.01);
        }
    }

    private void AssertScaleLineInfo(ScaleLineInfo expected, ScaleLineInfo actual)
    {
        Assert.AreEqual(expected.MetricValue, actual.MetricValue);
        Assert.AreEqual(expected.MetricUnits, actual.MetricUnits);
        Assert.AreEqual(expected.MetricLineLengthPixels, GetNormalizedPixelValue(actual.MetricLineLengthPixels), 3.0);
        Assert.AreEqual(expected.USValue, actual.USValue);
        Assert.AreEqual(expected.USUnits, actual.USUnits);
        Assert.AreEqual(expected.USLineLengthPixels, GetNormalizedPixelValue(actual.USLineLengthPixels), 3.0);
    }

    private ScaleLineInfo GetScaleLineInfo(ScaleLineType type)
    {
        var scaleLineElement = GetScaleElement(type);

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

    private AppiumElement GetScaleElement(ScaleLineType type)
    {
        if (type == ScaleLineType.Advanced)
            return FindElement("AdvancedScaleLine");
        else if (type == ScaleLineType.Simple)
            return FindElement("SimpleScaleLine");
        else
            throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid scale line type.");
    }

    private void UpdateViewpoint(int scale, int latitude)
    {
        var scaleInputElement = FindElement("ScaleTextBox");
        SubmitText(scaleInputElement, scale.ToString());

        var latitudeInputElement = FindElement("LatitudeTextBox");
        SubmitText(latitudeInputElement, latitude.ToString());

        var updateButtonElement = FindElement("UpdateViewpoint");
        updateButtonElement.Click();
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