using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using Popup = Esri.ArcGISRuntime.Mapping.Popups.Popup;

namespace Toolkit.UITests.App.TestPages;

// Fully offline test fixture for PopupViewer accessibility tests: builds a Popup from a plain
// Graphic + hand-authored PopupDefinition (no network/portal dependency), covering one FieldsPopupElement,
// one MediaPopupElement (an image with AlternativeText and a chart without it, to exercise the
// PopupMediaView alt-text fallback chain), and one multi-paragraph TextPopupElement.
public partial class PopupViewerFields : TestPage
{
    public const string PopupTitle = "Ridgeline Trailhead";

    public const string FieldsElementTitle = "Fields";
    public const string NameFieldLabel = "Name";
    public const string NameFieldValue = "Ridgeline Trailhead";
    public const string CategoryFieldLabel = "Category";
    public const string CategoryFieldValue = "Trailhead";
    public const string WebsiteFieldLabel = "Website";
    public const string WebsiteFieldValue = "https://example.com/ridgeline";

    public const string MediaElementTitle = "Media";
    public const string ImageMediaTitle = "Trail Map";
    public const string ImageMediaCaption = "Map of the ridgeline trail network";
    public const string ImageMediaAlternativeText = "Illustrated map showing three looping trails around the trailhead";
    public const string ChartMediaTitle = "Monthly Visitors";
    public const string ChartMediaCaption = "Visitor counts by month";

    public const string TextElementFirstParagraph = "This trailhead connects to the Ridgeline Loop and Summit Spur trails.";
    public const string TextElementSecondParagraph = "Parking is available year-round, but the upper trail closes seasonally.";

    // A minimal valid 1x1 PNG, embedded so image media renders with no network access.
    private const string OnePixelPngDataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    public PopupViewerFields()
    {
        InitializeComponent();
        _ = LoadPopupAsync();
    }

    private async Task LoadPopupAsync()
    {
        var popup = await BuildTestPopupAsync();
        MainPopupViewer.Popup = popup;
    }

    private static async Task<Popup> BuildTestPopupAsync()
    {
        var attributes = new Dictionary<string, object?>
        {
            { "name", NameFieldValue },
            { "category", CategoryFieldValue },
            { "website", WebsiteFieldValue },
            { "jan_visitors", 120 },
            { "feb_visitors", 150 },
            { "mar_visitors", 210 },
        };
        var graphic = new Graphic(new MapPoint(0, 0), attributes);

        var fieldsElement = new FieldsPopupElement(new List<PopupField>
        {
            new PopupField { FieldName = "name", Label = NameFieldLabel },
            new PopupField { FieldName = "category", Label = CategoryFieldLabel },
            new PopupField { FieldName = "website", Label = WebsiteFieldLabel },
        })
        {
            Title = FieldsElementTitle,
        };

        var imageMedia = new PopupMedia
        {
            Title = ImageMediaTitle,
            Caption = ImageMediaCaption,
            AlternativeText = ImageMediaAlternativeText,
            Type = PopupMediaType.Image,
            Value = new PopupMediaValue { SourceUrl = OnePixelPngDataUri },
        };

        var chartMedia = new PopupMedia
        {
            // No AlternativeText set here on purpose: exercises PopupMediaView's fallback to Title/Caption.
            Title = ChartMediaTitle,
            Caption = ChartMediaCaption,
            Type = PopupMediaType.ColumnChart,
            Value = new PopupMediaValue(),
        };
        chartMedia.Value.FieldNames.Add("jan_visitors");
        chartMedia.Value.FieldNames.Add("feb_visitors");
        chartMedia.Value.FieldNames.Add("mar_visitors");

        var mediaElement = new MediaPopupElement(new List<PopupMedia> { imageMedia, chartMedia })
        {
            Title = MediaElementTitle,
        };

        var textElement = new TextPopupElement(
            $"<p>{TextElementFirstParagraph}</p><p>{TextElementSecondParagraph}</p>");

        var popupDefinition = new PopupDefinition
        {
            Title = PopupTitle,
        };
        popupDefinition.Elements.Add(fieldsElement);
        popupDefinition.Elements.Add(mediaElement);
        popupDefinition.Elements.Add(textElement);

        var popup = new Popup(graphic, popupDefinition);
        await popup.EvaluateExpressionsAsync();
        return popup;
    }
}
