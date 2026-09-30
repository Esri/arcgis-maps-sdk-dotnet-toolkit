using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared.PopupViewer;

[TestClass]
public class PopupViewerTests : AppiumTestBase
{
    private const string PopupViewerFieldsPage = "PopupViewerFields";

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    // Mirrors the constants in Toolkit.UITests.App.TestPages.PopupViewerFields (the shared test-page
    // code-behind), which builds the same fully-offline Popup on every platform.
    private const string PopupTitle = "Ridgeline Trailhead";
    private const string MediaElementTitle = "Media";
    private const string TextElementFirstParagraph = "This trailhead connects to the Ridgeline Loop and Summit Spur trails.";
    private const string TextElementSecondParagraph = "Parking is available year-round, but the upper trail closes seasonally.";
    private const string ImageMediaAlternativeText = "Illustrated map showing three looping trails around the trailhead";
    private const string ChartMediaTitle = "Monthly Visitors";
    private const string ChartMediaCaption = "Visitor counts by month";

    // Mirrors the attachments created by Toolkit.UITests.App.TestPages.PopupViewerAttachments (the shared test-page code-behind).
    private const string PopupViewerAttachmentsPage = "PopupViewerAttachments";
    private static readonly string[] AttachmentNames = ["trail-map.png", "parking-permit.pdf", "ranger-notes.txt"];

    [TestMethod]
    public async Task PopupViewer_Text_FullTextIsExposed()
    {
        OpenSample(PopupViewerFieldsPage);

#if WPF_TEST
        // On WPF the full text is exposed on TextPopupElementView itself; the inner RichTextBox ("TextArea")
        // is intentionally hidden from UIA so the text isn't announced twice.
        var textArea = FindElementByClassName("TextPopupElementView", DefaultTimeout);
#else
        var textArea = FindElement("TextArea", DefaultTimeout);
#endif
        var name = GetAutomationName(textArea);
        Assert.IsTrue(name.Contains(TextElementFirstParagraph), "Expected the text element's accessible name to include the first paragraph.");
        Assert.IsTrue(name.Contains(TextElementSecondParagraph), "Expected the text element's accessible name to include the second paragraph, not just the first line.");
    }

#if WPF_TEST
    [TestMethod]
    public async Task PopupViewerFields_ImplementsTableControlPattern()
    {
        OpenSample(PopupViewerFieldsPage);

        // Name-based lookup would ambiguously match the "Fields" title header text above the table (a
        // separate element) rather than the table itself, which has no accessible name of its own - the
        // table's peer sets a distinct ClassName (see FieldsPopupElementViewAutomationPeer.GetClassNameCore()).
        var fieldsElement = FindElementByClassName("FieldsPopupElementView", DefaultTimeout);
        var controlType = GetControlType(fieldsElement);
        var localizedControlType = GetLocalizedControlType(fieldsElement);
        TestContext.WriteLine($"Fields element ControlType=\"{controlType}\" LocalizedControlType=\"{localizedControlType}\"");
        Assert.IsTrue(
            controlType.Contains("Table", StringComparison.OrdinalIgnoreCase) || localizedControlType.Contains("table", StringComparison.OrdinalIgnoreCase),
            $"Expected the fields element to be exposed as a table/grid to UIA, but ControlType was \"{controlType}\" and LocalizedControlType was \"{localizedControlType}\".");
    }

    [TestMethod]
    public async Task PopupViewer_Media_PrevNextButtonsHaveLocalizedNames()
    {
        OpenSample(PopupViewerFieldsPage);

        var previousButton = FindElement("PreviousButton", DefaultTimeout);
        var nextButton = FindElement("NextButton", DefaultTimeout);
        Assert.IsFalse(string.IsNullOrWhiteSpace(GetAutomationName(previousButton)), "Expected the previous-media button to have a non-empty accessible name.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(GetAutomationName(nextButton)), "Expected the next-media button to have a non-empty accessible name.");
    }

    [TestMethod]
    public async Task PopupViewer_Media_AnnouncesPositionInSet()
    {
        OpenSample(PopupViewerFieldsPage);

        var nextButton = FindElement("NextButton", DefaultTimeout);
        Click(nextButton);

        var currentItemHost = FindElement("CurrentMediaView", DefaultTimeout);
        var positionInSet = await WaitForAttributeAsync(currentItemHost, "PositionInSet", "2");
        var sizeOfSet = currentItemHost.GetAttribute("SizeOfSet");
        TestContext.WriteLine($"Media host PositionInSet=\"{positionInSet}\" SizeOfSet=\"{sizeOfSet}\"");
        Assert.AreEqual("2", positionInSet, "Expected the second media item to report position 2 in the set after clicking Next.");
        Assert.AreEqual("2", sizeOfSet, "Expected the media set size to be reported as 2.");
    }

    [TestMethod]
    public async Task PopupViewer_Media_KeyboardNavigationWorks()
    {
        OpenSample(PopupViewerFieldsPage);

        var previousButton = FindElement("PreviousButton", DefaultTimeout);
        Click(previousButton); // Focuses the button and wraps around to the last (second) media item.

        var currentItemHost = FindElement("CurrentMediaView", DefaultTimeout);
        Assert.AreEqual("2", await WaitForAttributeAsync(currentItemHost, "PositionInSet", "2"), "Expected clicking Previous from the first item to wrap around to the last item.");

        const int VK_LEFT = 0x25;
        const int VK_RIGHT = 0x27;
        PressKey(VK_LEFT);
        Assert.AreEqual("1", await WaitForAttributeAsync(currentItemHost, "PositionInSet", "1"), "Expected the Left arrow key to navigate to the previous media item.");

        PressKey(VK_RIGHT);
        Assert.AreEqual("2", await WaitForAttributeAsync(currentItemHost, "PositionInSet", "2"), "Expected the Right arrow key to navigate to the next media item.");
    }

    // Click()/PressKey() only queue input for the app, and UIA property reads are serviced ahead of queued input,
    // so reading an attribute immediately afterwards can observe the state from before the input was handled.
    // Polls until the attribute reaches the expected value (or the timeout elapses) and returns the last value read.
    private static async Task<string> WaitForAttributeAsync(OpenQA.Selenium.Appium.AppiumElement element, string attribute, string expected)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        var value = element.GetAttribute(attribute);
        while (value != expected && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
            value = element.GetAttribute(attribute);
        }
        return value;
    }

    [TestMethod]
    public async Task PopupViewer_Attachments_ListItemsExposeAttachmentName()
    {
        OpenSample(PopupViewerAttachmentsPage);

        // The attachment list is only made visible once the attachments have been fetched from the feature.
        var attachmentList = FindElement("AttachmentList", TimeSpan.FromSeconds(15));
        // WPF's ListViewItemAutomationPeer inherits its UIA ClassName from ListBoxItemAutomationPeer.
        var listItems = attachmentList.FindElements(MobileBy.ClassName("ListBoxItem"));
        Assert.AreEqual(AttachmentNames.Length, listItems.Count, "Expected one ListViewItem per attachment.");

        foreach (var attachmentName in AttachmentNames)
        {
            // Scope to ListViewItems so the attachment name TextBlock inside the item template can't satisfy the lookup.
            var matches = listItems.Where(item => GetAutomationName(item) == attachmentName).ToList();
            Assert.AreEqual(1, matches.Count, $"Expected exactly one ListViewItem with accessible name \"{attachmentName}\".");
            var controlType = GetControlType(matches[0]);
            Assert.IsTrue(controlType.Contains("ListItem", StringComparison.OrdinalIgnoreCase),
                $"Expected attachment \"{attachmentName}\" to be exposed as a list item, but ControlType was \"{controlType}\".");
        }
    }
#endif

    [TestMethod]
    public async Task PopupViewer_Media_AccessibleDescriptionForDiagrams()
    {
        OpenSample(PopupViewerFieldsPage);

        // The image media item is shown first and has an explicit AlternativeText.
        Assert.IsTrue(ElementExistsByName(ImageMediaAlternativeText, DefaultTimeout), "Expected the image media's accessible name/description to be its AlternativeText.");

#if WPF_TEST
        // Advance to the chart media item, which has no AlternativeText, to exercise the Title/Caption fallback.
        var nextButton = FindElement("NextButton", DefaultTimeout);
        Click(nextButton);
        Assert.IsTrue(
            ElementExistsByName(ChartMediaTitle, DefaultTimeout) || ElementExistsByName(ChartMediaCaption, DefaultTimeout),
            "Expected the chart media (which has no AlternativeText) to fall back to its Title or Caption for its accessible description.");
#endif
    }
}
