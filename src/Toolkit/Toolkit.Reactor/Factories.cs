using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Core.V1Protocol;
using Microsoft.UI.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.Reactor;

/// <summary>
/// Provides factory and fluent extension methods for Reactor elements backed by ArcGIS Maps SDK for .NET controls.
/// </summary>
public static partial class Factories
{
    internal static void Register<TElement, TControl>(Func<IElementHandler<TElement, TControl>> handlerFactory)
        where TElement : Element
        where TControl : UIElement
    {
        _ = MetadataRegistration.Done;
        ControlRegistry.Register(handlerFactory);
    }

    private static class MetadataRegistration
    {
        internal static readonly byte Done = Register();

        private static byte Register()
        {
            ReactorApp.RegisterControlAssembly(new Esri_ArcGISRuntime_WinUI_XamlTypeInfo.XamlMetaDataProvider());
            ReactorApp.RegisterControlAssembly(new Esri_ArcGISRuntime_Toolkit_WinUI_XamlTypeInfo.XamlMetaDataProvider());
            return 1;
        }
    }
}
