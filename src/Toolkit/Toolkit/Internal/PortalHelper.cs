using System.Text.RegularExpressions;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal class PortalHelper
    {
        /// <summary>
        /// Extracts the portal item id from a given Uri. The process of extracting the item id from the portal
        /// item url is non-exhaustive and may not work for all portal item urls. Currently it is meant to work
        /// with web maps and some layers.
        /// </summary>
        public static string? GetPortalItemId(Uri? uri)
        {
            if (uri is null)
                return null;

            // If map isn't loaded yet, look for Item ID parameter in URL and extract it:
            // Possible URLs with item id in different locations. The item id is a lower-case guid (e.g. 55ebf90799fa4a3fa57562700a68c405):
            // https://www.arcgis.com/apps/mapviewer/index.html?webmap=55ebf90799fa4a3fa57562700a68c405
            // https://www.arcgis.com/home/webmap/viewer.html?webmap=55ebf90799fa4a3fa57562700a68c405
            // https://www.arcgis.com/home/item.html?id=55ebf90799fa4a3fa57562700a68c405
            // https://www.arcgis.com/sharing/rest/content/items/55ebf90799fa4a3fa57562700a68c405/data
            // https://www.arcgis.com/sharing/rest/content/items/55ebf90799fa4a3fa57562700a68c405?f=json
            var itemId = System.Web.HttpUtility.ParseQueryString(uri.Query)["id"]
                ?? System.Web.HttpUtility.ParseQueryString(uri.Query)["webmap"];
            if (string.IsNullOrEmpty(itemId))
            {
                // If not found in query parameters, attempt to extract from path segment after items:
                itemId = Regex.Match(uri.AbsolutePath, @"(?:/items/|/id/)([a-f0-9]{32})(?:/|$)", RegexOptions.IgnoreCase).Groups[1].Value;
            }
            return itemId;
        }
    }
}
