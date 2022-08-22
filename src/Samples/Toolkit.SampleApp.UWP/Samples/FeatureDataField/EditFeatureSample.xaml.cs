using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
#if WINUI
using CommunityToolkit.WinUI.UI.Animations;
#elif NETCX_CORE
using Microsoft.Toolkit.Uwp.UI.Animations;
#endif
using System;
using System.Linq;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.FeatureDataField
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditFeatureSample : Page
    {
        public EditFeatureSample()
        {
            this.InitializeComponent();
            overlay.Visibility = Visibility.Collapsed;
            Map map = new Map(Basemap.CreateLightGrayCanvasVector());
            map.OperationalLayers.Add(new FeatureLayer(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/DamageAssessment/FeatureServer/0")));
            mapView.Map = map;
        }

        private System.Threading.CancellationTokenSource tcs;

        private async void mapView_GeoViewTapped(object sender, ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (tcs != null)
                tcs.Cancel();
            tcs = new System.Threading.CancellationTokenSource();

            try
            {
                var features = await mapView.IdentifyLayerAsync(mapView.Map.OperationalLayers[0], e.Position, 3, false, 1, tcs.Token);
                var feature = features.GeoElements.FirstOrDefault() as ArcGISFeature;
                if (feature == null)
                    return;
                ShowEditPanel(feature);
            }
            catch (System.Exception)
            {
            }
        }
        
        private void overlay_PointerDown(object sender, PointerRoutedEventArgs e)
        {
            CloseEditPanel();
        }

        private void ShowEditPanel(ArcGISFeature feature)
        {
            overlay.DataContext = feature;
            overlay.Visibility = Visibility.Visible;

#if !WINUI
            if (Microsoft.Toolkit.Uwp.UI.Animations.AnimationExtensions.IsBlurSupported)
            {
                Microsoft.Toolkit.Uwp.UI.Animations.AnimationExtensions.Blur(mapView, 10).Start();
            }
            else
#endif
            {
                overlay.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(192, 255, 255, 255));
            }
        }

        private void CloseEditPanel()
        {
            overlay.DataContext = null;
            overlay.Visibility = Visibility.Collapsed;
#if !WINUI
            if (Microsoft.Toolkit.Uwp.UI.Animations.AnimationExtensions.IsBlurSupported)
                Microsoft.Toolkit.Uwp.UI.Animations.AnimationExtensions.Blur(mapView, 0).Start();
#endif
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (DamageField.ValidationException != null || OccupantsField.ValidationException != null || DescriptionField.ValidationException != null)
            {
                var _ = new Windows.UI.Popups.MessageDialog("Some fields contain an invalid value").ShowAsync();
                return;
            }
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
                    var _ = new Windows.UI.Popups.MessageDialog("Failed to apply edit: " + ex.Message).ShowAsync();
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
                    var _ = new Windows.UI.Popups.MessageDialog("Failed to submit data back to the server: " + ex.Message).ShowAsync();
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
