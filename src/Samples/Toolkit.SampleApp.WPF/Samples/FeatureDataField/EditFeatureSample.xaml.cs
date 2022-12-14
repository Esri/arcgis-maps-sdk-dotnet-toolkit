using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Samples.FeatureDataField
{
    /// <summary>
    /// Interaction logic for EditFeatureSample.xaml
    /// </summary>
    public partial class EditFeatureSample : UserControl
    {
        public EditFeatureSample()
        {
            InitializeComponent();
            overlay.Visibility = Visibility.Collapsed;
            Map map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=979c6cc89af9449cbeb5342a439c6a76"));
            map.OperationalLayers.Add(new FeatureLayer(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/DamageAssessment/FeatureServer/0")));
            mapView.Map = map;
        }

        private System.Threading.CancellationTokenSource tcs;

        private async void mapView_GeoViewTapped(object sender, ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (tcs != null)
                tcs.Cancel();
            tcs = new System.Threading.CancellationTokenSource();

            mapView.Cursor = Cursors.Wait;
            try
            {
                var features = await mapView.IdentifyLayerAsync(mapView.Map.OperationalLayers[0], e.Position, 3, false, 1, tcs.Token);
                var feature = features.GeoElements.FirstOrDefault() as ArcGISFeature;
                if (feature == null)
                    return;
                ShowEditPanel(feature);
            }
            catch(System.Exception)
            {
            }
            finally
            {
                mapView.Cursor = Cursors.Arrow;
            }
        }

        private void overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CloseEditPanel();
        }

        private void ShowEditPanel(ArcGISFeature feature)
        {
            overlay.DataContext = feature;
            overlay.Visibility = Visibility.Visible;
            mapView.Effect = new BlurEffect() { Radius = 10 };
        }

        private void CloseEditPanel()
        {
            overlay.DataContext = null;
            overlay.Visibility = Visibility.Collapsed;
            mapView.Effect = null;
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if(DamageField.ValidationException != null || OccupantsField.ValidationException != null || DescriptionField.ValidationException != null)
            {
                MessageBox.Show("Some fields contain an invalid value");
                return;
            }
            var btn = (sender as System.Windows.Controls.Button);
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
                    MessageBox.Show("Failed to apply edit: " + ex.Message);
                    btn.IsEnabled = true;
                    return;
                }
                try
                {
                    var result = await table.ApplyEditsAsync(); //Push edits back to the server
                    CloseEditPanel();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Failed to submit data back to the server: " + ex.Message);
                }
                btn.IsEnabled = true;
            }
        }

        private void DescriptionField_ValueChanging(object sender, UI.AttributeValueChangedEventArgs e)
        {
            // Example of adding a custom value validation:
            if (e.NewValue as string == "TEST")
                throw new ArgumentException("Custom validation: You can't enter the value 'TEST'");
        }
    }
}
