using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hosting;
using static Microsoft.UI.Reactor.Factories;
using Toolkit.SampleApp.Reactor;

ReactorApp.Run<GalleryShell>("WinUI Gallery (Reactor)", width: 1400, height: 900,
    configure: host =>
    {
        XamlInterop.Register(host.Reconciler);
        Esri.ArcGISRuntime.Toolkit.Reactor.Factories.Register(host.Reconciler);
    });
