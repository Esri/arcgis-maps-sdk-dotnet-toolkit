#if WINDOWS
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Esri.ArcGISRuntime.Tasks;
#if WINUI || MAUI
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
#endif
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.System.Threading;

namespace Esri.ArcGISRuntime.Toolkit
{
    public sealed partial class JobManager
    {
        const string classGuid = "D998B238-C096-4181-B971-9CD1EB760547";

        private JobManager(string? id)
        {
            _id = id;
            Init();
#if WINUI || MAUI // Windows App SDK is required for background tasks
            //TODO RegisterBackgroundTask();
#endif
        }

#if WINUI || MAUI
		private void RegisterBackgroundTask()
        {
            
            var builder = new BackgroundTaskBuilder();
            //var trigger = new SystemTrigger(SystemTriggerType.TimeZoneChange, false);
            //var backgroundTrigger = trigger as IBackgroundTrigger;
            //builder.SetTrigger(backgroundTrigger);
            var trigger = new TimeTrigger(15, false);
            builder.SetTrigger(trigger);
            builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
            builder.SetTaskEntryPointClsid(new Guid(classGuid));
            builder.Register();

            var notification = new AppNotificationBuilder()
.AddText("Background registered")
.BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
#endif
        private const int MaxLength = 4090;

        private void SaveState(string? json)
        {
#if WPF
            throw new NotImplementedException("Job persistence is not implemented for WPF applications."); // TODO
#endif
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Containers.ContainsKey(DefaultsKey))
                localSettings.DeleteContainer(DefaultsKey);
            var entries = json?.Split('\n',StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if(entries is null || entries.Length == 0)
            {
                return;
            }
            var container = localSettings.CreateContainer(DefaultsKey, ApplicationDataCreateDisposition.Always);
            int i = 0;
            foreach (var entry in entries)
            {
                var jsonEntry = entry;
                if(jsonEntry.Length > MaxLength)
                {
                    while(jsonEntry.Length > MaxLength)
                    {   
                        var chunk = jsonEntry.Substring(0, MaxLength - 1);
                        container.Values[(i++).ToString()] = chunk + '\n';
                        jsonEntry = jsonEntry.Substring(MaxLength - 1);
                    }
                }
                container.Values[(i++).ToString()] = jsonEntry;
            }
        }

        partial void OnJobCollectionChanged()
        {
            SaveState();
        }

        private string? LoadStateInternal()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Containers.TryGetValue(DefaultsKey, out var container))
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in container.Values.Keys.OrderBy(k=>int.Parse(k)))
                {
                    var value = container.Values[item];
                    if (value is string json)
                    {
                        var lastchar = json[json.Length - 1];
                        sb.Append(json.TrimEnd());
                        if (lastchar != '\n')
                            sb.Append('\n');
                    }
                }
                return sb.ToString();
            }
            return null;
        }

#if WINUI || MAUI
        [ComVisible(true)]
        [ClassInterface(ClassInterfaceType.None)]
        [Guid(classGuid)]
        [ComSourceInterfaces(typeof(IBackgroundTask))]
        public sealed class BackgroundTask : IBackgroundTask
        {
            private BackgroundTaskDeferral? _deferral = null;
            private volatile bool _cancelRequested = false;
            private ThreadPoolTimer? _periodicTimer = null;
            private int _progress = 0;
            private JobManager? manager;
            private IBackgroundTaskInstance? _taskInstance;
            [MTAThread]
            public void Run(IBackgroundTaskInstance taskInstance)
            {
                
                var taskId = taskInstance.Task.Name;
                manager = JobManager.Create(taskId);
                if(manager.HasRunningJobs == false)
                {
                    return;
                }
                // Get deferral to indicate not to kill the background task process as soon as the Run method returns
                _deferral = taskInstance.GetDeferral();
                // Wire the cancellation handler.
                taskInstance.Canceled += this.OnCanceled;

                // Set the progress to indicate this task has started
                taskInstance.Progress = 0;
                var notification = new AppNotificationBuilder()
                    .AddText("Background task launched!")
                    .BuildNotification();

                AppNotificationManager.Default.Show(notification);

                _periodicTimer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(PeriodicTimerCallback), TimeSpan.FromSeconds(30));
            }

            private async void PeriodicTimerCallback(ThreadPoolTimer timer)
            {
                if (manager is not null && manager.Jobs.Count > 0)
                {
                    if (!_cancelRequested)
                    {
                        try
                        {
                            await manager.PerformStatusChecks();
                            manager.SaveState();
                            var progress = manager.Jobs.Sum(j => j.Progress) / (double)manager.Jobs.Count;
                            if (_taskInstance is not null)
                            {
                                _taskInstance.Progress = (uint)progress;
                            }
                            var notification = new AppNotificationBuilder().AddText("Progress " + progress.ToString() + "%").BuildNotification();
                            AppNotificationManager.Default.Show(notification);
                        }
                        catch { }
                    }
                }
                if (manager is null || manager.HasRunningJobs)
                    _deferral?.Complete();

                //BackgroundTaskBuilder.MainWindow.taskStatus(_progress);
            }

            private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
            {
                // Handle cancellation operations and flag the task to end
                _cancelRequested = true;
            }
        }
#endif
    }
}
#endif