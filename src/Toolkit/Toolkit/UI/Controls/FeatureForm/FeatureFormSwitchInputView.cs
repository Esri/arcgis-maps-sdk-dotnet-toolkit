#if WPF
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Checkbox switch for the <see cref="SwitchFormInput"/>.
    /// </summary>
    public class FeatureFormSwitchInputView : CheckBox
    {
        /// <summary>
        /// Initializes an instance of the <see cref="FeatureFormSwitchInputView"/> class.
        /// </summary>
        public FeatureFormSwitchInputView()
        {
            DefaultStyleKey = typeof(FeatureFormSwitchInputView);
        }

        /// <summary>
        /// Gets or sets the FieldFormElement.
        /// </summary>
        public FieldFormElement? Element
        {
            get { return (FieldFormElement)GetValue(ElementProperty); }
            set { SetValue(ElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(FeatureFormSwitchInputView), new PropertyMetadata(null, (s, e) => ((FeatureFormSwitchInputView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));

        private void OnElementPropertyChanged(FieldFormElement? oldValue, FieldFormElement? newValue)
        {
            if (oldValue is INotifyPropertyChanged inpcOld)
            {
                inpcOld.PropertyChanged += FeatureFormTextInputView_PropertyChanged;
            }
            if (newValue is INotifyPropertyChanged inpcNew)
            {
                inpcNew.PropertyChanged += FeatureFormTextInputView_PropertyChanged;
            }
            UpdateCheckState();
        }

        private void FeatureFormTextInputView_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FieldFormElement.Value))
            {
                UpdateCheckState();
            }
        }

        /// <inheritdoc/>
        protected override void OnChecked(RoutedEventArgs e)
        {
            if (Element is not null && Element.Input is SwitchFormInput input && !object.Equals(input.OnValue.Code, Element.Value))
            {
                Element.UpdateValue(input.OnValue.Code);
            }
            base.OnChecked(e);
        }

        /// <inheritdoc/>
        protected override void OnUnchecked(RoutedEventArgs e)
        {
            if (Element is not null && Element.Input is SwitchFormInput input && !object.Equals(input.OnValue.Code, Element.Value))
            {
                Element.UpdateValue(input.OffValue.Code);
            }
            base.OnUnchecked(e);
        }

        private void UpdateCheckState()
        {
            if (Element is not null && Element.Input is SwitchFormInput input)
            {
                IsChecked = object.Equals(input.OnValue.Code, Element.Value);
            }
            else
            {
                IsChecked = null;
            }
        }
    }
}
#endif