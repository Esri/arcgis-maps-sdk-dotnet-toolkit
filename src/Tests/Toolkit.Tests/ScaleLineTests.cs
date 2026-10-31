using System.Globalization;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Toolkit.Tests;

[TestClass]
public sealed class ScaleLineTests
{
    [TestMethod]
    [DataRow(0.0, 0.0)]
    [DataRow(0.4, 0.4)]
    [DataRow(0.43, 0.43)]
    [DataRow(1.0, 1.0)]
    [DataRow(250.0, 250.0)]
    [DataRow(1500.0, 1500.0)]
    public void FormatDistance_KeepsTheDistance(double value, double expected)
    {
        string formatted = ScaleLine.FormatDistance(value);

        Assert.AreEqual(expected, double.Parse(formatted, CultureInfo.CurrentCulture), 0.0001);
    }

    [TestMethod]
    public void FormatDistance_DoesNotTruncateValuesBelowOneUnit()
    {
        // A scale line shorter than one metre used to be displayed as "0 m".
        Assert.AreNotEqual(0.0, double.Parse(ScaleLine.FormatDistance(0.4), CultureInfo.CurrentCulture));
    }

    [TestMethod]
    public void FormatDistance_RoundsValuesBelowOneUnitToTwoDecimals()
    {
        Assert.AreEqual(0.43, double.Parse(ScaleLine.FormatDistance(0.4285), CultureInfo.CurrentCulture), 0.0001);
    }

    [TestMethod]
    public void FormatDistance_TruncatesWholeValues()
    {
        // Whole values were already truncated by GetRoundedValue; formatting must not reintroduce decimals.
        Assert.AreEqual(250.0, double.Parse(ScaleLine.FormatDistance(250.0), CultureInfo.CurrentCulture));
    }
}
