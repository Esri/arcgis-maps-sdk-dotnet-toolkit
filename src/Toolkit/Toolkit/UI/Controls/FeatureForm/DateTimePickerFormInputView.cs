#if WPF || MAUI
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using System.Globalization;

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
                // Get the Date from the date picker (null if the input is empty).
                // We display dates in local time but store them in UTC.
                DateTime? date = null;
#if MAUI
                // MAUI's DatePicker does not have a way to determine if it's empty. Check the platform control instead.
#if WINDOWS
                if (_datePicker.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.CalendarDatePicker winPicker)
                {
                    date = winPicker.Date?.UtcDateTime;
                }
#elif IOS || MACCATALYST || ANDROID
                date = _datePicker.Date.ToUniversalTime();
                if (_datePicker.Handler?.PlatformView is Microsoft.Maui.Platform.MauiDatePicker nativePicker && String.IsNullOrEmpty(nativePicker.Text))
                {
                    date = null;
                }
#endif
#else
                date = _datePicker.SelectedDate?.ToUniversalTime();
#endif
                if (date is DateTime newDate && input.IncludeTime && _timePicker?.Time is TimeSpan time)
                {
                    // User specified both date and time, combine them.
                    date = newDate.Add(time);
                }

                DateTime? oldDate = Element.Value as DateTime?;
                if (Element.Value is DateTimeOffset oldDto)
                {
                    // Old attribute value may be a DateTimeOffset (pre-200.4 or EnableTimestampOffsetSupport=false).
                    // Convert it to UTC DateTime before comparing.
                    oldDate = DateTime.SpecifyKind(oldDto.ToUniversalTime().DateTime, DateTimeKind.Utc);
                }

                // Only update the value if:
                // - the date has changed, or
                // - the value has changed to/from null, or
                // - the time has changed and IncludeTime is true
                if (date != oldDate && (date == null || oldDate == null || date != oldDate.Value.Date || input.IncludeTime))
                {
                    Element.UpdateValue(date);
                }
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
            else if (e.PropertyName == nameof(FieldFormElement.IsEditable))
            {
                this.Dispatch(ConfigurePickerVisibility);
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
                    // Min/Max are always converted to local time
                    _datePicker.MinimumDate = input.Min?.ToLocalTime().Date ?? DateTime.MinValue;
                    _datePicker.MaximumDate = input.Max?.ToLocalTime().Date ?? DateTime.MaxValue;
#if WINDOWS
                    if (selectedDate is DateTime date)
                    {
                        _datePicker.Date = selectedDate.Value;
                    }
                    else if (_datePicker.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.CalendarDatePicker winPicker)
                    {
                        winPicker.Date = selectedDate;
                    }
#elif IOS || MACCATALYST || ANDROID
                    if (selectedDate is DateTime date)
                    {
                        _datePicker.Date = date;
                    }
                    else if (_datePicker.Handler?.PlatformView is Microsoft.Maui.Platform.MauiDatePicker nativePicker)
                    {
                        nativePicker.Text = null;
                    }
#endif
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
#else
                    _timePicker.Visibility = input.IncludeTime ? Visibility.Visible : Visibility.Collapsed;
                    _timePicker.Time = selectedDate.HasValue ? selectedDate.Value.TimeOfDay : null;
#endif
                }
                ConfigurePickerVisibility();
            }
            _rentrancyFlag = false;
        }

        private void ConfigurePickerVisibility()
        {
            if (Element != null)
            {
                if (_datePicker != null)
                    _datePicker.IsEnabled = Element.IsEditable;
                if (_timePicker != null)
                    _timePicker.IsEnabled = Element.IsEditable && Element.Value != null;
#if MAUI && (IOS || MACCATALYST || ANDROID)
                if (_clearButton != null)
                    _clearButton.IsVisible = Element.IsEditable && Element.Value != null;
#endif
            }
        }
    }
}
#endif