using ImageMagick;
using OpenQA.Selenium.Appium;
using Toolkit.UITest.Shared;

namespace Toolkit.UITests.Shared.Compass;

[TestClass]
public class CompassTests : AppiumTestBase
{
    private const string CompassMapPage = "CompassMap";

    [TestMethod]
    [DataRow(CompassType.MapBound)]
    [DataRow(CompassType.HeadingBound)]
    public async Task Compass_Rotates(CompassType compassType)
    {
        OpenCompassMapPage();

        var rotations = new double[] { 0d, 90d, 180d, 270d };
        var expectedOrientations = new CompassOrientation[] { CompassOrientation.North, CompassOrientation.West, CompassOrientation.South, CompassOrientation.East };
        for (var i = 0; i < rotations.Length; i++)
        {
            SetRotation(rotations[i]);
            await WaitForCompassOrientationAsync(compassType, expectedOrientations[i]);
        }
    }

    [TestMethod]
    [DataRow(CompassType.MapBound)]
    [DataRow(CompassType.HeadingBound)]
    public async Task Compass_AutoHides(CompassType compassType)
    {
        OpenCompassMapPage();
        var compassElement = GetCompassElement(compassType);

        // Check that the compass hides when facing north
        var autoHideButton = FindElement("ToggleAutoHideButton");
        SetRotation(0);
        Click(autoHideButton);

        var maxTries = 5;
        var hidden = false;
        for (var tryCount = 0; tryCount < maxTries; tryCount++)
        {
            using var screenshot = GetScreenshot(compassElement);
            // If nothing renders (ie. the screenshot is a uniform color) the compass successfully hid
            if (screenshot.Histogram().Count < 2)
            {
                hidden = true;
                break;
            }

            // Otherwise continue waiting
            if (tryCount < maxTries - 1)
                await Task.Delay(500);
        }
        Assert.IsTrue(hidden, "Exceeded timeout while waiting for the compass to hide.");

        // Check that the compass renders back in when not facing north
        SetRotation(90);
        await WaitForCompassOrientationAsync(compassType, CompassOrientation.West);
    }

    [TestMethod]
    public async Task Compass_ClickToResetMapRotation()
    {
        OpenCompassMapPage();

        SetRotation(90d);

        var compassElement = GetCompassElement(CompassType.MapBound);
        Click(compassElement);

        await WaitForMapRotationAsync(0);
    }

    private void OpenCompassMapPage()
    {
        OpenSample(CompassMapPage);
        FindElement("RotateInput", TimeSpan.FromSeconds(2));
        GetCompassElement(CompassType.MapBound, TimeSpan.FromSeconds(2));
        GetCompassElement(CompassType.HeadingBound, TimeSpan.FromSeconds(2));
    }

    private AppiumElement GetCompassElement(CompassType compassType, TimeSpan? timeout = null)
    {
        return FindElement(compassType == CompassType.MapBound ? "MapCompass" : "HeadingCompass", timeout);
    }

    private void SetRotation(double rotation)
    {
        var rotationInputElement = FindElement("RotateInput");
        var rotateButtonElement = FindElement("RotateButton");
        SubmitText(rotationInputElement, rotation.ToString());
        Click(rotateButtonElement);
    }

    /// <summary>
    /// Waits for the compass to report the expected orientation based on screenshot analysis.
    /// </summary>
    private async Task WaitForCompassOrientationAsync(CompassType compassType, CompassOrientation expectedOrientation)
    {
        var compassElement = GetCompassElement(compassType);
        var timeout = TimeSpan.FromSeconds(4);
        var retryDelay = TimeSpan.FromMilliseconds(200);
        var endTime = DateTime.UtcNow + timeout;
        CompassAnalysis? lastAnalysis = null;

        do
        {
            using var screenshot = GetScreenshot(compassElement);
            lastAnalysis = AnalyzeCompassOrientation(screenshot);

            if (lastAnalysis.Value.Orientation == expectedOrientation)
                return;

            if (DateTime.UtcNow < endTime)
                await Task.Delay(retryDelay);
        }
        while (DateTime.UtcNow < endTime);

        Assert.Fail($"Compass {compassType} did not reach expected orientation {expectedOrientation} within {timeout.TotalSeconds:0.#} seconds. {FormatAnalysis(lastAnalysis)}");
    }

    private static CompassAnalysis AnalyzeCompassOrientation(MagickImage compassScreenshot)
    {
        // Mask to only red pixels (aka the north arrow).
        compassScreenshot.ColorThreshold(new MagickColor(128, 0, 0), new MagickColor(255, 80, 80));

        var connectedComponents = compassScreenshot.ConnectedComponents(4);
        var componentCount = Math.Max(0, connectedComponents.Count - 1);
        var imageCenterX = compassScreenshot.Width / 2d;
        var imageCenterY = compassScreenshot.Height / 2d;

        if (componentCount == 0)
            return new CompassAnalysis(CompassOrientation.Unknown, componentCount, null, null);

        // ConnectedComponents includes the background as the first component. The arrow is the largest remaining blob.
        var arrowComponent = connectedComponents.Skip(1).OrderByDescending(component => component.Area).First();
        var offset = new PointD(arrowComponent.Centroid.X - imageCenterX, arrowComponent.Centroid.Y - imageCenterY);

        var direction = CompassOrientation.Unknown;
        if (Math.Abs(offset.Y) > Math.Abs(offset.X))
        {
            if (offset.Y < -1d)
                direction = CompassOrientation.North;
            else if (offset.Y > 1d)
                direction = CompassOrientation.South;
        }
        else
        {
            if (offset.X < -1d)
                direction = CompassOrientation.West;
            else if (offset.X > 1d)
                direction = CompassOrientation.East;
        }
        return new CompassAnalysis(direction, componentCount, arrowComponent.Centroid, offset);
    }

    private static string FormatAnalysis(CompassAnalysis? analysis)
    {
        if (analysis is null)
            return "No screenshots were analyzed.";

        return $"Last orientation: {analysis.Value.Orientation}; component count: {analysis.Value.ComponentCount}; centroid: {FormatPoint(analysis.Value.Centroid)}; offset: {FormatPoint(analysis.Value.Offset)}.";
    }

    private static string FormatPoint(PointD? point)
    {
        return point is null ? "none" : $"({point.Value.X:0.##}, {point.Value.Y:0.##})";
    }

    private async Task WaitForMapRotationAsync(double expectedRotation)
    {
        var mapRotationElement = FindElement("MapRotationText");
        var timeout = TimeSpan.FromSeconds(4);
        var retryDelay = TimeSpan.FromMilliseconds(200);
        var endTime = DateTime.UtcNow + timeout;
        double? lastRotation = null;

        do
        {
            if (double.TryParse(GetElementText(mapRotationElement), out var mapRotation))
            {
                lastRotation = mapRotation;
                if (Math.Abs(mapRotation - expectedRotation) <= 0.001)
                    return;
            }

            if (DateTime.UtcNow < endTime)
                await Task.Delay(retryDelay);
        }
        while (DateTime.UtcNow < endTime);

        Assert.Fail($"Map rotation did not reach {expectedRotation} within {timeout.TotalSeconds:0.#} seconds. Last rotation: {(lastRotation.HasValue ? lastRotation.Value.ToString("0.###") : "unreadable")}.");
    }

    public enum CompassType
    {
        MapBound,
        HeadingBound
    }

    private enum CompassOrientation
    {
        Unknown = 0,
        North = 1,
        South = 2,
        East = 3,
        West = 4
    }

    private readonly record struct CompassAnalysis(CompassOrientation Orientation, int ComponentCount, PointD? Centroid, PointD? Offset);
}
