using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.SampleApp.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        SamplesList.ItemsSource = SampleDatasource.Current.Samples;
        CheckAPIKey();
    }

    private async void CheckAPIKey()
    {
        string key = Preferences.Get("APIKey", string.Empty);
        if (!string.IsNullOrWhiteSpace(key))
        {
            try
            {
                var basemap = new Basemap(BasemapStyle.ArcGISStreets) { ApiKey = key };
                await basemap.LoadAsync();
                ArcGISRuntimeEnvironment.ApiKey = key;
            }
            catch { }
        }
    }

    private async void SamplesList_ItemSelected(object? sender, SelectedItemChangedEventArgs e)
    {
        var sample = e.SelectedItem as Sample;
        if (sample != null)
        {
            SamplesList.SelectedItem = null;
            ApiKeyWindow.IsVisible = false;
            ApiKeyTask?.TrySetResult(false);
            if (sample.ApiKeyRequired && string.IsNullOrEmpty(ArcGISRuntimeEnvironment.ApiKey))
            {
                bool ok = await ShowApiKeyWindow();
                if (!ok) return;
            }
            try
            {
                await Navigation.PushAsync(Activator.CreateInstance(sample.Page) as Page);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }

    private TaskCompletionSource<bool>? ApiKeyTask;

    private Task<bool> ShowApiKeyWindow()
    {
        ApiKeyTask?.TrySetResult(false);
        ApiKeyWindow.IsVisible = true;
        ApiKeyTask = new TaskCompletionSource<bool>();
        return ApiKeyTask.Task;
    }

    private void CancelApiKey_Click(object sender, EventArgs e)
    {
        ApiKeyWindow.IsVisible = false;
        ApiKeyTask?.TrySetResult(false);
    }

    private async void SaveApiKey_Click(object sender, EventArgs e)
    {
        string key = ApiKeyInput.Text;
        if (!string.IsNullOrWhiteSpace(key))
        {
            // Test API Key
            try
            {
                var basemap = new Basemap(BasemapStyle.ArcGISStreets) { ApiKey = key };
                await basemap.LoadAsync();
                ArcGISRuntimeEnvironment.ApiKey = key;
                Preferences.Set("APIKey", key);
                ApiKeyWindow.IsVisible = false;
                ApiKeyTask?.TrySetResult(true);
            }
            catch (System.Exception ex)
            {
                _ = DisplayAlert("Invalid API Key", ex.Message, "OK");
            }
        }
    }

    private void DashboardLinkTapped(object sender, TappedEventArgs e)
    {
        Microsoft.Maui.ApplicationModel.Launcher.OpenAsync("https://links.esri.com/create-an-api-key");
    }

    private void MoreInfoLinkTapped(object sender, TappedEventArgs e)
    {
        Microsoft.Maui.ApplicationModel.Launcher.OpenAsync("https://developers.arcgis.com/net/security-and-authentication/tutorials/create-an-api-key/");
    }
}