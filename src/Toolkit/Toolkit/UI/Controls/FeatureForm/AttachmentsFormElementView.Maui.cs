#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class AttachmentsFormElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static AttachmentsFormElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.AttachmentsFormElement.Attachments), "Esri.ArcGISRuntime.Mapping.FeatureForms.AttachmentsFormElement", "Esri.ArcGISRuntime")]
        private static object BuildDefaultTemplate()
        {
            Grid view = new Grid();
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(view, nameScope);
            nameScope.RegisterName("Root", view);
            return view;
        }
    }
}
#endif