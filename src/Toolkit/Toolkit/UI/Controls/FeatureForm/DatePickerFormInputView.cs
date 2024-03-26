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
    /// Picker for the <see cref="DateTimePickerFormInput"/>.
    /// </summary>
    public partial class DateTimePickerFormInputView
    {
        private WeakEventListener<DateTimePickerFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;

        /// <summary>
        /// Initializes an instance of the <see cref="DateTimePickerFormInputView"/> class.
        /// </summary>
        public DateTimePickerFormInputView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(DateTimePickerFormInputView);
#endif
        }

        private void UpdateValue()
        { 
            if (Element?.Input is DateTimePickerFormInput input && _datePicker != null)
            {
#if MAUI
                var date = _datePicker.Date;
                if (date != DateTime.MinValue && input.IncludeTime && _timePicker != null)
                    date = date.Date.Add(_timePicker.Time);
#else
                var date = _datePicker.SelectedDate;
                if (date.HasValue && input.IncludeTime && _timePicker != null && _timePicker.Time.HasValue)
                {
                    date = date.Value.Date.Add(_timePicker.Time.Value);
                }
#endif
                if (!object.Equals(Element?.Value, date))
                    Element?.UpdateValue(date);
            }
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
            BindableProperty.Create(nameof(Element), typeof(FieldFormElement), typeof(DateTimePickerFormInputView), null, propertyChanged: (s, oldValue, newValue) => ((DateTimePickerFormInputView)s).OnElementPropertyChanged(oldValue as FieldFormElement, newValue as FieldFormElement));
#else
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(DateTimePickerFormInputView), new PropertyMetadata(null, (s, e) => ((DateTimePickerFormInputView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));
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
                _elementPropertyChangedListener = new WeakEventListener<DateTimePickerFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpcNew)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.Element_PropertyChanged(source, eventArgs),
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                inpcNew.PropertyChanged += _elementPropertyChangedListener.OnEvent;
            }
        }

        private void Element_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FieldFormElement.Value))
            {
                Dispatch(ConfigurePickers);
            }
        }

        private void Dispatch(Action action)
        {
#if WPF
            if (Dispatcher.CheckAccess())
                action();
            else
                Dispatcher.Invoke(action);
#elif MAUI
            if (Dispatcher.IsDispatchRequired)
                Dispatcher.Dispatch(action);
            else
                action();
#endif
        }

        private bool _rentrancyFlag;

        private void ConfigurePickers()
        {
            if (_rentrancyFlag) return;
            _rentrancyFlag = true;
            DateTime? selectedDate = Element?.Value as DateTime? ?? (Element?.Value as DateTimeOffset?)?.DateTime;
            if (Element?.Input is DateTimePickerFormInput input)
            {
                if (_datePicker is not null)
                {
#if MAUI
#else
                    _datePicker.SelectedDate = selectedDate;
                    _datePicker.DisplayDateStart = input.Min.HasValue ? input.Min.Value.Date : null;
                    _datePicker.DisplayDateEnd = input.Max.HasValue ? input.Max.Value.Date : null;
#endif
                }
                if (_timePicker != null)
                {
#if MAUI
                    _timePicker.IsVisible = input.IncludeTime;
                    _timePicker.Time = selectedDate.HasValue ? selectedDate.Value.TimeOfDay : TimeSpan.Zero;
#else
                    _timePicker.Visibility = input.IncludeTime ? Visibility.Visible : Visibility.Collapsed;
                    _timePicker.Time = selectedDate.HasValue ? selectedDate.Value.TimeOfDay : null;
#endif
                }
            }
            _rentrancyFlag = false;
        }
    }
}
#endif