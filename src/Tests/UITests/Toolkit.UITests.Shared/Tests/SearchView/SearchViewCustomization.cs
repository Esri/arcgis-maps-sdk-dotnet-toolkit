using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Enums;

namespace Toolkit.UITest.Shared.SearchViewControl;

[TestClass]
public class SearchViewCustomization : AppiumTestBase
{
    private const string SearchViewCustomizationPage = "SearchViewCustomization";

    public enum TextConfiguration
    {
        Default,
        Custom
    }

    public record SearchViewTextValues(
        TextConfiguration Type,
        string SearchTooltip,
        string ClearSearchTooltip,
        string AllSourcesButtonText,
        string Placeholder,
        string NoResultsMessage,
        string RepeatSearchButtonText);

    private static readonly SearchViewTextValues DefaultTextValues = new(
        TextConfiguration.Default,
        "Search",
        "Clear Search",
        "All Sources",
        "Find a place or address",
        "No Results",
        "Repeat Search Here");

    private static readonly SearchViewTextValues CustomTextValues = new(
        TextConfiguration.Custom,
        "Custom Search",
        "Custom Clear Search",
        "Custom All Sources",
        "Custom Find a place or address",
        "Custom No Results",
        "Custom Repeat Search Here");

    [TestMethod]
    [DataRow(TextConfiguration.Default)]
    [DataRow(TextConfiguration.Custom)]
    public async Task SearchViewCustomization_Text(TextConfiguration type)
    {
        OpenSample(SearchViewCustomizationPage);

        var expected = type == TextConfiguration.Default ? DefaultTextValues : CustomTextValues;

        // Apply custom values only when needed
        if (type == TextConfiguration.Custom)
        {
            FindElement("UpdateToCustomValuesButton", TimeSpan.FromSeconds(5)).Click();
        }

        // Check the text values
#if MAC_TEST
        Assert.AreEqual(expected.Placeholder, GetEntryText(FindElement("QueryEntry", TimeSpan.FromSeconds(5))), $"Expected the placeholder text to be visible and equal to {expected.Type} value.");
#else
        var placeholderElement = FindElementByText(expected.Placeholder, TimeSpan.FromSeconds(5));
        Assert.IsTrue(placeholderElement.Displayed, $"Expected the placeholder text to be visible and equal to {expected.Type} value.");
#endif
#if MAUI_TEST
        if (!string.Equals(expected.Type.ToString(), "Custom", StringComparison.OrdinalIgnoreCase))
#endif
        {
            // Check the Automation Names on Search and Clear Search buttons
            var searchButton = FindElement("SearchButton", TimeSpan.FromSeconds(5));
            Assert.AreEqual(expected.SearchTooltip, GetAutomationName(searchButton), $"Expected the automation name of search button to be set to {expected.Type} value.");
        }
            await ShowClearSearchButtonAndNoResultsMessage();
#if MAUI_TEST
        if (!string.Equals(expected.Type.ToString(), "Custom", StringComparison.OrdinalIgnoreCase))
#endif
        {
            var clearSearchButton = FindElement("ClearSearchButton", TimeSpan.FromSeconds(5));
                Assert.AreEqual(expected.ClearSearchTooltip, GetAutomationName(clearSearchButton), $"Expected the automation name of clear search button to be set to {expected.Type} value.");
        }
        // Check the text values
        var noResultsMessageElement = FindElementByText(expected.NoResultsMessage);
        Assert.IsTrue(noResultsMessageElement.Displayed, $"Expected the no results message to be visible and equal to {expected.Type} value.");

        FindElement("ClearSearchButton").Click();

        // Check the Automation Names and text values on Repeat search here and All sources buttons
        await ShowRepeatSearchHereButton();
        var repeatSearchHereButton = FindElement("RepeatSearchHereButton", TimeSpan.FromSeconds(5));
        Assert.AreEqual(expected.RepeatSearchButtonText, repeatSearchHereButton.Text, $"Expected the repeat search here button text to be set to {expected.Type} value.");
        Assert.AreEqual(expected.RepeatSearchButtonText, GetAutomationName(repeatSearchHereButton), $"Expected the automation name of repeat search here button to be set to {expected.Type} value.");
        FindElement("ClearSearchButton").Click();
        FindElement("AddEventTestSourceButton").Click();
        await ShowAllSourcesButton();
#if !MAUI_TEST
        var allSourcesButton = FindElement("AllSourcesButton", TimeSpan.FromSeconds(5));
        Assert.AreEqual(expected.AllSourcesButtonText, allSourcesButton.Text, $"Expected the all sources button text to be set to {expected.Type} value.");
        Assert.AreEqual(expected.AllSourcesButtonText, GetAutomationName(allSourcesButton), $"Expected the automation name of the all sources button to be set to {expected.Type} value.");
#else
        Assert.IsTrue(ElementExistsByText(expected.AllSourcesButtonText, TimeSpan.FromSeconds(5)), $"Expected the all sources button text to be set to {expected.Type} value.");
#endif

    }

    [TestMethod]
    public async Task SearchViewCustomization_GeoViewBinding()
    {
        OpenSample(SearchViewCustomizationPage);

        // Restrict the GeoView to Colorado so the query should not return results near Ontario, California.
        FindElement("UpdateViewpointExtentToColorado").Click();
        SubmitText(FindElement("QueryEntry", TimeSpan.FromSeconds(5)), "ontario international");

        Assert.IsFalse(
            ElementExistsByName("Ontario International Airport, Ontario, CA, USA", TimeSpan.FromSeconds(5)),
            "Not Expected to see search results relevant to the Ontario area");

        // Clear the previous search and disable GeoView binding so the search is no longer constrained by the current map extent.
        FindElement("ClearSearchButton").Click();
        FindElement("EnableGeoViewBindingCheck").Click();
        SubmitText(FindElement("QueryEntry", TimeSpan.FromSeconds(5)), "ontario international");

        Assert.IsTrue(
            ElementExistsByName("Ontario International Airport, Ontario, CA, USA", TimeSpan.FromSeconds(5)),
            "Expected to see search results relevant to the Ontario area when geoview binding is null");
    }

    [TestMethod]
    public async Task SearchViewCustomization_EnableRepeatSearchHereButtonBinding()
    {
        // Open the SearchView customization sample page.
        OpenSample(SearchViewCustomizationPage);

        // Verify the Repeat Search Here button is visible when enabled through binding.
        await ShowRepeatSearchHereButton();
        Assert.IsTrue(ElementExistsById("RepeatSearchHereButton", TimeSpan.FromSeconds(5)), "Expected the repeat search here button to be visible when enabled through binding.");

        // Clear the current search and toggle the binding off.
        FindElement("ClearSearchButton").Click();
        FindElement("EnableRepeatSearchHereButtonCheck").Click();

        // Verify the Repeat Search Here button is hidden when disabled through binding.
        await ShowRepeatSearchHereButton();
        Assert.IsFalse(ElementExistsById("RepeatSearchHereButton", TimeSpan.FromSeconds(5)), "Expected the repeat search here button to be hidden when disabled through binding.");
    }

    [TestMethod]
    public async Task SearchViewCustomization_EnableResultListViewBinding()
    {
        // Open the SearchView customization sample page.
        OpenSample(SearchViewCustomizationPage);

        // Show the result list and verify it is visible by default.
        await ShowResultListView();
        Assert.IsTrue(ElementExistsById("SearchResultsList", TimeSpan.FromSeconds(5)), "Expected the results list to be visible");

        // Clear the current search before changing the result list view binding setting.
        FindElement("ClearSearchButton").Click();

        // Toggle the binding setting that controls whether the result list view is enabled.
        FindElement("EnableResultListViewBindingCheck").Click();

        // Show the result list again and verify it is hidden after disabling the binding.
        await ShowResultListView();
        Assert.IsFalse(ElementExistsById("SearchResultsList", TimeSpan.FromSeconds(5)), "Expected the results list to be hidden");
    }

    [TestMethod]
    public async Task SearchViewCustomization_EnableIndividualResultDisplayBinding()
    {
        // Open the SearchView customization sample page.
        OpenSample(SearchViewCustomizationPage);

        // Show the result list and verify it is visible by default.
        await ShowIndividualResultList();
        Assert.IsFalse(ElementExistsById("SearchResultsList", TimeSpan.FromSeconds(5)), "Expected the results list to be hidden");

        // Clear the current search before changing the result list view binding setting.
        FindElement("ClearSearchButton").Click();

        // Toggle the binding setting that controls whether the result list view is enabled.
        FindElement("EnableIndividualResultDisplayCheck").Click();

        // Show the result list again and verify it is hidden after disabling the binding.
        await ShowIndividualResultList();
        Assert.IsTrue(ElementExistsById("SearchResultsList", TimeSpan.FromSeconds(5)), "Expected the results list to be visible");
    }

    [TestMethod]
    public async Task SearchViewCustomization_MultipleSources_Rendering()
    {
        OpenSample(SearchViewCustomizationPage);

        // Enable the event-based search source and open the source selector.
        await SearchWithEventSource();
        await ShowAllSourcesButton();

        // Verify that all available sources are displayed in the source list.
        Assert.IsTrue(ElementExistsById("SearchSourcesList", TimeSpan.FromSeconds(5)));
        Assert.IsTrue(ElementExistsByText("All Sources"));
        Assert.IsTrue(ElementExistsByText("World Geocoder"));
        Assert.IsTrue(ElementExistsByText("Event tester"));
    }

    [TestMethod]
    public async Task SearchViewCustomization_MultipleSources_AllSources()
    {
        OpenSample(SearchViewCustomizationPage);

        // Enable the event-based source while keeping all search sources selected.
        await SearchWithEventSource();

        // Submit a query that should return results from both configured sources.
        SubmitOntarioAddressQuery();

        // Verify source headers are shown for both source result groups.
        Assert.IsTrue(ElementExistsByText("World Geocoder", TimeSpan.FromSeconds(5)));
        Assert.IsTrue(ElementExistsByText("Event tester", TimeSpan.FromSeconds(5)));

        // Verify the suggestions list is visible.
#if WINUI_TEST
        Assert.IsTrue(ElementExistsById("SearchSuggestionsListGrouped", TimeSpan.FromSeconds(5)));
#else
        Assert.IsTrue(ElementExistsById("SearchSuggestionsList", TimeSpan.FromSeconds(5)));
#endif

        // Verify the world geocoder result is displayed.
        Assert.IsTrue(
            ElementExistsByName(
                "2000 E Convention Center Way, Ontario, CA, 91764, USA",
                TimeSpan.FromSeconds(5)));

        // Verify the event tester suggestion is displayed.
        Assert.IsTrue(
            ElementExistsByName(
                "suggestion one",
                TimeSpan.FromSeconds(5)));
    }

    [TestMethod]
    public async Task SearchViewCustomization_MultipleSources_WorldGeocoderOnly()
    {
        OpenSample(SearchViewCustomizationPage);

        // Enable the event-based source so the source selector contains multiple options.
        await SearchWithEventSource();

        // Workaround for issue: https://devtopia.esri.com/runtime/dotnet-api/issues/14195
        SubmitText(
            FindElement("QueryEntry", TimeSpan.FromSeconds(5)),
            "air");
        FindElement("ClearSearchButton").Click();

        // Select only the world geocoder source.
        await ShowAllSourcesButton();
        FindElementByText("World Geocoder", TimeSpan.FromSeconds(5)).Click();

        // Submit a query that should only return world geocoder suggestions.
        SubmitOntarioAddressQuery();

        // In Maui the source header is not visible when there is only one source
#if !MAUI_TEST
        // Verify only the world geocoder source header is visible.
        Assert.IsTrue(ElementExistsByText("World Geocoder", TimeSpan.FromSeconds(5)));
        Assert.IsFalse(ElementExistsByText("Event tester"));
#endif

        // Verify the suggestions list is visible.
#if WINUI_TEST
        Assert.IsTrue(ElementExistsById("SearchSuggestionsListGrouped", TimeSpan.FromSeconds(5)));
#else
        Assert.IsTrue(ElementExistsById("SearchSuggestionsList", TimeSpan.FromSeconds(5)));
#endif

        // Verify the world geocoder result is displayed.
        Assert.IsTrue(
            ElementExistsByName(
                "2000 E Convention Center Way, Ontario, CA, 91764, USA"));

        // Verify the event tester suggestion is not displayed.
        Assert.IsFalse(
            ElementExistsByName(
                "suggestion one"));
    }

    [TestMethod]
    public async Task SearchViewCustomization_MultipleSources_EventTesterOnly()
    {
        OpenSample(SearchViewCustomizationPage);

        // Enable the event-based source so the source selector contains multiple options.
        await SearchWithEventSource();

        // Workaround for issue: https://devtopia.esri.com/runtime/dotnet-api/issues/14195
        SubmitText(
            FindElement("QueryEntry", TimeSpan.FromSeconds(5)),
            "air");
        FindElement("ClearSearchButton").Click();

        // Select only the event tester source.
        await ShowAllSourcesButton();
        FindElementByText("Event tester", TimeSpan.FromSeconds(5)).Click();

        // Submit a query that should only return event tester suggestions.
        SubmitOntarioAddressQuery();

        //In Maui the source header is not visible when there is only one source
#if !MAUI_TEST
        // Verify only the event tester source header is visible.
        Assert.IsTrue(ElementExistsByText("Event tester", TimeSpan.FromSeconds(5)));
        Assert.IsFalse(ElementExistsByText("World Geocoder"));
#endif

        // Verify the suggestions list is visible.
#if WINUI_TEST
        Assert.IsTrue(ElementExistsById("SearchSuggestionsListGrouped", TimeSpan.FromSeconds(5)));
#else
        Assert.IsTrue(ElementExistsById("SearchSuggestionsList", TimeSpan.FromSeconds(5)));
#endif

        // Verify the world geocoder result is not displayed.
        Assert.IsFalse(
            ElementExistsByName(
                "2000 E Convention Center Way, Ontario, CA, 91764, USA"));

        // Verify the event tester suggestion is displayed.
        Assert.IsTrue(
            ElementExistsByName(
                "suggestion one"));
    }

    [TestMethod]
    public async Task SearchViewCustomization_EventTestSource_EventFired()
    {
        OpenSample(SearchViewCustomizationPage);

        // Enable the event-based source
        await SearchWithEventSource();

        Assert.AreEqual("Event verification text", GetLabelText(FindElement("VerifyEventTextBlock")));

        // Submit a query to show search results.
        SubmitText(FindElement("QueryEntry", TimeSpan.FromSeconds(5)), "sugg");

        FindElementByName("suggestion one", TimeSpan.FromSeconds(5)).Click();

        Assert.AreEqual("Selected event fired", GetLabelText(FindElement("VerifyEventTextBlock")));

        FindElement("ClearSearchButton").Click();

        Assert.AreEqual("Deselected event fired", GetLabelText(FindElement("VerifyEventTextBlock")));

    }

#region Helper methods

    private async Task ShowClearSearchButtonAndNoResultsMessage()
    {
        // Enter text to show the clear search button
        SubmitText(FindElement("QueryEntry", TimeSpan.FromSeconds(5)), "testSearchview");
    }

    private async Task ShowRepeatSearchHereButton()
    {
        // Zoom to the extent of Colorado so the entered query returns expected category suggestions and results.
        FindElement("UpdateViewpointExtentToColorado").Click();
        SubmitText(FindElement("QueryEntry"), "airport");
        var selectSuggestion = FindElementByName("Airport", TimeSpan.FromSeconds(5));
        selectSuggestion.Click();
        await Task.Delay(2000);
        // Move the map to a new location so the previous search can be repeated in the new visible extent.
        FindElement("UpdateViewpointExtentToOntario").Click();
    }
    private async Task ShowAllSourcesButton()
    {
        FindElement("SourceSelectToggle").Click();
    }
    private async Task ShowResultListView()
    {
        // Zoom to the extent of Colorado so the entered query returns expected category suggestions and results.
        FindElement("UpdateViewpointExtentToColorado").Click();
        SubmitText(FindElement("QueryEntry"), "airport");
        var selectSuggestion = FindElementByName("Airport", TimeSpan.FromSeconds(5));
        selectSuggestion.Click();
    }

    private async Task ShowIndividualResultList()
    {
        await ShowResultListView();

        // Select a known search result from the result list
        var selectedResult = FindElementByName("Colorado Springs Airport, Colorado Springs, Colorado", TimeSpan.FromSeconds(5));
        selectedResult.Click();
    }

    private const string OntarioAddress =
    "2000 E Convention Center Way, Ontario, CA, 91764, USA";

    private async Task SearchWithEventSource()
    {
        FindElement("AddEventTestSourceButton").Click();
        FindElement("UpdateViewpointExtentToOntario").Click();
    }

    private void SubmitOntarioAddressQuery()
    {
        SubmitText(
            FindElement("QueryEntry", TimeSpan.FromSeconds(5)),
            OntarioAddress);
    }

#endregion
}