using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.SearchView
{
    public partial class SearchViewCustomizationSample : UserControl
    {
        public SearchViewCustomizationSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
            MySearchView.GeoView = MyMapView;
        }

        private void GeoViewConnection_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (EnableGeoViewBindingCheck.IsChecked ?? false)
            {
                MySearchView.GeoView = MyMapView;
            }
            else
            {
                MySearchView.GeoView = null;
            }
        }

        private async void AddDefaultLocator_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var source = await LocatorSearchSource.CreateDefaultSourceAsync();
                source.DisplayName = GeocoderNameTextBox.Text;
                MySearchView.SearchViewModel.Sources.Add(source);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error", ex.Message);
            }
        }

        private async  void AddSMPLocator_Click(object sender, RoutedEventArgs e)
        {
            // NOTE: You can download a sample of StreetMap Premium for testing purposes by visiting the downloads section of the ArcGIS Developer dashboard.
            string path = LocatorPathText.Text;
            if (!File.Exists(path))
            {
                MessageBox.Show("Error", "Input Path Does Not Exist");
                return;
            }

            try
            {
                MobileMapPackage mmpk = await MobileMapPackage.OpenAsync(path);
                if (mmpk.LocatorTask is LocatorTask packagedLocator)
                {
                    MySearchView.SearchViewModel.Sources.Add(new LocatorSearchSource(packagedLocator));
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error", ex.Message);
            }
        }

        private void RemoveLocator_Click(object sender, RoutedEventArgs e)
        {
            if (MySearchView.SearchViewModel?.Sources?.Count > 0)
            {
                MySearchView.SearchViewModel.Sources.RemoveAt(MySearchView.SearchViewModel.Sources.Count - 1);
            }
        }
    }
}
