#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    [TemplatePart(Name = "DatePicker", Type = typeof(DatePicker))]
    public partial class DateTimePickerFormInputView : Control
    {
        private DatePicker? _datePicker;
        private TimePicker? _timePicker;


        /// <inheritdoc />
#if WPF
        public override void OnApplyTemplate()
#else
        protected override void OnApplyTemplate()
#endif
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
#if WINDOWS_XAML
                _timePicker.SelectedTimeChanged += TimePicker_SelectedTimeChanged;
#else
                _timePicker.TimeChanged += TimePicker_TimeChanged;
#endif
            }
            ConfigurePickers();
        }

#if WINDOWS_XAML
        private void TimePicker_SelectedTimeChanged(object? sender, TimePickerSelectedValueChangedEventArgs e) => UpdateValue();
        private void DatePicker_SelectedDateChanged(DatePicker? sender, DatePickerSelectedValueChangedEventArgs e) => UpdateValue();
#else
        private void TimePicker_TimeChanged(object? sender, EventArgs e) => UpdateValue();
        private void DatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e) => UpdateValue();
#endif
    }
}
#endif