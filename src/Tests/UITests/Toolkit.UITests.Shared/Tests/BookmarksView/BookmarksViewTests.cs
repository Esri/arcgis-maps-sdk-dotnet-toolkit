using ImageMagick;
using OpenQA.Selenium.Appium;

namespace Toolkit.UITest.Shared.BookmarksView;

[TestClass]
public class BookmarksViewTests : AppiumTestBase
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    private const string BookmarksMapPage = "BookmarksMap";
    private const string BookmarksOnlineMapPage = "BookmarksOnlineMap";
    private const string BookmarksScenePage = "BookmarksScene";
    private const string BookmarksTemplateMapPage = "BookmarksTemplateMap";
    private const string BookmarksUpdatesPage = "BookmarksUpdates";

    [TestMethod]
    public void BookmarksView_Map()
    {
        OpenSample(BookmarksMapPage);

        var bookmarksList = FindElement("BookmarksListView", DefaultTimeout);
#if WPF_TEST || WINUI_TEST
        // A list-level name makes the MAUI CollectionView an accessibility leaf on iOS, hiding its bookmark items.
        Assert.AreEqual("Bookmarks", GetAutomationName(bookmarksList), "The bookmarks list should expose its accessible name.");
#endif

        var bookmarks = new (string name, string expectedCenterText)[]
        {
            ("Red bookmark", "10.0,10.0"),
            ("Green bookmark", "30.0,20.0"),
            ("Blue bookmark", "-30.0,-20.0"),
        };
        var map = FindElement("BookmarksMap", DefaultTimeout);
        AssertNoBookmarkMarkersAreVisible(map, bookmarks);

        foreach (var (name, expectedCenterText) in bookmarks)
        {
            FindElementByName(name, DefaultTimeout).Click();
            FindElementByText(expectedCenterText, DefaultTimeout);
            var selectedBookmarkText = FindElement("SelectedBookmarkText", DefaultTimeout);
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
            FindElementByText($"Custom: {bookmarkName}", DefaultTimeout);
            FindElementByName($"Custom: {bookmarkName}", DefaultTimeout);
        }

        FindElement("SetRuntimeTemplateButton", DefaultTimeout).Click();
        foreach (var bookmarkName in new[] { "Red bookmark", "Green bookmark", "Blue bookmark" })
        {
            FindElementByText($"Runtime: {bookmarkName}", DefaultTimeout);
            Assert.IsFalse(ElementExistsByText($"Custom: {bookmarkName}", TimeSpan.FromSeconds(1)), "The previous item template should no longer be rendered.");
            Assert.IsFalse(ElementExistsByName($"Custom: {bookmarkName}", TimeSpan.FromSeconds(1)), "The previous item template should no longer be rendered.");
        }
    }

    [TestMethod]
    public void BookmarksView_Scene()
    {
        OpenSample(BookmarksScenePage);

        var scene = FindElement("BookmarksScene", DefaultTimeout);
        var bookmarks = new (string name, string colorName)[]
        {
            ("Red scene bookmark", "Red bookmark"),
            ("Green scene bookmark", "Green bookmark"),
            ("Blue scene bookmark", "Blue bookmark"),
        };

        foreach (var (name, colorName) in bookmarks)
        {
            FindElementByName(name, DefaultTimeout).Click();
            FindElementByText($"Ready: {name}", DefaultTimeout);
            var selectedBookmarkText = FindElement("SelectedBookmarkText", DefaultTimeout);
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
        var selectedBookmarkText = FindElement("SelectedBookmarkText", DefaultTimeout);
        Assert.AreEqual("Red sands", GetLabelText(selectedBookmarkText), "The BookmarkSelected event should identify the selected bookmark.");
    }

    [TestMethod]
    public void BookmarksView_TracksDocumentBookmarks()
    {
        OpenSample(BookmarksUpdatesPage);
        FindElementByName("Map A bookmark", DefaultTimeout);

        var addBookmarkButton = FindElement("AddDocumentBookmarkButton", DefaultTimeout);
        addBookmarkButton.Click();
        var addedBookmark = FindElementByName("Added map bookmark", DefaultTimeout);

        addedBookmark.Click();
        FindElementByName("Renamed map bookmark", DefaultTimeout);
        Assert.IsFalse(ElementExistsByName("Added map bookmark", TimeSpan.FromSeconds(1)));
    }

    [TestMethod]
    public void BookmarksView_TracksBookmarksOverride()
    {
        OpenSample(BookmarksUpdatesPage);
        FindElementByName("Map A bookmark", DefaultTimeout);

        // Change the current map before overriding it, then verify its latest bookmarks are restored below.
        FindElement("AddDocumentBookmarkButton", DefaultTimeout).Click();
        FindElementByName("Added map bookmark", DefaultTimeout);

        // Set BookmarksOverride to an observable collection, replacing the map's bookmarks.
        FindElement("UseOverrideButton", DefaultTimeout).Click();
        FindElementByName("Override bookmark", DefaultTimeout);
        Assert.IsFalse(ElementExistsByName("Map A bookmark", TimeSpan.FromSeconds(1)));
        Assert.IsFalse(ElementExistsByName("Added map bookmark", TimeSpan.FromSeconds(1)));

        // Add a bookmark to the active override collection.
        FindElement("AddOverrideButton", DefaultTimeout).Click();
        FindElementByName("Added override bookmark", DefaultTimeout);

        // Clear BookmarksOverride so the current map's bookmarks are shown again.
        FindElement("ClearOverrideButton", DefaultTimeout).Click();
        FindElementByName("Map A bookmark", DefaultTimeout);
        FindElementByName("Added map bookmark", DefaultTimeout);
        Assert.IsFalse(ElementExistsByName("Override bookmark", TimeSpan.FromSeconds(1)));
    }

    [TestMethod]
    public void BookmarksView_TracksGeoViewDocument()
    {
        OpenSample(BookmarksUpdatesPage);
        FindElementByName("Map A bookmark", DefaultTimeout);

        FindElement("UseMapBButton", DefaultTimeout).Click();
        FindElementByName("Map B bookmark", DefaultTimeout);
        Assert.IsFalse(ElementExistsByName("Map A bookmark", TimeSpan.FromSeconds(1)));
    }

    [TestMethod]
    public void BookmarksView_TracksGeoView()
    {
        OpenSample(BookmarksUpdatesPage);
        FindElementByName("Map A bookmark", DefaultTimeout);

        FindElement("UseSceneButton", DefaultTimeout).Click();
        FindElementByName("Scene bookmark", DefaultTimeout);
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
