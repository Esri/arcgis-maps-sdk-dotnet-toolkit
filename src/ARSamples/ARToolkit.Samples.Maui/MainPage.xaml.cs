namespace ARToolkit.Samples.Maui
{
    public partial class MainPage : ContentPage
    {
        private static ARToolkit.SampleApp.SampleDatasource sampleList = new ARToolkit.SampleApp.SampleDatasource(typeof(Page));

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
            var sample = e.SelectedItem as ARToolkit.SampleApp.Sample;
            if (sample != null)
            {
                samples.SelectedItem = null;
                if (!sample.IsDeviceSupported)
                {
                    await DisplayAlert("Not Supported", "This device does not support running this sample.", "OK");
                    return;
                }
                if (sample.HasSampleData)
                {
                    try
                    {
                        await sample.GetDataAsync((status) =>
                        {
                            Dispatcher.Dispatch(() =>
                            {
                                dataDialog.IsVisible = true;
                                this.status.Text = status;
                            });
                        });
                    }
                    catch (System.Exception ex)
                    {
                        await DisplayAlert("Failed to download data: ", ex.Message, "OK");
                        return;
                    }
                    finally
                    {
                        dataDialog.IsVisible = false;
                    }
                }

                var _ = Navigation.PushAsync(Activator.CreateInstance(sample.Type) as Page);
            }
        }
    }
}