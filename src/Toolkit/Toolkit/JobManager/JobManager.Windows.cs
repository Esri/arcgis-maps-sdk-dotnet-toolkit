#if WINDOWS
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Esri.ArcGISRuntime.Tasks;
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
            RegisterBackgroundTask();
        }

        private void RegisterBackgroundTask()
        {
            if (!IsAppPackaged)
            {
                return;
            }
            try
            {
                // TODO: Check app manifest for background task declaration
                var builder = new BackgroundTaskBuilder();
                var trigger = new TimeTrigger(15, false);
                builder.SetTrigger(trigger);
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                builder.SetTaskEntryPointClsid(new Guid(classGuid));
                builder.Register();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Failed to register background task for JobManager: " + ex.Message);
            }
        }

        private void SaveState(string? json) => File.WriteAllText(GetStateFilename(), json);

        private string? LoadStateInternal()
        {
            var filename = GetStateFilename();
            if (System.IO.File.Exists(filename))
                return System.IO.File.ReadAllText(GetStateFilename());
            return null;
        }

        private string GetStateFilename()
        {
            if (IsAppPackaged)
            {
                var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(folderPath, $"{DefaultsKey}.json");
            }
            else
            {
                // If the app is unpackaged, we'll generate a unique filename based on the process and assembly and place it in the temp folder
                string location = Environment.ProcessPath + "|" + typeof(JobManager).Assembly.FullName + "|" + DefaultsKey;
                using (var hasher = System.Security.Cryptography.SHA256.Create())
                {
                    StringBuilder hash = new StringBuilder();
                    byte[] bytes = hasher.ComputeHash(new UTF8Encoding().GetBytes(location));
                    for (int i = 0; i < bytes.Length / 2; i++) // Only grab the first half to keep path name short
                    {
                        hash.Append(bytes[i].ToString("x2"));
                    }

                    return Path.Combine(Path.GetTempPath(), hash.ToString() + ".json");
                }
            }
        }

#pragma warning disable SA1203 // Constants should appear before fields
        private const long AppModelErrorNoPackage = 15700L;
#pragma warning restore SA1203 // Constants should appear before fields

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder packageFullName);

        private static bool IsAppPackaged
        {
            get
            {
                // Application is MSIX packaged if it has an identity: https://learn.microsoft.com/en-us/windows/msix/detect-package-identity
                int length = 0;
                var sb = new StringBuilder(0);
                int result = GetCurrentPackageFullName(ref length, sb);
                return result != AppModelErrorNoPackage;
            }
        }

        [ComVisible(true)]
        [ClassInterface(ClassInterfaceType.None)]
        [Guid(classGuid)]
        [ComSourceInterfaces(typeof(IBackgroundTask))]
        internal sealed partial class BackgroundTask : IBackgroundTask
        {
            private BackgroundTaskDeferral? _deferral = null;
            private volatile bool _cancelRequested = false;
            private ThreadPoolTimer? _periodicTimer = null;
            private JobManager? manager;
            private IBackgroundTaskInstance? _taskInstance;

            [MTAThread]
            public void Run(IBackgroundTaskInstance taskInstance)
            {
                
                var taskId = taskInstance.Task.Name;
                manager = JobManager.Create(taskId);
                if(!manager.HasRunningJobs)
                {
                    return;
                }
                // Get deferral to indicate not to kill the background task process as soon as the Run method returns
                _deferral = taskInstance.GetDeferral();
                // Wire the cancellation handler.
                taskInstance.Canceled += this.OnCanceled;

                // Set the progress to indicate this task has started
                taskInstance.Progress = 0;
                
                _periodicTimer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(PeriodicTimerCallback), TimeSpan.FromSeconds(30));
            }

            private async void PeriodicTimerCallback(ThreadPoolTimer timer)
            {
                if (manager is not null && manager.HasRunningJobs)
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
                        }
                        catch { }
                    }
                }
                else
                {
                    _periodicTimer?.Cancel();
                }
                if (manager is null || !manager.HasRunningJobs)
                    _deferral?.Complete();
            }

            private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
            {
                // Handle cancellation operations and flag the task to end
                _cancelRequested = true;
            }
        }
    }
}
#endif