#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.Downloading
{
    /// <summary>
    /// Interaction logic for FileDownloadSample.xaml
    /// </summary>
    public partial class FileDownloadSample : UserControl
    {
        public FileDownloadSample()
        {
            InitializeComponent();
            FileDownloadTask.MaxConcurrentDownloads = 2; // Only allow two downloads to run at the same time.
            DownloadItems.ItemsSource = Items;
            Action<Action> dispatcher = (action) => { if (Dispatcher.CheckAccess()) { action(); } else { Dispatcher.Invoke(action); }; };
            Items.Add(new DownloadItemVM("7dd2f97bb007466ea939160d0de96a9d", dispatcher));
            Items.Add(new DownloadItemVM("34da965ca51d4c68aa9b3a38edb29e00", dispatcher));
            Items.Add(new DownloadItemVM("dce76fb7cf1146c990427555fb3c74d2", dispatcher)); // This item isn't resumable, and will always restart after a pause
            this.Unloaded += FileDownloadSample_Unloaded;
        }

        private void FileDownloadSample_Unloaded(object sender, RoutedEventArgs e)
        {
            // Pause all downloads when the sample is unloaded
            foreach (var item in Items)
            {
                if (item.PauseDownloadCommand.CanExecute(null))
                    item.PauseDownloadCommand.ExecuteAsync(null);
            }
        }

        private ObservableCollection<DownloadItemVM> Items = new ObservableCollection<DownloadItemVM>();

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var newValue = Math.Round(e.NewValue, 0);
            ((Slider)sender).Value = newValue;
            FileDownloadTask.MaxConcurrentDownloads = (uint)newValue;
        }
    }

}
