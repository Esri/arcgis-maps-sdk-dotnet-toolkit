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

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
#if NET8_0_OR_GREATER
using System.Text.Json.Serialization;
#else
using JsonIncludeAttribute = System.Runtime.Serialization.DataMemberAttribute;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit;
#else
namespace Esri.ArcGISRuntime.Toolkit;
#endif

/// <summary>
/// Extension methods for downloading files.
/// </summary>
/// <seealso cref="FileDownloadTask"/>
public static class FileDownloadTaskExtensions
{
    /// <summary>
    /// Downloads the data content from a Portal Item.
    /// </summary>
    /// <param name="portalItem">Portal Item who's data should be downloaded</param>
    /// <param name="destinationPath">File path to save the download to.</param>
    /// <returns>The download task.</returns>
    /// <exception cref="IOException">The destination file already exists</exception>.
    public static Task<FileDownloadTask> BeginDownloadAsync(this Portal.PortalItem portalItem, string destinationPath)
    {
        string requestUri = $"{portalItem.Portal.Uri.OriginalString.TrimEnd('/')}/sharing/rest/content/items/{portalItem.ItemId}/data";
        return FileDownloadTask.BeginDownloadAsync(destinationPath, new Uri(requestUri));
    }
}

/// <summary>
/// Provides the progress information of a download.
/// </summary>
/// <seealso cref="FileDownloadTask"/>
/// <seealso cref="FileDownloadTask.Progress"/>
public class FileDownloadProgress
{
    internal FileDownloadProgress(long totalBytes, long? totalLength)
    {
        TotalBytes = totalBytes;
        TotalLength = totalLength;
    }

    /// <summary>
    /// Gets the percentage of the download completed if available.
    /// </summary>
    public int? Percentage => TotalLength.HasValue && TotalLength.Value > 0 ? (int)Math.Round(TotalBytes * 100d / TotalLength!.Value, 0) : null;

    /// <summary>
    /// Gets the total number of bytes downloaded so far.
    /// </summary>
    public long TotalBytes { get; }

    /// <summary>
    /// Gets the total size of the download, if available.
    /// </summary>
    public long? TotalLength { get; } = null;
}

/// <summary>
/// Represents the status of a download.
/// </summary>
/// <seealso cref="FileDownloadTask"/>
/// <seealso cref="FileDownloadTask.Status"/>
public enum FileDownloadStatus
{
    /// <summary>
    /// The download is currently running.
    /// </summary>
    Downloading,
    
    /// <summary>
    /// The download is currently paused.
    /// </summary>
    /// <seealso cref="FileDownloadTask.PauseAsync"/>
    Paused,

    /// <summary>
    /// The download is started and currently waiting for space in the queue.
    /// </summary>
    /// <seealso cref="FileDownloadTask.Priority"/>
    Queued,

    /// <summary>
    /// The download is currently resuming.
    /// </summary>
    /// <seealso cref="FileDownloadTask.ResumeAsync"/>
    Resuming,

    /// <summary>
    /// The download is starting.
    /// </summary>
    /// <seealso cref="FileDownloadTask.BeginDownloadAsync(string, Uri, HttpMessageHandler?)"/>
    Starting,

    /// <summary>
    /// The download is has completed successfully.
    /// </summary>
    Completed,
    
    /// <summary>
    /// The download has an error.
    /// </summary>
    /// <seealso cref="FileDownloadTask.Exception"/>
    /// <seealso cref="FileDownloadTask.Error"/>
    Error,
    
    /// <summary>
    /// The download has been cancelled.
    /// </summary>
    /// <seealso cref="FileDownloadTask.CancelAsync"/>
    Cancelled
}

/// <summary>
/// Task for helping managing large downloads that can be paused and resumed across application sessions.
/// </summary>
/// <remarks>
/// Once a download has started, you should call <see cref="ToJson"/> to serialize the task to a string. This
/// allows you to resume the download later if the app closes for whatever reason before the download completes.
/// </remarks>
#if !NET8_0_OR_GREATER
[DataContract]
#endif
public sealed class FileDownloadTask : IDisposable
#if NET8_0_OR_GREATER
        , IJsonOnDeserialized
#endif
{
    private HttpResponseMessage? content;
    private HttpClient client;
    private CancellationTokenSource? cancellationSource;
    private Task? transferTask;


    #region Queue Management
    private static object _queueLock = new object();
    private static List<FileDownloadTask> _queuedItems = new List<FileDownloadTask>();
    private static List<FileDownloadTask> _inProgressItems = new List<FileDownloadTask>();
    private static uint _maxConcurrentDownloads = 3;

    /// <summary>
    /// Gets or sets the maximum number of downloads that can be in progress at the same time before getting queued. The default is <c>3</c>.
    /// </summary>
    /// <remarks>
    /// If this value is zero, no limits are put on the number of simultanous downloads. If the value decreases, any downloads that are currently in progress will continue,
    /// but no new downloads will be started until the number of in-progress downloads is below the new limit.
    /// </remarks>
    public static uint MaxConcurrentDownloads
    {
        get { return _maxConcurrentDownloads; }
        set
        {
            if (_maxConcurrentDownloads != value)
            {
                bool queueSizeIncreased = (value > _maxConcurrentDownloads) || (value == 0);
                _maxConcurrentDownloads = value;
                if (queueSizeIncreased)
                    ProcessQueue();
            }
        }
    }

    /// <summary>
    /// Clears any downloads still in the download queue. This will pause all downloads that are queued, and remove them from the queue.
    /// </summary>
    public static void ClearQueue()
    {
        lock (_queueLock)
        {
            foreach (var item in _queuedItems)
            {
                item.Status = FileDownloadStatus.Paused;
            }
            _queuedItems.Clear();
        }
    }

    private static void ProcessQueue()
    {
        FileDownloadTask? next = null;
        lock (_queueLock)
        {
            if ((MaxConcurrentDownloads == 0 || _inProgressItems.Count < MaxConcurrentDownloads) && _queuedItems.Count > 0)
            {
                next = _queuedItems.OrderByDescending(t => t.Priority).FirstOrDefault();
                if (next is not null)
                {
                    _queuedItems.Remove(next);
                }
            }
        }
        if (next is not null)
        {
            _ = next.ResumeAsync();
            ProcessQueue(); // There might be room to dequeue more
        }
    }

    #endregion Queue Management

    private FileDownloadTask(string filename, Uri requestUri, HttpResponseMessage? content, HttpClient client) : this()
    {
        RequestUri = requestUri;
        this.client = client;
        this.Filename = filename;
#if NETFX_CORE || __IOS__ || __ANDROID__
        this.TempFile = Path.GetTempFileName();
#else
        this.TempFile = Path.Combine(ArcGISRuntimeEnvironment.TempPath, Path.GetRandomFileName());
#endif
        if (content is not null)
        {
            lock (_queueLock)
            {
                if (MaxConcurrentDownloads > 0 && _inProgressItems.Count >= MaxConcurrentDownloads && !_inProgressItems.Contains(this))
                {
                    // Another download likely started at the same time, so pause this one for now
                    _queuedItems.Add(this);
                    this.Status = FileDownloadStatus.Queued;
                    content.Dispose();
                    this.content = null;
                    return;
                }
                if (!_inProgressItems.Contains(this))
                    _inProgressItems.Add(this);
            }
            this.content = content;
            transferTask = BeginTransfer(content);
        }
    }

#if NET8_0_OR_GREATER
    // not creatable
    [JsonConstructor]
#endif
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    internal FileDownloadTask()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        downloadSpeedSamples = new ConcurrentQueue<double>();
    }

    /// <summary>
    /// Gets or sets the download priority if multiple items are queued. The higher the number, the higher the priority of the download.
    /// </summary>
    public int Priority { get; set; }

    /// <inheritdoc />
    public void Dispose() => Dispose(true);

    /// <inheritdoc />
    ~FileDownloadTask() => Dispose(false);

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            cancellationSource?.Dispose();
            client.Dispose();
            content?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Gets the the exception of the download if it failed.
    /// </summary>
#if NET8_0_OR_GREATER
    [JsonIgnore]
#endif
    public Exception? Exception { get; private set; }

    private FileDownloadStatus _status;

    /// <summary>
    /// Gets the current download status of the download.
    /// </summary>
    /// <seealso cref="StatusChanged"/>
    [JsonInclude]
    public FileDownloadStatus Status
    {
        get => _status;
        internal set
        {
            if (value != _status)
            {
                _status = value;
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Event triggered when the <see cref="Status"/> property changes.
    /// </summary>
    public event EventHandler? StatusChanged;

    /// <summary>
    /// Gets or sets the buffer size used for downloading the file. The default is 65535 bytes.
    /// </summary>
    [JsonInclude]
    public int BufferSize { get; set; } = 65535;

    [JsonInclude]
    internal string? ETag { get; set; }

    [JsonInclude]
    internal DateTimeOffset? Date { get; set; }

    /// <summary>
    /// Gets a value indicating whether the download can be resumed from the last position, or will start over on resume.
    /// </summary>
    /// <remarks>
    /// This is determined by the server response. If the server supports byte ranges and provides an ETag or Date header, this will be <c>true</c>.
    /// </remarks>
    /// <seealso cref="ResumeAsync"/>
    public bool IsResumable { get; private set; }

    [JsonInclude]
    internal Uri RequestUri { get; set; }

    [JsonInclude]
    internal string Filename { get; set; }

    [JsonInclude]
    internal string TempFile { get; set; }

    /// <summary>
    /// The number of bytes downloaded so far.
    /// </summary>
#if NET8_0_OR_GREATER
    [JsonIgnore]
#endif
    public long BytesDownloaded { get; private set; }

    /// <summary>
    /// The total length of the file being downloaded. This is only available if the server provides the content length.
    /// </summary>
    [JsonInclude]
    public long? TotalLength { get; internal set; }

    /// <summary>
    /// Event raised when the download is completed successfully.
    /// </summary>
    public event EventHandler? Completed;

    /// <summary>
    /// Event raised as the download progresses.
    /// </summary>
    public event EventHandler<FileDownloadProgress>? Progress;

    /// <summary>
    /// Event raised when an error occurs during the download.
    /// </summary>
    public event EventHandler<Exception>? Error;

#if NET8_0_OR_GREATER
    void IJsonOnDeserialized.OnDeserialized()
#else
    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
#endif
    {
        if (Status == FileDownloadStatus.Downloading || Status == FileDownloadStatus.Resuming || Status == FileDownloadStatus.Queued)
            Status = FileDownloadStatus.Paused;
        var fi = new FileInfo(TempFile);

        if (fi.Exists)
        {
            IsResumable = !string.IsNullOrEmpty(ETag) || Date.HasValue;
            BytesDownloaded = fi.Length;
        }
        client = new HttpClient(new Http.ArcGISHttpMessageHandler());
        downloadSpeedSamples = new ConcurrentQueue<double>();
    }

    /// <summary>
    /// Restores a previously serialized <see cref="FileDownloadTask"/> from a JSON string.
    /// </summary>
    /// <param name="json">JSON to deserialize.</param>
    /// <param name="handler">Optional custom HTTP handler to be used.</param>
    /// <returns></returns>
    public static FileDownloadTask FromJson(string json, HttpMessageHandler? handler = null)
    {
#if NET8_0_OR_GREATER
        var fdt = System.Text.Json.JsonSerializer.Deserialize(json, FileDownloadTaskSerializationContext.Default.FileDownloadTask)!;
        if (handler is not null)
        {
            fdt.client?.Dispose();
            fdt.client = new HttpClient(handler);
        }
        return fdt;
#else
        DataContractJsonSerializer s = new DataContractJsonSerializer(typeof(FileDownloadTask));
        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
        {
            var fdt = s.ReadObject(ms) as FileDownloadTask;
            fdt.client = new HttpClient(handler ?? new Http.ArcGISHttpMessageHandler());
            return fdt;
        }
#endif
    }

    /// <summary>
    /// Serializes the current <see cref="FileDownloadTask"/> to a JSON string for later resuming downloads.
    /// </summary>
    /// <returns>JSON serialized <see cref="FileDownloadTask"/>.</returns>
    public string ToJson()
    {
#if NET8_0_OR_GREATER
        return System.Text.Json.JsonSerializer.Serialize(this, FileDownloadTaskSerializationContext.Default.FileDownloadTask);
#else
        DataContractJsonSerializer s = new DataContractJsonSerializer(typeof(FileDownloadTask));
        using (var ms = new MemoryStream())
        {
            s.WriteObject(ms, this);
            return Encoding.UTF8.GetString(ms.ToArray());
        }
#endif
    }

    /// <summary>
    /// Initiates a download to the specified file from the specified URL
    /// </summary>
    /// <param name="destinationPath">File where the download should be saved to.</param>
    /// <param name="requestUri">URL to download</param>
    /// <param name="handler">Optional custom HTTP handler to be used.</param>
    /// <param name="cancellationToken">Cancellation token for starting the download. Note that once download has started, use <see cref="CancelAsync"/> to cancel an active download.</param>
    /// <returns>FileDownloadTask</returns>
    /// <exception cref="IOException">The destination file already exists</exception>.
    public static Task<FileDownloadTask> BeginDownloadAsync(string destinationPath, Uri requestUri, HttpMessageHandler? handler = null, CancellationToken cancellationToken = default)
    {
        return BeginDownloadAsync(requestUri, new HttpRequestMessage(HttpMethod.Get, requestUri), destinationPath, handler, cancellationToken);
    }

    private static async Task<FileDownloadTask> BeginDownloadAsync(Uri requestUri, HttpRequestMessage request, string filename, HttpMessageHandler? handler, CancellationToken cancellationToken)
    {
        if(requestUri is null)
            throw new ArgumentNullException(nameof(requestUri));
        if (File.Exists(filename))
            throw new IOException("The destination file already exists");
        HttpClient client;
        if (handler is null)
            client = new HttpClient(new Http.ArcGISHttpMessageHandler(), true);
        else 
            client = new HttpClient(handler, false);
        lock(_queueLock)
        {
            if(MaxConcurrentDownloads > 0 && _inProgressItems.Count >= MaxConcurrentDownloads)
            {
                var task = new FileDownloadTask(filename, requestUri, null, client);
                _queuedItems.Add(task);
                task.Status = FileDownloadStatus.Queued;
                return task;
            }
        }
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var content = response.EnsureSuccessStatusCode();

        FileDownloadTask result = new FileDownloadTask(filename, requestUri, content, client);
        return result;
    }

    /// <summary>
    /// Restarts the download from the beginning.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for resuming the download. Note that once download has started, use <see cref="CancelAsync"/> to cancel an active download.</param>
    /// <returns></returns>
    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        if (Status == FileDownloadStatus.Cancelled)
            throw new TaskCanceledException("Download was previously cancelled");
        var task = transferTask;
        if (task != null && cancellationSource != null)
        {
            try
            {
                await CancelAsync();
            }
            catch { }
        }
        await BeginDownload(0, cancellationToken);
    }

    /// <summary>
    /// Resumes the download from the last position.
    /// </summary>
    /// <remarks>
    /// If the task isn't resumable, the download will start over, and if it is already running, this is a no-op.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token for resuming the download. Note that once download has started, use <see cref="CancelAsync"/> to cancel an active download.</param>
    /// <returns></returns>
    /// <exception cref="TaskCanceledException">Thrown if the download has already been cancelled.</exception>
    public Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        if (Status == FileDownloadStatus.Cancelled)
            throw new TaskCanceledException("Download was previously cancelled");
        if (Status == FileDownloadStatus.Paused || Status == FileDownloadStatus.Error || Status == FileDownloadStatus.Queued)
        {
            if (!IsResumable)
                return RestartAsync();
            Status = FileDownloadStatus.Resuming;
            FileInfo f = new FileInfo(TempFile);
            long offset = f.Exists ? f.Length : 0;
            if (offset < TotalLength)
                Math.Max(0, offset - BufferSize); // We rewind one buffer-length, just to make sure any last file flushing wasn't interrupted/corrupted, unless we already reached the end
            return BeginDownload(offset, cancellationToken); 
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Aborts the running download and deletes the partial download.
    /// </summary>
    /// <returns></returns>
    public Task CancelAsync()
    {
        var task = transferTask;
        if (Status == FileDownloadStatus.Paused)
        {
            if (File.Exists(TempFile))
                File.Delete(TempFile);
            BytesDownloaded = 0;
            Status = FileDownloadStatus.Cancelled;
            return Task.CompletedTask;
        }
        else
        {
            lock(_queueLock)
            {
                if (_inProgressItems.Contains(this))
                {
                    _inProgressItems.Remove(this);
                }
                if (_queuedItems.Contains(this))
                {
                    _queuedItems.Remove(this);
                }
            }
            if (cancellationSource is null)
            {
                Status = FileDownloadStatus.Cancelled;
            }
            else
            {
                cancellationSource?.Cancel();
            }
            if (task is not null)
            {
                return task.ContinueWith(t => { File.Delete(TempFile); BytesDownloaded = 0; Status = FileDownloadStatus.Cancelled; });
            }
            else
            {
                if (task is null && File.Exists(TempFile))
                    File.Delete(TempFile);
                ProcessQueue();
                return Task.CompletedTask;
            }
        }
    }

    /// <summary>
    /// Pauses the active download.
    /// </summary>
    /// <returns></returns>
    public Task PauseAsync()
    {
        if (Status == FileDownloadStatus.Cancelled)
            throw new TaskCanceledException("Download was previously cancelled");
        lock (_queueLock)
        {
            if (_queuedItems.Contains(this))
                _queuedItems.Remove(this);
        }

        var task = transferTask;
        if (cancellationSource == null || task == null)
        {
            Status = FileDownloadStatus.Paused;
            return Task.CompletedTask;
        }
        cancellationSource.Cancel();
        return task.ContinueWith(t => { Status = FileDownloadStatus.Paused; });
    }

    /// <summary>
    /// Initiates the download by downloading the headers, before handing the streaming download off to <see cref="BeginTransfer">the transfer task</see>.
    /// </summary>
    /// <param name="offset">Byte offset where the download should start.</param>
    /// <param name="cancellationToken">Cancellation token for initializing the download request. Note that you will need to use CancelAsync() to cancel the streaming download after it has begun.</param>
    /// <returns></returns>
    private async Task BeginDownload(long offset, CancellationToken cancellationToken)
    {
        if (offset == TotalLength)
        {
            // This should only happen on a call to resume if a move had failed previously
            try
            {
                File.Move(TempFile, Filename);
            }
            catch (Exception ex)
            {
                Exception = ex;
                Status = FileDownloadStatus.Error;
                Error?.Invoke(this, ex);
                throw;
            }
            transferTask = Task.CompletedTask;
            Status = FileDownloadStatus.Completed;
            Completed?.Invoke(this, EventArgs.Empty);
            return;
        }
        lock (_queueLock)
        {
            if (MaxConcurrentDownloads > 0 && _inProgressItems.Count >= MaxConcurrentDownloads)
            {
                _queuedItems.Add(this);
                Status = FileDownloadStatus.Queued;

                return;
            }
            _inProgressItems.Add(this);
        }
        if (Status == FileDownloadStatus.Paused)
            Status = FileDownloadStatus.Resuming;
        else
            Status = FileDownloadStatus.Starting;

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, RequestUri);
        if (offset > 0)
        {
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(offset, null);
            if (ETag != null)
                request.Headers.IfRange = new System.Net.Http.Headers.RangeConditionHeaderValue(ETag);
            else if (Date != null)
                request.Headers.IfRange = new System.Net.Http.Headers.RangeConditionHeaderValue(Date.Value);
        }
        try
        {
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            content = response.EnsureSuccessStatusCode();
            transferTask = BeginTransfer(content);
        }
        catch (System.Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                Status = FileDownloadStatus.Cancelled;
            }
            else
            {
                Status = FileDownloadStatus.Error;
                Exception = ex;
                Error?.Invoke(this, ex);
            }
            lock (_queueLock)
            {
                if (_inProgressItems.Contains(this))
                {
                    _inProgressItems.Remove(this);
                }
            }
            ProcessQueue();
            throw;
        }
    }

    private async Task BeginTransfer(HttpResponseMessage content)
    {
        Exception = null;
        cancellationSource = new CancellationTokenSource();
        IsResumable = content.Headers.AcceptRanges?.Contains("bytes") == true;
        if (IsResumable)
        {
            ETag = content.Headers.ETag?.Tag;
            Date = content.Headers.Date;
        }
        var token = cancellationSource.Token;
        Status = FileDownloadStatus.Downloading;
        byte[] buffer = new byte[BufferSize];
        long? length = content.Content.Headers.ContentLength;
        if (!length.HasValue)
            length = content.Content.Headers.ContentDisposition?.Size;
        long position = 0;
        if (content.StatusCode == System.Net.HttpStatusCode.PartialContent)
        {
            if (content.Content.Headers.ContentRange?.HasRange == true)
            {
                position = content.Content.Headers.ContentRange.From!.Value;
            }
            if (content.Content.Headers.ContentRange?.HasLength == true)
            {
                length = content.Content.Headers.ContentRange.Length;
            }
        }
        TotalLength = length;
        int count = 0;
        try
        {
            using (var readStream = await content.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                using (var file = File.Open(TempFile, position == 0 ? FileMode.Create : FileMode.Open, FileAccess.Write))
                {
                    if (position > 0)
                    {
                        if (file.Length < position)
                            throw new System.IO.IOException("File is smaller than starting position");
                        file.Seek(position, SeekOrigin.Begin);
                        file.SetLength(position);
                    }

                    long total = position;
                    System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                    timer.Start();

                    var timestamp = timer.ElapsedMilliseconds;
                    var lastProgressTimestamp = timestamp;
                    while ((count = await readStream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
                    {
                        if (downloadSpeedSamples.Count > (1024 * 1024 / BufferSize)) // Limit to 1 megabyte worth samples
                            downloadSpeedSamples.TryDequeue(out _);
                        await file.WriteAsync(buffer, 0, count, token).ConfigureAwait(false);
                        await file.FlushAsync(token).ConfigureAwait(false);
                        var newtimestamp = timer.ElapsedMilliseconds;
                        var ms = newtimestamp - timestamp;
                        if (ms > 0)
                        {
                            timestamp = newtimestamp;
                            downloadSpeedSamples.Enqueue(count / (double)ms * 1000);
                        }
                        total += count;
                        BytesDownloaded = total;
                        // Throttle progress updates to maximum every 100 milliseconds, so we don't flood the UI too much if you have fast download speeds
                        if (lastProgressTimestamp + 100 < newtimestamp)
                        {
                            lastProgressTimestamp = newtimestamp;
                            Progress?.Invoke(this, new FileDownloadProgress(total, length));
                        }
                        if (token.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                    }
                }
            }
            File.Move(TempFile, Filename);
            Status = FileDownloadStatus.Completed;
            Completed?.Invoke(this, EventArgs.Empty);
        }
        catch (System.Exception error)
        {
            if (!token.IsCancellationRequested)
            {
                Exception = error;
                Status = FileDownloadStatus.Error;
                Error?.Invoke(this, error);
            }
        }
        finally
        {
            cancellationSource = null;
            transferTask = null;
#if WPF && !NETCORE_APP
            downloadSpeedSamples = new();
#else
            downloadSpeedSamples.Clear();
#endif
            lock (_queueLock)
            {
                if (_inProgressItems.Contains(this))
                    _inProgressItems.Remove(this);
            }
            ProcessQueue();
        }
    }
    private ConcurrentQueue<double> downloadSpeedSamples;
    
    /// <summary>
    ///  Gets the current download speed as bytes per second based on a moving average calculation
    /// </summary>
    public int DownloadSpeed
    {
        get
        {
            if (downloadSpeedSamples.Count == 0 || Status != FileDownloadStatus.Downloading)
                return 0;
            return (int)(downloadSpeedSamples.Sum() / downloadSpeedSamples.Count);
        }
    }
}

#if NET8_0_OR_GREATER
/// <summary>
/// Partial class for the JSON serialization code generator.
/// </summary>
[JsonSerializable(typeof(FileDownloadTask))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class FileDownloadTaskSerializationContext : JsonSerializerContext
{
}
#endif