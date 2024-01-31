#if WPF
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Checkbox switch for the <see cref="SwitchFormInput"/>.
    /// </summary>
    public class SwitchFormInputView : CheckBox
    {
        /// <summary>
        /// Initializes an instance of the <see cref="SwitchFormInputView"/> class.
        /// </summary>
        public SwitchFormInputView()
        {
            DefaultStyleKey = typeof(SwitchFormInputView);
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
            DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(SwitchFormInputView), new PropertyMetadata(null, (s, e) => ((SwitchFormInputView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));

        private void OnElementPropertyChanged(FieldFormElement? oldValue, FieldFormElement? newValue)
        {
            if (oldValue is INotifyPropertyChanged inpcOld)
            {
                inpcOld.PropertyChanged += Element_PropertyChanged;
            }
            if (newValue is INotifyPropertyChanged inpcNew)
            {
                inpcNew.PropertyChanged += Element_PropertyChanged;
            }
            UpdateCheckState();
        }

        private void Element_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FieldFormElement.Value))
            {            
                if (Dispatcher.CheckAccess())
                    UpdateCheckState();
                else
                    Dispatcher.Invoke(UpdateCheckState);
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
            if (Element is not null && Element.Input is SwitchFormInput input && !object.Equals(input.OffValue.Code, Element.Value))
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