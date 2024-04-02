#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class DateTimePickerFormInputView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private DatePicker? _datePicker;
        private TimePicker? _timePicker;
        private Switch? _hasValueButton;

        static DateTimePickerFormInputView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FieldFormElement.IsEditable), "Esri.ArcGISRuntime.Mapping.FeatureForms.FeatureForm", "Esri.ArcGISRuntime")]
        private static object BuildDefaultTemplate()
        {
            var container = new Grid();
            container.AddRowDefinition(new RowDefinition() { Height = GridLength.Auto });
            container.AddRowDefinition(new RowDefinition() { Height = GridLength.Auto });
            container.AddRowDefinition(new RowDefinition() { Height = GridLength.Auto });
            container.SetBinding(IsEnabledProperty, "Element.IsEditable");

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(container, nameScope);

#if !WINDOWS
            var hasValueButton = new Switch();
            container.Children.Add(hasValueButton);
            nameScope.RegisterName("HasValueButton", hasValueButton);
#endif

            var datePicker = new DatePicker();
            datePicker.HorizontalOptions = LayoutOptions.Fill;
            container.Children.Add(datePicker);
            Grid.SetRow(datePicker, 1);
            nameScope.RegisterName("DatePickerInput", datePicker);

            var timePicker = new TimePicker();
            container.Children.Add(timePicker);
            Grid.SetRow(timePicker, 2);
            nameScope.RegisterName("TimePickerInput", timePicker);

            return container;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _datePicker = GetTemplateChild("DatePickerInput") as DatePicker;
            _timePicker = GetTemplateChild("TimePickerInput") as TimePicker;
#if !WINDOWS
            _hasValueButton = GetTemplateChild("HasValueButton") as Switch;
            if (_hasValueButton is not null)
            {
                _hasValueButton.Toggled += NoValueButton_Toggled;
            }
#endif
            if (_datePicker is not null)
            {
                _datePicker.DateSelected += DatePicker_DateSelected;
                _datePicker.HandlerChanged += DatePicker_HandlerChanged;
            }
            if (_timePicker is not null)
            {
                _timePicker.PropertyChanged += TimePicker_PropertyChanged;
            }
        }

        private void DatePicker_HandlerChanged(object? sender, EventArgs e)
        {
#if WINDOWS
            if (_datePicker is not null && _datePicker.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.CalendarDatePicker winPicker)
            {
                winPicker.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
                winPicker.DateChanged += (s, e) => UpdateValue();
                if (Element?.Value is null)
                    winPicker.Date = null;
            }
#endif
        }

        private void NoValueButton_Toggled(object? sender, ToggledEventArgs e)
        {
            if (_hasValueButton is not null)
            {
                if (_datePicker is not null)
                    _datePicker.IsEnabled = _hasValueButton.IsToggled;
                if (_timePicker is not null)
                    _timePicker.IsEnabled = _hasValueButton.IsToggled;
                UpdateValue();
            }
        }

        private void TimePicker_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimePicker.Time))
                UpdateValue();
        }

        private void DatePicker_DateSelected(object? sender, DateChangedEventArgs e) => UpdateValue();
    }
}
#endif