using Esri.ArcGISRuntime.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.FeatureDataField
{
    /// <summary>
    /// Demonstrates how to display or edit attributes of an <see cref="ArcGISFeature"/>
    /// from a <see cref="ServiceFeatureTable"/> using <see cref="UI.FeatureDataField"/>.
    /// </summary>
    public partial class DataGridSample : UserControl
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FeatureDataFieldSample"/> class.
        /// </summary>
        public DataGridSample()
        {
            InitializeComponent();
            var _ = LoadFeaturesAsync();
        }

        private ServiceFeatureTable table;

        /// <summary>
        /// Requests for features whose attributes will be displayed in <see cref="UI.FeatureDataField"/>
        /// </summary>
        /// <returns>A task that represents the asynchronous loading of features. </returns>
        private async Task LoadFeaturesAsync()
        {
            try
            {
                table = new ServiceFeatureTable(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/DamageAssessment/FeatureServer/0"));
                table.FeatureRequestMode = FeatureRequestMode.ManualCache;
                await table.LoadAsync();
                var queryParameters = new QueryParameters()
                {
                    WhereClause = "incidentid <> ''",
                    MaxFeatures = 100
                };
                // Request for the same fields defined in the ListView.ItemTemplate.
                var outFields = new string[] { "objectid", "incidentid", "typdamage", "habitable", "predisval", "inspdate", "lastupdate" };
                var features = await table.PopulateFromServiceAsync(queryParameters, true, outFields);
                FeatureList.ItemsSource = features.ToList();
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error occured : {exception.Message}", "Sample error");
            }
        }
        
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                _ = HandleApply(btn);
            }
        }

        private async Task HandleApply(Button btn)
        {
            var feature = btn.DataContext as ArcGISFeature;
            if(feature != null)
            {
                btn.IsEnabled = false;
                var table = (feature.FeatureTable as ServiceFeatureTable);
                try
                {
                    await table.UpdateFeatureAsync(feature);
                }
                catch(System.Exception ex)
                {
                    MessageBox.Show("Failed to apply edit: " + ex.Message);
                    btn.IsEnabled = true;
                    return;
                }
                try
                {
                    var result = await table.ApplyEditsAsync();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Failed to submit data back to the server: " + ex.Message);
                }
                btn.IsEnabled = true;
            }
        }
    }
}
