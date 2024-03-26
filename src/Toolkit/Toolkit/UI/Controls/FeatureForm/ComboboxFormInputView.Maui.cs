#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
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
            Picker view = new Picker();
            view.SetBinding(Picker.IsEnabledProperty, "Element.IsEditable");
            view.ItemDisplayBinding = new Binding(nameof(CodedValue.Name));
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(view, nameScope);
            nameScope.RegisterName("Picker", view);
            return view;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            if (_selector is not null)
            {
                _selector.SelectedIndexChanged -= Picker_SelectedIndexChanged;
            }
            _selector = GetTemplateChild("Picker") as Picker;
            if (_selector is not null)
            {
                _selector.SelectedIndexChanged += Picker_SelectedIndexChanged;
            }
            base.OnApplyTemplate();
        }

        private void Picker_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var value = (_selector?.SelectedItem as CodedValue)?.Code;
            Element?.UpdateValue(value);
        }
    }
}
#endif