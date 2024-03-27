#if WPF
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
    }
}
#endif