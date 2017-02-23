using Esri.ArcGISRuntime.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Esri.ArcGISRuntime.Toolkit.Samples
{
    /// <summary>
    /// Demonstrates how to display or edit attributes of an <see cref="ArcGISFeature"/>
    /// from a <see cref="ServiceFeatureTable"/> using <see cref="UI.FeatureDataField"/>.
    /// </summary>
    public partial class FeatureDataFieldSample : Window
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FeatureDataFieldSample"/> class.
        /// </summary>
        public FeatureDataFieldSample()
        {
            InitializeComponent();
            var _ = LoadFeaturesAsync();
        }

        /// <summary>
        /// Requests for features whose attributes will be displayed in <see cref="UI.FeatureDataField"/>
        /// </summary>
        /// <returns>A task that represents the asynchronous loading of features. </returns>
        private async Task LoadFeaturesAsync()
        {
            try
            {
                var table = new ServiceFeatureTable(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/DamageAssessment/FeatureServer/0"));
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
    }
}
