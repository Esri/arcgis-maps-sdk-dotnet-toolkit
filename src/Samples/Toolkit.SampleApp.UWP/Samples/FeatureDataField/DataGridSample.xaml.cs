using Esri.ArcGISRuntime.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Telerik.Data.Core;
using Telerik.UI.Xaml.Controls.Grid;
using Telerik.UI.Xaml.Controls.Grid.Primitives;
using System.ComponentModel;

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
        /// <summary>
        /// Requests for features whose attributes will be displayed in <see cref="UI.FeatureDataField"/>
        /// </summary>
        /// <returns>A task that represents the asynchronous loading of features. </returns>
        private async Task LoadFeaturesAsync()
        {
            string error = null;
            try
            {
                var table = new ServiceFeatureTable(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/DamageAssessmentStatePlane/FeatureServer/0"));
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
                grid.ItemsSource = features.ToList();
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
    }
}
