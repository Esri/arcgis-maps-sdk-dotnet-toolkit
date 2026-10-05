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

#if WPF || WINDOWS_XAML || __ANDROID__ || __IOS__ || (MAUI && WINDOWS)
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

// Reads an image for the panorama decoders. After a load, an oriented image's data URI is a local file, so the web
// branch is a fallback.
internal static class PanoramaImageFetcher
{
    // A local image's path, or the bytes of a web image.
    internal static async Task<(string? Path, byte[]? Bytes)> FetchAsync(Uri uri, CancellationToken token)
    {
        if (uri.IsFile)
            return (uri.LocalPath, null);

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new NotSupportedException($"Images can't be read from '{uri.Scheme}' locations.");

        using var httpClient = new System.Net.Http.HttpClient();
        return (null, await httpClient.GetByteArrayAsync(uri, token));
    }
}
#endif
