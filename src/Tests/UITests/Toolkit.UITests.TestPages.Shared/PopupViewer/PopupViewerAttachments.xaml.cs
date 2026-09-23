using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping.Popups;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Popup = Esri.ArcGISRuntime.Mapping.Popups.Popup;

namespace Toolkit.UITests.App.TestPages;

// Fully offline test fixture for PopupViewer attachment accessibility tests: PopupAttachments can only come
// from an ArcGISFeature in a table that supports attachments, so this creates a throwaway mobile geodatabase
// with one feature and several attachments, then shows a Popup containing a single AttachmentsPopupElement.
public partial class PopupViewerAttachments : TestPage
{
    public const string PopupTitle = "Ridgeline Trailhead Attachments";
    public const string AttachmentsElementTitle = "Attachments";

    public static readonly (string Name, string ContentType)[] Attachments =
    [
        ("trail-map.png", "image/png"),
        ("parking-permit.pdf", "application/pdf"),
        ("ranger-notes.txt", "text/plain"),
    ];

    // A minimal valid 1x1 PNG.
    private const string OnePixelPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    public PopupViewerAttachments()
    {
        InitializeComponent();
        _ = LoadPopupAsync();
    }

    private async Task LoadPopupAsync()
    {
        try
        {
            MainPopupViewer.Popup = await BuildTestPopupAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Failed to build attachments test popup: {ex}");
        }
    }

    private static async Task<Popup> BuildTestPopupAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"PopupViewerAttachments_{Guid.NewGuid():N}.geodatabase");
        var geodatabase = await Geodatabase.CreateAsync(path);

        var tableDescription = new TableDescription("Trailheads", SpatialReferences.Wgs84, GeometryType.Point)
        {
            HasAttachments = true,
        };
        tableDescription.FieldDescriptions.Add(new FieldDescription("name", FieldType.Text));
        var table = await geodatabase.CreateTableAsync(tableDescription);

        var feature = (ArcGISFeature)table.CreateFeature();
        feature.Geometry = new MapPoint(0, 0, SpatialReferences.Wgs84);
        feature.Attributes["name"] = "Ridgeline Trailhead";
        await table.AddFeatureAsync(feature);

        foreach (var (name, contentType) in Attachments)
        {
            var data = contentType == "image/png"
                ? Convert.FromBase64String(OnePixelPngBase64)
                : Encoding.UTF8.GetBytes($"Contents of {name}");
            await feature.AddAttachmentAsync(name, contentType, data);
        }
        await table.UpdateFeatureAsync(feature);

        var popupDefinition = new PopupDefinition
        {
            Title = PopupTitle,
        };
        popupDefinition.Elements.Add(new AttachmentsPopupElement(PopupAttachmentsDisplayType.List)
        {
            Title = AttachmentsElementTitle,
        });

        var popup = new Popup(feature, popupDefinition);
        await popup.EvaluateExpressionsAsync();
        return popup;
    }
}
