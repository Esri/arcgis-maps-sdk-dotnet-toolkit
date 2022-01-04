using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Toolkit.Samples.Forms.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "SearchView", Description = "Exercises bindings and advanced customization options.")]
    public partial class SearchViewCustomizationSample : ContentPage
    {
        public SearchViewCustomizationSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
            MySearchView.GeoView = MyMapView;
        }

        private void GeoViewConnection_Checked(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                MySearchView.GeoView = MyMapView;
            }
            else
            {
                MySearchView.GeoView = null;
            }
        }

        private void AddDefaultLocator_Click(object sender, EventArgs e) => _ = HandleAddDefault();

        private async Task HandleAddDefault()
        {
            try
            {
                var source = await LocatorSearchSource.CreateDefaultSourceAsync();
                source.DisplayName = GeocoderNameEntry.Text;
                MySearchView.SearchViewModel.Sources.Add(source);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "Ok");
            }
        }

        private void RemoveLocator_Click(object sender, EventArgs e)
        {
            if (MySearchView.SearchViewModel?.Sources?.Count > 0)
            {
                MySearchView.SearchViewModel.Sources.RemoveAt(MySearchView.SearchViewModel.Sources.Count - 1);
            }
        }

        private void SearchModePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (SearchModePicker.SelectedIndex)
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