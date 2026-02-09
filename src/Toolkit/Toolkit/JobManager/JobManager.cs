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
        private bool _lastHasRunningJobsValue = false;

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
            foreach (var job in e.NewItems?.OfType<IJob>() ?? Array.Empty<IJob>())
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
            CheckAnyJobRunningStateChanged();
        }

        private void Job_StatusChanged(object? sender, JobStatus e)
        {
            SaveState();
            CheckAnyJobRunningStateChanged();
        }

        private void CheckAnyJobRunningStateChanged()
        {
            if (_lastHasRunningJobsValue != HasRunningJobs)
            {
                _lastHasRunningJobsValue = HasRunningJobs;
                JobsRunningChanged(_lastHasRunningJobsValue);
            }
        }

        partial void JobsRunningChanged(bool hasRunningJobs);

        /// <summary>
        /// The jobs being managed by the job manager.
        /// </summary>
        public IList<IJob> Jobs { get; } = new JobCollection();

        // A Boolean value indicating if there are jobs running.
        private bool HasRunningJobs
        {
            get 
            {
                lock (((JobCollection)Jobs).SyncRoot) { return Jobs.Any(j => j.Status == JobStatus.Started); }
            }
        }

        /// <summary>
        /// Check the status of all managed jobs.
        /// </summary>
        /// <returns></returns>
        public async Task PerformStatusChecks()
        {
            Task[] tasks;
            lock (((JobCollection)Jobs).SyncRoot)
            {
                tasks = Jobs.Select(async job =>
                {
                    try { await job.CheckStatusAsync(); } catch { }
                }).ToArray();
            }
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Resumes all paused jobs.
        /// </summary>
        public async Task ResumeAllPausedJobsAsync()
        {
            await PerformStatusChecks(); // Won't throw
            lock (((JobCollection)Jobs).SyncRoot)
            {
                foreach (var job in Jobs.Where(j => j.Status == JobStatus.Paused))
                {
                    job.Start();
                }
            }
        }

        /// <summary>
        /// Saves the current job state
        /// </summary>
        public void SaveState()
        {
            string[] array;
            lock (((JobCollection)Jobs).SyncRoot)
                array = Jobs.Select(j => j.ToJson()).ToArray();
            try
            {
                SaveState(string.Join("|\n|", array));
            }
            catch { }
        }

        private void LoadState()
        {
            string? data = LoadStateInternal();
            if (!string.IsNullOrEmpty(data))
            {
                foreach (var job in data.Split("|\n|")
                    .Select(json =>
                    {
                        try { return IJob.FromJson(json)!; }
                        catch { return null; }
                    })
                    .Where(j => j != null))
                    Jobs.Add(job!);
            }
        }

        /// <summary>
        /// Exposes a lock to ensure edits to the collection is thread safe, and exposes the syncroot
        /// to allow thread-safe reads.
        /// </summary>
        private sealed partial class JobCollection : System.Collections.ObjectModel.ObservableCollection<IJob>
        {
            public object SyncRoot { get; } = new object();

            public JobCollection() { }

            protected override void InsertItem(int index, IJob item)
            {
                lock (SyncRoot)
                    base.InsertItem(index, item);
            }

            protected override void MoveItem(int oldIndex, int newIndex)
            {
                lock (SyncRoot)
                    base.MoveItem(oldIndex, newIndex);
            }

            protected override void ClearItems()
            {
                lock (SyncRoot)
                    base.ClearItems();
            }

            protected override void RemoveItem(int index)
            {
                lock (SyncRoot)
                    base.RemoveItem(index);
            }

            protected override void SetItem(int index, IJob item)
            {
                lock (SyncRoot)
                    base.SetItem(index, item);
            }
        }
    }
}
