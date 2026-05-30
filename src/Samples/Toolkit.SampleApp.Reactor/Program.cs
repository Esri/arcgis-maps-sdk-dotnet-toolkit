using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hosting;
using static Microsoft.UI.Reactor.Factories;
using Toolkit.SampleApp.Reactor;

ReactorApp.Run<GalleryShell>("WinUI Gallery (Reactor)", width: 1400, height: 900,
    configure: host =>
    {
        Init();
        XamlInterop.Register(host.Reconciler);
        Esri.ArcGISRuntime.Toolkit.Reactor.Interop.Register(host.Reconciler);
    });

async void Init()
{
    if (await SecureStorage.GetAsync("APIKey") is string key)
        Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = key;
}
