// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

#nullable enable

using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.Tasks.Offline;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.Toolkit
{
    /// <summary>
    /// A utility object that maintains the state of offline map areas and their
    /// storage on the device.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This component provides high-level APIs to manage offline map areas and
    /// access their data.
    /// This component manages offline jobs and it utilizes the <see cref="JobManager"/>
    /// in order to facilitate jobs continuing to run while the app is backgrounded.
    /// </para>
    /// <para>
    /// <b>Features</b><br/>
    /// The component supports both ahead-of-time(preplanned) and on-demand
    /// workflows for offline mapping. It allows you to:
    /// <list type="bullet">
    /// <item>Observe job status.</item>
    /// <item>Access map info for web maps that have saved map areas via <see cref="OfflineMapInfos"/>.</item>
    /// <item>Remove offline map areas from the device.</item>
    /// <item>Run download jobs while the app is in the background.</item>
    /// <item>Get notified when the jobs complete via the <see cref="JobCompleted"/> event.</item>
    /// </list>
    /// </para>
    /// <para>
    /// The component is useful both for building custom UI with the provided APIs,
    /// and for supporting workflows that require retrieving offline map areas
    /// information from the device. By using the `OfflineManager`, you can create
    /// an `OfflineMapAreasView` using the ``OfflineMapAreasView/init(offlineMapInfo:selection:)``
    /// initializer.
    /// </para>
    /// <para>
    /// <b>Behavior</b><br/>
    /// The offline manager is not instantiable; use the shared instance.
    /// Configure the offline manager through the <see cref="Configuration"/> property
    /// before starting jobs.
    /// </para>
    /// <note>
    /// The <see cref="OfflineManager"/> can be used independently of any UI components,
    /// making it suitable for automated workflows or custom implementations.
    /// </note>
    /// </remarks>
    public sealed class OfflineManager
    {
        private readonly JobManager _jobManager = JobManager.Create("offlineManager");
        private readonly ObservableCollection<OfflineMapInfo> _offlineMapInfos = new ObservableCollection<OfflineMapInfo>();
        private readonly HashSet<IJob> _observedJobs = new HashSet<IJob>(ReferenceEqualityComparer<IJob>.Instance);
        private readonly object _observedJobsLock = new object();
        private readonly SynchronizationContext? _synchronizationContext;
        private OfflineManagerConfiguration _configuration = new OfflineManagerConfiguration();

        private OfflineManager()
        {
            _synchronizationContext = SynchronizationContext.Current;
            OfflineMapInfos = new ReadOnlyObservableCollection<OfflineMapInfo>(_offlineMapInfos);

            ApplyConfiguration(_configuration);
            LoadOfflineMapInfos();

            foreach (var job in _jobManager.Jobs)
            {
                ObserveJob(job);
            }

            _ = _jobManager.ResumeAllPausedJobsAsync();
        }

        /// <summary>
        /// Gets the shared offline manager instance.
        /// </summary>
        public static OfflineManager Instance { get; } = new OfflineManager();

        /// <summary>
        /// Gets the shared offline manager instance.
        /// </summary>
        public static OfflineManager Shared => Instance;

        /// <summary>
        /// Gets or sets the configuration for this offline manager.
        /// </summary>
        public OfflineManagerConfiguration Configuration
        {
            get => _configuration;
            set
            {
                _configuration = value ?? throw new ArgumentNullException(nameof(value));
                ApplyConfiguration(_configuration);
            }
        }

        /// <summary>
        /// Gets the jobs currently managed by this instance.
        /// </summary>
        public IList<IJob> Jobs => _jobManager.Jobs;

        /// <summary>
        /// Gets the portal item information for web maps that have downloaded map areas.
        /// </summary>
        public ReadOnlyObservableCollection<OfflineMapInfo> OfflineMapInfos { get; }

        /// <summary>
        /// Occurs when a managed job completes.
        /// </summary>
        public event EventHandler<JobCompletedEventArgs>? JobCompleted;

        /// <summary>
        /// Starts a job that will be managed by this instance.
        /// </summary>
        /// <param name="job">The job to start.</param>
        /// <param name="portalItem">The portal item whose map is being taken offline.</param>
        /// <param name="title">The title of the map area being taken offline.</param>
        public Task StartJobAsync(OfflineMapSyncJob job, PortalItem portalItem, string title)
            => StartJobAsync(job, portalItem, title);

        /// <summary>
        /// Starts a job that will be managed by this instance.
        /// </summary>
        /// <param name="job">The job to start.</param>
        /// <param name="portalItem">The portal item whose map is being taken offline.</param>
        /// <param name="title">The title of the map area being taken offline.</param>
        public Task StartJobAsync(GenerateOfflineMapJob job, PortalItem portalItem, string title)
            => StartJobAsync(job, portalItem, title);

        /// <summary>
        /// Starts a job that will be managed by this instance.
        /// </summary>
        /// <param name="job">The job to start.</param>
        /// <param name="portalItem">The portal item whose map is being taken offline.</param>
        /// <param name="title">The title of the map area being taken offline.</param>
        public Task StartJobAsync(DownloadPreplannedOfflineMapJob job, PortalItem portalItem, string title)
            => StartJobAsync(job, portalItem, title);

        private async Task StartJobAsync(IJob job, PortalItem portalItem, string title)
        {
            _ = title;

            if (job is null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            if (portalItem is null)
            {
                throw new ArgumentNullException(nameof(portalItem));
            }

            if (!_jobManager.Jobs.Contains(job))
            {
                _jobManager.Jobs.Add(job);
            }

            ObserveJob(job);
            job.Start();

            try
            {
                await SavePendingMapInfoAsync(portalItem).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"ArcGIS Toolkit - Error saving pending offline map info: {ex}");
            }
        }

        /// <summary>
        /// Removes all downloads for all offline maps.
        /// </summary>
        public void RemoveAllDownloads()
        {
            foreach (var offlineMapInfo in OfflineMapInfos.ToArray())
            {
                RemoveDownloads(offlineMapInfo);
            }
        }

        /// <summary>
        /// Removes any downloaded map areas for a particular map.
        /// </summary>
        /// <param name="offlineMapInfo">The information for the offline map for which all downloads will be removed.</param>
        public void RemoveDownloads(OfflineMapInfo offlineMapInfo)
        {
            if (offlineMapInfo is null)
            {
                throw new ArgumentNullException(nameof(offlineMapInfo));
            }

            var portalItemDirectory = GetPortalItemFilename(offlineMapInfo.Id);
            if (File.Exists(portalItemDirectory))
            {
                File.Delete(portalItemDirectory);
            }

            RunOnCapturedContext(() =>
            {
                var existing = _offlineMapInfos.FirstOrDefault(info => string.Equals(info.Id, offlineMapInfo.Id, StringComparison.Ordinal));
                if (existing is not null)
                {
                    _offlineMapInfos.Remove(existing);
                }
            });
        }

        internal void RemoveMapInfo(string portalItemId)
        {
            if (string.IsNullOrWhiteSpace(portalItemId))
            {
                throw new ArgumentException("Portal item ID cannot be null or whitespace.", nameof(portalItemId));
            }

            RunOnCapturedContext(() =>
            {
                var existing = _offlineMapInfos.FirstOrDefault(info => string.Equals(info.Id, portalItemId, StringComparison.Ordinal));
                if (existing is not null)
                {
                    _offlineMapInfos.Remove(existing);
                }
            });

            OfflineMapInfo.RemoveFromDirectory(GetOfflineManagerDirectory(), portalItemId);
        }

        private void ApplyConfiguration(OfflineManagerConfiguration configuration)
        {
#if __IOS__
            _jobManager.PreferredBackgroundStatusCheckSchedule =
                configuration.PreferredBackgroundStatusCheckSchedule.IsDisabled
                    ? BackgroundStatusCheckSchedule.Disabled
                    : BackgroundStatusCheckSchedule.RegularInterval(configuration.PreferredBackgroundStatusCheckSchedule.Interval);
#endif
        }

        private void ObserveJob(IJob job)
        {
            lock (_observedJobsLock)
            {
                if (!_observedJobs.Add(job))
                {
                    return;
                }
            }

            _ = ObserveJobCoreAsync(job);
        }

        private async Task ObserveJobCoreAsync(IJob job)
        {
            try
            {
                await job.GetResultAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"ArcGIS Toolkit - Offline job completed with an error: {ex}");
            }
            finally
            {
                lock (_observedJobsLock)
                {
                    _observedJobs.Remove(job);
                }

                if (_jobManager.Jobs.Contains(job))
                {
                    _jobManager.Jobs.Remove(job);
                    _jobManager.SaveState();
                }

                var portalItem = GetOnlineMapPortalItem(job);
                if (!string.IsNullOrWhiteSpace(portalItem?.ItemId))
                {
                    HandlePendingMapInfo(job.Status == JobStatus.Succeeded, portalItem!.ItemId!);
                }

                RunOnCapturedContext(() => JobCompleted?.Invoke(this, new JobCompletedEventArgs(job)));
            }
        }

        private PortalItem? GetOnlineMapPortalItem(IJob job)
        {
            return job switch
            {
                DownloadPreplannedOfflineMapJob downloadPreplanned => downloadPreplanned.OnlineMap?.Item as PortalItem,
                GenerateOfflineMapJob generateOfflineMapJob => generateOfflineMapJob.OnlineMap?.Item as PortalItem,
                _ => null,
            };
        }

        private void LoadOfflineMapInfos()
        {
            var offlineManagerDirectory = GetOfflineManagerDirectory();
            if (!Directory.Exists(offlineManagerDirectory))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(offlineManagerDirectory, "*.json"))
            {
                var info = OfflineMapInfo.FromFile(file);
                if (info is not null)
                {
                    _offlineMapInfos.Add(info);
                }
            }
        }

        private async Task SavePendingMapInfoAsync(PortalItem portalItem)
        {
            if (string.IsNullOrWhiteSpace(portalItem.ItemId))
            {
                return;
            }

            var pendingFile = GetPendingMapInfoFileName(portalItem.ItemId);
            if (File.Exists(pendingFile))
            {
                return;
            }

            var info = await OfflineMapInfo.CreateAsync(portalItem).ConfigureAwait(false);
            if (info is null)
            {
                return;
            }
            var pendingDirectory = GetPendingDirectory();
            Directory.CreateDirectory(pendingDirectory);
            await info.SaveToDirectoryAsync(pendingDirectory).ConfigureAwait(false);
        }

        private void HandlePendingMapInfo(bool succeeded, string portalItemId)
        {
            if (!succeeded || _offlineMapInfos.Any(info => string.Equals(info.Id, portalItemId, StringComparison.Ordinal)))
            {
                return;
            }

            var pendingFilename = GetPendingMapInfoFileName(portalItemId);
            if (!File.Exists(pendingFilename))
            {
                return;
            }

            var destination = GetPortalItemFilename(portalItemId);
            if (!File.Exists(destination))
            {
                File.Move(pendingFilename, destination);
            }
            
            var info = OfflineMapInfo.FromFile(destination);
            if (info is not null)
            {
                RunOnCapturedContext(() =>
                {
                    if (!_offlineMapInfos.Any(existing => string.Equals(existing.Id, info.Id, StringComparison.Ordinal)))
                    {
                        _offlineMapInfos.Add(info);
                    }
                });
            }
        }

        private void RunOnCapturedContext(Action action)
        {
            if (_synchronizationContext is null || SynchronizationContext.Current == _synchronizationContext)
            {
                action();
                return;
            }

            _synchronizationContext.Send(static state => ((Action)state!).Invoke(), action);
        }

        internal static string GetOfflineManagerDirectory()
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(root, "Esri", "ArcGISToolkit", "OfflineManager"); //TODO: Unpackaged apps needs to make this more unique
        }

        internal static string GetPendingMapInfoFileName(string portalItemId)
        {
            return Path.Combine(GetPendingDirectory(), portalItemId + ".json");
        }


        internal static string GetPendingDirectory()
        {
            return Path.Combine(GetOfflineManagerDirectory(), PendingFolderName);
        }

        internal static string GetPortalItemFilename(string portalItemId)
        {
            return Path.Combine(GetOfflineManagerDirectory(), portalItemId + ".json");
        }

        private const string PendingFolderName = ".pending";

        private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
            where T : class
        {
            public static ReferenceEqualityComparer<T> Instance { get; } = new ReferenceEqualityComparer<T>();

            public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

            public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }

    /// <summary>
    /// Event arguments for a completed offline job.
    /// </summary>
    public sealed class JobCompletedEventArgs : EventArgs
    {
        internal JobCompletedEventArgs(IJob job)
        {
            Job = job;
        }

        /// <summary>
        /// Gets the completed job.
        /// </summary>
        public IJob Job { get; }
    }

    /// <summary>
    /// The configuration properties for the <see cref="OfflineManager"/>.
    /// </summary>
    public sealed class OfflineManagerConfiguration
    {
        /// <summary>
        /// Gets or sets the preferred schedule for performing status checks while the application is in the background.
        /// </summary>
        public OfflineManagerBackgroundStatusCheckSchedule PreferredBackgroundStatusCheckSchedule { get; set; } = OfflineManagerBackgroundStatusCheckSchedule.Disabled;

        /// <summary>
        /// Gets or sets the update mode of any new on-demand map areas taken offline.
        /// </summary>
        public GenerateOfflineMapUpdateMode OnDemandUpdateMode { get; set; } = GenerateOfflineMapUpdateMode.NoUpdates;

        /// <summary>
        /// Gets or sets the update mode of any new preplanned map areas taken offline.
        /// </summary>
        public PreplannedUpdateMode PreplannedUpdateMode { get; set; } = PreplannedUpdateMode.NoUpdates;
    }

    /// <summary>
    /// Defines a schedule for background status checks for the <see cref="OfflineManager"/>.
    /// </summary>
    public sealed class OfflineManagerBackgroundStatusCheckSchedule
    {
        private OfflineManagerBackgroundStatusCheckSchedule()
        {
        }

        /// <summary>
        /// Gets a schedule that disables background status checks.
        /// </summary>
        public static OfflineManagerBackgroundStatusCheckSchedule Disabled { get; } = new OfflineManagerBackgroundStatusCheckSchedule();

        /// <summary>
        /// Creates a schedule that requests background checks at a regular interval.
        /// </summary>
        /// <param name="interval">The number of seconds between status checks.</param>
        /// <returns>A configured background status check schedule.</returns>
        public static OfflineManagerBackgroundStatusCheckSchedule RegularInterval(double interval)
        {
            if (interval <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero.");
            }

            return new OfflineManagerBackgroundStatusCheckSchedule
            {
                Interval = interval,
            };
        }

        internal double Interval { get; private set; }

        internal bool IsDisabled => Interval <= 0;
    }
}
