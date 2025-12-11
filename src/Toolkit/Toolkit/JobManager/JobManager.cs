using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Esri.ArcGISRuntime.Tasks;

namespace Esri.ArcGISRuntime.Toolkit
{
    /// <summary>An object that manages saving and loading jobs so that they can continue to run if the
    /// app is backgrounded or even terminated.</summary>
    /// <remarks>
    /// <para>
    /// The job manager is instantiable, but the ``shared`` instance is suitable for most applications.
    ///</para><para>
    /// <b>Background</b><br/>
    /// Jobs are long running server operations. When a job instance is started on the client,
    /// it makes a request to a service asking it to start work on the server. At that point, the client
    /// polls the server intermittently to check the status of the work. Once the work is completed
    /// the result is downloaded with a background `URLSession`. This allows the download to
    /// complete out of process, and the download task can relaunch the app upon completion, even
    /// in the case where the app was terminated.
    ///</para><para>
    /// We do not expect users to keep an application in the foreground and wait for a job to complete.
    /// Once the job is started, if the app is backgrounded, we can use an app refresh
    /// background task to check the status of the work on the server. If the server work is complete
    /// we can start downloading the result in the background at that point. If the work on the server
    /// is not complete, we can reschedule another background app refresh to recheck status.
    ///</para><para>
    /// There is some iOS behavior to be aware of as well. In iOS, if an application is backgrounded,
    /// the operating system can terminate the app at its discretion. This means that jobs need to be
    /// serialized when an app is backgrounded so that if the app is terminated the jobs can be
    /// rehydrated upon relaunch of the app.
    ///</para><para>
    /// Also, in iOS, if the user of an app removes the app from the multitasking UI (aka force quits it),
    /// the system interprets this as a strong indication that the app should
    /// do no more work in the background. The consequences of this are two-fold for jobs.
    /// One, any background fetch tasks are not given any time until the app is relaunched again.
    /// And two, any background downloads that are in progress are canceled by the operating system.
    ///</para><para>
    /// <b>Features</b><br/>
    /// The job manager is an `ObservableObject` with a mutable ``jobs`` property. Adding a job to this
    /// property will allow the job manager to do the work to make sure that we can rehydrate a job
    /// if an app is terminated.
    ///</para><para>
    /// As such, the job manager will:
    /// <list>
    ///  <item>Serialize the job to the user defaults when the app is backgrounded.</item>
    ///  <item>Deserialize the job when an application is relaunched.</item>
    ///  </list>
    ///</para><para>
    /// The job manager will help with the lifetime of jobs in other ways as well.
    ///</para><para>
    /// <b>iOS specific behavior:</b>
    /// The job manager will ask the system for some background processing time when an app
    /// is backgrounded so that jobs that are not yet started on the server, can have some time to
    /// allow them to start. This means if you kick off a job and it hasn't actually started on the server
    /// when the app is backgrounded, the job should have enough time to start on the server which
    /// will cause it to enter into a polling state. When the job reaches the polling state the
    /// status of the work on the server can be checked intermittently.
    ///</para><para>
    /// To enable polling while an app is backgrounded, the job manager will request from the system a
    /// background refresh task (if enabled via the <c>JobManager.PreferredBackgroundStatusCheckSchedule</c>
    /// property). If the system later executes the background refresh task then the
    /// job manager will check the status of any running jobs. At that point the jobs may start
    /// downloading their result. Note, this does not work on the simulator, this behavior can only
    /// be tested on an actual device.
    ///</para><para>
    /// Now that the job can check status in the background, it can start downloading in the background.
    /// By default, jobs will download their results with background URL session. This means that the
    /// download can execute out of process, even if the app is terminated. If the app is terminated and
    /// then later relaunched by the system because a background downloaded completed, then you may
    /// call the <c>JobManager.ResumeAllPausedJobs()</c> method from the application relaunch point,
    /// which will correlate the jobs to their respective downloads that completed and the jobs will
    /// then finish. The app relaunch point can happen via the SwiftUI modifier `.backgroundTask(.urlSession(...))`.
    /// In UIKit it would be the `UIApplicationDelegate` method `func application(UIApplication, handleEventsForBackgroundURLSession: String, completionHandler: () -> Void)`
    /// </para>
    /// </remarks>
#pragma warning disable CA1060 // Move pinvokes to native methods class
    public sealed partial class JobManager
#pragma warning restore CA1060 // Move pinvokes to native methods class
    {
        /// <summary>
        /// Gets the shared job manager.
        /// </summary>
        public static JobManager Shared { get; } = new JobManager(null);

        private string? _id;

        // The key for which state will be serialized under the user defaults.
        private string DefaultsKey =>
            _id != null
                ? $"com.esri.ArcGISToolkit.jobManager.{_id}.statusCheck"
                : "com.esri.ArcGISToolkit.jobManager.statusCheck";

        /// <summary>Creates a job manager with a unique id.</summary>
        /// <remarks><para>This initializer allows you to create a specific instance of a job manager
        /// for cases when you don't want to take over the shared job manager instance.
        ///</para><para>
        /// The provided ID should be unique to a specific purpose in your application.
        /// On each successive run of the app, you must re-use the same id when you initialize
        /// your job manager for it to be able to properly reload its state.
        ///</para><para>
        /// If you create multiple instances with the same id the behavior is undefined.
        ///</para></remarks>
        /// <param name="id">The unique identifier for this job manager.</param>
        public static JobManager Create(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id cannot be null or whitespace", nameof(id));

            return new JobManager(id);
        }

        private void Init()
        {
            LoadState();
            foreach (var job in Jobs)
            {
                job.StatusChanged += Job_StatusChanged;
                job.ProgressChanged += Job_ProgressChanged;
            }
            ((INotifyCollectionChanged)Jobs).CollectionChanged += JobCollection_CollectionChanged;
        }

        private void Job_ProgressChanged(object? sender, EventArgs e) => SaveState();

        private void JobCollection_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach(var job in e.NewItems?.OfType<IJob>() ?? Array.Empty<IJob>())
            {
                job.StatusChanged += Job_StatusChanged;
                job.ProgressChanged += Job_ProgressChanged;
            }
            foreach (var job in e.OldItems?.OfType<IJob>() ?? Array.Empty<IJob>())
            {
                job.StatusChanged -= Job_StatusChanged;
                job.ProgressChanged -= Job_ProgressChanged;
            }
            SaveState();
        }

        private void Job_StatusChanged(object? sender, JobStatus e) =>  SaveState();

        /// <summary>
        /// The jobs being managed by the job manager.
        /// </summary>
        public IList<IJob> Jobs { get; } = new System.Collections.ObjectModel.ObservableCollection<IJob>();

        // A Boolean value indicating if there are jobs running.
        private bool HasRunningJobs => Jobs.Any(j => j.Status == JobStatus.Started);

        /// <summary>
        /// Check the status of all managed jobs.
        /// </summary>
        /// <returns></returns>
        public async Task PerformStatusChecks()
        {
            var tasks = Jobs.Select(async job =>
            {
                try { await job.CheckStatusAsync(); } catch { }
            });
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Resumes all paused jobs.
        /// </summary>
        public async void ResumeAllPausedJobs()
        {
            // Make sure the default background URLSession is re-created here
            // in case this method is called from an app relaunch due to background downloads
            // completed for a terminated app. We need the session to be re-created in that case.
            // TODO ?? _ = ArcGISEnvironment.backgroundURLSession;

            await PerformStatusChecks(); // Won't throw
            foreach (var job in Jobs.Where(j => j.Status == JobStatus.Paused))
            {
                job.Start();
            }
        }

        private object StateLock = new object();

        /// <summary>
        /// Saves the current job state
        /// </summary>
        public void SaveState()
        {
            var array = Jobs.Select(j => j.ToJson()).ToArray();
            lock (StateLock)
            {
                SaveState(string.Join("\n", array));
            }
        }

        private void LoadState()
        {

            lock (StateLock)
            {
                string? data = LoadStateInternal();
                if (!string.IsNullOrEmpty(data))
                {
                    Jobs.Clear();
                    foreach (var job in data.Split('\n')
                        .Select(json =>
                        {
                            try { return IJob.FromJson(json)!; }
                            catch { return null; }
                        })
                        .Where(j => j != null))
                        Jobs.Add(job);
                }
            }
        }
    }
}
