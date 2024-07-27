// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

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
#elif IOS || ANDROID
                if (_datePicker.Handler?.PlatformView is not Microsoft.Maui.Platform.MauiDatePicker nativePicker || !String.IsNullOrEmpty(nativePicker.Text))
                {
                    date = _datePicker.Date.ToUniversalTime();
                }
#elif MACCATALYST
                if (_hasValueSwitch?.IsToggled != false)
                {
                    date = _datePicker.Date.ToUniversalTime();
                }
#endif
#elif WINDOWS_XAML
                var doffset = _datePicker.SelectedDate?.ToUniversalTime();
                if (doffset.HasValue)
                    date = new DateTime(doffset.Value.Ticks, DateTimeKind.Utc);
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
                this.Dispatch(ConfigurePickerIsEditable);
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
                if (_datePicker != null)
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
#elif IOS || ANDROID
                    if (selectedDate is DateTime date)
                    {
                        _datePicker.Date = date;
                    }
                    else if (_datePicker.Handler?.PlatformView is Microsoft.Maui.Platform.MauiDatePicker nativePicker)
                    {
                        nativePicker.Text = null;
                    }
#elif MACCATALYST
                    if (selectedDate is DateTime date)
                    {
                        _datePicker.Date = date;
                    }
                    if (_hasValueSwitch != null)
                    {
                        _hasValueSwitch.IsToggled = (selectedDate != null);
                    }
#endif
#else
                    _datePicker.SelectedDate = selectedDate;
#if WINDOWS_XAML
                    _datePicker.MinYear = input.Min?.ToLocalTime() ?? DateTimeOffset.MinValue;
                    _datePicker.MaxYear = input.Max?.ToLocalTime() ?? DateTimeOffset.MaxValue;
#else
                    _datePicker.DisplayDateStart = input.Min?.ToLocalTime().Date;
                    _datePicker.DisplayDateEnd = input.Max?.ToLocalTime().Date;
#endif
#endif
                }
                if (_timePicker != null)
                {
#if MAUI
                    _timePicker.IsVisible = input.IncludeTime;
                    _timePicker.Time = selectedDate?.TimeOfDay ?? TimeSpan.Zero;
#else
                    _timePicker.Visibility = input.IncludeTime ? Visibility.Visible : Visibility.Collapsed;
#if WINDOWS_XAML
                    _timePicker.SelectedTime = selectedDate.HasValue ? selectedDate.Value.TimeOfDay : null;
#else
                    _timePicker.Time = selectedDate.HasValue ? selectedDate.Value.TimeOfDay : null;
#endif
#endif
                }
                ConfigurePickerIsEditable();
            }
            _rentrancyFlag = false;
        }

        private void ConfigurePickerIsEditable()
        {
            if (Element != null)
            {
#if MACCATALYST
                if (_hasValueSwitch != null)
                    _hasValueSwitch.IsEnabled = Element.IsEditable;
                if (_datePicker != null)
                    _datePicker.IsEnabled = Element.IsEditable && Element.Value != null; // On Mac, picker is only enabled when the switch is on
#else
                if (_datePicker != null)
                    _datePicker.IsEnabled = Element.IsEditable;
#endif
                if (_timePicker != null)
                    _timePicker.IsEnabled = Element.IsEditable && Element.Value != null;
#if IOS || ANDROID
                if (_clearButton != null)
                    _clearButton.IsVisible = Element.IsEditable && Element.Value != null;
#endif
            }
        }
    }
}