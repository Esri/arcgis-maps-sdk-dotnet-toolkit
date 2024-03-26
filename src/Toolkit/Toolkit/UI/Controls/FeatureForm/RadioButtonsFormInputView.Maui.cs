#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Radio button view for the <see cref="RadioButtonsFormInput"/>.
    /// </summary>
    public partial class RadioButtonsFormInputView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        private System.Collections.IEnumerable? ItemsSource; //TODO
        private object? SelectedItem; //TODO
        private int SelectedIndex; //TODO

        static RadioButtonsFormInputView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            Label view = new Label() { Text = "Radio Buttons Goes Here" };
#warning TODO
            //view.SetBinding(Switch.IsEnabledProperty, "Element.IsEditable");
            //INameScope nameScope = new NameScope();
            //NameScope.SetNameScope(view, nameScope);
            //nameScope.RegisterName(ViewName, view);
            return view;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateItems();
        }



    }
}
#endif