#if WPF
using Esri.ArcGISRuntime.Mapping.FeatureForms;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Picker for the <see cref="DateTimePickerFormInput"/>.
    /// </summary>
    [TemplatePart(Name = "DatePicker", Type = typeof(DatePicker))]
    public class DateTimePickerFormInputView : Control
    {
        private DatePicker? _datePicker;
        private TimePicker? _timePicker;

        /// <summary>
        /// Initializes an instance of the <see cref="DateTimePickerFormInputView"/> class.
        /// </summary>
        public DateTimePickerFormInputView()
        {
            DefaultStyleKey = typeof(DateTimePickerFormInputView);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _datePicker = GetTemplateChild("DatePicker") as DatePicker;
            _timePicker = GetTemplateChild("TimePicker") as TimePicker;
            if (_datePicker != null)
            {
                _datePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            }
            if (_timePicker != null)
            {
                _timePicker.TimeChanged += TimePicker_TimeChanged;
            }
            ConfigurePickers();
        }

        private void TimePicker_TimeChanged(object? sender, EventArgs e) => UpdateValue();

        private void DatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e) => UpdateValue();

        private void UpdateValue()
        { 
            if (Element?.Input is DateTimePickerFormInput input && _datePicker != null)
            {
                var date = _datePicker.SelectedDate;
                if(date.HasValue && input.IncludeTime && _timePicker != null && _timePicker.Time.HasValue)
                {
                    date = date.Value.Subtract(date.Value.TimeOfDay).Add(_timePicker.Time.Value);
                }
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
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(DateTimePickerFormInputView), new PropertyMetadata(null, (s, e) => ((DateTimePickerFormInputView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));

        private void OnElementPropertyChanged(FieldFormElement? oldValue, FieldFormElement? newValue)
        {
            if (newValue is not null)
            {
                ((System.ComponentModel.INotifyPropertyChanged)newValue).PropertyChanged += Element_PropertyChanged;
            }
        }

        private void Element_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FieldFormElement.Value))
            {
                if (Dispatcher.CheckAccess())
                    ConfigurePickers();
                else
                    Dispatcher.Invoke(ConfigurePickers);
            }
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
                    _datePicker.SelectedDate = selectedDate;
                    _datePicker.DisplayDateStart = input.Min.HasValue ? input.Min.Value.Date : null;
                    _datePicker.DisplayDateEnd = input.Max.HasValue ? input.Max.Value.Date : null;
                }
                if (_timePicker != null)
                {
                    _timePicker.Visibility = input.IncludeTime ? Visibility.Visible : Visibility.Collapsed;
                    _timePicker.Time = selectedDate.HasValue ? selectedDate.Value.TimeOfDay : null;
                }
            }
            _rentrancyFlag = false;
        }
    }
}
#endif