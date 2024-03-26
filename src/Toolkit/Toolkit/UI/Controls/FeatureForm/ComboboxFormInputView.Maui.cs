#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Checkbox switch for the <see cref="ComboBoxFormInput"/>.
    /// </summary>
    public partial class ComboBoxFormInputView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        private Picker? _selector;

        static ComboBoxFormInputView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            Label view = new Label() { Text = "ComboBox Goes Here" };
#warning TODO
            //view.SetBinding(Switch.IsEnabledProperty, "Element.IsEditable");
            //INameScope nameScope = new NameScope();
            //NameScope.SetNameScope(view, nameScope);
            //nameScope.RegisterName(ViewName, view);
            return view;
        }
    }
}
#endif