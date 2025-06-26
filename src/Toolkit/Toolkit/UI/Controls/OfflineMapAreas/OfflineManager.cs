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
#if NET8_0_OR_GREATER
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Tasks.Offline;
#else
using JsonIncludeAttribute = System.Runtime.Serialization.DataMemberAttribute;
#endif

namespace Esri.ArcGISRuntime.Toolkit;

// Swift reference: https://github.com/Esri/arcgis-maps-sdk-swift-toolkit/blob/main/Sources/ArcGISToolkit/Components/Offline/OfflineManager.swift

/// <summary>
/// Manages offline map operations and storage.
/// </summary>
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

    internal string OfflineMapsFolder
    {
        get => _offlineMapsFolder ?? DefaultFolder;
    }

    private OfflineManager() // Will use the default folder
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OfflineManager"/> class with a specific folder for storing offline maps.
    /// </summary>
    /// <param name="offlineMapsFolder">Folder where the maps should be stored</param>
    /// <exception cref="DirectoryNotFoundException">Thrown if the folder does not exist</exception>
    /// <seealso cref="Default"/>
    public OfflineManager(string offlineMapsFolder)
    {
        if (string.IsNullOrEmpty(offlineMapsFolder)) throw new ArgumentNullException(nameof(offlineMapsFolder));
        if (!Directory.Exists(offlineMapsFolder)) throw new DirectoryNotFoundException(offlineMapsFolder);
        _offlineMapsFolder = offlineMapsFolder;
    }

    private static string GetDefaultFolder()
    {
#if WINDOWS
        //bool isPackaged = false; // TODO: Check if app is packaged
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Esri", "ArcGISToolkit", "OfflineManager");
#elif __IOS__
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "com.esri.ArcGISToolkit.offlineManager");
#elif __ANDROID__
        return Path.Combine(Android.App.Application.Context.GetExternalFilesDir(), "com.arcgismaps.toolkit.offline");
#endif
        throw new PlatformNotSupportedException();
    }

    private static string _defaultFolder = GetDefaultFolder();

    /// <summary>
    /// Gets or sets the folder used for storing maps when using the <see cref="OfflineManager.Default"/> instance.
    /// </summary>
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
    /// A shared singleton instance of offline manager.
    /// </summary>
    public static OfflineManager Default { get; } = new OfflineManager(); // TODO: Consider allowing creation with specific folder name

    private ReadonlyObservableCollection<OfflineMapInfo> _offlineMapAreas = new ReadonlyObservableCollection<OfflineMapInfo>();

    /// <summary>
    /// Gets the portal item information for web maps that have downloaded map areas.
    /// </summary>
    public IReadOnlyList<OfflineMapInfo> OfflineMapAreas => _offlineMapAreas;

    private List<DownloadPreplannedOfflineMapJob> preplannedJobs = new List<DownloadPreplannedOfflineMapJob>();
    private List<GenerateOfflineMapJob> generateOfflineMapJobs = new List<GenerateOfflineMapJob>();
    private List<OfflineMapSyncJob> offlineMapSyncJobs = new List<OfflineMapSyncJob>();

    public async Task Start(DownloadPreplannedOfflineMapJob job)
    {
        preplannedJobs.Add(job);
        var result = await job.GetResultAsync();
        preplannedJobs.Remove(job);
    }
    public async void Start(GenerateOfflineMapJob job)
    {
        generateOfflineMapJobs.Add(job);
        var result = await job.GetResultAsync();
        generateOfflineMapJobs.Remove(job);
    }
    public async void Start(OfflineMapSyncJob job)
    {
        offlineMapSyncJobs.Add(job);
        var result = await job.GetResultAsync();
        offlineMapSyncJobs.Remove(job);
    }

    /// <summary>
    /// Removes all downloads for all offline maps.
    /// </summary>
    public void RemoveAllDownloads()
    {
        foreach (var item in OfflineMapAreas.ToArray())
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
}