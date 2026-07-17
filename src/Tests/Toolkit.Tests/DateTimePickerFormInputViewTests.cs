using System;
using Esri.ArcGISRuntime.Toolkit.Primitives;

namespace Toolkit.Tests;

// Regression tests for DateTimePickerFormInputView.ConvertToLocalDisplayValue (devtopia #14203).
// A TimestampOffset field's value is a DateTimeOffset carrying a non-zero offset; the picker must display
// its UTC instant, not the wall-clock reading. Assertions are time-zone independent: the local value shown
// must map back to the stored instant.
[TestClass]
public sealed class DateTimePickerFormInputViewTests
{
    // A Date field returns a UTC DateTime; the display is its local-time projection.
    [TestMethod]
    public void DateTimeValue_ProjectsToLocalTime()
    {
        var utc = new DateTime(2024, 6, 15, 18, 0, 0, DateTimeKind.Utc);

        DateTime? display = DateTimePickerFormInputView.ConvertToLocalDisplayValue(utc);

        Assert.IsNotNull(display);
        Assert.AreEqual(utc, display.Value.ToUniversalTime());
    }

    // The bug: a TimestampOffset value must display its stored instant. The old code used
    // DateTimeOffset.DateTime and shifted the value by the offset. Rows cover the reported Pacific offset,
    // UTC (no regression), and a fractional east offset.
    [TestMethod]
    [DataRow(-7.0)]
    [DataRow(0.0)]
    [DataRow(5.5)]
    public void TimestampOffsetValue_DisplaysCorrectInstant_Issue14203(double offsetHours)
    {
        var offset = TimeSpan.FromHours(offsetHours);
        var value = new DateTimeOffset(2024, 6, 15, 13, 30, 0, offset);

        DateTime? display = DateTimePickerFormInputView.ConvertToLocalDisplayValue(value);

        Assert.IsNotNull(display);
        Assert.AreEqual(value.UtcDateTime, display.Value.ToUniversalTime(), $"Offset {offsetHours}h must display the stored instant.");
        // The old projection differs from the fix by exactly the stored offset (zero when the offset is zero).
        Assert.AreEqual(offset, value.DateTime.ToLocalTime() - display.Value, $"Offset {offsetHours}h regression guard.");
    }

    [TestMethod]
    public void NullValue_ReturnsNull()
    {
        Assert.IsNull(DateTimePickerFormInputView.ConvertToLocalDisplayValue(null));
    }
}
