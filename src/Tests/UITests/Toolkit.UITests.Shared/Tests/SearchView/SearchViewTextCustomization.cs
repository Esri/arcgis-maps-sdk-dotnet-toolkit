using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Enums;

namespace Toolkit.UITest.Shared.SearchViewControl;

[TestClass]
public class SearchViewTextCustomization : AppiumTestBase
{
    private const string SearchViewTextCustomizationPage = "SearchViewTextCustomization";

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

    private static readonly SearchViewTextValues DefaultValues = new(
        TextConfiguration.Default,
        "Search",
        "Clear Search",
        "All Sources",
        "Find a place or address",
        "No Results",
        "Repeat Search Here");

    private static readonly SearchViewTextValues CustomValues = new(
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
    public async Task SearchViewTextCustomization_VerifyValues(TextConfiguration type)
    {
        OpenSample(SearchViewTextCustomizationPage);

        var expected = type == TextConfiguration.Default ? DefaultValues : CustomValues;

        // Apply custom values only when needed
        if (type == TextConfiguration.Custom)
        {
            FindElement("UpdateToCustomValuesButton", TimeSpan.FromSeconds(5)).Click();
        }

        Assert.AreEqual(expected.SearchTooltip, GetEntryText(FindElement("SearchTooltipText", TimeSpan.FromSeconds(5))), $"Expected the search tooltip textbox to be set to {expected.Type} value.");
        Assert.AreEqual(expected.ClearSearchTooltip, GetEntryText(FindElement("ClearSearchTooltipText")), $"Expected the clear search tooltip textbox to be set to {expected.Type} value.");
        Assert.AreEqual(expected.AllSourcesButtonText, GetEntryText(FindElement("AllSourceButtonText")), $"Expected the all sources button textbox to be set to {expected.Type} value.");
        Assert.AreEqual(expected.Placeholder, GetEntryText(FindElement("DefaultPlaceholderText")), $"Expected the default placeholder textbox to be set to {expected.Type} value.");
        Assert.AreEqual(expected.NoResultsMessage, GetEntryText(FindElement("NoResultMessageText")), $"Expected the No results textbox to be set to {expected.Type} value.");
        Assert.AreEqual(expected.RepeatSearchButtonText, GetEntryText(FindElement("RepeatSearchButtonText")), $"Expected the repeat search here textbox to be set to {expected.Type} value.");

        // Check the default text values
        Assert.IsTrue(ElementExistsByText(expected.Placeholder), $"Expected the search input placeholder to be equal to {expected.Type} value.");

        // Check the Automation Names and default Tooltip values (Help text) on Search and Clear Search buttons
        var searchButton = FindElement("SearchButton", TimeSpan.FromSeconds(5));
        Assert.AreEqual(expected.SearchTooltip, searchButton.GetAttribute("HelpText"), $"Expected the search tooltip to be set to {expected.Type} value.");
        Assert.AreEqual(expected.SearchTooltip, searchButton.GetAttribute("Name"), $"Expected the automation name of search button to be set to {expected.Type} value.");
        await ShowClearSearchButtonAndNoResultsMessage();
        var clearSearchButton = FindElement("ClearSearchButton", TimeSpan.FromSeconds(5));
        Assert.AreEqual(expected.ClearSearchTooltip, clearSearchButton.GetAttribute("HelpText"), $"Expected the clear search tooltip to be set to {expected.Type} value.");
        Assert.AreEqual(expected.ClearSearchTooltip, clearSearchButton.GetAttribute("Name"), $"Expected the automation name of clear search button to be set to {expected.Type} value.");

        // Check the default text values
        Assert.IsTrue(ElementExistsByText(expected.NoResultsMessage), $"Expected the no results message to be equal to {expected.Type} value.");

        FindElement("ClearSearchButton").Click();

        // Check the Automation Names and default button text values on Repeat search here and All sources buttons
        await ShowRepeatSearchHereButton();
        var repeatSearchHereButton = FindElement("RepeatSearchHereButton", TimeSpan.FromSeconds(5));
        Assert.AreEqual(expected.RepeatSearchButtonText, repeatSearchHereButton.Text, $"Expected the repeat search here button text to be set to {expected.Type} value.");
        Assert.AreEqual(expected.RepeatSearchButtonText, repeatSearchHereButton.GetAttribute("Name"), $"Expected the automation name of repeat search here button to be set to {expected.Type} value.");
        FindElement("ClearSearchButton").Click();
        await ShowAllSourcesButton();
        var allSourcesButton = FindElement("AllSourceButton", TimeSpan.FromSeconds(5));
        Assert.AreEqual(expected.AllSourcesButtonText, allSourcesButton.Text, $"Expected the all sources button text to be set to {expected.Type} value.");
        Assert.AreEqual(expected.AllSourcesButtonText, allSourcesButton.GetAttribute("Name"), $"Expected the automation name of all sources button to be set to {expected.Type} value.");

    }

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
        await Task.Delay(1000);
        // Move the map to a new location so the previous search can be repeated in the new visible extent.
        FindElement("UpdateViewpointExtentToOntario").Click();
    }
    private async Task ShowAllSourcesButton()
    {
        FindElement("SourceSelectToggle").Click();
    }

}