using System.Diagnostics;
using System.Reflection;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.Toolkit;

namespace Toolkit.Tests
{
    public sealed partial class JobManagerTests
    {
        private static readonly Uri GenerateGeodatabaseServiceUri = new("https://sampleserver6.arcgisonline.com/arcgis/rest/services/Sync/WildfireSync/FeatureServer");
        private static readonly Uri OfflineMapPortalItemUri = new("https://www.arcgis.com/home/item.html?id=acc027394bc84c2fb04d1ed317aac674");
        private static readonly Envelope NapervilleExtent = new(-9813416.487598, 5126112.596989, -9812775.435463, 5127101.526749, SpatialReferences.WebMercator);

        [TestMethod]
        [Timeout(900000, CooperativeCancellation = true)]
        public async Task GenerateGeodatabaseJobCanPausePersistResumeAndReportProgress()
        {
            var workingDirectory = CreateWorkingDirectory();
            var geodatabasePath = Path.Combine(workingDirectory, "wildfire.geodatabase");
            Geodatabase? geodatabase = null;

            try
            {
                await ExerciseLiveJobPauseResumeAsync(
                    createJobAsync: () => CreateGenerateGeodatabaseJobAsync(geodatabasePath),
                    assertResultAsync: async job =>
                    {
                        geodatabase = await job.GetResultAsync();
                        Assert.AreEqual(geodatabasePath, geodatabase.Path);
                        Assert.IsTrue(File.Exists(geodatabasePath));
                    });
            }
            finally
            {
                geodatabase?.Close();
                DeleteWorkingDirectory(workingDirectory);
            }
        }

        [TestMethod]
        [Timeout(900000, CooperativeCancellation = true)]
        public async Task GenerateOfflineMapJobCanPausePersistResumeAndReportProgress()
        {
            var workingDirectory = CreateWorkingDirectory();
            var downloadDirectory = Path.Combine(workingDirectory, "offline-map");
            MobileMapPackage? package = null;

            try
            {
                await ExerciseLiveJobPauseResumeAsync(
                    createJobAsync: () => CreateGenerateOfflineMapJobAsync(downloadDirectory),
                    assertResultAsync: async job =>
                    {
                        var result = await job.GetResultAsync();
                        package = result.MobileMapPackage;
                        Assert.IsNotNull(result.OfflineMap);
                        Assert.IsNotNull(result.MobileMapPackage);
                        Assert.AreEqual(downloadDirectory, job.DownloadDirectoryPath);
                        Assert.IsTrue(Directory.Exists(downloadDirectory));
                    });
            }
            finally
            {
                package?.Close();
                DeleteWorkingDirectory(workingDirectory);
            }
        }

        private static string CreateWorkingDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), CreateManagerId());
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteWorkingDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        private static async Task<GenerateGeodatabaseJob> CreateGenerateGeodatabaseJobAsync(string geodatabasePath)
        {
            var task = await GeodatabaseSyncTask.CreateAsync(GenerateGeodatabaseServiceUri);
            var serviceInfo = task.ServiceInfo ?? throw new AssertFailedException("The geodatabase sync task did not expose service information.");
            var extent = serviceInfo.FullExtent ?? throw new AssertFailedException("The geodatabase sync service did not expose a full extent.");
            var parameters = new GenerateGeodatabaseParameters
            {
                Extent = extent,
                OutSpatialReference = extent.SpatialReference,
                SyncModel = SyncModel.Layer,
            };

            foreach (var info in serviceInfo.LayerInfos)
            {
                parameters.LayerOptions.Add(new GenerateLayerOption(info.Id));
            }

            return task.GenerateGeodatabase(parameters, geodatabasePath);
        }

        private static async Task<GenerateOfflineMapJob> CreateGenerateOfflineMapJobAsync(string downloadDirectory)
        {
            var portalItem = await Esri.ArcGISRuntime.Portal.PortalItem.CreateAsync(OfflineMapPortalItemUri);
            var map = new Map(portalItem);
            var task = await OfflineMapTask.CreateAsync(map);
            var parameters = await task.CreateDefaultGenerateOfflineMapParametersAsync(NapervilleExtent);
            return task.GenerateOfflineMap(parameters, downloadDirectory);
        }

        private static async Task ExerciseLiveJobPauseResumeAsync<TJob>(
            Func<Task<TJob>> createJobAsync,
            Func<TJob, Task> assertResultAsync)
            where TJob : class, IJob
        {
            var managerId = CreateManagerId();
            var manager = JobManager.Create(managerId);
            JobManager? resumedManager = null;
            TJob? originalJob = null;
            TJob? resumedJob = null;
            var initialHistory = new JobHistory();
            var resumedHistory = new JobHistory();

            try
            {
                originalJob = await createJobAsync();
                using var initialSubscription = initialHistory.Attach(originalJob);

                manager.Jobs.Add(originalJob);
                Assert.IsTrue(originalJob.Start(), "The live job did not start.");

                await WaitForProgressAsync(originalJob, minimumProgress: 5, timeout: TimeSpan.FromMinutes(5));
                var pausedProgress = originalJob.Progress;
                Assert.IsGreaterThan(0, pausedProgress, "The live job never reported a positive progress percentage before pausing.");

                await PauseJobAsync(originalJob, TimeSpan.FromMinutes(2));
                Assert.AreEqual(JobStatus.Paused, originalJob.Status);

                manager.SaveState();

                resumedManager = JobManager.Create(managerId);
                Assert.HasCount(1, resumedManager.Jobs);
                resumedJob = resumedManager.Jobs.Single() as TJob
                    ?? throw new AssertFailedException($"Expected a rehydrated job of type {typeof(TJob).Name}.");

                using var resumedSubscription = resumedHistory.Attach(resumedJob);

                Assert.AreEqual(typeof(TJob), resumedJob.GetType());
                Assert.AreEqual(JobStatus.Paused, resumedJob.Status);

                await resumedManager.ResumeAllPausedJobsAsync();
                await WaitForCompletionAsync(resumedJob, TimeSpan.FromMinutes(10));

                Assert.AreEqual(JobStatus.Succeeded, resumedJob.Status);
                Assert.AreEqual(100, resumedJob.Progress);
                Assert.IsTrue(initialHistory.Statuses.Contains(JobStatus.Started) || resumedHistory.Statuses.Contains(JobStatus.Started));
                Assert.IsTrue(initialHistory.Statuses.Contains(JobStatus.Paused) || resumedHistory.Statuses.Contains(JobStatus.Paused));
                Assert.IsTrue(initialHistory.ProgressValues.Concat(resumedHistory.ProgressValues).Any(progress => progress > 0));
                Assert.IsTrue(resumedHistory.ProgressValues.Any(progress => progress > pausedProgress) || resumedJob.Progress > pausedProgress);

                await assertResultAsync(resumedJob);
            }
            finally
            {
                if (resumedJob is not null && resumedJob.Status is not JobStatus.Succeeded and not JobStatus.Failed)
                {
                    await resumedJob.CancelAsync();
                }

                if (originalJob is not null && originalJob.Status is not JobStatus.Succeeded and not JobStatus.Failed and not JobStatus.Paused)
                {
                    await originalJob.CancelAsync();
                }

                if (resumedManager is not null)
                {
                    CleanupManager(resumedManager);
                }

                CleanupManager(manager);
            }
        }

        private static async Task WaitForProgressAsync(IJob job, int minimumProgress, TimeSpan timeout)
        {
            await WaitForConditionAsync(
                () => job.Progress >= minimumProgress || IsTerminal(job.Status),
                timeout,
                () => $"The job never reached {minimumProgress}% progress. Current state: {DescribeJob(job)}");

            if (job.Status != JobStatus.Succeeded && job.Progress < minimumProgress)
            {
                Assert.Fail($"The job completed before reaching the expected progress threshold. Current state: {DescribeJob(job)}");
            }
        }

        private static async Task PauseJobAsync(IJob job, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                if (job.Status == JobStatus.Paused)
                {
                    return;
                }

                if (IsTerminal(job.Status))
                {
                    Assert.Fail($"The job completed before it could be paused. Current state: {DescribeJob(job)}");
                }

                _ = job.Pause();
                if (job.Status == JobStatus.Paused)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }

            Assert.Fail($"The job did not enter the paused state within {timeout}. Current state: {DescribeJob(job)}");
        }

        private static async Task WaitForCompletionAsync(IJob job, TimeSpan timeout)
        {
            await WaitForConditionAsync(
                () => IsTerminal(job.Status),
                timeout,
                () => $"The job did not complete within {timeout}. Current state: {DescribeJob(job)}");

            if (job.Status != JobStatus.Succeeded)
            {
                Assert.Fail($"The job did not succeed. Current state: {DescribeJob(job)}");
            }
        }

        private static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout, Func<string> failureMessage)
        {
            var stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                if (stopwatch.Elapsed >= timeout)
                {
                    Assert.Fail(failureMessage());
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }

        private static bool IsTerminal(JobStatus status) => status is JobStatus.Succeeded or JobStatus.Failed;

        private static string DescribeJob(IJob job)
        {
            var latestMessage = job.Messages.LastOrDefault()?.Message ?? "<none>";
            return $"Status={job.Status}, Progress={job.Progress}, ServerJobId={job.ServerJobId}, Error={job.Error?.Message ?? "<none>"}, LatestMessage={latestMessage}";
        }



        private sealed class JobHistory
        {
            public List<int> ProgressValues { get; } = new();

            public List<JobStatus> Statuses { get; } = new();

            public IDisposable Attach(IJob job)
            {
                Statuses.Add(job.Status);
                ProgressValues.Add(job.Progress);

                void StatusHandler(object? _, JobStatus status) => Statuses.Add(status);
                void ProgressHandler(object? sender, EventArgs args) => ProgressValues.Add(job.Progress);

                job.StatusChanged += StatusHandler;
                job.ProgressChanged += ProgressHandler;

                return new DelegateDisposer(() =>
                {
                    job.StatusChanged -= StatusHandler;
                    job.ProgressChanged -= ProgressHandler;
                });
            }
        }

        private sealed class DelegateDisposer : IDisposable
        {
            private readonly Action _dispose;
            private bool _disposed;

            public DelegateDisposer(Action dispose) => _dispose = dispose;

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _dispose();
                _disposed = true;
            }
        }
    }
}
