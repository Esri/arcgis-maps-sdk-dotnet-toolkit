using OpenQA.Selenium;

namespace Toolkit.UITest.Shared.SearchViewControl;

[TestClass]
public class SearchViewControlMapTests : AppiumTestBase
{
    private const string SearchViewControlMapPage = "SearchViewControlMap";

    [TestMethod]
    public async Task SearchViewControl_InitialRendering()
    {
        OpenSample(SearchViewControlMapPage);

        // Check the inital display of UI elements
        Assert.IsTrue(ElementExistsById("QueryEntry", TimeSpan.FromSeconds(5)), "Expected the search input to be visible.");
#if MAC_TEST
        Assert.AreEqual("Find a place or address", GetEntryText(FindElement("QueryEntry", TimeSpan.FromSeconds(5))), "Expected the search input placeholder to be visible.");
#else
        Assert.IsTrue(ElementExistsByText("Find a place or address", TimeSpan.FromSeconds(5)), "Expected the search input placeholder to be visible.");
#endif
        Assert.IsTrue(ElementExistsByName("Search"), "Expected the Search button to be visible.");
        Assert.IsFalse(ElementExistsByName("Clear Search"), "Expected the Clear Search button to be hidden before search text is entered.");
        Assert.IsFalse(ElementExistsById("SearchResultsList"), "Expected search results to be hidden before a search is performed.");
        Assert.IsFalse(ElementExistsById("SearchSuggestionsList"), "Expected search suggestions to be hidden before search text is entered.");
        Assert.IsFalse(ElementExistsById("SearchSourcesList"), "Expected search sources to be hidden initially.");
        Assert.IsFalse(ElementExistsByName("Select search source"), "Expected the source selector to be hidden initially.");
    }

    [TestMethod]
    public async Task SearchViewControl_SearchSuggestions()
    {
        OpenSample(SearchViewControlMapPage);

        // Zoom to the extent of the United States so the entered query returns expected suggestions.
        UpdateViewpoint(50000000, -95, 37);

        // Enter a partial place name to trigger search suggestions.
        SubmitText(FindElement("QueryEntry", TimeSpan.FromSeconds(5)), "onta");

        // Verify the clear button appears once text has been entered.
        Assert.IsTrue(ElementExistsByName("Clear Search", TimeSpan.FromSeconds(5)), "Expected clear search button to be visible after starting to enter the query");

        // Allow suggestions to populate, then verify the suggestions list is visible.
        Assert.IsTrue(ElementExistsById("SearchSuggestionsList", TimeSpan.FromSeconds(5)), "Expected to see the suggestions for the text entered");

        // Select a known suggestion and wait for the map and search UI to update.
        var selectSuggestion = FindElementByText("Ontario International Airport, Ontario, CA, USA", TimeSpan.FromSeconds(5));
        selectSuggestion.Click();

        // Verify the search text and callout reflect the selected suggestion.
        Assert.AreEqual("Ontario International Airport, Ontario, CA, USA", GetEntryText(FindElement("QueryEntry"), TimeSpan.FromSeconds(5)), "The text at search control should be updated");
        Assert.IsTrue(ElementExistsByText("Ontario International Airport", TimeSpan.FromSeconds(5)), $"Callout title \"Ontario International Airport\" not found.");
        Assert.IsTrue(ElementExistsByText("Ontario, California", TimeSpan.FromSeconds(5)), $"Callout description \"Ontario, California\" not found.");

        // Verify selecting the suggestion zoomed the map to the expected scale range.
        var scaleText = GetEntryText(FindElement("ScaleTextBox"));

        Assert.IsTrue(double.TryParse(scaleText, out var currentScale), $"Invalid scale value: {scaleText}");
        Assert.IsLessThanOrEqualTo(100000, currentScale, $"Expected selecting a suggestion to zoom in. Actual scale: {currentScale}");

        // Clear the current search before testing category suggestions.
        FindElementByName("Clear Search").Click();
    }

    [TestMethod]
    public async Task SearchViewControl_SearchCategoryResults()
    {
        OpenSample(SearchViewControlMapPage);
        UpdateViewpoint(60000, -117.602000, 34.055845);

        // Enter a partial category name to trigger category suggestions.
        SubmitText(FindElement("QueryEntry"), "rest");

        // Verify the clear button appears once text has been entered.
        Assert.IsTrue(ElementExistsByName("Clear Search", TimeSpan.FromSeconds(5)), "Expected clear search button to be visible after starting to enter the query");

        // Allow suggestions to populate, then verify the suggestions list is visible.
        Assert.IsTrue(ElementExistsById("SearchSuggestionsList", TimeSpan.FromSeconds(5)), "Expected to see the suggestions for the text entered");

        // Select the Restaurants category suggestion and verify results are shown.
        var selectSuggestion = FindElementByName("Restaurants", TimeSpan.FromSeconds(5));
        selectSuggestion.Click();

        Assert.AreEqual("Restaurants", GetEntryText(FindElement("QueryEntry"), TimeSpan.FromSeconds(5)), "The search box value is not as expected.");
        Assert.IsTrue(ElementExistsById("SearchResultsList", TimeSpan.FromSeconds(5)));

        // Select a known search result and verify its callout content.
        var selectedResult = FindElementByName("Pizzas, Ontario, California, 91761", TimeSpan.FromSeconds(5));
        selectedResult.Click();

        Assert.IsTrue(ElementExistsByText("Pizzas", TimeSpan.FromSeconds(5)), "Callout title Pizzas is not visible");
        Assert.IsTrue(ElementExistsByText("Ontario, California, 91761", TimeSpan.FromSeconds(5)), "Callout Description is not visible");
    }

    [TestMethod]
    public async Task SearchViewControl_RepeatSearchHere()
    {
        OpenSample(SearchViewControlMapPage);

        // Zoom to the extent of California so the entered query returns expected category suggestions and results.
        UpdateViewpoint(3000000, -105.143243, 38.888975);

        // Enter a partial category name to trigger category suggestions.
        SubmitText(FindElement("QueryEntry"), "air");

        // Verify the clear button appears once text has been entered.
        Assert.IsTrue(ElementExistsByName("Clear Search", TimeSpan.FromSeconds(5)), "Expected clear search button to be visible after starting to enter the query");

        // Allow suggestions to populate, then verify the suggestions list is visible.
        Assert.IsTrue(ElementExistsById("SearchSuggestionsList", TimeSpan.FromSeconds(5)), "Expected to see the suggestions for the text entered");

        // Select the Airport category suggestion.
        var selectSuggestion = FindElementByName("Airport", TimeSpan.FromSeconds(5));
        selectSuggestion.Click();

        // Verify the selected suggestion updates the query text and displays search results.
        Assert.AreEqual("Airport", GetEntryText(FindElement("QueryEntry"), TimeSpan.FromSeconds(5)), "The search box value is not as expected.");
        Assert.IsTrue(ElementExistsById("SearchResultsList", TimeSpan.FromSeconds(5)));

        // Select a known search result from the current map extent.
        var selectedResult = FindElementByName("Colorado Springs Airport, Colorado Springs, Colorado", TimeSpan.FromSeconds(5));
        selectedResult.Click();

        // Verify the selected search result displays the expected callout content.
        Assert.IsTrue(ElementExistsByText("Colorado Springs Airport", TimeSpan.FromSeconds(5)), "Callout title Colorado Springs Airport is not visible");
        Assert.IsTrue(ElementExistsByText("Colorado Springs, Colorado", TimeSpan.FromSeconds(5)), "Callout Description is not visible");

        // After selecting a result, the results list should be hidden and repeat search should not be shown yet.
        Assert.IsFalse(ElementExistsById("SearchResultsList"));
        Assert.IsFalse(ElementExistsByName("Repeat Search Here"), "Expected 'Repeat Search Here' button to be hidden initially");

        // Move the map to a new location so the previous search can be repeated in the new visible extent.
        UpdateViewpoint(107962, -117.593468, 34.069134);

        // Wait for the map viewpoint update to complete and for the repeat search button to become available.
        await Task.Delay(1000);
        Assert.IsTrue(ElementExistsByName("Repeat Search Here", TimeSpan.FromSeconds(5)), "Expected 'Repeat Search Here' button to be visible after moving the map");

        // Run the same search again using the new map extent.
        FindElementByName("Repeat Search Here").Click();

        // Verify new results are shown and are relevant to the updated map location.
        Assert.IsTrue(ElementExistsById("SearchResultsList", TimeSpan.FromSeconds(5)), "Expected search results to be visible after clicking 'Repeat Search Here'");
        Assert.IsTrue(ElementExistsByName("Ontario International Airport, Ontario, California", TimeSpan.FromSeconds(5)), "Expected to see search results relevant to the new map location after clicking 'Repeat Search Here'");
    }
    // Helper method to update the viewpoint by entering values in the text boxes and clicking the update button
    private void UpdateViewpoint(int scale, double longitude, double latitude)
    {
        var scaleInputElement = FindElement("ScaleTextBox", TimeSpan.FromSeconds(5));
        SubmitText(scaleInputElement, scale.ToString());

        var longitudeInputElement = FindElement("LongitudeTextBox", TimeSpan.FromSeconds(5));
        SubmitText(longitudeInputElement, longitude.ToString());

        var latitudeInputElement = FindElement("LatitudeTextBox", TimeSpan.FromSeconds(5));
        SubmitText(latitudeInputElement, latitude.ToString());

        var updateButtonElement = FindElement("UpdateViewpoint", TimeSpan.FromSeconds(5));
        Click(updateButtonElement);
    }
}