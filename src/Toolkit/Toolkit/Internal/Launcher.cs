using System;
using System.Collections.Generic;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal static class Launcher
    {
        public static async Task<bool> LaunchUriAsync(Uri uri)
        {
#if NET6_0_OR_GREATER && WINDOWS || NETFX_CORE
            return await Windows.System.Launcher.LaunchUriAsync(uri);
#elif WINDOWS
            Process.Start(uri.OriginalString);
            return true;
#elif __IOS__ || __ANDROID__
            return await Microsoft.Maui.ApplicationModel.Launcher.Default.TryOpenAsync(uri);
#endif

        }
    }
}
