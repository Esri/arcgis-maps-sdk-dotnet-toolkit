using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Support.UI;

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
#if WPF_TEST || WINUI_TEST
    [TestMethod]
    public async Task PopupViewer_Text_FullTextIsExposed()
    {
        OpenSample(PopupViewerFieldsPage);


        // On WPF the full text is exposed on TextPopupElementView itself; the inner RichTextBox ("TextArea")
        // is intentionally hidden from UIA so the text isn't announced twice.
#if WPF_TEST
        var textArea = FindElementByClassName("TextPopupElementView", DefaultTimeout);
#else
        var textArea = FindElement("TextArea", DefaultTimeout);
#endif
        var name = GetAutomationName(textArea);
        Assert.IsTrue(name.Contains(TextElementFirstParagraph), "Expected the text element's accessible name to include the first paragraph.");
        Assert.IsTrue(name.Contains(TextElementSecondParagraph), "Expected the text element's accessible name to include the second paragraph, not just the first line.");
    }

    [TestMethod]
    public async Task PopupViewerFields_ImplementsTableControlPattern()
    {
        OpenSample(PopupViewerFieldsPage);

        // Name-based lookup would ambiguously match the "Fields" title header text above the table (a
        // separate element) rather than the table itself, which has no accessible name of its own. The
        // FieldsPopupElementView itself is hidden from UIA and the table is exposed by its inner AccessibleGrid,
        // so look it up by its control type.
        var fieldsElement = new WebDriverWait(Driver, DefaultTimeout).Until(d => Driver.FindElement(MobileBy.XPath("//Table")));
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

#if WPF_TEST
        var previousButton = FindElement("PreviousButton", DefaultTimeout);
        var nextButton = FindElement("NextButton", DefaultTimeout);
        Assert.IsFalse(string.IsNullOrWhiteSpace(GetAutomationName(previousButton)), "Expected the previous-media button to have a non-empty accessible name.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(GetAutomationName(nextButton)), "Expected the next-media button to have a non-empty accessible name.");
#else
        // WinUI pages through media with a FlipView + PipsPager instead of prev/next buttons; each pip is the
        // navigation control, and WinUI gives it a localized name ("Page 1", "Page 2", ...).
        var pips = FindMediaPagerPips();
        Assert.AreEqual(2, pips.Count, "Expected one pager pip per media item.");
        foreach (var pip in pips)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(GetAutomationName(pip)), "Expected each media pager pip to have a non-empty accessible name.");
        }
#endif
    }

    [TestMethod]
    public async Task PopupViewer_Media_AnnouncesPositionInSet()
    {
        OpenSample(PopupViewerFieldsPage);

#if WPF_TEST
        var nextButton = FindElement("NextButton", DefaultTimeout);
        Click(nextButton);

        var currentItemHost = FindElement("CurrentMediaView", DefaultTimeout);
        var positionInSet = await WaitForAttributeAsync(currentItemHost, "PositionInSet", "2");
        var sizeOfSet = currentItemHost.GetAttribute("SizeOfSet");
        TestContext.WriteLine($"Media host PositionInSet=\"{positionInSet}\" SizeOfSet=\"{sizeOfSet}\"");
        Assert.AreEqual("2", positionInSet, "Expected the second media item to report position 2 in the set after clicking Next.");
        Assert.AreEqual("2", sizeOfSet, "Expected the media set size to be reported as 2.");
#else
        // On WinUI the position is announced from the pager pip that selects the item ("Page 2, 2 of 2").
        var secondPip = FindMediaPagerPips()[1];
        Click(secondPip);
        Assert.IsTrue(ElementExistsByName(ChartMediaTitle, DefaultTimeout), "Expected clicking the second pager pip to show the second media item.");

        var positionInSet = secondPip.GetAttribute("PositionInSet");
        var sizeOfSet = secondPip.GetAttribute("SizeOfSet");
        TestContext.WriteLine($"Second pip PositionInSet=\"{positionInSet}\" SizeOfSet=\"{sizeOfSet}\"");
        Assert.AreEqual("2", positionInSet, "Expected the second media item's pip to report position 2 in the set.");
        Assert.AreEqual("2", sizeOfSet, "Expected the media set size to be reported as 2.");
#endif
    }

    [TestMethod]
    public async Task PopupViewer_Media_KeyboardNavigationWorks()
    {
        OpenSample(PopupViewerFieldsPage);

        const int VK_LEFT = 0x25;
        const int VK_RIGHT = 0x27;
#if WPF_TEST
        var previousButton = FindElement("PreviousButton", DefaultTimeout);
        Click(previousButton); // Focuses the button and wraps around to the last (second) media item.

        var currentItemHost = FindElement("CurrentMediaView", DefaultTimeout);
        Assert.AreEqual("2", await WaitForAttributeAsync(currentItemHost, "PositionInSet", "2"), "Expected clicking Previous from the first item to wrap around to the last item.");

        PressKey(VK_LEFT);
        Assert.AreEqual("1", await WaitForAttributeAsync(currentItemHost, "PositionInSet", "1"), "Expected the Left arrow key to navigate to the previous media item.");

        PressKey(VK_RIGHT);
        Assert.AreEqual("2", await WaitForAttributeAsync(currentItemHost, "PositionInSet", "2"), "Expected the Right arrow key to navigate to the next media item.");
#else
        // WinUI's PipsPager moves focus between pips with the arrow keys and selects the focused pip with
        // Space/Enter. Clicking the (already selected) first pip puts keyboard focus on the pager.
        const int VK_RETURN = 0x0D;
        const int VK_SPACE = 0x20;
        Click(FindMediaPagerPips()[0]);
        Assert.IsTrue(ElementExistsByName(ImageMediaAlternativeText, DefaultTimeout), "Expected the first media item to be shown initially.");

        PressKey(VK_RIGHT);
        PressKey(VK_SPACE);
        Assert.IsTrue(ElementExistsByName(ChartMediaTitle, DefaultTimeout), "Expected Right arrow + Space to navigate to the next media item.");

        PressKey(VK_LEFT);
        PressKey(VK_RETURN);
        Assert.IsTrue(ElementExistsByName(ImageMediaAlternativeText, DefaultTimeout), "Expected Left arrow + Enter to navigate to the previous media item.");
#endif
    }

#if WINUI_TEST
    // The FlipView's own UIA control type isn't one WinAppDriver understands, so its items can't be inspected
    // directly; the PipsPager's page buttons are the accessible navigation surface for the media element instead.
    private System.Collections.ObjectModel.ReadOnlyCollection<AppiumElement> FindMediaPagerPips()
    {
        var pager = FindElement("PipsPager", DefaultTimeout);
        return pager.FindElements(MobileBy.ClassName("Button"));
    }
#endif

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
#if WPF_TEST
        // WPF's ListViewItemAutomationPeer inherits its UIA ClassName from ListBoxItemAutomationPeer.
        var listItems = attachmentList.FindElements(MobileBy.ClassName("ListBoxItem"));
#else
        var listItems = attachmentList.FindElements(MobileBy.ClassName("ListViewItem"));
#endif
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
#endif
}
