using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.SearchView
{
    public sealed partial class SearchViewCustomizationSample : Page
    {
        public SearchViewCustomizationSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
            MySearchView.GeoView = MyMapView;
        }

        private void GeoViewConnection_Checked(object sender, RoutedEventArgs e)
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

        private async void AddDefaultLocator_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var source = await LocatorSearchSource.CreateDefaultSourceAsync();
                source.DisplayName = GeocoderNameTextBox.Text;
                MySearchView.SearchViewModel.Sources.Add(source);
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "Error").ShowAsync();
            }
        }

        private void RemoveLocator_Click(object sender, RoutedEventArgs e)
        {
            if (MySearchView.SearchViewModel?.Sources?.Count > 0)
            {
                MySearchView.SearchViewModel.Sources.RemoveAt(MySearchView.SearchViewModel.Sources.Count - 1);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (SearchModeCombo.SelectedIndex)
            {
                case 0:
                    MySearchView.SearchViewModel.SearchMode = SearchResultMode.Automatic;
                    break;
                case 1:
                    MySearchView.SearchViewModel.SearchMode = SearchResultMode.Single;
                    break;
                case 2:
                    MySearchView.SearchViewModel.SearchMode = SearchResultMode.Multiple;
                    break;
            }
        }
    }
}
