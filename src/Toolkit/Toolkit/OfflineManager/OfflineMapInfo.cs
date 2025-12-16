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
using System.Text;
using Esri.ArcGISRuntime.Portal;


#if NET5_0_OR_GREATER
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json;
#else
using JsonIncludeAttribute = System.Runtime.Serialization.DataMemberAttribute;
#endif

namespace Esri.ArcGISRuntime.Toolkit;

// Swift reference: https://github.com/Esri/arcgis-maps-sdk-swift-toolkit/blob/main/Sources/ArcGISToolkit/Components/Offline/OfflineMapInfo.swift


#if NET5_0_OR_GREATER
[JsonSerializable(typeof(SerializableInfo))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class InfoSerializationContext : JsonSerializerContext { }
#endif

[DataContract]
internal partial class SerializableInfo
{
#if NET5_0_OR_GREATER
    public static SerializableInfo? FromJson(string json) => System.Text.Json.JsonSerializer.Deserialize(json, typeof(SerializableInfo), InfoSerializationContext.Default) as SerializableInfo;

    public string ToJson() => System.Text.Json.JsonSerializer.Serialize(this, InfoSerializationContext.Default.SerializableInfo);
#else
        private static System.Runtime.Serialization.Json.DataContractJsonSerializer serializer  = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(SerializableInfo));
        public static SerializableInfo? FromJson(string json)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return serializer.ReadObject(stream) as SerializableInfo;
            }
        }

        public string ToJson()
        {
            string result;
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, this);
                stream.Position = 0;
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
            }
            return result;
        }
#endif

    [DataMember(Name = "title")]
#if NET5_0_OR_GREATER
    [JsonPropertyName("title")]
#endif
    public string Title { get; set; } = string.Empty;
    [DataMember(Name = "id")]
#if NET5_0_OR_GREATER
    [JsonPropertyName("id")]
#endif
    public string Id { get; set; } = string.Empty;
    [DataMember(Name = "description")]
#if NET5_0_OR_GREATER
    [JsonPropertyName("description")]
#endif
    public string Description { get; set; } = string.Empty;
    [DataMember(Name = "portalItemURL")]
#if NET5_0_OR_GREATER
    [JsonPropertyName("portalItemURL")]
#endif
    public Uri? PortalItemUrl { get; set; }

}

/// <summary>
///  A model type that maintains information about an offline map area.
/// </summary>
public class OfflineMapInfo
{
    internal const string offlineMapInfoJsonFile = "info.json";
    internal const string offlineMapInfoThumbnailFile = "thumbnail.png";

    private SerializableInfo _info;

    internal string? InfoFile { get; }

    internal OfflineMapInfo(string infoFile, Esri.ArcGISRuntime.UI.RuntimeImage? image, SerializableInfo info)
    {
        InfoFile = infoFile;
        _info = info;
        Thumbnail = image;
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
    public Uri? PortalItemUrl => _info.PortalItemUrl;

    internal static async Task<OfflineMapInfo> CreateAsync(PortalItem portalItem, string directoryPath)
    {
        var info = new SerializableInfo
        {
            Title = portalItem.Title,
            Id = portalItem.ItemId,
            Description = portalItem.Description,
            PortalItemUrl = portalItem.Url,
        };
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
        string infoFile = Path.Combine(directoryPath, offlineMapInfoJsonFile);
        string? thumbnailFile = null;
        File.WriteAllText(infoFile, info.ToJson());
        if (portalItem.Thumbnail != null)
        {
            using var thumbnailstream = await portalItem.Thumbnail.GetEncodedBufferAsync();
            thumbnailFile = Path.Combine(directoryPath, offlineMapInfoThumbnailFile);
            using var fileStream = File.OpenWrite(thumbnailFile);
            await thumbnailstream.CopyToAsync(fileStream);
        }
        return new OfflineMapInfo(infoFile, portalItem.Thumbnail, info);
    }

    internal static OfflineMapInfo FromFolder(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException(directoryPath);
        string path = Path.Combine(directoryPath, offlineMapInfoJsonFile);
        if(!File.Exists(path))
            throw new FileNotFoundException("Offline map info file not found", path);
        string infoFile = Path.Combine(directoryPath, offlineMapInfoJsonFile);
        var info = SerializableInfo.FromJson(File.ReadAllText(infoFile)) ?? throw new FileLoadException("Failed to read OfflineMapInfo");
        string thumbnailFile = Path.Combine(directoryPath, offlineMapInfoThumbnailFile);
        Esri.ArcGISRuntime.UI.RuntimeImage? thumbNail = null;
        if (File.Exists(thumbnailFile))
        {
            var bytes = File.ReadAllBytes(thumbnailFile);
            thumbNail = new Esri.ArcGISRuntime.UI.RuntimeImage(bytes);
        }
        return new OfflineMapInfo(path, thumbNail, info);
    }

    //internal async Task Save(string directoryPath)
    //{
    //    if (!Directory.Exists(directoryPath))
    //        Directory.CreateDirectory(directoryPath);
    //    File.WriteAllText(Path.Combine(directoryPath, offlineMapInfoJsonFile), _info.ToJson());
    //    if (Thumbnail != null)
    //    {
    //        using var thumbnailstream = await Thumbnail.GetEncodedBufferAsync();
    //        using var fileStream = File.OpenWrite(Path.Combine(directoryPath, offlineMapInfoThumbnailFile));
    //        await thumbnailstream.CopyToAsync(fileStream);
    //    }
    //}

    internal static bool InfoExists(string directoryPath)
    {
        return File.Exists(Path.Combine(directoryPath, offlineMapInfoJsonFile));
    }

    internal static void Delete(string directoryPath)
    {
        File.Delete(Path.Combine(directoryPath, offlineMapInfoJsonFile));
        File.Delete(Path.Combine(directoryPath, offlineMapInfoThumbnailFile));
    }
}