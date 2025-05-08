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
                _selector.GotFocus -= Selector_GotFocus;
            }
            _selector = GetTemplateChild("Selector") as Selector;
            if(_selector != null)
            {
                _selector.SelectionChanged += Selector_SelectionChanged;
                _selector.GotFocus += Selector_GotFocus;
            }
            UpdateItems();
        }

        private void Selector_GotFocus(object sender, RoutedEventArgs e) => UI.Controls.FeatureFormView.GetParent<FieldFormElementView>(this)?.OnGotFocus();

        private void Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
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