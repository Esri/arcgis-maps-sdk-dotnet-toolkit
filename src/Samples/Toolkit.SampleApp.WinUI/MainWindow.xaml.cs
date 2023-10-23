using Esri.ArcGISRuntime.Mapping;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp
{
    public sealed partial class MainWindow : Window
    {
        private const string WindowTitle = "ArcGIS Maps SDK for .NET Toolkit (WinUI) - Functional Tests";
        private readonly Microsoft.UI.Windowing.AppWindow appWindow;

        public MainWindow()
        {
            this.InitializeComponent();
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Title = WindowTitle;
            CheckAPIKey();
        }

        private async void CheckAPIKey()
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("APIKey") && localSettings.Values["APIKey"] is string key)
            {
                try
                {
                    var basemap = new Mapping.Basemap(Mapping.BasemapStyle.ArcGISStreets) { ApiKey = key };
                    await basemap.LoadAsync();
                    ArcGISRuntimeEnvironment.ApiKey = key;
                }
                catch { }
            }
        }

        private void sampleView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var sample = e.ClickedItem as Sample;
            NavigateSample(sample);
        }

        public async void NavigateSample(Sample sample)
        {
            if (sample == null) return;

            ApiKeyWindow.Visibility = Visibility.Collapsed;
            ApiKeyTask?.TrySetResult(false);
            if (sample.ApiKeyRequired && string.IsNullOrEmpty(ArcGISRuntimeEnvironment.ApiKey))
            {
                bool ok = await ShowApiKeyWindow();
                if (!ok) return;
            }
            if (!rootFrame.Navigate(sample.Page))
            {
                throw new Exception("Failed to create initial page");
            }
            else
            {
                appWindow.Title = WindowTitle + " - " + sample.Name;
            }
        }
        public Frame SampleFrame
        {
            get { return rootFrame; }
        }

        public ICollectionView Samples => SampleDatasource.Current.CollectionViewSource.View;

        private TaskCompletionSource<bool> ApiKeyTask; private Task<bool> ShowApiKeyWindow()
        {
            ApiKeyTask?.TrySetResult(false);
            ApiKeyWindow.Visibility = Visibility.Visible;
            ApiKeyTask = new TaskCompletionSource<bool>();
            return ApiKeyTask.Task;
        }

        private void CancelApiKey_Click(object sender, RoutedEventArgs e)
        {
            ApiKeyWindow.Visibility = Visibility.Collapsed;
            ApiKeyTask.TrySetResult(false);
        }

        private async void SaveApiKey_Click(object sender, RoutedEventArgs e)
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
                    ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    localSettings.Values["APIKey"] = key;
                    ApiKeyWindow.Visibility = Visibility.Collapsed;
                    ApiKeyTask.TrySetResult(true);
                }
                catch (System.Exception ex)
                {
                    var dialog = new ContentDialog();
                    dialog.Title = "Invalid API Key";
                    dialog.Content = ex.Message;
                    dialog.XamlRoot = Content.XamlRoot;
                    dialog.PrimaryButtonText = "OK";
                    await dialog.ShowAsync();
                }
            };
        }
    }
}
