using System.Reflection;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.Toolkit;

namespace Toolkit.Tests
{
    [TestClass]
    [DoNotParallelize]
    public sealed partial class JobManagerTests
    {
        [TestMethod]
        public void CreateThrowsForNullOrWhitespaceIds()
        {
            Assert.ThrowsExactly<ArgumentException>(() => JobManager.Create(null!));
            Assert.ThrowsExactly<ArgumentException>(() => JobManager.Create(string.Empty));
            Assert.ThrowsExactly<ArgumentException>(() => JobManager.Create(" \t "));
        }

        [TestMethod]
        public void AddingAndRemovingJobsPersistsCurrentCollection()
        {
            var manager = CreateTestManager();
            var filePath = GetStateFilename(manager);
            var firstJob = new FakeJob("job-1");
            var secondJob = new FakeJob("job-2");

            try
            {
                manager.Jobs.Add(firstJob);
                Assert.AreEqual("job-1", File.ReadAllText(filePath));

                manager.Jobs.Add(secondJob);
                Assert.AreEqual("job-1|\n|job-2", File.ReadAllText(filePath));

                manager.Jobs.Remove(firstJob);
                Assert.AreEqual("job-2", File.ReadAllText(filePath));

                manager.Jobs.Remove(secondJob);
                Assert.IsFalse(File.Exists(filePath));
            }
            finally
            {
                CleanupManager(manager);
            }
        }

        [TestMethod]
        public void StatusAndProgressChangesPersistManagedJobsOnly()
        {
            var manager = CreateTestManager();
            var filePath = GetStateFilename(manager);
            var job = new FakeJob("job-1");

            try
            {
                manager.Jobs.Add(job);

                job.SetProgress(25);
                Assert.AreEqual("job-1:NotStarted:25", File.ReadAllText(filePath));

                job.SetStatus(JobStatus.Started);
                Assert.AreEqual("job-1:Started:25", File.ReadAllText(filePath));

                manager.Jobs.Remove(job);
                Assert.IsFalse(File.Exists(filePath));

                job.SetProgress(75);
                job.SetStatus(JobStatus.Succeeded);
                Assert.IsFalse(File.Exists(filePath));
            }
            finally
            {
                CleanupManager(manager);
            }
        }

        [TestMethod]
        public async Task PerformStatusChecksInvokesEveryJobAndSuppressesFailures()
        {
            var manager = CreateTestManager();
            var successfulJob = new FakeJob("job-1");
            var failingJob = new FakeJob("job-2")
            {
                CheckStatusAsyncImpl = () => Task.FromException<bool>(new InvalidOperationException("boom")),
            };

            try
            {
                manager.Jobs.Add(successfulJob);
                manager.Jobs.Add(failingJob);

                await manager.PerformStatusChecks();

                Assert.AreEqual(1, successfulJob.CheckStatusCallCount);
                Assert.AreEqual(1, failingJob.CheckStatusCallCount);
            }
            finally
            {
                CleanupManager(manager);
            }
        }

        [TestMethod]
        public async Task ResumeAllPausedJobsChecksStatusThenStartsOnlyPausedJobs()
        {
            var manager = CreateTestManager();
            var pausedJob = new FakeJob("paused", JobStatus.Paused);
            var startedJob = new FakeJob("started", JobStatus.Started);
            var notStartedJob = new FakeJob("not-started", JobStatus.NotStarted);

            try
            {
                manager.Jobs.Add(pausedJob);
                manager.Jobs.Add(startedJob);
                manager.Jobs.Add(notStartedJob);

                await manager.ResumeAllPausedJobsAsync();

                Assert.AreEqual(1, pausedJob.CheckStatusCallCount);
                Assert.AreEqual(1, startedJob.CheckStatusCallCount);
                Assert.AreEqual(1, notStartedJob.CheckStatusCallCount);
                Assert.AreEqual(1, pausedJob.StartCallCount);
                Assert.AreEqual(0, startedJob.StartCallCount);
                Assert.AreEqual(0, notStartedJob.StartCallCount);
                Assert.AreEqual(JobStatus.Started, pausedJob.Status);
            }
            finally
            {
                CleanupManager(manager);
            }
        }

        [TestMethod]
        public void CreateIgnoresMalformedPersistedJobs()
        {
            var managerId = CreateManagerId();
            var filePath = GetStateFilename(JobManager.Create(managerId));

            try
            {
                File.WriteAllText(filePath, "not-json|\n|still-not-json");

                var manager = JobManager.Create(managerId);
                try
                {
                    Assert.IsEmpty(manager.Jobs);
                }
                finally
                {
                    CleanupManager(manager);
                }
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        private static JobManager CreateTestManager() => JobManager.Create(CreateManagerId());

        private static string CreateManagerId() => $"job-manager-tests-{Guid.NewGuid():N}";

        private static string GetStateFilename(JobManager manager)
        {
            var method = typeof(JobManager).GetMethod("GetStateFilename", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (string)method.Invoke(manager, null)!;
        }

        private static void CleanupManager(JobManager manager)
        {
            var filePath = GetStateFilename(manager);
            manager.Jobs.Clear();
            manager.SaveState();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private sealed class FakeJob : IJob
        {
            public FakeJob(string jsonPrefix, JobStatus status = JobStatus.NotStarted)
            {
                JsonPrefix = jsonPrefix;
                Status = status;
            }

            public event EventHandler<JobMessage>? MessageAdded;

            public event EventHandler<EventArgs>? ProgressChanged;

            public event EventHandler<JobStatus>? StatusChanged;

            public Func<Task<bool>>? CancelAsyncImpl { get; set; }

            public Func<Task<bool>>? CheckStatusAsyncImpl { get; set; }

            public Func<Task<object>>? GetResultAsyncImpl { get; set; }

            public Exception? Error { get; private set; }

            public IReadOnlyList<JobMessage> Messages { get; } = Array.Empty<JobMessage>();

            public int Progress { get; private set; }

            public string ServerJobId { get; set; } = string.Empty;

            public JobStatus Status { get; private set; }

            public int CancelCallCount { get; private set; }

            public int CheckStatusCallCount { get; private set; }

            public string JsonPrefix { get; }

            public int StartCallCount { get; private set; }

            public Task<bool> CancelAsync()
            {
                CancelCallCount++;
                return CancelAsyncImpl?.Invoke() ?? Task.FromResult(true);
            }

            public Task<bool> CheckStatusAsync()
            {
                CheckStatusCallCount++;
                return CheckStatusAsyncImpl?.Invoke() ?? Task.FromResult(true);
            }

            public Task<object> GetResultAsync() => GetResultAsyncImpl?.Invoke() ?? Task.FromResult<object>(new object());

            public bool Pause()
            {
                SetStatus(JobStatus.Paused);
                return true;
            }

            public void RaiseMessage(JobMessage message) => MessageAdded?.Invoke(this, message);

            public bool Start()
            {
                StartCallCount++;
                SetStatus(JobStatus.Started);
                return true;
            }

            public void SetError(Exception error) => Error = error;

            public void SetProgress(int progress)
            {
                Progress = progress;
                ProgressChanged?.Invoke(this, EventArgs.Empty);
            }

            public void SetStatus(JobStatus status)
            {
                Status = status;
                StatusChanged?.Invoke(this, status);
            }

            public string ToJson()
            {
                if (Progress == 0 && Status == JobStatus.NotStarted)
                {
                    return JsonPrefix;
                }

                return $"{JsonPrefix}:{Status}:{Progress}";
            }
        }
    }
}
