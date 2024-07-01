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

// Based on https://github.com/samuelneff/MimeTypeMap/blob/master/MimeTypeMap.cs
// by Samuel Neff, MIT License

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal static class MimeTypeMap
    {
        private static readonly Lazy<IDictionary<string, string>> _mappings = new Lazy<IDictionary<string, string>>(BuildMappings);

        // Based on the following list: https://doc.arcgis.com/en/arcgis-online/manage-data/edit-tables-mv.htm#ESRI_SECTION2_3C99CF74A5C74A0AA0AD4A619F4457D7
        // Note: application/octet-stream types are not included, since they'll use the fallback.
        private static IDictionary<string, string> BuildMappings()
        {
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                {".7z", "application/x-7z-compressed"},
                {".aif", "audio/aiff"},
                {".aifc", "audio/aiff"},
                {".aiff", "audio/aiff"},
                {".avi", "video/x-msvideo"},{".avif", "image/avif"},
                { ".bmp", "image/bmp"},
                { ".csv", "text/csv"},
                { ".doc", "application/msword"},
                { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
                { ".dot", "application/msword"},
                { ".ecw", "image/ecw"},
                { ".emf", "image/emf"},
                { ".eps", "application/postscript"},
                { ".geojson", "application/geo+json"},
                { ".gif", "image/gif"},
                { ".gml", "application/gml+xml"},
                { ".gtar", "application/x-gtar"},
                { ".gz", "application/x-gzip"},
                { ".j2k", "image/j2k"},
                { ".jp2", "image/jp2"},
                { ".jpc", "image/jpc"},
                { ".jpe", "image/jpeg"},
                { ".jpeg", "image/jpeg"},
                { ".jpg", "image/jpeg"},
                { ".json", "application/json"},
                { ".m4a", "audio/m4a"},
                { ".mdb", "application/x-msaccess"},
                { ".mid", "audio/mid"},
                { ".midi", "audio/mid"},
                { ".mov", "video/quicktime"},
                { ".mp2", "video/mpeg"},
                { ".mp2v", "video/mpeg"},
                { ".mp3", "audio/mpeg"},
                { ".mp4", "video/mp4"},
                { ".mp4v", "video/mp4"},
                { ".mpa", "video/mpeg"},
                { ".mpe", "video/mpeg"},
                { ".mpeg", "video/mpeg"},
                { ".mpg", "video/mpeg"},
                { ".mpv2", "video/mpeg"},
                { ".pdf", "application/pdf"},
                { ".png", "image/png"},
                { ".ppt", "application/vnd.ms-powerpoint"},
                { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
                { ".ps", "application/postscript"},
                { ".psd", "application/x-photoshop"},
                { ".qt", "video/quicktime"},
                { ".ra", "audio/x-pn-realaudio"},
                { ".ram", "audio/x-pn-realaudio"},
                { ".rmi", "audio/mid"},
                { ".tar", "application/x-tar"},
                { ".tgz", "application/x-compressed"},
                { ".tif", "image/tiff"},
                { ".tiff", "image/tiff"},
                { ".txt", "text/plain"},
                { ".vrml", "model/x3d-vrml"},
                { ".wav", "audio/wav"},
                { ".wma", "audio/x-ms-wma"},
                { ".wmf", "application/x-msmetafile"},
                { ".wmv", "video/x-ms-wmv"},
                { ".wps", "application/vnd.ms-works"},
                { ".xls", "application/vnd.ms-excel"},
                { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                { ".xlt", "application/vnd.ms-excel"},
                { ".xml", "text/xml"},
                { ".zip", "application/zip"},

                };

            return mappings;
        }

        public static string GetMimeType(string extension)
        {
            if (!string.IsNullOrEmpty(extension))
            {
#if WINDOWS && !WINDOWS_UWP
                string? mimeType = Microsoft.Win32.Registry.GetValue(@"HKEY_CLASSES_ROOT\" + extension, "Content Type", null) as string;
                if (!string.IsNullOrEmpty(mimeType))
                    return mimeType;
#endif
                if (_mappings.Value.TryGetValue(extension, out string? result))
                    return result;
            }
            return "application/octet-stream";
        }
    }
}