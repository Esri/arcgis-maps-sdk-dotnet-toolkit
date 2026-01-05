using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System.Diagnostics;
using System.Reflection;
using ImageMagick;

namespace Toolkit.UITest.Shared;

internal abstract partial class AppiumTestBase
{
    private string? baselinesDirPath;
    private string BaselinesDirectory
    {
        get
        {
            if (baselinesDirPath == null)
                baselinesDirPath = GetBaselinesFolder();
            return baselinesDirPath;
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
        Screenshot screenshot;
        if (element == null)
        {
            screenshot = Driver.GetScreenshot();
        }
        else
        {
            screenshot = element.GetScreenshot();
        }

        var baselinePath = Path.Combine(BaselinesDirectory, baselineName + ".png");
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
}
