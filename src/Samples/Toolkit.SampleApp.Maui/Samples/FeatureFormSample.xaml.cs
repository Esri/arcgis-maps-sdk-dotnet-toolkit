using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.Security;

namespace Toolkit.SampleApp.Maui.Samples;

[XamlCompilation(XamlCompilationOptions.Compile)]
[SampleInfo(Category = "FeatureForms", Description = "Placeholder FeatureForm Demo")]
public partial class FeatureFormSample : ContentPage
{
    public FeatureFormSample()
    {
        AuthenticationManager.Current.ChallengeHandler = new ChallengeHandler(async (info) =>
        {
            return await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri, "apollo_user", "apollo_user.1234");
        });
        InitializeComponent();
        WebmapList.ItemsSource = new[]
        {
            "Simple https://runtimecoretest.maps.arcgis.com/home/item.html?id=17ff3dec5aea49ee98f650861de0930b",
            "All Input Types https://runtimecoretest.maps.arcgis.com/home/item.html?id=d3e813521d4f4dc898c9a48a49c03843",
            "Expression https://runtimecoretest.maps.arcgis.com/home/item.html?id=33bfa499342448e588beedae33613a15",
            "Complex Expression https://runtimecoretest.maps.arcgis.com/apps/mapviewer/index.html?webmap=a7bb3ee9d5b445d4af92c682bfdd0ac7",
            "Validation https://runtimecoretest.maps.arcgis.com/apps/mapviewer/index.html?webmap=9cdda5d7a9404564837b933038ad948a",
            "Validation NotNull https://runtimecoretest.maps.arcgis.com/home/item.html?id=ec5de8d96df94e818bbd2f1a839583b9",
            "Group https://runtimecoretest.maps.arcgis.com/apps/mapviewer/index.html?webmap=beef909c533f4b44af33c13f1d9513ab",
            "IsRequired https://runtimecoretest.maps.arcgis.com/home/item.html?id=4f226eaaa53146aead6193f9e8bbc7bf",
        };
        MyMapView.GeoViewTapped += MyMapView_GeoViewTapped;
    }

    private async void MyMapView_GeoViewTapped(object? sender, GeoViewInputEventArgs e)
    {
        if (MyMapView.Map == null)
            return;
        var layer = MyMapView.Map.OperationalLayers.OfType<FeatureLayer>().First();
        await layer.LoadAsync();
        var result = await MyMapView.IdentifyLayerAsync(layer, e.Position, 1, false);
        var feature = result.GeoElements.OfType<ArcGISFeature>().FirstOrDefault();
        if (feature == null)
            return;

        MyFormView.FeatureForm = new FeatureForm(feature, layer.FeatureFormDefinition);
    }

    private void WebmapList_SelectedIndexChanged(object sender, EventArgs e)
    {
        MyFormView.FeatureForm = null;
        if (WebmapList.SelectedItem is not string selectedStr)
            return;
        var uri = selectedStr.Split(" ").Last();
        var map = new Map(new Uri(uri));
        MyMapView.Map = map;
    }

    private async void SaveButton_Clicked(object sender, EventArgs e)
    {
        if (MyFormView.FeatureForm == null)
            return;
        var errors = MyFormView.FeatureForm.ValidationErrors;
        if (errors.Any())
        {
            await DisplayAlert("Can't apply", "Form has errors:\n" + string.Join("\n", errors.SelectMany(e => e.Value).Select(e => e.Message)), "OK");
            return;
        }
        try
        {
            await MyFormView.FeatureForm.Feature.FeatureTable.UpdateFeatureAsync(MyFormView.FeatureForm.Feature);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Failed to apply edits:\n" + ex.Message, "OK");
        }
    }

    private async void ResetButton_Clicked(object sender, EventArgs e)
    {
        if (MyFormView.FeatureForm == null)
            return;
        if (await DisplayAlert("Confirm", "Discard edits?", "Discard", "Cancel"))
            MyFormView.FeatureForm.DiscardEdits();
    }
}