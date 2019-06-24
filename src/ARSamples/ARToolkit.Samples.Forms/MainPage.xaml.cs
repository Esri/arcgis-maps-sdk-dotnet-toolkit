using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ArcGISAR.Forms
{
    public partial class MainPage : ContentPage
    {
        private static SampleHelpers.SampleDatasource sampleList = new SampleHelpers.SampleDatasource(typeof(Xamarin.Forms.Page));

        public MainPage()
        {
            InitializeComponent();
            Load();
        }

        private void Load()
        {
            samples.ItemsSource = sampleList.Samples;
        }

        private async void Samples_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var sample = e.SelectedItem as SampleHelpers.Sample;
            if (sample != null)
            {
                if (sample.HasSampleData)
                {
                    try
                    {
                        await sample.GetDataAsync((status) =>
                        {
                            dataDialog.IsVisible = true;
                            this.status.Text = status;
                        });
                    }
                    catch (System.Exception ex)
                    {
                        await DisplayAlert("Failed to download data: ", ex.Message, "OK");
                        return;
                    }
                    finally
                    {
                        dataDialog.IsVisible = true;
                    }
                }

                var _ = Navigation.PushAsync(Activator.CreateInstance(sample.Type) as Page);
                samples.SelectedItem = null;
            }
        }
    }
}
