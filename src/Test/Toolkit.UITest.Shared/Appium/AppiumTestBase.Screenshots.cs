using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System.Reflection;
using ImageMagick;

namespace Toolkit.UITest.Shared;

internal abstract partial class AppiumTestBase
{
    private string? _baselinesDirPath;
    private ScreenDensity? _screenDensity;
#if WINDOWS_TEST
    private float[] _supportedDensities = new float[] { 1.0f, 1.25f, 1.5f, 1.75f };
#elif ANDROID_TEST
    private float[] _supportedDensities = new float[] { 1.0f, 2.0f, 3.0f, 4.0f };
#elif IOS_TEST || MAC_TEST
    private float[] _supportedDensities = new float[] { 1.0f, 2.0f, 3.0f, 4.0f };
#endif

    private ScreenDensity DeviceScreenDensity
    {
        get
        {
            if (_screenDensity == null)
                _screenDensity = GetScreenDensity();
            return _screenDensity.Value;
        }
    }

    private string BaselinesDirectory
    {
        get
        {
            if (_baselinesDirPath == null)
                _baselinesDirPath = GetBaselinesFolder();
            return _baselinesDirPath;
        }
    }
    private string ComparisonFailuresDirectory {
        get {
            var path = Path.Combine(BaselinesDirectory, "Failures");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }

    protected void CompareBaseline(string baselineName, AppiumElement? element = null)
    {
        if (DeviceScreenDensity == ScreenDensity.Unsupported)
        {
            TestContext.Out.WriteLine($"Screen density is unsupported; skipping baseline comparison for baseline {baselineName}.");
            return;
        }

        Screenshot screenshot;
        if (element == null)
        {
            screenshot = Driver.GetScreenshot();
        }
        else
        {
            screenshot = element.GetScreenshot();
        }

        var baselinePath = Path.Combine(BaselinesDirectory, baselineName + "_" + DeviceScreenDensity + ".png");
        if (!File.Exists(baselinePath))
        {
            screenshot.SaveAsFile(baselinePath);
        }
        else
        {
            var screenshotImage = new MagickImage(screenshot.AsByteArray);
            using (var baselineImage = new MagickImage(baselinePath))
            {
                var diffImage = baselineImage.Compare(screenshotImage, ErrorMetric.RootMeanSquared, out var distortion);
                if (distortion > 0)
                {
                    screenshotImage.Write(Path.Combine(ComparisonFailuresDirectory, baselineName + "_failed.png"));
                    diffImage.Write(Path.Combine(ComparisonFailuresDirectory, baselineName + "_diff.png"));
                    throw new Exception($"\nBaseline comparison failed for '{baselineName}'. " +
                        $"\nDistortion: {distortion}. " +
                        $"\nSee failed screenshot and diff image in '{ComparisonFailuresDirectory}'.");
                }
            }
        }
    }

    private string GetBaselinesFolder()
    {
        var testAssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new InvalidOperationException("Could not determine test assembly directory.");

        var baselinesFolder = Path.Combine(testAssemblyDir, "..", "..", "..", "..", "UITestBaselines", GetPlatformFolderName());
        baselinesFolder = Path.GetFullPath(baselinesFolder);

        if (!Directory.Exists(baselinesFolder))
        {
            throw new DirectoryNotFoundException(
                $"Baselines folder not found at '{baselinesFolder}'.");
        }

        return baselinesFolder;
    }

    private MagickImage? GetBaseline(string testName)
    {
        var imagePath = Path.Combine(BaselinesDirectory, testName + ".png");
        if (!File.Exists(imagePath))
            return null;
        else
            return new MagickImage(imagePath);
    }

    private string GetPlatformFolderName()
    {
#if WPF_TEST
        return "WPF";
#elif WINUI_TEST
        return "WinUI";
#elif WINDOWS_TEST && MAUI_TEST
        return "MauiWindows";
#elif ANDROID_TEST && MAUI_TEST
        return "MauiAndroid";
#elif IOS_TEST && MAUI_TEST
        return "MauiIOS";
#elif MAC_TEST && MAUI_TEST
        return "MauiMac";
#else
        throw new NotImplementedException("GetPlatformFolderName is not implemented for this platform.");
#endif
    }

    private ScreenDensity GetScreenDensity()
    {
        if (AppiumSetup.ScreenDensity == null)
        {
            TestContext.Out.WriteLine("Warning: Screen density could not be determined.");
            return ScreenDensity.Unsupported;
        }

        ScreenDensity density;
        var densityIndex = _supportedDensities.IndexOf(AppiumSetup.ScreenDensity.Value);

        density = densityIndex switch
        {
            0 => ScreenDensity.Low,
            1 => ScreenDensity.Medium,
            2 => ScreenDensity.High,
            3 => ScreenDensity.XHigh,
            4 => ScreenDensity.XXHigh,
            _ => ScreenDensity.Unsupported,
        };

        if (density == ScreenDensity.Unsupported)
        {
            TestContext.Out.WriteLine($"Warning: Unsupported screen scale {AppiumSetup.ScreenDensity}. Screenshot coparisons will either fail or be skipped.");
            TestContext.Out.WriteLine($"Supported screen scales are {string.Join(", ", _supportedDensities)}.");
#if WINDOWS_TEST
            TestContext.Out.WriteLine("Consider changing the display scale in the Windows Display settings.");
#elif ANDROID_TEST
            TestContext.Out.WriteLine("Consider changing the screen density using the `adb shell wm density {density}` terminal command. Supported DPIs are multiples of 160.");
#endif
        }
        return density;
    }

    /// <summary>
    /// Enums corresponding to different screen densities. The exact DPIs each enum value represents vary by platform.
    /// </summary>
    private enum ScreenDensity
    {
        Low,
        Medium,
        High,
        XHigh,
        XXHigh,
        Unsupported
    }
}
