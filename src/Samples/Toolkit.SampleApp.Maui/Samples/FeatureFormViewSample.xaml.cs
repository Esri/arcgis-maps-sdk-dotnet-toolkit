using System.Diagnostics;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Security;

namespace Toolkit.SampleApp.Maui.Samples
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "FeatureForm", Description = "Demonstrates FeatureFormView.", ApiKeyRequired = false)]
    public partial class FeatureFormViewSample : ContentPage
    {
        public FeatureFormViewSample()
		{
            this.SizeChanged += FeatureFormViewSample_SizeChanged;
			InitializeComponent ();
        }

        private async void mapView_GeoViewTapped(object sender, Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
        {
            try
            {
                var result = await mapView.IdentifyLayersAsync(e.Position, 3, false);

                // Retrieves feature from IdentifyLayerResult with a form definition
                var feature = GetFeature(result);
                if (feature != null)
                {
                    formViewer.FeatureForm = new FeatureForm(feature);
                    SidePanel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(ex.GetType().Name, ex.Message, "OK");
            }
        }

        private ArcGISFeature? GetFeature(IEnumerable<IdentifyLayerResult> results)
        {
            if (results == null)
                return null;
            foreach (var result in results.Where(r => r.LayerContent is FeatureLayer layer && (layer.FeatureFormDefinition is not null || (layer.FeatureTable as ArcGISFeatureTable)?.FeatureFormDefinition is not null)))
            {
                var feature = result.GeoElements?.OfType<ArcGISFeature>()?.FirstOrDefault();
                if (feature != null)
                {
                    return feature;
                }
            }

            return null;
        }

        private void FormAttachmentClicked(object sender, Esri.ArcGISRuntime.Toolkit.Maui.FormAttachmentClickedEventArgs e)
        {
            // User clicked an attachment,
            // e.Handled = true; // Uncomment to override default open attachment action
            Debug.WriteLine("Attachment clicked: " + e.Attachment.Name);
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            formViewer.FeatureForm = null;
            SidePanel.IsVisible = false;
        }

        private void FeatureFormViewSample_SizeChanged(object? sender, EventArgs e)
        {
            // Programmatic adaptive layout
            // Consider using AdaptiveTriggers instead once they work predictably
            if (this.Width > 500)
            {
                // Use side panel
                Grid.SetColumnSpan(mapView, 1);
                Grid.SetColumn(SidePanel, 1);
                SidePanel.WidthRequest = 300;
            }
            else
            {
                // Full screen panel
                Grid.SetColumnSpan(mapView, 2);
                Grid.SetColumn(SidePanel, 0);
                SidePanel.WidthRequest = -1;
            }
        }
    }
}