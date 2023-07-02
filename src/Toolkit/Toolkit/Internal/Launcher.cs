using System;
using System.Collections.Generic;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal static class Launcher
    {
        public static
            Task<bool> LaunchUriAsync(Uri? uri)
        {
            if (uri is null)
            {
                return Task.FromResult(false);
            }
#if NET6_0_OR_GREATER && WINDOWS || NETFX_CORE
            return Windows.System.Launcher.LaunchUriAsync(uri).AsTask();
#elif WINDOWS
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "rundll32.exe";
            process.StartInfo.Arguments = "url.dll,FileProtocolHandler " + uri.OriginalString;
            process.StartInfo.UseShellExecute = true;
            return Task.FromResult(process.Start());
#elif MAUI
            return Microsoft.Maui.ApplicationModel.Launcher.Default.TryOpenAsync(uri);
#endif
        }
    }
}
