#if WINDOWS
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Esri.ArcGISRuntime.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.System.Threading;
using WinRT;

namespace Esri.ArcGISRuntime.Toolkit
{
    public sealed partial class JobManager
    {
        const string classGuid = "D998B238-C096-4181-B971-9CD1EB760547";
        private readonly object taskRegistrationLock = new object();

        private JobManager(string? id)
        {
            _id = id;
            Init();
        }

        partial void JobsRunningChanged(bool hasRunningJobs)
        {
            if (hasRunningJobs)
                RegisterBackgroundTask();
            else
                UnregisterBackgroundTask();
        }

        private bool IsBackgroundTaskRegistered()
        {
            if (!IsAppPackaged)
                return false;
            lock (taskRegistrationLock)
                return BackgroundTaskRegistration.AllTasks.Where(b => b.Value.Name == DefaultsKey).Any();
        }

        private void UnregisterBackgroundTask()
        {
            if (IsAppPackaged)
            {
                lock (taskRegistrationLock)
                {
                    foreach (var task in BackgroundTaskRegistration.AllTasks.Where(b => b.Value.Name == DefaultsKey).ToArray())
                        task.Value.Unregister(true);
                }
            }
        }

        private void RegisterBackgroundTask()
        {
            if (!IsAppPackaged)
                return;
            if (IsBackgroundTaskRegistered())
                return;
            try
            {
                lock (taskRegistrationLock)
                {
                    var builder = new BackgroundTaskBuilder();
                    var trigger = new TimeTrigger(15, false); // Minimum allowed time trigger is 15 minutes
                    builder.SetTrigger(trigger);
                    builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable)); // No reason to run this while offline
                    builder.SetTaskEntryPointClsid(new Guid(classGuid));
                    builder.Name = DefaultsKey;
                    builder.Register();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Failed to register background task for JobManager: " + ex.Message);
            }
        }

        private readonly object fileLock = new object();

        private void SaveState(string? json)
        {
            lock (fileLock)
            {
                var stateFile = GetStateFilename();
                if (string.IsNullOrEmpty(json))
                {
                    if (File.Exists(stateFile))
                        File.Delete(stateFile);
                    return;
                }
                var tmpFile = Path.GetTempFileName();
                File.WriteAllText(tmpFile, json);
                if (File.Exists(stateFile))
                    File.Replace(tmpFile, stateFile, stateFile + ".bak");
                else
                    File.Move(tmpFile, stateFile, true);
            }
        }

        private string? LoadStateInternal()
        {
            lock (fileLock)
            {
                var filename = GetStateFilename();
                if (File.Exists(filename))
                    return File.ReadAllText(GetStateFilename());
            }
            return null;
        }

        private string GetStateFilename()
        {
            if (IsAppPackaged)
            {
                var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(folderPath, $"{HashString(DefaultsKey)}.json");
            }
            else
            {
                // If the app is unpackaged, we'll generate a unique filename based on the process and assembly and place it in the temp folder
                string location = Environment.ProcessPath + "|" + typeof(JobManager).Assembly.FullName + "|" + DefaultsKey;
                return Path.Combine(Path.GetTempPath(), HashString(location) + ".json");
            }
        }

        private static string HashString(string input)
        {
            using (var hasher = System.Security.Cryptography.SHA256.Create())
            {
                StringBuilder hash = new StringBuilder();
                byte[] bytes = hasher.ComputeHash(new UTF8Encoding().GetBytes(input));
                for (int i = 0; i < bytes.Length / 2; i++) // Only grab the first half to keep path name short
                {
                    hash.Append(bytes[i].ToString("x2"));
                }
                return hash.ToString();
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


        static private uint _RegistrationToken;

        static private ManualResetEvent _exitEvent = new ManualResetEvent(false);

        /// <summary>
        /// Registers the background task COM server with the application. Only call this if the program startup arguments contains <c>-RegisterForBGTaskServer</c>.
        /// </summary>
        /// <remarks>
        /// For the JobManager to also run in the background, it must be registered as a background task COM server on application startup.
        /// In the Main startup method, add the following code:
        /// <code language="csharp">
        /// static void Main(string[] args)
        /// {
        ///     if (args.Contains("-RegisterForBGTaskServer"))
        ///     {
        ///         JobManager.RegisterForBGTaskServer();
        ///     }
        ///     else
        ///     {
        ///         // Normal app startup code here
        ///     }
        /// }
        /// </code>
        /// For the job manager background task to work, the application manifest must also be configured to declare the background task using the COM server class ID "D998B238-C096-4181-B971-9CD1EB760547".
        /// Under the Application section add:
        /// <code language="xml">
        /// /// *lt;Extensions>
        ///     *lt;!-- Below Entry Point is to be mentioned in case of C# application for usage of WinAppSDK Background Task API -->
        ///     *lt;Extension Category="windows.backgroundTasks" EntryPoint="Microsoft.Windows.ApplicationModel.Background.UniversalBGTask.Task">
        ///     *lt;BackgroundTasks>
        ///         *lt;Task Type="general"/>
        ///     *lt;/BackgroundTasks>
        ///     *lt;/Extension>
        ///     *lt;com:Extension Category="windows.comServer">
        ///     *lt;com:ComServer>
        ///         *lt;!-- COM Server for the background task, LaunchAndActivationPermission is required to give permission
        ///      for backgroundtaskhost process to cocreate this COM component -->
        ///         *lt;com:ExeServer Executable="MyApplicationName.exe" Arguments="-RegisterForBGTaskServer" DisplayName="BackgroundTask"
        ///               LaunchAndActivationPermission="O:PSG:BUD:(A;;11;;;IU)(A;;11;;;S-1-15-2-1)S:(ML;;NX;;;LW)">
        ///         *lt;com:Class Id="D998B238-C096-4181-B971-9CD1EB760547" DisplayName="BackgroundTask" />
        ///         *lt;/com:ExeServer>
        ///     *lt;/com:ComServer>
        ///     *lt;/com:Extension>
        /// *lt;/Extensions>
        /// </code>
        /// And under the <c>&ltPackage&gt;</c> section add the following:
        /// <code language="xml">
        /// &lt;Extensions>
        ///     &lt;Extension Category="windows.activatableClass.inProcessServer">
        ///         &lt;InProcessServer>
        ///             &lt;Path>Microsoft.Windows.ApplicationModel.Background.UniversalBGTask.dll&lt;/Path>
        ///             &lt;ActivatableClass ActivatableClassId="Microsoft.Windows.ApplicationModel.Background.UniversalBGTask.Task" ThreadingModel="both"/>
        ///         &lt;/InProcessServer>
        ///     &lt;/Extension>
        /// &lt;/Extensions>
        /// </code>
        /// </remarks>
        public static void RegisterForBGTaskServer()
        {
            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.Initialize();
            // TODO: Check that manifest has been correctly configured
            Guid taskGuid = typeof(BackgroundTask).GUID;
            ComServer.CoRegisterClassObject(ref taskGuid,
                                            new ComServer.BackgroundTaskFactory(),
                                            ComServer.CLSCTX_LOCAL_SERVER,
                                            ComServer.REGCLS_MULTIPLEUSE,
                                            out _RegistrationToken);

            // Wait for the exit event to be signaled before exiting the program
            _exitEvent.WaitOne();
        }

        [ComVisible(true)]
        [ClassInterface(ClassInterfaceType.None)]
        [Guid(classGuid)]
        [ComSourceInterfaces(typeof(IBackgroundTask))]
        internal sealed partial class BackgroundTask : IBackgroundTask, IDisposable
        {
            private BackgroundTaskDeferral? _deferral = null;
            private volatile bool _cancelRequested = false;
            private ThreadPoolTimer? _periodicTimer = null;
            private JobManager? manager;
            private IBackgroundTaskInstance? _taskInstance;
            private bool disposed = false;

            [MTAThread]
            public async void Run(IBackgroundTaskInstance taskInstance)
            {
                var taskName = taskInstance.Task.Name;
                if (taskName == "com.esri.ArcGISToolkit.jobManager.statusCheck")
                    manager = JobManager.Shared;
                else if(taskName.StartsWith("com.esri.ArcGISToolkit.jobManager.") && taskName.EndsWith(".statusCheck"))
                {
                    string taskId = taskName.Substring(34).Replace(".statusCheck", "");
                    if (string.IsNullOrEmpty(taskId)) return;
                    manager = JobManager.Create(taskId);
                }
                else
                {
                    return; // Unknown task id
                }
                // Get deferral to indicate not to kill the background task process as soon as the Run method returns
                _deferral = taskInstance.GetDeferral();
                try
                {
                    await manager.ResumeAllPausedJobsAsync();
                    if (!manager.HasRunningJobs)
                    {
                        _deferral.Complete();
                        return;
                    }

                    // Wire the cancellation handler.
                    taskInstance.Canceled += this.OnCanceled;

                    // Set the progress to indicate this task has started
                    taskInstance.Progress = 0;

                    _periodicTimer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(PeriodicTimerCallback), TimeSpan.FromSeconds(30));
                    _taskInstance = taskInstance;
                }
                catch
                {
                    _deferral?.Complete();
                    JobManager._exitEvent.Set();
                }
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
                {
                    _deferral?.Complete();
                    JobManager._exitEvent.Set();
                }
            }

            private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
            {
                // Handle cancellation operations and flag the task to end
                _cancelRequested = true;
            }
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        // Stop the server when the background task is disposed.
                        JobManager._exitEvent.Set();
                    }
                    disposed = true;
                }
            }

            ~BackgroundTask()
            {
                Dispose(false);
            }
        }

        // COM server startup code.
        internal static partial class ComServer
        {
            [LibraryImport("ole32.dll")]
            public static partial int CoRegisterClassObject(
                ref Guid classId,
                [MarshalAs(UnmanagedType.Interface)] IClassFactory objectAsUnknown,
                uint executionContext,
                uint flags,
                out uint registrationToken);

            public const uint CLSCTX_LOCAL_SERVER = 4;
            public const uint REGCLS_MULTIPLEUSE = 1;

            public const uint S_OK = 0x00000000;
            public const uint CLASS_E_NOAGGREGATION = 0x80040110;
            public const uint E_NOINTERFACE = 0x80004002;

            public const string IID_IUnknown = "00000000-0000-0000-C000-000000000046";
            public const string IID_IClassFactory = "00000001-0000-0000-C000-000000000046";

            [GeneratedComInterface]
            [Guid(IID_IClassFactory)]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            public partial interface IClassFactory
            {
                [PreserveSig]
                uint CreateInstance(IntPtr objectAsUnknown, in Guid interfaceId, out IntPtr objectPointer);

                [PreserveSig]
                uint LockServer([MarshalAs(UnmanagedType.Bool)] bool Lock);
            }

            [GeneratedComClass]
            internal sealed partial class BackgroundTaskFactory : IClassFactory
            {
                public uint CreateInstance(IntPtr objectAsUnknown, in Guid interfaceId, out IntPtr objectPointer)
                {
                    if (objectAsUnknown != IntPtr.Zero)
                    {
                        objectPointer = IntPtr.Zero;
                        return CLASS_E_NOAGGREGATION;
                    }

                    if ((interfaceId != typeof(BackgroundTask).GUID) && (interfaceId != new Guid(IID_IUnknown)))
                    {
                        objectPointer = IntPtr.Zero;
                        return E_NOINTERFACE;
                    }

                    objectPointer = MarshalInterface<IBackgroundTask>.FromManaged(new BackgroundTask());
                    return S_OK;
                }

                public uint LockServer(bool lockServer) => S_OK;
            }
        }
    }
}
#endif