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
            if (_rentrancyFlag) return;
            if (Element?.Input is DateTimePickerFormInput input && _datePicker != null)
            {
                TimeSpan time = TimeSpan.Zero;
#if MAUI
                // "No Value" handling for MAUI, where picker's DateTime cannot be set
                if (_hasValueButton is not null && !_hasValueButton.IsToggled)
                {
                    if (Element?.Value is not null)
                        Element?.UpdateValue(null);
                    return;
                }
                var date = _datePicker.Date; // local
                if (input.IncludeTime && _timePicker != null)
                    time = _timePicker.Time;
#else
                var maybeDate = _datePicker.SelectedDate; // local
                if (maybeDate is not DateTime date) // "Not set" represented by null on WPF
                {
                    if (Element?.Value is not null)
                        Element?.UpdateValue(null);
                    return;
                }
                if (input.IncludeTime && _timePicker != null && _timePicker.Time.HasValue)
                    time = _timePicker.Time.Value;
#endif
                DateTime utcNew;
                if (input.IncludeTime)
                {
                    utcNew = date.Add(time).ToUniversalTime();
                }
                else
                {
                    // Truncate time component again after converting "local date" to "UTC date"
                    utcNew = date.ToUniversalTime().Date;
                }

                if (Element?.Value is DateTimeOffset attrDto)
                {
                    // Attribute value may be a DateTimeOffset (pre-200.4 or EnableTimestampOffsetSupport=false)
                    var utcOld = DateTime.SpecifyKind(attrDto.ToUniversalTime().DateTime, DateTimeKind.Utc);
                    if (utcOld == utcNew)
                        return;
                }
                else if (Element?.Value is DateTime attrDt)
                {
                    if (attrDt == utcNew)
                        return;
                }
                Element?.UpdateValue(utcNew);
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
            ConfigurePickers();
        }

        private void Element_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FieldFormElement.Value))
            {
                this.Dispatch(ConfigurePickers);
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
                // Dates are always converted to local time, even if IncludeTime is false
                selectedDate = selectedDate?.ToLocalTime();
                if (_datePicker is not null)
                {
#if MAUI
                    // "No Value" has to be tracked separately on MAUI because picker's DateTime is not nullable
                    // See https://github.com/dotnet/maui/issues/1100
                    if (_hasValueButton is not null)
                    {
                        _hasValueButton.IsToggled = selectedDate is not null;
                    }

                    // Min/Max are always converted to local time
                    _datePicker.MinimumDate = input.Min?.ToLocalTime().Date ?? DateTime.MinValue;
                    _datePicker.MaximumDate = input.Max?.ToLocalTime().Date ?? DateTime.MaxValue;

                    _datePicker.Date = selectedDate ?? _datePicker.MinimumDate;
                    _datePicker.IsEnabled = selectedDate is not null;
#else
                    _datePicker.SelectedDate = selectedDate;
                    _datePicker.DisplayDateStart = input.Min?.ToLocalTime().Date;
                    _datePicker.DisplayDateEnd = input.Max?.ToLocalTime().Date;
#endif
                }
                if (_timePicker != null)
                {
#if MAUI
                    _timePicker.IsVisible = input.IncludeTime;
                    _timePicker.Time = selectedDate?.TimeOfDay ?? TimeSpan.Zero;
                    _timePicker.IsEnabled = selectedDate is not null;
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