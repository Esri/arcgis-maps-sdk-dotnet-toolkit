using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hosting;
using static Microsoft.UI.Reactor.Factories;
using Toolkit.SampleApp.Reactor;
using Esri.ArcGISRuntime.Security;

if (WinUIEx.WebAuthenticator.CheckOAuthRedirectionActivation())
    return;

ReactorApp.Run<GalleryShell>("WinUI Gallery (Reactor)", width: 1200, height: 800,
    configure: host =>
    {
        InitMapsSDK();
        XamlInterop.Register(host.Reconciler);
        Esri.ArcGISRuntime.Toolkit.Reactor.Interop.Register(host.Reconciler);
    });

async void InitMapsSDK()
{
    if (await SecureStorage.GetAsync("APIKey") is string key)
        Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = key;
    else
        AuthenticationManager.Current.Persistence = CredentialPersistence.CreateDefault() ?? new SecureStorageCredentialPersistence();

    AuthenticationManager.Current.OAuthUserConfigurations.Add(
        new OAuthUserConfiguration(
            new Uri("https://www.arcgis.com/portal"),
            clientId: "rdpQIZlaemFowcdn",
            redirectUrl: new Uri("agsreactor://")
           ));
    AuthenticationManager.Current.OAuthHandler = new OAuthHandler();
}

internal class OAuthHandler : IOAuthHandler
{
    public async Task<IDictionary<string, string>> LoginAsync(OAuthLoginParameters parameters)
    {
        Microsoft.Windows.AppLifecycle.ActivationRegistrationManager.RegisterForProtocolActivation("agsreactor", "Assets\\Square150x150Logo.scale-100", "ArcGIS Reactor Sample", null);
        try
        {
            var result = await WinUIEx.WebAuthenticator.AuthenticateAsync(parameters.AuthorizeUri, parameters.RedirectUri);
            return result.Properties;
        }
        finally
        {
            Microsoft.Windows.AppLifecycle.ActivationRegistrationManager.UnregisterForProtocolActivation("agsreactor", null);
        }
    }
}

