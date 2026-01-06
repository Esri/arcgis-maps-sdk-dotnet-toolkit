namespace Toolkit.UITest.Shared;

internal abstract partial class AppiumTestBase
{
    protected void OpenSample(string sampleName)
    {
        // TODO: Modify the Sample app to allow a more generic means to navigate to particular samples
        switch (sampleName)
        {
            case "ScaleLine":
                FindElementByName("Scale Line ").Click();
                break;
            case "PopupViewer":
                FindElementByName("Popup Viewer ").Click();
                break;
            case null:
            case "":
                throw new ArgumentException("Sample name cannot be null or empty.", nameof(sampleName));
            default:
                throw new ArgumentException("Navigation for sample is not implemented. Consider modifying the Sample app to allow a more generic means to navigate to particular samples, then rewrite this method.", nameof(sampleName));
        }
    }
}
