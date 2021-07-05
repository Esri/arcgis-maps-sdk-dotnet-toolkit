using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Esri.ArcGISRuntime.Toolkit.SampleApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = "";

            if (string.IsNullOrWhiteSpace(Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey))
            {
                throw new InvalidOperationException("Please edit MainPage.xaml.cs to supply an API key");
            }

            this.Loaded += RootFrame_Loaded;
            rootFrame.Navigated += RootFrame_Navigated;
        }
        private void RootFrame_Loaded(object sender, RoutedEventArgs e)
        {
            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.BackRequested += (s, args) =>
            {
                if (rootFrame.CanGoBack)
                {
                    rootFrame.GoBack();
                    args.Handled = true;
                }
            };

            var samples = SampleDatasource.Current.Samples;
            Sample sample = null;
            if (sample == null)
            {
                rootFrame.Navigate(typeof(WelcomePage));
            }
            else
            {
                if (!rootFrame.Navigate(sample.Page))
                {
                    throw new Exception("Failed to create initial page");
                }

                //Window.Current.SetTitleBar(new TextBlock() { Text = sample.Name });
                //SampleTitle.Text = sample.Name;
            }
        }
        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = rootFrame.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
            if (rootFrame.Content is WelcomePage)
            {
                splitView.DisplayMode = SplitViewDisplayMode.Inline;
                splitView.IsPaneOpen = true;
            }
            else
            {
                splitView.DisplayMode = SplitViewDisplayMode.Overlay;
                splitView.IsPaneOpen = false;
            }
        }
        private void sampleView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var sample = e.ClickedItem as Sample;
            NavigateSample(sample);
        }

        public void NavigateSample(Sample sample)
        {
            if (sample == null) return;
            if (!rootFrame.Navigate(sample.Page, null))
            {
                throw new Exception("Failed to create initial page");
            }
        }
        public Frame SampleFrame
        {
            get { return rootFrame; }
        }
    }

    public class SamplesVM
    {
        public ICollectionView Samples
        {
            get
            {
                return SampleDatasource.Current.CollectionViewSource.View;
            }
        }
    }
}
