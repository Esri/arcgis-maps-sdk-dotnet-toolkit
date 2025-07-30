using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Esri.ArcGISRuntime.Toolkit.Samples;
using Windows.Foundation;
using Windows.Foundation.Collections;
#if WINUI
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
#endif

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Downloading
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FileDownloadSample : Page
    {
        public FileDownloadSample()
        {
            this.InitializeComponent();
            FileDownloadTask.MaxConcurrentDownloads = 2; // Only allow two downloads to run at the same time.
#if WINDOWS_UWP
            Action<Action> dispatcher = (action) => { if (Dispatcher.HasThreadAccess) { action(); } else { _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action()); }; };
#elif WINUI
            Action<Action> dispatcher = (action) => { if (DispatcherQueue.HasThreadAccess) { action(); } else { _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () => action()); }; };
#endif
            Items.Add(new DownloadItemVM("7dd2f97bb007466ea939160d0de96a9d", dispatcher, Windows.Storage.ApplicationData.Current.LocalFolder.Path));
            Items.Add(new DownloadItemVM("34da965ca51d4c68aa9b3a38edb29e00", dispatcher, Windows.Storage.ApplicationData.Current.LocalFolder.Path));
            Items.Add(new DownloadItemVM("dce76fb7cf1146c990427555fb3c74d2", dispatcher, Windows.Storage.ApplicationData.Current.LocalFolder.Path)); // This item isn't resumable, and will always restart after a pause
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

        public ObservableCollection<DownloadItemVM> Items = new ObservableCollection<DownloadItemVM>();
    }
}
