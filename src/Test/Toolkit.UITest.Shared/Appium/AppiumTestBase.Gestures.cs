using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;

namespace Toolkit.UITest.Shared;

internal abstract partial class AppiumTestBase
{
    protected void DragCoordinates(double fromX, double fromY, double toX, double toY)
    {
        OpenQA.Selenium.Appium.Interactions.PointerInputDevice touchDevice = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(PointerKind.Touch);
        var dragSequence = new ActionSequence(touchDevice, 0);
        dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, (int)fromX, (int)fromY, TimeSpan.Zero));
        dragSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));
        dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, (int)toX, (int)toY, TimeSpan.FromMilliseconds(250)));
        dragSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
        Driver.PerformActions(new List<ActionSequence> { dragSequence });
    }

    protected void ZoomIn(int x, int y)
    {
        switch (Platform)
        {
            case TestPlatform.WinUI:
            case TestPlatform.MauiWinUI:
            case TestPlatform.WPF:
                WindowsScrollZoomIn(x, y);
                break;
            case TestPlatform.MauiAndroid:
            case TestPlatform.MauiIOS:
                PinchToZoomInCoordinates(x, y);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Platform), Platform, "Unrecognized platform.");
        }
    }

    protected void WindowsScrollZoomIn(int x, int y, int deltaY = 10)
    {
        Driver.ExecuteScript("windows: scroll", new Dictionary<string, object>
            {
                { "x", x },
                { "y", y },
                { "deltaY", deltaY }
            });
    }

    protected void PinchToZoomInCoordinates(int x, int y, int distance = 100, int durationMilliseconds = 250)
    {
        var duration = TimeSpan.FromMilliseconds(durationMilliseconds);

        int touch1StartX = x;
        int touch1StartY = y;
        int touch1EndX = x - distance;
        int touch1EndY = y;

        OpenQA.Selenium.Appium.Interactions.PointerInputDevice touch1 = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(PointerKind.Touch);
        ActionSequence touch1Sequence = new ActionSequence(touch1, 0);
        touch1Sequence.AddAction(touch1.CreatePointerMove(CoordinateOrigin.Viewport, touch1StartX, touch1StartY, TimeSpan.Zero));
        touch1Sequence.AddAction(touch1.CreatePointerDown(PointerButton.TouchContact));
        touch1Sequence.AddAction(touch1.CreatePointerMove(CoordinateOrigin.Viewport, touch1EndX, touch1EndY, duration));
        touch1Sequence.AddAction(touch1.CreatePointerUp(PointerButton.TouchContact));

        int touch2StartX = x;
        int touch2StartY = y;
        int touch2EndX = x + distance;
        int touch2EndY = y;

        OpenQA.Selenium.Appium.Interactions.PointerInputDevice touch2 = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(PointerKind.Touch);
        ActionSequence touch2Sequence = new ActionSequence(touch2, 0);
        touch2Sequence.AddAction(touch2.CreatePointerMove(CoordinateOrigin.Viewport, touch2StartX, touch2StartY, TimeSpan.Zero));
        touch2Sequence.AddAction(touch2.CreatePointerDown(PointerButton.TouchContact));
        touch2Sequence.AddAction(touch2.CreatePointerMove(CoordinateOrigin.Viewport, touch2EndX, touch2EndY, duration));
        touch2Sequence.AddAction(touch2.CreatePointerUp(PointerButton.TouchContact));

        Driver.PerformActions(new List<ActionSequence> { touch1Sequence, touch2Sequence });
    }
}
