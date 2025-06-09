using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Esri.ArcGISRuntime.Portal;

namespace Esri.ArcGISRuntime.Toolkit.Samples
{
    // View model for managing file download tasks
    public partial class DownloadItemVM : ObservableObject
    {
        Action<Action> _uiDispatcher;
        string _folder; // folder for downloads
        public DownloadItemVM(string itemId, Action<Action> uidispatcher, string folder = "./")
        {
            ItemId = itemId;
            _uiDispatcher = uidispatcher;
            _folder = folder;
            Initialize();
        }

        private async void Initialize()
        {
            var portal = await Portal.ArcGISPortal.CreateAsync();
            var item = await Portal.PortalItem.CreateAsync(portal, ItemId);
            LoadSerializedItem();
            Item = item;
        }

        private void LoadSerializedItem()
        {
            if (System.IO.File.Exists(JsonFilename))
            {
                var task = FileDownloadTask.FromJson(System.IO.File.ReadAllText(JsonFilename));
                DownloadTask = task;
            }
        }

        [RelayCommand(CanExecute = nameof(CanDownloadStart))]
        private async Task StartDownload()
        {
            if (Item is not null)
            {
                try
                {
                    if (DownloadTask is null)
                    {
                        DownloadTask = await Item.BeginDownloadAsync(Filename);
                    }
                    else
                    {
                        await DownloadTask.ResumeAsync();
                    }
                }
                catch (System.Exception ex)
                {
#if WPF
                    MessageBox.Show(ex.Message, "Failed to begin download");
#endif
                    return;
                }
            }
            else
            {
                return;
            }
        }

        private void OnStatusChanged(object? sender, EventArgs e)
        {
            _uiDispatcher(() =>
            {
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(DownloadSpeed));
                OnPropertyChanged(nameof(HasError));
                StartDownloadCommand.NotifyCanExecuteChanged();
                PauseDownloadCommand.NotifyCanExecuteChanged();
                CancelDownloadCommand.NotifyCanExecuteChanged();
                DeleteDownloadCommand.NotifyCanExecuteChanged();
            });
        }

        private bool CanDownloadStart() => Item is not null && (DownloadTask is null || DownloadTask.Status == FileDownloadStatus.Paused || DownloadTask.Status == FileDownloadStatus.Cancelled || DownloadTask.Status == FileDownloadStatus.Error) && !System.IO.File.Exists(Filename);

        [RelayCommand(CanExecute = nameof(CanCancelDownload))]
        private async Task CancelDownload()
        {
            if (DownloadTask is not null)
                await DownloadTask.CancelAsync();
            DownloadTask = null;
            OnStatusChanged(this, EventArgs.Empty);
        }

        private bool CanCancelDownload() => DownloadTask is not null && (DownloadTask.Status != FileDownloadStatus.Completed && DownloadTask.Status != FileDownloadStatus.Cancelled);

        [RelayCommand(CanExecute = nameof(CanDownloadPause))]
        private async Task PauseDownload()
        {
            if (DownloadTask is null)
                return;
            await DownloadTask.PauseAsync();
        }

        private bool CanDownloadPause() => DownloadTask is not null && DownloadTask.Status == FileDownloadStatus.Downloading && DownloadTask.IsResumable;

        public string ItemId { get; }

        [ObservableProperty]
        private Portal.PortalItem? _item;

        partial void OnItemChanged(PortalItem? value)
        {
            OnPropertyChanged(nameof(Filename));
            OnStatusChanged(null, EventArgs.Empty);
        }

        [ObservableProperty]
        public FileDownloadTask? _DownloadTask;

        partial void OnDownloadTaskChanged(FileDownloadTask? oldValue, FileDownloadTask? newValue)
        {
            if (oldValue != null)
            {
                oldValue.StatusChanged -= OnStatusChanged;
                if (System.IO.File.Exists(JsonFilename))
                    System.IO.File.Delete(JsonFilename);
            }
            if (newValue != null)
            {
                newValue.Progress += (s, e) => { _uiDispatcher(() => { OnPropertyChanged(nameof(Progress)); OnPropertyChanged(nameof(DownloadSpeed)); }); };
                newValue.Completed += (s, e) =>
                {
                    _uiDispatcher(() =>
                    {
                        DownloadTask = null;
                    });
                };
                newValue.StatusChanged += OnStatusChanged;
                System.IO.File.WriteAllText(JsonFilename, DownloadTask.ToJson()); // Store json for later resuming 
            }
            OnStatusChanged(null, EventArgs.Empty);
        }

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private void DeleteDownload()
        {
            if (System.IO.File.Exists(Filename))
            {
                System.IO.File.Delete(Filename);
                OnStatusChanged(null, EventArgs.Empty);
            }
        }
        private bool CanDelete => Item is not null && System.IO.File.Exists(Filename);

        public bool HasError => DownloadTask?.Exception is not null;

        public double Progress
        {
            get
            {
                if (Item is null) return 0;
                if (System.IO.File.Exists(Filename)) return 1; // Already downloaded
                if (DownloadTask is null) return 0;
                if (!DownloadTask.TotalLength.HasValue) return 0; // Unknown size. You should consider setting the progress bar to indeterminate, and display number of bytes downloaded
                if (DownloadTask.TotalLength.Value == 0) return 1;
                return DownloadTask.BytesDownloaded / (double)DownloadTask.TotalLength.Value;
            }
        }

        public string DownloadSpeed
        {
            get
            {
                if (DownloadTask is null || DownloadTask.DownloadSpeed <= 0) return string.Empty;
                if (DownloadTask.DownloadSpeed < 1048)
                    return $"{DownloadTask.DownloadSpeed} bytes/s";
                if (DownloadTask.DownloadSpeed < 1024 * 1024)
                    return $"{DownloadTask.DownloadSpeed / 1024d:0.0} kb/s";
                return $"{DownloadTask.DownloadSpeed / 1024d / 1024:0.0} mb/s";
            }
        }

        public string Status
        {
            get
            {
                if (Item is null)
                    return "Loading item data...";
                if (DownloadTask is null)
                    return System.IO.File.Exists(Filename) ? "Completed" : "Not started";
                return DownloadTask.Status.ToString();
            }
        }

        private string Filename => Item is null ? "" : System.IO.Path.Combine(_folder, Item?.Name!); // Where we will save the file
        private string JsonFilename => System.IO.Path.Combine(_folder, ItemId + ".json"); // Where we store the JSON for the task
    }
}
