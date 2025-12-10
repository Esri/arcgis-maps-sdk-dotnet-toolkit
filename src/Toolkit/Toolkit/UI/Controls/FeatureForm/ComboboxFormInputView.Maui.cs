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
            view.SetBinding(Picker.IsEnabledProperty, static (ComboBoxFormInputView view) => view.Element?.IsEditable, source: RelativeBindingSource.TemplatedParent, converter: BoolOrNullToBoolConverter.Instance);
            view.ItemDisplayBinding = new Binding(nameof(CodedValue.Name));
            view.Focused += static (s, e) => FeatureFormView.GetParent<FieldFormElementView>(s as Element)?.OnGotFocus();
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
            if(_selector?.SelectedItem is not ComboBoxPlaceHolderValue && _selector?.ItemsSource is IList<object> list && list.LastOrDefault() is ComboBoxPlaceHolderValue)
            {
                // Remove placeholder if it isn't selected any longer
                list.RemoveAt(list.Count - 1);
            }
            if (_selector?.SelectedItem is ComboBoxPlaceHolderValue)
                return;
            var value = (_selector?.SelectedItem as CodedValue)?.Code;
            Element?.UpdateValue(value);
        }
    }
}
#endif