using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;

namespace Toolkit.UITest.Shared;

public abstract partial class AppiumTestBase
{
    protected void TapCoordinates(int x, int y)
    {
#if WINDOWS_TEST
        Driver.ExecuteScript("windows: click", new Dictionary<string, object>
        {
            { "x", x },
            { "y", y }
        });
#else
        throw new NotImplementedException("TapCoordinates is not implemented for this platform.");
#endif
    }

    protected void DragCoordinates(double fromX, double fromY, double toX, double toY)
    {
#if ANDROID_TEST || IOS_TEST || WINDOWS_TEST
        OpenQA.Selenium.Appium.Interactions.PointerInputDevice touchDevice = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(PointerKind.Touch);
        var dragSequence = new ActionSequence(touchDevice, 0);
        dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, (int)fromX, (int)fromY, TimeSpan.Zero));
        dragSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));
        dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, (int)toX, (int)toY, TimeSpan.FromMilliseconds(250)));
        dragSequence.AddAction(touchDevice.CreatePause(TimeSpan.FromMilliseconds(100)));
        dragSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
        Driver.PerformActions(new List<ActionSequence> { dragSequence });
#elif MAC_TEST
        var a1 = new Actions(Driver);
        a1
            .MoveToLocation((int)fromX, (int)fromY)
            .ClickAndHold()
            .MoveToLocation((int)toX, (int)toY)
            .Release()
            .Perform();
#endif
    }

    protected void ZoomIn(int x, int y)
    {
#if WINDOWS_TEST
        WindowsScrollZoomIn(x, y);
#elif MAC_TEST
        MacDoubleTapZoomIn(x, y);
#elif ANDROID_TEST || IOS_TEST
        PinchToZoomInCoordinates(x, y);
#else
        throw new NotImplementedException("ZoomIn is not implemented for this platform.");
#endif
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

    protected void MacDoubleTapZoomIn(int x, int y, int deltaY = 10, AppiumElement? element = null)
    {
        Driver.ExecuteScript("macos: doubleClick", new Dictionary<string, object>
        {
            {"x", x},
            {"y", y}
        });
    }

    protected void PinchToZoomInCoordinates(int x, int y, int distance = 100, int durationMilliseconds = 250)
    {
        var duration = TimeSpan.FromMilliseconds(durationMilliseconds);

        int touch1StartX = x;
        int touch1StartY = y;
        int touch1EndX = x - distance / 2;
        int touch1EndY = y;

        OpenQA.Selenium.Appium.Interactions.PointerInputDevice touch1 = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(PointerKind.Touch);
        ActionSequence touch1Sequence = new ActionSequence(touch1, 0);
        touch1Sequence.AddAction(touch1.CreatePointerMove(CoordinateOrigin.Viewport, touch1StartX, touch1StartY, TimeSpan.Zero));
        touch1Sequence.AddAction(touch1.CreatePointerDown(PointerButton.TouchContact));
        touch1Sequence.AddAction(touch1.CreatePause(TimeSpan.FromMilliseconds(100)));
        touch1Sequence.AddAction(touch1.CreatePointerMove(CoordinateOrigin.Viewport, touch1EndX, touch1EndY, duration));
        touch1Sequence.AddAction(touch1.CreatePause(TimeSpan.FromMilliseconds(100)));
        touch1Sequence.AddAction(touch1.CreatePointerUp(PointerButton.TouchContact));

        int touch2StartX = x;
        int touch2StartY = y;
        int touch2EndX = x + distance / 2;
        int touch2EndY = y;

        OpenQA.Selenium.Appium.Interactions.PointerInputDevice touch2 = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(PointerKind.Touch);
        ActionSequence touch2Sequence = new ActionSequence(touch2, 0);
        touch2Sequence.AddAction(touch2.CreatePointerMove(CoordinateOrigin.Viewport, touch2StartX, touch2StartY, TimeSpan.Zero));
        touch2Sequence.AddAction(touch2.CreatePointerDown(PointerButton.TouchContact));
        touch1Sequence.AddAction(touch1.CreatePause(TimeSpan.FromMilliseconds(100)));
        touch2Sequence.AddAction(touch2.CreatePointerMove(CoordinateOrigin.Viewport, touch2EndX, touch2EndY, duration));
        touch2Sequence.AddAction(touch2.CreatePause(TimeSpan.FromMilliseconds(100)));
        touch2Sequence.AddAction(touch2.CreatePointerUp(PointerButton.TouchContact));

        Driver.PerformActions(new List<ActionSequence> { touch1Sequence, touch2Sequence });
    }
}
