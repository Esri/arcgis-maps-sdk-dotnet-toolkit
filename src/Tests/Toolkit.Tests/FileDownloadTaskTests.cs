using System.Net;
using System.Net.Http.Headers;
using Esri.ArcGISRuntime.Toolkit;

namespace Toolkit.Tests;

[TestClass]
[DoNotParallelize]
public sealed class FileDownloadTaskTests
{
    private uint _originalMaxConcurrentDownloads;

    [TestInitialize]
    public void Initialize()
    {
        _originalMaxConcurrentDownloads = FileDownloadTask.MaxConcurrentDownloads;
        FileDownloadTask.ClearQueue();
        FileDownloadTask.MaxConcurrentDownloads = 0;
    }

    [TestCleanup]
    public void Cleanup()
    {
        FileDownloadTask.ClearQueue();
        FileDownloadTask.MaxConcurrentDownloads = _originalMaxConcurrentDownloads;
    }

    [TestMethod]
    [Timeout(30000, CooperativeCancellation = true)]
    public async Task PauseAndResumeResumableDownloadUsesRangeRequestAndCompletes()
    {
        await using var context = new TestDownloadContext();
        var content = CreatePayload(512);
        var resource = context.AddResource("resumable.bin", content, supportsRanges: true);
        var destinationPath = context.GetDestinationPath("resumable.bin");
        var downloadTask = await FileDownloadTask.BeginDownloadAsync(destinationPath, resource.Uri, context.Handler);
        context.Track(downloadTask);

        await WaitForDownloadedBytesAsync(downloadTask, minimumBytes: 96, TimeSpan.FromSeconds(10));

        await downloadTask.PauseAsync();
        var pausedBytes = downloadTask.BytesDownloaded;

        Assert.AreEqual(FileDownloadStatus.Paused, downloadTask.Status);
        Assert.IsTrue(downloadTask.IsResumable);
        Assert.IsGreaterThanOrEqualTo(96L, pausedBytes);
        Assert.IsTrue(File.Exists(downloadTask.TempFile));

        await downloadTask.ResumeAsync();
        await WaitForCompletionAsync(downloadTask, TimeSpan.FromSeconds(10));

        Assert.AreEqual(FileDownloadStatus.Completed, downloadTask.Status);
        Assert.AreEqual((long)content.Length, downloadTask.BytesDownloaded);
        Assert.AreEqual((long)content.Length, downloadTask.TotalLength);
        CollectionAssert.AreEqual(new long[] { 0L, pausedBytes }, resource.RequestOffsets.ToArray());
        CollectionAssert.AreEqual(new string?[] { null, resource.ETag }, resource.IfRangeValues.ToArray());
        CollectionAssert.AreEqual(content, File.ReadAllBytes(destinationPath));
    }

    [TestMethod]
    [Timeout(30000, CooperativeCancellation = true)]
    public async Task ResumeFromJsonRestoresPartialDownloadAndCompletes()
    {
        await using var context = new TestDownloadContext();
        var content = CreatePayload(640);
        var resource = context.AddResource("resume-from-json.bin", content, supportsRanges: true);
        var destinationPath = context.GetDestinationPath("resume-from-json.bin");
        var originalTask = await FileDownloadTask.BeginDownloadAsync(destinationPath, resource.Uri, context.Handler);
        context.Track(originalTask);

        await WaitForDownloadedBytesAsync(originalTask, minimumBytes: 128, TimeSpan.FromSeconds(10));
        await originalTask.PauseAsync();

        var pausedBytes = originalTask.BytesDownloaded;
        var tempFile = originalTask.TempFile;
        var serializedTask = originalTask.ToJson();
        originalTask.Dispose();

        var resumedTask = FileDownloadTask.FromJson(serializedTask, context.Handler);
        context.Track(resumedTask);

        Assert.AreEqual(FileDownloadStatus.Paused, resumedTask.Status);
        Assert.AreEqual(tempFile, resumedTask.TempFile);
        Assert.AreEqual(pausedBytes, resumedTask.BytesDownloaded);
        Assert.IsTrue(resumedTask.IsResumable);

        await resumedTask.ResumeAsync();
        await WaitForCompletionAsync(resumedTask, TimeSpan.FromSeconds(10));

        CollectionAssert.AreEqual(new long[] { 0L, pausedBytes }, resource.RequestOffsets.ToArray());
        CollectionAssert.AreEqual(new string?[] { null, resource.ETag }, resource.IfRangeValues.ToArray());
        CollectionAssert.AreEqual(content, File.ReadAllBytes(destinationPath));
    }

    [TestMethod]
    [Timeout(30000, CooperativeCancellation = true)]
    public async Task QueuedDownloadStartsWhenRunningDownloadPauses()
    {
        await using var context = new TestDownloadContext();
        FileDownloadTask.MaxConcurrentDownloads = 1;

        var firstContent = CreatePayload(768);
        var secondContent = CreatePayload(256);
        var firstResource = context.AddResource("first.bin", firstContent, supportsRanges: true, chunkDelay: TimeSpan.FromMilliseconds(15));
        var secondResource = context.AddResource("second.bin", secondContent, supportsRanges: true, chunkDelay: TimeSpan.FromMilliseconds(5));

        var firstTask = await FileDownloadTask.BeginDownloadAsync(context.GetDestinationPath("first.bin"), firstResource.Uri, context.Handler);
        var secondTask = await FileDownloadTask.BeginDownloadAsync(context.GetDestinationPath("second.bin"), secondResource.Uri, context.Handler);
        context.Track(firstTask);
        context.Track(secondTask);

        await WaitForDownloadedBytesAsync(firstTask, minimumBytes: 96, TimeSpan.FromSeconds(10));

        Assert.AreEqual(FileDownloadStatus.Queued, secondTask.Status);
        Assert.IsEmpty(secondResource.RequestOffsets);

        await firstTask.PauseAsync();

        await WaitForConditionAsync(
            () => secondTask.Status is FileDownloadStatus.Downloading or FileDownloadStatus.Completed,
            TimeSpan.FromSeconds(10),
            () => $"The queued download never started. Current state: {secondTask.Status}");
        await WaitForCompletionAsync(secondTask, TimeSpan.FromSeconds(10));

        CollectionAssert.AreEqual(new long[] { 0L }, secondResource.RequestOffsets.ToArray());
        CollectionAssert.AreEqual(secondContent, File.ReadAllBytes(context.GetDestinationPath("second.bin")));
    }

    [TestMethod]
    [Timeout(30000, CooperativeCancellation = true)]
    public async Task PauseAndResumeNonResumableDownloadRestartsFromBeginning()
    {
        await using var context = new TestDownloadContext();
        var content = CreatePayload(384);
        var resource = context.AddResource("non-resumable.bin", content, supportsRanges: false);
        var destinationPath = context.GetDestinationPath("non-resumable.bin");
        var downloadTask = await FileDownloadTask.BeginDownloadAsync(destinationPath, resource.Uri, context.Handler);
        context.Track(downloadTask);

        await WaitForDownloadedBytesAsync(downloadTask, minimumBytes: 96, TimeSpan.FromSeconds(10));
        await downloadTask.PauseAsync();
        var pausedBytes = downloadTask.BytesDownloaded;

        Assert.AreEqual(FileDownloadStatus.Paused, downloadTask.Status);
        Assert.IsFalse(downloadTask.IsResumable);
        Assert.IsGreaterThanOrEqualTo(96L, pausedBytes);

        await downloadTask.ResumeAsync();
        await WaitForCompletionAsync(downloadTask, TimeSpan.FromSeconds(10));

        CollectionAssert.AreEqual(new long[] { 0L, 0L }, resource.RequestOffsets.ToArray());
        CollectionAssert.AreEqual(new string?[] { null, null }, resource.IfRangeValues.ToArray());
        CollectionAssert.AreEqual(content, File.ReadAllBytes(destinationPath));
    }

    [TestMethod]
    [Timeout(30000, CooperativeCancellation = true)]
    public async Task CancelDeletesTemporaryFileAndMarksTaskCancelled()
    {
        await using var context = new TestDownloadContext();
        var content = CreatePayload(512);
        var resource = context.AddResource("cancel.bin", content, supportsRanges: true);
        var destinationPath = context.GetDestinationPath("cancel.bin");
        var downloadTask = await FileDownloadTask.BeginDownloadAsync(destinationPath, resource.Uri, context.Handler);
        context.Track(downloadTask);

        await WaitForDownloadedBytesAsync(downloadTask, minimumBytes: 96, TimeSpan.FromSeconds(10));
        var tempFile = downloadTask.TempFile;

        await downloadTask.CancelAsync();

        Assert.AreEqual(FileDownloadStatus.Cancelled, downloadTask.Status);
        Assert.AreEqual(0, downloadTask.BytesDownloaded);
        Assert.IsFalse(File.Exists(tempFile));
        Assert.IsFalse(File.Exists(destinationPath));
        CollectionAssert.AreEqual(new long[] { 0L }, resource.RequestOffsets.ToArray());
    }

    private static byte[] CreatePayload(int length)
    {
        var payload = new byte[length];
        for (var i = 0; i < payload.Length; i++)
        {
            payload[i] = (byte)(i % 251);
        }

        return payload;
    }

    private static async Task WaitForDownloadedBytesAsync(FileDownloadTask task, long minimumBytes, TimeSpan timeout)
    {
        await WaitForConditionAsync(
            () => task.BytesDownloaded >= minimumBytes || task.Status is FileDownloadStatus.Completed or FileDownloadStatus.Error or FileDownloadStatus.Cancelled,
            timeout,
            () => $"The download never reached {minimumBytes} bytes. Current state: {DescribeTask(task)}");

        if (task.Status != FileDownloadStatus.Completed && task.BytesDownloaded < minimumBytes)
        {
            Assert.Fail($"The download stopped before reaching {minimumBytes} bytes. Current state: {DescribeTask(task)}");
        }
    }

    private static async Task WaitForCompletionAsync(FileDownloadTask task, TimeSpan timeout)
    {
        await WaitForConditionAsync(
            () => task.Status is FileDownloadStatus.Completed or FileDownloadStatus.Error or FileDownloadStatus.Cancelled,
            timeout,
            () => $"The download did not finish within {timeout}. Current state: {DescribeTask(task)}");

        if (task.Status != FileDownloadStatus.Completed)
        {
            Assert.Fail($"The download did not complete successfully. Current state: {DescribeTask(task)}");
        }
    }

    private static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout, Func<string> failureMessage)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - start >= timeout)
            {
                Assert.Fail(failureMessage());
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20));
        }
    }

    private static string DescribeTask(FileDownloadTask task)
        => $"Status={task.Status}, BytesDownloaded={task.BytesDownloaded}, TotalLength={task.TotalLength?.ToString() ?? "<unknown>"}, Exception={task.Exception?.Message ?? "<none>"}";

    private sealed class TestDownloadContext : IAsyncDisposable
    {
        private readonly List<FileDownloadTask> _trackedTasks = new();

        public TestDownloadContext()
        {
            WorkingDirectory = Path.Combine(Path.GetTempPath(), $"file-download-task-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(WorkingDirectory);
            Handler = new TestHttpMessageHandler();
        }

        public TestHttpMessageHandler Handler { get; }

        public string WorkingDirectory { get; }

        public TestDownloadResource AddResource(string filename, byte[] content, bool supportsRanges, TimeSpan? chunkDelay = null)
            => Handler.AddResource(filename, content, supportsRanges, chunkDelay ?? TimeSpan.FromMilliseconds(10));

        public string GetDestinationPath(string filename) => Path.Combine(WorkingDirectory, filename);

        public void Track(FileDownloadTask task)
        {
            if (!_trackedTasks.Contains(task))
            {
                _trackedTasks.Add(task);
            }
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var task in _trackedTasks)
            {
                try
                {
                    if (task.Status != FileDownloadStatus.Completed && task.Status != FileDownloadStatus.Cancelled)
                    {
                        await task.CancelAsync();
                    }
                }
                catch
                {
                }
                finally
                {
                    task.Dispose();
                }
            }

            if (Directory.Exists(WorkingDirectory))
            {
                Directory.Delete(WorkingDirectory, recursive: true);
            }
        }
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<Uri, TestDownloadResource> _resources = new();

        public TestDownloadResource AddResource(string filename, byte[] content, bool supportsRanges, TimeSpan chunkDelay)
        {
            var uri = new Uri($"https://example.test/{Guid.NewGuid():N}/{filename}");
            var resource = new TestDownloadResource(uri, content, supportsRanges, chunkDelay);
            _resources.Add(uri, resource);
            return resource;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is null || !_resources.TryGetValue(request.RequestUri, out var resource))
            {
                throw new InvalidOperationException($"No test resource was registered for {request.RequestUri}.");
            }

            var offset = request.Headers.Range?.Ranges.Single().From ?? 0;
            var actualOffset = resource.SupportsRanges ? offset : 0;
            var ifRange = request.Headers.IfRange?.EntityTag?.Tag ?? request.Headers.IfRange?.Date?.ToString("R");
            resource.RecordRequest(actualOffset, ifRange);

            var response = new HttpResponseMessage(actualOffset > 0 ? HttpStatusCode.PartialContent : HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StreamContent(new ThrottledContentStream(resource.Content, actualOffset, resource.ChunkDelay)),
            };

            response.Content.Headers.ContentLength = resource.Content.Length - actualOffset;
            if (resource.SupportsRanges)
            {
                response.Headers.AcceptRanges.Add("bytes");
                response.Headers.ETag = new EntityTagHeaderValue(resource.ETag);
                response.Headers.Date = resource.Date;

                if (actualOffset > 0)
                {
                    response.Content.Headers.ContentRange = new ContentRangeHeaderValue(actualOffset, resource.Content.Length - 1, resource.Content.Length);
                }
            }

            return Task.FromResult(response);
        }
    }

    private sealed class TestDownloadResource
    {
        private readonly object _lock = new();

        public TestDownloadResource(Uri uri, byte[] content, bool supportsRanges, TimeSpan chunkDelay)
        {
            Uri = uri;
            Content = content;
            SupportsRanges = supportsRanges;
            ChunkDelay = chunkDelay;
            ETag = $"\"{Guid.NewGuid():N}\"";
            Date = DateTimeOffset.UtcNow;
        }

        public byte[] Content { get; }

        public TimeSpan ChunkDelay { get; }

        public DateTimeOffset Date { get; }

        public string ETag { get; }

        public List<string?> IfRangeValues { get; } = new();

        public List<long> RequestOffsets { get; } = new();

        public bool SupportsRanges { get; }

        public Uri Uri { get; }

        public void RecordRequest(long offset, string? ifRange)
        {
            lock (_lock)
            {
                RequestOffsets.Add(offset);
                IfRangeValues.Add(ifRange);
            }
        }
    }

    private sealed class ThrottledContentStream : Stream
    {
        private const int ChunkSize = 16;
        private readonly byte[] _content;
        private readonly TimeSpan _chunkDelay;
        private long _position;

        public ThrottledContentStream(byte[] content, long offset, TimeSpan chunkDelay)
        {
            _content = content;
            _position = offset;
            _chunkDelay = chunkDelay;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _content.Length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
            => ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_position >= _content.Length)
            {
                return 0;
            }

            await Task.Delay(_chunkDelay, cancellationToken);
            var bytesToCopy = (int)Math.Min(Math.Min(count, ChunkSize), _content.Length - _position);
            Array.Copy(_content, _position, buffer, offset, bytesToCopy);
            _position += bytesToCopy;
            return bytesToCopy;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_position >= _content.Length)
            {
                return 0;
            }

            await Task.Delay(_chunkDelay, cancellationToken);
            var bytesToCopy = (int)Math.Min(Math.Min(buffer.Length, ChunkSize), _content.Length - _position);
            _content.AsMemory((int)_position, bytesToCopy).CopyTo(buffer);
            _position += bytesToCopy;
            return bytesToCopy;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
