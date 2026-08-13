using ImageMagick;
using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared.BookmarksView;

[TestClass]
public class BookmarksViewTests : AppiumTestBase
{
    private const string BookmarksMapPage = "BookmarksMap";
    private const string BookmarksOnlineMapPage = "BookmarksOnlineMap";
    private const string BookmarksScenePage = "BookmarksScene";
    private const string BookmarksTemplateMapPage = "BookmarksTemplateMap";
    private const string BookmarksUpdatesPage = "BookmarksUpdates";

    [TestMethod]
    public void BookmarksView_Map()
    {
        OpenSample(BookmarksMapPage);

        var bookmarksList = FindElement("BookmarksListView", TimeSpan.FromSeconds(5));
#if WPF_TEST || WINUI_TEST
        Assert.AreEqual("Bookmarks", GetAutomationName(bookmarksList), "The bookmarks list should expose its accessible name.");
#endif

        var bookmarks = new (string name, string expectedCenterText)[]
        {
            ("Red bookmark", "10.0,10.0"),
            ("Green bookmark", "30.0,20.0"),
            ("Blue bookmark", "-30.0,-20.0"),
        };
        var map = FindElement("BookmarksMap", TimeSpan.FromSeconds(5));
        AssertNoBookmarkMarkersAreVisible(map, bookmarks);

        foreach (var (name, expectedCenterText) in bookmarks)
        {
            FindElementByName(name, TimeSpan.FromSeconds(5)).Click();
            FindElementByText(expectedCenterText, TimeSpan.FromSeconds(5));
            var selectedBookmarkText = FindElement("SelectedBookmarkText", TimeSpan.FromSeconds(5));
            Assert.AreEqual(name, GetLabelText(selectedBookmarkText), "The BookmarkSelected event should identify the selected bookmark.");
            AssertSelectedMarkerIsVisible(map, name);
        }
    }

    [TestMethod]
    public void BookmarksView_Style()
    {
        OpenSample(BookmarksTemplateMapPage);

        foreach (var bookmarkName in new[] { "Red bookmark", "Green bookmark", "Blue bookmark" })
        {
            FindElementByText($"Custom: {bookmarkName}", TimeSpan.FromSeconds(5));
#if WPF_TEST || WINUI_TEST
            FindElementByName(bookmarkName, TimeSpan.FromSeconds(5));
#endif
        }

        FindElement("SetRuntimeTemplateButton", TimeSpan.FromSeconds(5)).Click();
        foreach (var bookmarkName in new[] { "Red bookmark", "Green bookmark", "Blue bookmark" })
        {
            FindElementByText($"Runtime: {bookmarkName}", TimeSpan.FromSeconds(5));
            Assert.IsFalse(ElementExistsByText($"Custom: {bookmarkName}", TimeSpan.FromSeconds(1)), "The previous item template should no longer be rendered.");
        }
    }

    [TestMethod]
    public void BookmarksView_Scene()
    {
        OpenSample(BookmarksScenePage);

        var scene = FindElement("BookmarksScene", TimeSpan.FromSeconds(5));
        var bookmarks = new (string name, string colorName)[]
        {
            ("Red scene bookmark", "Red bookmark"),
            ("Green scene bookmark", "Green bookmark"),
            ("Blue scene bookmark", "Blue bookmark"),
        };

        foreach (var (name, colorName) in bookmarks)
        {
            FindElementByName(name, TimeSpan.FromSeconds(5)).Click();
            FindElementByText($"Ready: {name}", TimeSpan.FromSeconds(5));
            var selectedBookmarkText = FindElement("SelectedBookmarkText", TimeSpan.FromSeconds(5));
            Assert.AreEqual(name, GetLabelText(selectedBookmarkText), "The BookmarkSelected event should identify the selected bookmark.");
            AssertSelectedMarkerIsVisible(scene, colorName);
        }
    }

    [TestMethod]
    public void BookmarksView_OnlineMap()
    {
        OpenSample(BookmarksOnlineMapPage);

        FindElementByName("Red sands", TimeSpan.FromSeconds(30)).Click();
        FindElementByText("-12147304.5,4388705.4", TimeSpan.FromSeconds(30));
        var selectedBookmarkText = FindElement("SelectedBookmarkText", TimeSpan.FromSeconds(5));
        Assert.AreEqual("Red sands", GetLabelText(selectedBookmarkText), "The BookmarkSelected event should identify the selected bookmark.");
    }

    [TestMethod]
    public void BookmarksView_TracksDocumentBookmarks()
    {
        OpenSample(BookmarksUpdatesPage);
        FindElementByName("Map A bookmark", TimeSpan.FromSeconds(5));

        var addBookmarkButton = FindElement("AddDocumentBookmarkButton", TimeSpan.FromSeconds(5));
        addBookmarkButton.Click();
        var addedBookmark = FindElementByName("Added map bookmark", TimeSpan.FromSeconds(5));

        addedBookmark.Click();
        FindElementByName("Renamed map bookmark", TimeSpan.FromSeconds(5));
        Assert.IsFalse(ElementExistsByName("Added map bookmark", TimeSpan.FromSeconds(1)));
    }

    [TestMethod]
    public void BookmarksView_TracksBookmarksOverride()
    {
        OpenSample(BookmarksUpdatesPage);
        FindElementByName("Map A bookmark", TimeSpan.FromSeconds(5));

        // Change the current map before overriding it, then verify its latest bookmarks are restored below.
        FindElement("AddDocumentBookmarkButton", TimeSpan.FromSeconds(5)).Click();
        FindElementByName("Added map bookmark", TimeSpan.FromSeconds(5));

        // Set BookmarksOverride to an observable collection, replacing the map's bookmarks.
        FindElement("UseOverrideButton", TimeSpan.FromSeconds(5)).Click();
        FindElementByName("Override bookmark", TimeSpan.FromSeconds(5));
        Assert.IsFalse(ElementExistsByName("Map A bookmark", TimeSpan.FromSeconds(1)));
        Assert.IsFalse(ElementExistsByName("Added map bookmark", TimeSpan.FromSeconds(1)));

        // Add a bookmark to the active override collection.
        FindElement("AddOverrideButton", TimeSpan.FromSeconds(5)).Click();
        FindElementByName("Added override bookmark", TimeSpan.FromSeconds(5));

        // Clear BookmarksOverride so the current map's bookmarks are shown again.
        FindElement("ClearOverrideButton", TimeSpan.FromSeconds(5)).Click();
        FindElementByName("Map A bookmark", TimeSpan.FromSeconds(5));
        FindElementByName("Added map bookmark", TimeSpan.FromSeconds(5));
        Assert.IsFalse(ElementExistsByName("Override bookmark", TimeSpan.FromSeconds(1)));
    }

    [TestMethod]
    public void BookmarksView_TracksGeoViewDocument()
    {
        OpenSample(BookmarksUpdatesPage);
        FindElementByName("Map A bookmark", TimeSpan.FromSeconds(5));

        FindElement("UseMapBButton", TimeSpan.FromSeconds(5)).Click();
        FindElementByName("Map B bookmark", TimeSpan.FromSeconds(5));
        Assert.IsFalse(ElementExistsByName("Map A bookmark", TimeSpan.FromSeconds(1)));
    }

    [TestMethod]
    public void BookmarksView_TracksGeoView()
    {
        OpenSample(BookmarksUpdatesPage);
        FindElementByName("Map A bookmark", TimeSpan.FromSeconds(5));

        FindElement("UseSceneButton", TimeSpan.FromSeconds(5)).Click();
        FindElementByName("Scene bookmark", TimeSpan.FromSeconds(5));
        Assert.IsFalse(ElementExistsByName("Map A bookmark", TimeSpan.FromSeconds(1)));
    }

    private void AssertSelectedMarkerIsVisible(AppiumElement map, string bookmarkName)
    {
        var markerArea = GetMarkerArea(map, bookmarkName);
        Assert.IsGreaterThanOrEqualTo<uint>(100, markerArea, $"Expected the {bookmarkName} marker to be visible. Largest matching color area: {markerArea} pixels.");
    }

    private void AssertNoBookmarkMarkersAreVisible(AppiumElement map, (string name, string center)[] bookmarks)
    {
        foreach (var (name, center) in bookmarks)
        {
            var markerArea = GetMarkerArea(map, name);
            Assert.IsLessThan<uint>(100, markerArea, $"Expected the {name} marker to be outside the initial map extent. Largest matching color area: {markerArea} pixels.");
        }
    }

    private uint GetMarkerArea(AppiumElement map, string bookmarkName)
    {
        var (minimumColor, maximumColor) = bookmarkName switch
        {
            "Red bookmark" => (new MagickColor(160, 0, 0), new MagickColor(255, 120, 120)),
            "Green bookmark" => (new MagickColor(0, 30, 0), new MagickColor(120, 255, 120)),
            "Blue bookmark" => (new MagickColor(0, 0, 160), new MagickColor(120, 120, 255)),
            _ => throw new ArgumentOutOfRangeException(nameof(bookmarkName)),
        };

        using var screenshot = GetScreenshot(map);
        screenshot.Crop(200, 200, Gravity.Center);
        screenshot.ColorThreshold(minimumColor, maximumColor);
        return screenshot.ConnectedComponents(4).Skip(1).Select(component => component.Area).DefaultIfEmpty(0u).Max();
    }
}
