using Esri.ArcGISRuntime.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Esri.ArcGISRuntime.Toolkit.Samples
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FeatureDataFieldSample : Page
    {
        public FeatureDataFieldSample()
        {
            this.InitializeComponent();
        }

        private async Task LoadFeaturesAsync()
        {
            string error = null;
            try
            {
                var table = new ServiceFeatureTable(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/DamageAssessment/FeatureServer/0"));
                table.FeatureRequestMode = FeatureRequestMode.ManualCache;
                await table.LoadAsync();
                var queryParameters = new QueryParameters() { WhereClause = "incidentid <> ''", MaxFeatures = 100 };
                var outFields = new string[] { "objectid", "incidentid", "typdamage", "habitable", "predisval", "inspdate", "lastupdate" };
                var assessments = await table.PopulateFromServiceAsync(queryParameters, true, outFields);
                FeatureList.ItemsSource = assessments.ToList();
            }
            catch (Exception exception)
            {
                error = exception.Message;
            }
            if(!string.IsNullOrEmpty(error))
            {
                await new MessageDialog($"Error occured : {error}", "Sample error").ShowAsync();
            }
        }

        private async void LoadFeatures_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await LoadFeaturesAsync();
        }
    }

    public class ValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
