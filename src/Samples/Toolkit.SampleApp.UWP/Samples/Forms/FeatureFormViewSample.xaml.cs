﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Popups;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Forms
{
    public sealed partial class FeatureFormViewSample : Page
    {
        public FeatureFormViewSample()
        {
            this.InitializeComponent();
        }

        // Webmap configured with feature forms
        public Map Map { get; } = new Map(new Uri("https://www.arcgis.com/home/item.html?id=f72207ac170a40d8992b7a3507b44fad"));
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
               // MessageBox.Show(ex.Message, ex.GetType().Name);
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

        private async void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            // var result = MessageBox.Show("Discard edits?", "Confirm", MessageBoxButton.YesNo);
            // if (result == MessageBoxResult.Yes)
            // {
            //     ((Button)sender).IsEnabled = false;
            //     try
            //     {
            //         await formViewer.DiscardEditsAsync();
            //     }
            //     catch { }
            //     ((Button)sender).IsEnabled = true;
            // }
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
                    //MessageBox.Show("Form has errors:\n" + string.Join("\n", errorsMessages), "Can't apply");
                    return;
                }
            }
            try
            {
                await formViewer.FinishEditingAsync();
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Failed to apply edits:\n" + ex.Message, "Error");
            }
        }
    }
}