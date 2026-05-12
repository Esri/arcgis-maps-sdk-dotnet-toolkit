#if __IOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.Tasks;
using Foundation;
using UIKit;
using BackgroundTasks;

// Port of https://github.com/Esri/arcgis-maps-sdk-swift-toolkit/blob/117a0a35662f7a2403b22d7048e4f0c53a97c07c/Sources/ArcGISToolkit/Components/JobManager/JobManager.swift
// Also see https://mediaspace.esri.com/media/t/1_a756nhrs/368599242 at 15min+ mark.

namespace  Esri.ArcGISRuntime.Toolkit
{
    public sealed partial class JobManager
    {
        private BackgroundStatusCheckSchedule _preferredBackgroundStatusCheckSchedule = BackgroundStatusCheckSchedule.Disabled;

        /// <summary>
        /// The preferred schedule for performing status checks while the application is in the
        /// background. This allows an application to check to see if jobs have completed and optionally
        /// post a local notification to update the user. The default value is `disabled`.
        /// When the value of this property is not `disabled`, this setting is just a preference.
        /// The operating system ultimately decides when to allow a background task to run.
        /// If you enable background status checks then you must also make sure to have enabled
        /// the "Background fetch" background mode in your application settings.
        /// </summary>
        /// <remarks>
        /// <note>
        /// You must also add the <see cref="StatusChecksTaskIdentifier"/> to the "Permitted
        /// background task scheduler identifiers" in your application's plist file.
        /// The status checks task identifier will be <c>com.esri.ArcGISToolkit.jobManager.statusCheck</c> if using the shared instance.
        /// If you are using a job manager instance that you created with a specific ID, then the
        /// identifier will be <c>com.esri.ArcGISToolkit.jobManager.&lt;id&gt;.statusCheck</c>.
        /// </note>
        /// <para>
        /// Background checks only work on device and not on the simulator.
        /// More information can be found <a href="https://developer.apple.com/documentation/backgroundtasks/refreshing_and_maintaining_your_app_using_background_tasks">here</a>.
        /// </para>
        /// </remarks>
        public BackgroundStatusCheckSchedule PreferredBackgroundStatusCheckSchedule
        {
            get { return _preferredBackgroundStatusCheckSchedule; }
            set
            {
                if (value == BackgroundStatusCheckSchedule.Disabled || ValidateInfoPlist())
                    _preferredBackgroundStatusCheckSchedule = value;
            }
        }

        /// <summary>
        /// Gets the task identifier used for scheduling background status checks.
        /// </summary>
        public string StatusChecksTaskIdentifier =>
            _id != null
                ? $"com.esri.ArcGISToolkit.jobManager.{_id}.statusCheck"
                : "com.esri.ArcGISToolkit.jobManager.statusCheck";

        private bool _isBackgroundStatusChecksScheduled = false;

        private nint? _backgroundTaskIdentifier;

        private JobManager(string? id)
        {
            _id = id;
            NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillEnterForegroundNotification, _ => AppWillEnterForeground());
            NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillResignActiveNotification, _ => AppWillResignActive());
            NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillTerminateNotification, _ => AppWillTerminate());

            BGTaskScheduler.Shared.Register(StatusChecksTaskIdentifier, null, task =>
            {
                _isBackgroundStatusChecksScheduled = false;
                Task.Run(async () =>
                {
                    await PerformStatusChecks();
                    ScheduleBackgroundStatusCheck();
                    task.SetTaskCompleted(true);
                });
            });
            Init();
        }

        // Schedules a status check in the background if one is not already scheduled.
        private void ScheduleBackgroundStatusCheck()
        {
            // Return if already scheduled.
            if (_isBackgroundStatusChecksScheduled)
                return;
            // Do not schedule if there are no running jobs.
            if (!HasRunningJobs)
                return;
            // Make sure the preferred background status check schedule
            if (PreferredBackgroundStatusCheckSchedule == BackgroundStatusCheckSchedule.Disabled)
                return;

            _isBackgroundStatusChecksScheduled = true;
            var request = new BGAppRefreshTaskRequest(StatusChecksTaskIdentifier)
            {
                EarliestBeginDate = NSDate.Now.AddSeconds(PreferredBackgroundStatusCheckSchedule.Interval)
            };
            BGTaskScheduler.Shared.Submit(request, out var Error);
            if (Error != null)
            {
                _isBackgroundStatusChecksScheduled = false;
                System.Diagnostics.Trace.WriteLine($"Error scheduling background status check: {Error.LocalizedDescription}", "ArcGIS Toolkit");
            }
        }


        /// <summary>
        /// Called when the app moves back to the foreground.
        /// </summary>
        private void AppWillEnterForeground() => EndCurrentBackgroundTask();

        /// <summary>
        /// Called when the app moves to the background.
        /// </summary>
        private void AppWillResignActive()
        {
            // Start a background task if necessary.
            BeginBackgroundTask();
            // Schedule background status checks.
            ScheduleBackgroundStatusCheck();
            // Save the jobs to the user defaults when the app moves to the background.
            SaveState();
        }

        /// <summary>
        /// Called when the app will be terminated.
        /// </summary>
        private void AppWillTerminate() => SaveState();

        

        /// <summary>
        /// Saves all managed jobs to User Defaults.
        /// </summary>
        private void SaveState(string json)
        {
            NSUserDefaults.StandardUserDefaults.SetString(json, DefaultsKey);
        }

        /// <summary>
        /// Load any jobs that have been saved to UserDefaults.
        /// </summary>
        private string? LoadStateInternal()
        {
            return NSUserDefaults.StandardUserDefaults.StringForKey(DefaultsKey);
        }

        /// <summary>
        /// Ends any current background task.
        /// </summary>
        private void EndCurrentBackgroundTask()
        {
            if (_backgroundTaskIdentifier.HasValue)
            {
                UIApplication.SharedApplication.EndBackgroundTask(_backgroundTaskIdentifier.Value);
                _backgroundTaskIdentifier = null;
            }
        }

        private void BeginBackgroundTask()
        {
            // Jobs that are started but do not yet have a server job ID are jobs that
            // can benefit from starting a background task for extra execution time.
            // This will hopefully allow a job to get to the polling state before the app is suspended.
            // Once in a polling state that's where background refreshes can check job status.
            if (!Jobs.Any(j => j.Status == JobStatus.Started && string.IsNullOrEmpty(j.ServerJobId))) return;
            // Already started.
            if (_backgroundTaskIdentifier.HasValue) return;

            _backgroundTaskIdentifier = UIApplication.SharedApplication.BeginBackgroundTask(() =>
            {
                EndCurrentBackgroundTask();
            });
        }

        internal bool ValidateInfoPlist()
        {
            var bgtask = Foundation.NSBundle.MainBundle.ObjectForInfoDictionary("BGTaskSchedulerPermittedIdentifiers");
            if (bgtask is null)
            {
                //throw new InvalidOperationException("Missing 'BGTaskSchedulerPermittedIdentifiers' entry in info.plist");
                System.Diagnostics.Trace.WriteLine("Missing 'BGTaskSchedulerPermittedIdentifiers' entry in info.plist", "ArcGIS Toolkit");
                return false;
            }
            // Get the array of permitted identifiers from the info.plist BGTaskSchedulerPermittedIdentifiers
            // and ensure that `com.esri.ArcGISToolkit.jobManager.statusCheck` is present.
            var array = bgtask as Foundation.NSArray;
            if (array == null)
            {
                System.Diagnostics.Trace.WriteLine("'BGTaskSchedulerPermittedIdentifiers' entry in info.plist is not an array.", "ArcGIS Toolkit");
                //throw new InvalidOperationException("'BGTaskSchedulerPermittedIdentifiers' entry in info.plist is not an array.");
                return false;
            }
            for (nuint i = 0; i < array.Count; i++)
            {
                var value = array.GetItem<Foundation.NSString>(i);
                if (value != null && value.ToString() == DefaultsKey)
                {
                    return true;
                }
            }
            System.Diagnostics.Trace.WriteLine($"'BGTaskSchedulerPermittedIdentifiers' must contain '{DefaultsKey}'.", "ArcGIS Toolkit");
            return false;
        }
    }

    /// <summary>
    /// Defines a schedule for background status checks.
    /// </summary>
    /// <seealso cref="JobManager.PreferredBackgroundStatusCheckSchedule"/>
    public sealed class BackgroundStatusCheckSchedule
    {
        private BackgroundStatusCheckSchedule()
        {
        }

        /// <summary>
        /// No background status checks will be requested.
        /// </summary>
        public static readonly BackgroundStatusCheckSchedule Disabled = new BackgroundStatusCheckSchedule();
        /// <summary>
        /// Requests that the system schedule a background check at a regular interval.
        /// Ultimately it is up to the discretion of the system if that check is run.
        /// </summary>
        /// <param name="interval">Number of seconds between each background status check.</param>
        /// <returns>BackgroundStatusCheckSchedule</returns>
        public static BackgroundStatusCheckSchedule RegularInterval(double interval)
        {
            if (interval <= 0) throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero.");
            return new BackgroundStatusCheckSchedule { Interval = interval };
        }

        /// <summary>
        /// Number of seconds between each background status check or <c>zero</c> if disabled.
        /// </summary>
        public double Interval { get; private set; }
    }
}
#endif
