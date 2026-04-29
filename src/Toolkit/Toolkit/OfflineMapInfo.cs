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
using Esri.ArcGISRuntime.UI;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.Toolkit
{
    /// <summary>
    /// Information for an online map that has been taken offline.
    /// </summary>
    /// <remarks>
    /// When a map is taken offline, map information needs to be saved on device
    /// so that the map can be reloaded later when the device is offline. It
    /// provides access to and also enables display locally saved map information
    /// about downloaded map areas. This type is typically used when there is no network connection.
    /// </remarks>
    public sealed class OfflineMapInfo
    {
        private const string ThumbnailFileName = "thumbnail.bin";

        private OfflineMapInfo(SerializableOfflineMapInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.PortalItemId))
            {
                throw new ArgumentException("Portal item ID cannot be null or whitespace.", nameof(info));
            }

            if (!Uri.TryCreate(info.PortalItemUrl, UriKind.Absolute, out var portalItemUrl))
            {
                throw new ArgumentException("Portal item URL must be an absolute URI.", nameof(info));
            }

            Id = info.PortalItemId;
            Title = info.Title;
            Description = info.Description;
            PortalItemUrl = portalItemUrl;
            ThumbnailData = info.ThumbnailData;
        }

        /// <summary>
        /// Gets the portal item ID associated with the map.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the title of the portal item associated with the map.
        /// </summary>
        public string? Title { get; }

        /// <summary>
        /// Gets the description of the portal item associated with the map.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the URL of the portal item associated with the map.
        /// </summary>
        public Uri PortalItemUrl { get; }

        /// <summary>
        /// Gets the thumbnail data for the portal item associated with the map.
        /// </summary>
        public byte[]? ThumbnailData { get; }

        private string InfoFileName => $"{Id}.json";

        internal static async Task<OfflineMapInfo?> CreateAsync(PortalItem portalItem)
        {
            if (portalItem is null)
            {
                throw new ArgumentNullException(nameof(portalItem));
            }

            if (string.IsNullOrWhiteSpace(portalItem.ItemId) || portalItem.Url is null)
            {
                return null;
            }

            byte[]? thumbnailData = null;
            if (portalItem.Thumbnail is not null)
            {
                try
                {
                    if (portalItem.Thumbnail.LoadStatus != LoadStatus.Loaded)
                    {
                        await portalItem.Thumbnail.LoadAsync().ConfigureAwait(false);
                    }

                    if (portalItem.Thumbnail.LoadStatus == LoadStatus.Loaded)
                    {
                        using var thumbnailStream = await portalItem.Thumbnail.GetEncodedBufferAsync().ConfigureAwait(false);
                        thumbnailData = new byte[thumbnailStream.Length];
                        await thumbnailStream.ReadExactlyAsync(thumbnailData).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error loading offline map thumbnail: {ex}", "ArcGIS Maps SDK Toolkit");
                }
            }

            return new OfflineMapInfo(
                new SerializableOfflineMapInfo
                {
                    PortalItemId = portalItem.ItemId!,
                    Title = portalItem.Title,
                    Description = portalItem.Description,
                    PortalItemUrl = portalItem.Url.AbsoluteUri,
                    ThumbnailData = thumbnailData
                });
        }

        internal static OfflineMapInfo? FromFile(string infoPath)
        {
            if (string.IsNullOrWhiteSpace(infoPath))
            {
                throw new ArgumentException("File cannot be null or whitespace.", nameof(infoPath));
            }

            if (!File.Exists(infoPath))
            {
                return null;
            }

            var json = File.ReadAllText(infoPath);
            var info = SerializableOfflineMapInfo.FromJson(json);
            if (info is null)
            {
                return null;
            }

            return new OfflineMapInfo(info);
        }

        internal async Task SaveToDirectoryAsync(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Directory cannot be null or whitespace.", nameof(directory));
            }

            var infoPath = Path.Combine(directory, InfoFileName);
            var info = new SerializableOfflineMapInfo
            {
                PortalItemId = Id,
                Title = Title,
                Description = Description,
                PortalItemUrl = PortalItemUrl.AbsoluteUri,
            };

            await using (var fileStream = File.Create(infoPath))
            {
                var json = info.ToJson();
                await fileStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(json)).ConfigureAwait(false);
                await fileStream.FlushAsync().ConfigureAwait(false);
            }
        }

        internal static void RemoveFromDirectory(string directory, string portalId)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Directory cannot be null or whitespace.", nameof(directory));
            }

            var infoPath = Path.Combine(directory, portalId + ".json");
            if (File.Exists(infoPath))
            {
                File.Delete(infoPath);
            }
        }

        internal partial class SerializableOfflineMapInfo
        {
            public static SerializableOfflineMapInfo? FromJson(string json) => JsonSerializer.Deserialize(json, typeof(SerializableOfflineMapInfo), InfoSerializationContext.Default) as SerializableOfflineMapInfo;

            public string ToJson() => JsonSerializer.Serialize(this, InfoSerializationContext.Default.SerializableOfflineMapInfo);

            [JsonPropertyName("description")]
            public string? Description { get; set; }
            [JsonPropertyName("id")]
            public string? PortalItemId { get; set; }
            [JsonPropertyName("portalItemURL")]
            public string? PortalItemUrl { get; set; }
            [JsonPropertyName("title")]
            public string? Title { get; set; }
            [JsonPropertyName("thumbnailData")]
            public byte[]? ThumbnailData { get; set; }
        }
    }


    [JsonSerializable(typeof(OfflineMapInfo.SerializableOfflineMapInfo))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    internal partial class InfoSerializationContext : JsonSerializerContext { }
}
