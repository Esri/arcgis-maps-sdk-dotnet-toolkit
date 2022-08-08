using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ARToolkit.SampleApp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SampleDataAttribute : Attribute
    {
        public string ItemId { get; set; }

        public string Path { get; set; }

        public async Task GetDataAsync(string path, Action<string> progress)
        {
            if (System.IO.File.Exists(path))
                return;
            DownloadManager.FileDownloadTask task = null;
            string tempFile = null;
#if NETCOREAPP
            if (Preferences.Default.ContainsKey("DataDownloadTask"))
            {
                var previousTask = DownloadManager.FileDownloadTask.FromJson(Preferences.Default.Get("DataDownloadTask", string.Empty));
#else
            if (Plugin.Settings.CrossSettings.Current.Contains("DataDownloadTask"))
            {
                var previousTask = DownloadManager.FileDownloadTask.FromJson(Plugin.Settings.CrossSettings.Current.GetValueOrDefault("DataDownloadTask", string.Empty));
#endif
                if (previousTask.IsResumable)
                {
                    task = previousTask;
                    tempFile = task.Filename;
                }
            }
            if (task == null)
            {
                progress?.Invoke("Fetching offline data info...");
                var portal = await Esri.ArcGISRuntime.Portal.ArcGISPortal.CreateAsync(new Uri("https://www.arcgis.com/sharing/rest")).ConfigureAwait(false);
                var item = await Esri.ArcGISRuntime.Portal.PortalItem.CreateAsync(portal, ItemId).ConfigureAwait(false);

                progress?.Invoke("Initiating download...");
                tempFile = System.IO.Path.GetTempFileName();
                task = await DownloadManager.FileDownloadTask.StartDownload(tempFile, item);
                string downloadTask = task.ToJson();
#if NETCOREAPP
                Preferences.Default.Set("DataDownloadTask", task.ToJson());
#else
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("DataDownloadTask", task.ToJson());
#endif
            }
            progress?.Invoke("Downloading data...");

            if (progress != null)
            {
                string lastState = "";
                task.Progress += (s, e) =>
                {
                    var state = "Downloading data " + (e.HasPercentage ? e.Percentage.ToString() + "%" : e.TotalBytes / 1024 + " kb...");
                    if (state != lastState)
                    {
                        progress?.Invoke(state);
                        lastState = state;
                    }
                };
            }
            await task.DownloadAsync();
            //AppSettings.Remove("DataDownloadTask");
            //if(tempFile.EndsWith(".zip"))
            //{  progress?.Invoke("Unpacking data...");
            //   await UnpackData(tempFile, path); //If zipped
            //}
            //else
            //{
            System.IO.File.Move(tempFile, path);
            //}
            progress?.Invoke("Complete");
        }
    }
}
