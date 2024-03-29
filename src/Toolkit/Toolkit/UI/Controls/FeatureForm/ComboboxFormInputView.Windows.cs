﻿#if WPF
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    [TemplatePart(Name ="Selector", Type = typeof(System.Windows.Controls.Primitives.Selector))]
    public partial class ComboBoxFormInputView : Control
    {
        private System.Windows.Controls.Primitives.Selector? _selector;


        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_selector != null)
            {
                _selector.SelectionChanged -= Selector_SelectionChanged;
            }
            _selector = GetTemplateChild("Selector") as System.Windows.Controls.Primitives.Selector;
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