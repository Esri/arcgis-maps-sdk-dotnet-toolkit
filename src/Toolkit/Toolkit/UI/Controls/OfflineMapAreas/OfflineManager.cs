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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Esri.ArcGISRuntime.Tasks.Offline;
#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui;
#else
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif
#if NET8_0_OR_GREATER
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.Portal;
#else
using JsonIncludeAttribute = System.Runtime.Serialization.DataMemberAttribute;
#endif

namespace Esri.ArcGISRuntime.Toolkit;

// Swift reference: https://github.com/Esri/arcgis-maps-sdk-swift-toolkit/blob/main/Sources/ArcGISToolkit/Components/Offline/OfflineManager.swift

/// <summary>
/// A utility object that maintains the state of offline map areas and their storage on the device.
/// </summary>
/// <remarks>
/// <para>This component provides high-level APIs to manage offline map areas and
/// access their data.</para>
///<para>
/// <b>Features</b><br/>
/// The component supports both ahead-of-time(preplanned) and on-demand workflows for offline mapping. It allows you to:
/// <list type="bullet">
/// <item>Observe job status.</item>
/// <item>Access map info for web maps that have saved map areas via <see cref="OfflineManager.OfflineMapInfos"/>.</item>
/// <item>Remove offline map areas from the device.</item>
/// <item>Run download jobs while the app is in the background.</item>
/// </list>
/// </para><para>
/// The component is useful both for building custom UI with the provided APIs,
/// and for supporting workflows that require retrieving offline map areas
/// information from the device. By using the <see cref="OfflineManager"/>, you can create
/// an <see cref="OfflineMapAreasView" />.</para>
/// <note type="note">
/// The <see cref="OfflineManager"/> can be used independently of any UI components,
/// making it suitable for automated workflows or custom implementations.
/// </note>
/// </remarks>

public class OfflineManager
{
    // Work Manager constants
    // internal const string jobAreaTitleKey = "JobAreaTitle"
    // internal const string jsonJobPathKey = "JsonJobPath"
    // internal const string jobWorkerUuidKey = "WorkerUUID"
    // internal const string mobileMapPackagePathKey = "MobileMapPackagePath"
    // internal const string downloadJobJsonFile = "Job.json"
    // 
    // // Offline URLs constants
    // internal const string offlineRepositoryDir = "com.arcgismaps.toolkit.offline"
    // internal const string pendingMapInfoDir = "PendingMapInfo"
    // internal const string pendingJobsDir = "PendingJobs"
    // internal const string offlineMapAreasCacheDir = "OfflineMapAreasCache"
    // internal const string preplannedMapAreas = "Preplanned"
    // internal const string onDemandAreas = "OnDemand"

    private readonly string? _offlineMapsFolder;
    private readonly ReadonlyObservableCollection<OfflineMapInfo> _offlineMapInfos = new ReadonlyObservableCollection<OfflineMapInfo>();
    private readonly JobManager _jobManager;

    internal string OfflineMapsFolder
    {
        get => _offlineMapsFolder ?? DefaultFolder;
    }

    private OfflineManager() // Will use the default folder
    {
        _jobManager = JobManager.Shared;
        _jobManager.ResumeAllPausedJobs();
        
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OfflineManager"/> class with a specific folder for storing offline maps.
    /// </summary>
    /// <param name="offlineMapsFolder">Folder where the maps should be stored</param>
    /// <param name="jobManager">Optional job manager instance. If not specified, the <see cref="JobManager.Shared"/> instance is used.</param>
    /// <exception cref="DirectoryNotFoundException">Thrown if the <paramref name="offlineMapsFolder"/> folder does not exist</exception>
    public OfflineManager(string offlineMapsFolder, JobManager? jobManager = null) 
    {
        if (string.IsNullOrEmpty(offlineMapsFolder)) throw new ArgumentNullException(nameof(offlineMapsFolder));
        if (!Directory.Exists(offlineMapsFolder)) throw new DirectoryNotFoundException(offlineMapsFolder);
        _offlineMapsFolder = offlineMapsFolder;
        _jobManager = jobManager ?? JobManager.Shared;
    }

    private static string GetDefaultFolder()
    {
#if WINDOWS
        if (Windows.ApplicationModel.Package.Current is null)
            throw new PlatformNotSupportedException("Default folder is only supported for packaged applications");

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Esri", "ArcGISToolkit", "OfflineManager");
#elif __IOS__
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "com.esri.ArcGISToolkit.offlineManager");
#elif __ANDROID__
        return Path.Combine(Android.App.Application.Context.GetExternalFilesDir(null /*TODO*/)!.AbsolutePath, "com.arcgismaps.toolkit.offline");
#endif
        throw new PlatformNotSupportedException();
    }

    private static string _defaultFolder = GetDefaultFolder();

    /// <summary>
    /// Gets or sets the folder used for storing maps when using the <see cref="OfflineManager.Default"/> instance.
    /// </summary>
    /// <remarks>
    /// If you develop a non-packaged app on Windows, it's recommended to instead use <see cref="OfflineManager.OfflineManager(string, JobManager?)"> the constructor that takes a specific folder path</see>,
    /// so that maps won't get shared among multiple applications.
    /// </remarks>

    public static string DefaultFolder
    {
        get => _defaultFolder;
        set
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
            if (!Directory.Exists(value)) throw new DirectoryNotFoundException(value);
            _defaultFolder = value;
        }
    }

    /// <summary>
    /// A shared singleton instance of offline manager using the <see cref="DefaultFolder"/> location.
    /// </summary>
    public static OfflineManager Default { get; } = new OfflineManager();

    /// <summary>
    /// Gets the portal item information for web maps that have downloaded map areas.
    /// </summary>
    public IReadOnlyList<OfflineMapInfo> OfflineMapInfos => _offlineMapInfos;

    /// <summary>
    /// Starts a job that will be managed by this instance.
    /// </summary>
    /// <param name="job">The job to start.</param>
    /// <param name="portalItem">The portal item whose map is being taken offline.</param>
    /// <returns></returns>
    public void Start(DownloadPreplannedOfflineMapJob job, PortalItem portalItem ) => Start((IJob)job, portalItem);

    public void Start(GenerateOfflineMapJob job, PortalItem portalItem) => Start((IJob)job, portalItem);

    private void Start(IJob job, PortalItem portalItem)
    {
        _jobManager.Jobs.Add(job);
        _ = ObserveJob(job);
        job.Start();
        SavePendingMapInfo(portalItem);
     
    }

    private async Task ObserveJob(IJob job)
    {
        try
        {
            var result = await job.GetResultAsync();
        }
        catch { }
        if (_jobManager.Jobs.Contains(job))
            _jobManager.Jobs.Remove(job);
        // JobCompleted?.Invoke(this, job);

        // Check pending map infos.
         HandlePendingMapInfo(GetPortalItemForJob(job)?.ItemId);
    }

    /// <summary>
    /// Figures out and returns the portal item associated with the online map for a particular offline job.
    /// </summary>
    /// <param name="job"></param>
    /// <returns></returns>
    private PortalItem? GetPortalItemForJob(IJob job)
    {
        if (job is DownloadPreplannedOfflineMapJob downloadPreplanned)
            return downloadPreplanned.OnlineMap?.Item as PortalItem;
        if (job is GenerateOfflineMapJob generateOffline)
            return generateOffline.OnlineMap?.Item as PortalItem;
        return null;
    }

    private void RemoveMapInfo(string portalItemID)
    {
        var item = _offlineMapInfos.Where(o => o.Id == portalItemID).FirstOrDefault();
        if (item is not null)
        {
            _offlineMapInfos.RemoveItem(item);
            OfflineMapInfo.Delete(GetPortalItemDirectory(portalItemID));
        }
    }

    private async void LoadOfflineMapInfos()
    {
        if (!Directory.Exists(OfflineMapsFolder))
            return;
        var directories = Directory.GetDirectories(OfflineMapsFolder);
        var mapInfos = new List<OfflineMapInfo>();
        foreach (var dir in directories)
        {
            try
            {
                var mapInfo = await OfflineMapInfo.FromFolderAsync(dir);
                mapInfos.Add(mapInfo);
            }
            catch
            {
                // Ignore invalid folders
            }
        }
        _offlineMapInfos.AddRange(mapInfos);
    }

    // Saves the map info to the pending folder for a particular portal item.
    // The info will stay in that folder until the job completes.
    private async Task SavePendingMapInfo(PortalItem portalItem)
    {
        var id = portalItem.ItemId;
        if (string.IsNullOrEmpty(id))
            return;
        string path = GetPendingMapInfoDirectory(id);
        if (OfflineMapInfo.InfoExists(path))
            return; //No need to save pending info as pending offline map info already exists.

        var info = new OfflineMapInfo(portalItem);
        await info.Save(path);
    }

    // For a successful job, this function moves the pending map info from the pending
    // folder to its final destination.
    private void HandlePendingMapInfo(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return;
        // TODO
    }

    /// <summary>
    /// Removes all downloads for all offline maps.
    /// </summary>
    public void RemoveAllDownloads()
    {
        foreach (var item in OfflineMapInfos.ToArray())
        {
            RemoveDownloads(item);
        }
    }

    /// <summary>
    /// Removes any downloaded map areas for a particular map.
    /// </summary>
    /// <param name="mapInfo">The information for the offline map for which all downloads will be removed.</param>
    public void RemoveDownloads(OfflineMapInfo mapInfo)
    {
        if (mapInfo == null)
        {
            throw new ArgumentNullException(nameof(mapInfo));
        }
        throw new NotImplementedException();
    }



    private string GetPortalItemDirectory(string portalItemID)
    {
        return Path.Combine(OfflineMapsFolder, "Maps", portalItemID);
    }
    private string GetPendingMapInfoDirectory(string portalItemID)
    {
        return Path.Combine(OfflineMapsFolder, "Pending", portalItemID);
    }
}