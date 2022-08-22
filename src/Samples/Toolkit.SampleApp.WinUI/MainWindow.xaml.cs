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
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Esri.ArcGISRuntime.Toolkit.SampleApp
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            rootFrame.Loaded += RootFrame_Loaded;
            rootFrame.Navigated += RootFrame_Navigated;
        }
        private void RootFrame_Loaded(object sender, RoutedEventArgs e)
        {
            splitView.BackRequested += (s, args) =>
            {
                if (rootFrame.CanGoBack)
                {
                    rootFrame.GoBack();
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
            //splitView.IsBackEnabled = rootFrame.CanGoBack;
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
