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
        }

        private void sampleView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var sample = e.ClickedItem as Sample;
            NavigateSample(sample);
        }

        public void NavigateSample(Sample sample)
        {
            if (sample == null) return;
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
    }
}
