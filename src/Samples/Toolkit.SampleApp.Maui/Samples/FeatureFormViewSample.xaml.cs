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
                var feature = GetFeature(result, out var def);
                if (feature != null)
                {
                    formViewer.FeatureForm = new FeatureForm(feature, def);
                    SidePanel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(ex.GetType().Name, ex.Message, "OK");
            }
        }

        private ArcGISFeature? GetFeature(IEnumerable<IdentifyLayerResult> results, out FeatureFormDefinition? def)
        {
            def = null;
            if (results == null)
                return null;
            foreach (var result in results.Where(r => r.LayerContent is FeatureLayer layer && (layer.FeatureFormDefinition is not null || (layer.FeatureTable as ArcGISFeatureTable)?.FeatureFormDefinition is not null)))
            {
                var feature = result.GeoElements?.OfType<ArcGISFeature>()?.FirstOrDefault();
                def = (result.LayerContent as FeatureLayer)?.FeatureFormDefinition ?? ((result.LayerContent as FeatureLayer)?.FeatureTable as ArcGISFeatureTable)?.FeatureFormDefinition;
                if (feature != null && def != null)
                {
                    return feature;
                }
            }

            return null;
        }

        private async void BarcodeButtonClicked(object sender, Esri.ArcGISRuntime.Toolkit.Maui.BarcodeButtonClickedEventArgs e)
        {
            // If user clicks the barcode button in a barcode input element, use ZXing library (https://github.com/Redth/ZXing.Net.Maui)
            // to scan a barcode using the device camera
            ZXing.Net.Maui.Controls.CameraBarcodeReaderView view = new ZXing.Net.Maui.Controls.CameraBarcodeReaderView();
            TaskCompletionSource<string?> tcs = new TaskCompletionSource<string?>();
            view.BarcodesDetected += (s, e) =>
            {
                if (tcs.TrySetResult(e.Results.FirstOrDefault()?.Value))
                    Dispatcher.Dispatch(() => _ = Navigation.PopModalAsync());
            };
            ContentPage p = new ContentPage() { Content = view };
            await Navigation.PushModalAsync(p);
            p.NavigatedFrom += (s, e) => tcs.TrySetResult(null);
            var barcode = await tcs.Task;
            if (!string.IsNullOrEmpty(barcode))
                e.FormElement.UpdateValue(barcode);
        }

        private void FormAttachmentClicked(object sender, Esri.ArcGISRuntime.Toolkit.Maui.FormAttachmentClickedEventArgs e)
        {
            // User clicked an attachment,
            // e.Handled = true; // Uncomment to override default open attachment action
            Debug.WriteLine("Attachment clicked: " + e.Attachment.Name);
        }

        private async void DiscardButton_Click(object sender, EventArgs e)
        {
            var result = await DisplayAlert("Confirm", "Discard edits?", "Yes", "Cancel");
            if (result)
            {

                ((Button)sender).IsEnabled = false;
                try
                {
                    await formViewer.DiscardEditsAsync();
                } catch { }

                ((Button)sender).IsEnabled = true;
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            formViewer.FeatureForm = null;
            SidePanel.IsVisible = false;
        }

        private async void UpdateButton_Click(object sender, EventArgs e)
        {
            if (formViewer.FeatureForm == null) return;
            if (!formViewer.IsValid)
            {
                var errorsMessages = formViewer.FeatureForm.Elements.OfType<FieldFormElement>().Where(e=>e.ValidationErrors.Any()).Select(s => s.FieldName + ": " + string.Join(",", s.ValidationErrors.Select(e=>e.Message)));
                if (errorsMessages.Any())
                {
                    await DisplayAlert("Form has errors", string.Join("\n", errorsMessages), "OK");
                    return;
                }
            }
            try
            {
                await formViewer.FinishEditingAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to apply edits:\n" + ex.Message, "OK");
            }
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