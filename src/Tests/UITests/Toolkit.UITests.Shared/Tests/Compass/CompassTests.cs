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
        OpenSample(CompassMapPage);

        var rotations = new double[] { 0d, 90d, 180d, 270d };
        var expectedOrientations = new CompassOrientation[] { CompassOrientation.North, CompassOrientation.West, CompassOrientation.South, CompassOrientation.East };
        for (var i = 0; i < rotations.Length; i++)
        {
            SetRotation(rotations[i]);
            var orientation = await GetCompassOrientationAsync(compassType);
            Assert.AreEqual(expectedOrientations[i], orientation);
        }
    }

    [TestMethod]
    [DataRow(CompassType.MapBound)]
    [DataRow(CompassType.HeadingBound)]
    public async Task Compass_AutoHides(CompassType compassType)
    {
        OpenSample(CompassMapPage);
        var compassElement = GetCompassElement(compassType);

        // Check that the compass hides when facing north
        var autoHideButton = FindElement("ToggleAutoHideButton");
        SetRotation(0);
        autoHideButton.Click();

        var maxTries = 5;
        var tryCount = 0;
        while (tryCount < maxTries)
        {
            var screenshot = GetScreenshot(compassElement);
            // If nothing renders (ie. the screenshot is a uniform color) the compass successfully hid
            if (screenshot.Histogram().Count < 2)
                break;

            // Otherwise continue waiting
            tryCount++;
            if (tryCount < maxTries)
                await Task.Delay(500);
        }
        Assert.IsLessThan(maxTries, tryCount, "Exceeded timeout while waiting for the compass to hide.");

        // Check that the compass renders back in when not facing north
        SetRotation(90);
        var compassDirection = await GetCompassOrientationAsync(compassType);
        Assert.AreEqual(CompassOrientation.West, compassDirection);
    }

    [TestMethod]
    public async Task Compass_ClickToResetMapRotation()
    {
        OpenSample(CompassMapPage);

        SetRotation(90d);

        var compassElement = GetCompassElement(CompassType.MapBound);
        compassElement.Click();

        var mapRotationElement = FindElement("MapRotationText");
        var mapRotation = double.Parse(mapRotationElement.Text);
        Assert.AreEqual(0, mapRotation, 0.001);
    }

    private AppiumElement GetCompassElement(CompassType compassType)
    {
        return FindElement(compassType == CompassType.MapBound ? "MapCompass" : "HeadingCompass");
    }

    private void SetRotation(double rotation)
    {
        var rotationInputElement = FindElement("RotateInput");
        var rotateButtonElement = FindElement("RotateButton");
        SubmitText(rotationInputElement, rotation.ToString());
        rotateButtonElement.Click();
    }

    /// <summary>
    /// Gets the orientation of the specified compass based on a screenshot analysis. If the compass is hidden or stuck partially
    /// transparent, the function will raise an Assert.Fail exception.
    /// </summary>
    private async Task<CompassOrientation> GetCompassOrientationAsync(CompassType compassType)
    {
        var compassElement = GetCompassElement(compassType);
        MagickImage? compassScreenshot = null;

        // Wait for the compass to fade in
        var loaded = false;
        var tryCount = 0;
        while (tryCount < 10)
        {
            var screenshot = GetScreenshot(compassElement);

            foreach (var color in screenshot.GetPixels())
            {
                if (color[0] >= 128 && (color[1] + color[2] == 0))
                {
                    loaded = true;
                    compassScreenshot = screenshot;
                    break;
                }
            }

            tryCount++;
            await Task.Delay(200);
        }

        if (!loaded)
            Assert.Fail("Compass never finished fading in. If the compass is rendering visually, make sure any screen overlays (such as blue light filters) are turned off.");

        // Mask to only red pixels (aka the north arrow)
        compassScreenshot!.ColorThreshold(new MagickColor(128, 0, 0), new MagickColor(255, 80, 80));

        // Get the arrow connected component
        var connectedComponents = compassScreenshot.ConnectedComponents(4);
        var arrowComponent = connectedComponents[1];

        // Determine direction based on the arrow's offset from center
        var imageCenterX = compassScreenshot.Width / 2d;
        var imageCenterY = compassScreenshot.Height / 2d;
        var offset = new PointD(arrowComponent.Centroid.X - imageCenterX, arrowComponent.Centroid.Y - imageCenterY);

        var direction = CompassOrientation.Unknown;
        if (offset.Y < -1d)
        {
            direction = CompassOrientation.North;
        }
        else if (offset.Y > 1d)
        {
            direction = CompassOrientation.South;
        }
        if (offset.X < -1d)
        {
            direction = CompassOrientation.West;
        }
        else if (offset.X > 1d)
        {
            direction = CompassOrientation.East;
        }
        return direction;
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
}