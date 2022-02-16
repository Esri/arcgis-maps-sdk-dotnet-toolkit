using Esri.ArcGISRuntime.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.FeatureDataField
{
    public sealed partial class DataGridSample : Page
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DataGridSample"/> class.
        /// </summary>
        public DataGridSample()
        {
            this.InitializeComponent();
            var _ = LoadFeaturesAsync();
        }

        private ServiceFeatureTable table;

        /// <summary>
        /// Requests for features whose attributes will be displayed in <see cref="UI.FeatureDataField"/>
        /// </summary>
        /// <returns>A task that represents the asynchronous loading of features. </returns>
        private async Task LoadFeaturesAsync()
        {
            string error = null;
            try
            {
                table = new ServiceFeatureTable(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/DamageAssessmentStatePlane/FeatureServer/0"));
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
                error = exception.Message;
            }
            if (!string.IsNullOrEmpty(error))
            {
                await new MessageDialog($"Error occured : {error}", "Sample error").ShowAsync();
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = (sender as Button);
            var feature = btn.DataContext as ArcGISFeature;
            if (feature != null)
            {
                btn.IsEnabled = false;
                var table = (feature.FeatureTable as ServiceFeatureTable);
                try
                {
                    await table.UpdateFeatureAsync(feature);
                }
                catch (System.Exception ex)
                {
                    await new MessageDialog("Failed to apply edit: " + ex.Message).ShowAsync();
                    btn.IsEnabled = true;
                    return;
                }
                try
                {
                    var result = await table.ApplyEditsAsync();
                }
                catch (System.Exception ex)
                {
                    await new MessageDialog("Failed to submit data back to the server: " + ex.Message).ShowAsync();
                }
                btn.IsEnabled = true;
            }
        }
    }
}
