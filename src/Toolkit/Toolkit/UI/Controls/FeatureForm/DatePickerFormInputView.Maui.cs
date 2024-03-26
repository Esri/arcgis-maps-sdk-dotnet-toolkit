#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Picker for the <see cref="DateTimePickerFormInput"/>.
    /// </summary>
    public partial class DateTimePickerFormInputView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private DatePicker? _datePicker;
        private TimePicker? _timePicker;
        static DateTimePickerFormInputView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            Label view = new Label() { Text = "DateTime Picker Goes Here" };
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