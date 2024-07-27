#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
#if WPF
using System.Windows.Controls.Primitives;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    [TemplatePart(Name ="Selector", Type = typeof(Selector))]
    public partial class ComboBoxFormInputView : Control
    {
        private Selector? _selector;

        /// <inheritdoc />
#if WPF
        public override void OnApplyTemplate()
#else
        protected override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            if (_selector != null)
            {
                _selector.SelectionChanged -= Selector_SelectionChanged;
            }
            _selector = GetTemplateChild("Selector") as Selector;
            if(_selector != null)
            {
                _selector.SelectionChanged += Selector_SelectionChanged;
            }
            UpdateItems();
        }

        private void Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var value = (_selector?.SelectedItem as CodedValue)?.Code;
            Element?.UpdateValue(value);
        }
    }
}
#endif