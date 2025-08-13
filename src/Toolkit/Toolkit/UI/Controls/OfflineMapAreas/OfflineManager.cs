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
/// <item>Access map info for web maps that have saved map areas via <see cref="OfflineManager.OfflineMapAreas"/>.</item>
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
    private readonly List<DownloadPreplannedOfflineMapJob> _preplannedJobs = new List<DownloadPreplannedOfflineMapJob>();
    private readonly List<GenerateOfflineMapJob> _generateOfflineMapJobs = new List<GenerateOfflineMapJob>();
    private readonly List<OfflineMapSyncJob> _offlineMapSyncJobs = new List<OfflineMapSyncJob>();
    private readonly ReadonlyObservableCollection<OfflineMapInfo> _offlineMapAreas = new ReadonlyObservableCollection<OfflineMapInfo>();


    internal string OfflineMapsFolder
    {
        get => _offlineMapsFolder
#if !NETFRAMEWORK
            ?? DefaultFolder
#endif
            ;
    }

#if !NETFRAMEWORK
    private OfflineManager() // Will use the default folder
    {
    }
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="OfflineManager"/> class with a specific folder for storing offline maps.
    /// </summary>
    /// <param name="offlineMapsFolder">Folder where the maps should be stored</param>
    /// <exception cref="DirectoryNotFoundException">Thrown if the folder does not exist</exception>
#if !NETFRAMEWORK
    /// <seealso cref="Default"/>
#endif
    public OfflineManager(string offlineMapsFolder)
    {
        if (string.IsNullOrEmpty(offlineMapsFolder)) throw new ArgumentNullException(nameof(offlineMapsFolder));
        if (!Directory.Exists(offlineMapsFolder)) throw new DirectoryNotFoundException(offlineMapsFolder);
        _offlineMapsFolder = offlineMapsFolder;
    }

#if !NETFRAMEWORK
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
    /// If you develop a non-packaged app on Windows, it's recommended to instead use <see cref="OfflineManager.OfflineManager(string)"> the constructor that takes a specific folder path</see>,
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
#endif

    /// <summary>
    /// Gets the portal item information for web maps that have downloaded map areas.
    /// </summary>
    public IReadOnlyList<OfflineMapInfo> OfflineMapAreas => _offlineMapAreas;

    internal async Task Start(DownloadPreplannedOfflineMapJob job)
    {
        _preplannedJobs.Add(job);
        var result = await job.GetResultAsync();
        _preplannedJobs.Remove(job);
    }

    internal async void Start(GenerateOfflineMapJob job)
    {
        _generateOfflineMapJobs.Add(job);
        var result = await job.GetResultAsync();
        _generateOfflineMapJobs.Remove(job);
    }

    internal async void Start(OfflineMapSyncJob job)
    {
        _offlineMapSyncJobs.Add(job);
        var result = await job.GetResultAsync();
        _offlineMapSyncJobs.Remove(job);
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