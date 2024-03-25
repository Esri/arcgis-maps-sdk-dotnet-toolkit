#if WPF || MAUI
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Checkbox switch for the <see cref="SwitchFormInput"/>.
    /// </summary>
    public partial class SwitchFormInputView
    {
        private WeakEventListener<SwitchFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;

        /// <summary>
        /// Initializes an instance of the <see cref="SwitchFormInputView"/> class.
        /// </summary>
        public SwitchFormInputView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(SwitchFormInputView);
#endif
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
#if MAUI
        public static readonly BindableProperty ElementProperty =
            BindableProperty.Create(nameof(Element), typeof(FieldFormElement), typeof(SwitchFormInputView), null, propertyChanged: (s, oldValue, newValue) => ((SwitchFormInputView)s).OnElementPropertyChanged(oldValue as FieldFormElement, newValue as FieldFormElement));
#else
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(SwitchFormInputView), new PropertyMetadata(null, (s, e) => ((SwitchFormInputView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));
#endif

        private void OnElementPropertyChanged(FieldFormElement? oldValue, FieldFormElement? newValue)
        {
            if (oldValue is INotifyPropertyChanged inpcOld)
            {
                _elementPropertyChangedListener?.Detach();
                _elementPropertyChangedListener = null;
            }
            if (newValue is INotifyPropertyChanged inpcNew)
            {
                _elementPropertyChangedListener = new WeakEventListener<SwitchFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpcNew)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.Element_PropertyChanged(source, eventArgs),
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                inpcNew.PropertyChanged += _elementPropertyChangedListener.OnEvent;
            }
            UpdateCheckState();
#if MAUI && WINDOWS
            UpdateOnOffContent();
#endif
        }

        private void Element_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FieldFormElement.Value))
            {
#if MAUI
                Dispatcher.Dispatch(UpdateCheckState);
#else
                _ = Dispatcher.InvokeAsync(UpdateCheckState);
#endif
            }
            
        }

        private void Checked()
        {
            if (Element is not null && Element.Input is SwitchFormInput input && !object.Equals(input.OnValue.Code, Element.Value))
            {
                Element.UpdateValue(input.OnValue.Code);
            }
        }

        private void Unchecked()
        {
            if (Element is not null && Element.Input is SwitchFormInput input && !object.Equals(input.OffValue.Code, Element.Value))
            {
                Element.UpdateValue(input.OffValue.Code);
            }
        }

        private void UpdateCheckState()
        {
            if (Element is not null && Element.Input is SwitchFormInput input)
            {
                IsChecked = object.Equals(input.OnValue.Code, Element.Value);
            }
            else
            {
#if MAUI
                IsChecked = false;
#else
                IsChecked = null;
#endif
            }
        }
    }
}
#endif