using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.Forms
{
    [SampleInfo(Category = "FeatureForms", DisplayName = "FeatureFormView - Sample", Description = "FeatureFormView - Sample")]
    public partial class FeatureFormViewSample : UserControl
    {
        public FeatureFormViewSample()
        {
            InitializeComponent();
        }
        private async void mapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            try
            {
                var result = await mapView.IdentifyLayersAsync(e.Position, 3, false);

                // Retrieves feature from IdentifyLayerResult with a form definition
                var feature = GetFeature(result, out var def);
                if (feature != null)
                {
                    formViewer.FeatureForm = new FeatureForm(feature, def);
                    SidePanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name);
            }
        }

        private ArcGISFeature GetFeature(IEnumerable<IdentifyLayerResult> results, out FeatureFormDefinition def)
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

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Discard edits?", "Confirm", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                formViewer.FeatureForm.DiscardEdits();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            formViewer.FeatureForm = null;
            SidePanel.Visibility = Visibility.Collapsed;
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!formViewer.IsValid)
            {
                var errorsMessages = formViewer.FeatureForm.Elements.OfType<FieldFormElement>().Where(e => e.ValidationErrors.Any()).Select(s => s.FieldName + ": " + string.Join(",", s.ValidationErrors.Select(e => e.Message)));
                if (errorsMessages.Any())
                {
                    MessageBox.Show("Form has errors:\n" + string.Join("\n", errorsMessages), "Can't apply");
                    return;
                }
            }
            try
            {
                await formViewer.FeatureForm.Feature.FeatureTable.UpdateFeatureAsync(formViewer.FeatureForm.Feature);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to apply edits:\n" + ex.Message, "Error");
            }
        }
    }
}
