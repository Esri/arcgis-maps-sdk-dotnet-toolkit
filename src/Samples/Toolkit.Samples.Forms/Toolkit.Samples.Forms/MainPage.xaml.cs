using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Toolkit.Samples.Forms
{
	public partial class MainPage : ContentPage
	{
        public MainPage()
        {
            InitializeComponent();

            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = "";

            SamplesList.ItemsSource = SampleDatasource.Current.Samples;
        }

        private void SamplesList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var sample = e.SelectedItem as Sample;
            if(sample != null)
            {
                SamplesList.SelectedItem = null;
                Navigation.PushAsync(Activator.CreateInstance(sample.Page) as Page);
            }
        }
    }
}
