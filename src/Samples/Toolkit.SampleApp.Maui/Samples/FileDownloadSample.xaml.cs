using System.Collections.ObjectModel;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit;
using Esri.ArcGISRuntime.Toolkit.Samples;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "Downloading", Description = "Downloading Portal Item data")]
    public partial class FileDownloadSample : ContentPage
    {
        public FileDownloadSample()
        {
            InitializeComponent();
            FileDownloadTask.MaxConcurrentDownloads = 2; // Only allow two downloads to run at the same time.
            DownloadItems.ItemsSource = Items;
            Action<Action> dispatcher = (action) => { if (!Dispatcher.IsDispatchRequired) { action(); } else { Dispatcher.Dispatch(action); }; };
            Items.Add(new DownloadItemVM("7dd2f97bb007466ea939160d0de96a9d", dispatcher, FileSystem.Current.AppDataDirectory));
            Items.Add(new DownloadItemVM("34da965ca51d4c68aa9b3a38edb29e00", dispatcher, FileSystem.Current.AppDataDirectory));
            Items.Add(new DownloadItemVM("dce76fb7cf1146c990427555fb3c74d2", dispatcher, FileSystem.Current.AppDataDirectory)); // This item isn't resumable, and will always restart after a pause
            this.Unloaded += FileDownloadSample_Unloaded;
            
        }

        private void FileDownloadSample_Unloaded(object? sender, EventArgs e)
        {
            // Pause all downloads when the sample is unloaded
            foreach (var item in Items)
            {
                if (item.PauseDownloadCommand.CanExecute(null))
                    item.PauseDownloadCommand.ExecuteAsync(null);
            }
        }

        private ObservableCollection<DownloadItemVM> Items = new ObservableCollection<DownloadItemVM>();
    }
}
