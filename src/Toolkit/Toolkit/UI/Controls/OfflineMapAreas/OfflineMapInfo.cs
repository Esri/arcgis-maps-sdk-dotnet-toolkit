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
using Esri.ArcGISRuntime.Portal;
#if NET8_0_OR_GREATER
using System.Text.Json.Serialization;
#else
using JsonIncludeAttribute = System.Runtime.Serialization.DataMemberAttribute;
#endif

namespace Esri.ArcGISRuntime.Toolkit;

// Swift reference: https://github.com/Esri/arcgis-maps-sdk-swift-toolkit/blob/main/Sources/ArcGISToolkit/Components/Offline/OfflineMapInfo.swift

/// <summary>
///  A model type that maintains information about an offline map area.
/// </summary>
public class OfflineMapInfo
{
    internal const string offlineMapInfoJsonFile = "info.json";
    internal const string offlineMapInfoThumbnailFile = "thumbnail.png";

    private class SerializableInfo
    {
        public string Title { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Uri? PortalItemURL { get; set; }

    }

    private SerializableInfo _info;

    /// <summary>
    /// Initializes a new instance of the <see cref="OfflineMapInfo"/> class.
    /// </summary>
    /// <param name="portalItem">The portal item of the web map which the offline map area was downloaded from.</param>
    internal OfflineMapInfo(PortalItem portalItem)
    {
        _info = new SerializableInfo
        {
            Title = portalItem.Title,
            Id = portalItem.ItemId,
            Description = portalItem.Description,
            PortalItemURL = portalItem.Url
        };
        Thumbnail = portalItem.Thumbnail;
    }

    internal string InfoFile { get; }

    internal string ThumbnailFile { get; }

    internal OfflineMapInfo(string infoFile, string thumbnailFile)
    {
        InfoFile = infoFile;
        ThumbnailFile = thumbnailFile;
        //TODO: Load from json
    }

    /// <summary>
    /// The title of the portal item associated with the map.
    /// </summary>
    public string Title => _info.Title;

    /// <summary>
    /// The portal item ID of the map.
    /// </summary>
    public string Id => _info.Id;

    /// <summary>
    /// The description of the portal item associated with the map.
    /// </summary>
    public string Description => _info.Description;

    /// <summary>
    /// The thumbnail of the portal item associated with the map.
    /// </summary>
    public Esri.ArcGISRuntime.UI.RuntimeImage? Thumbnail { get; }

    /// <summary>
    /// The URL of the portal item associated with the map.
    /// </summary>
    public Uri? PortalItemURL => _info.PortalItemURL;

    internal static Task<OfflineMapInfo> FromFolder(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException(directoryPath);
        throw new NotImplementedException();
    }
    private void Save(string directoryPath)
    {
        throw new NotImplementedException();
        //TODO: Save to JSON + save thumbnail to file

    }

    internal static void Delete(string directoryPath)
    {
        File.Delete(Path.Combine(directoryPath, offlineMapInfoJsonFile));
        File.Delete(Path.Combine(directoryPath, offlineMapInfoThumbnailFile));
    }
}