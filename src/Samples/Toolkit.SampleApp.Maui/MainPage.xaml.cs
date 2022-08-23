namespace Toolkit.SampleApp.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        SamplesList.ItemsSource = SampleDatasource.Current.Samples;
    }

    private async void SamplesList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        var sample = e.SelectedItem as Sample;
        if (sample != null)
        {
            SamplesList.SelectedItem = null;
            try
            {
                await Navigation.PushAsync(Activator.CreateInstance(sample.Page) as Page);
            }
            catch(System.Exception ex)
            {

            }
        }
    }
}

