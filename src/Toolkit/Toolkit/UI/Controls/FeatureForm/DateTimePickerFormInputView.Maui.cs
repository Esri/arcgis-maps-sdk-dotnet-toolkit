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

            var hasValueButton = new Switch();
            container.Children.Add(hasValueButton);

            var datePicker = new DatePicker();
            datePicker.HorizontalOptions = LayoutOptions.Fill;
            container.Children.Add(datePicker);
            Grid.SetRow(datePicker, 1);

            var timePicker = new TimePicker();
            container.Children.Add(timePicker);
            Grid.SetRow(timePicker, 2);

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(container, nameScope);
            nameScope.RegisterName("DatePickerInput", datePicker);
            nameScope.RegisterName("TimePickerInput", timePicker);
            nameScope.RegisterName("HasValueButton", hasValueButton);
            return container;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _datePicker = GetTemplateChild("DatePickerInput") as DatePicker;
            _timePicker = GetTemplateChild("TimePickerInput") as TimePicker;
            _hasValueButton = GetTemplateChild("HasValueButton") as Switch;
            if(_hasValueButton is not null)
            {
                _hasValueButton.Toggled += NoValueButton_Toggled;
            }
            if (_datePicker is not null)
            {
                _datePicker.DateSelected += DatePicker_DateSelected;
            }
            if (_timePicker is not null)
            {
                _timePicker.PropertyChanged += TimePicker_PropertyChanged;
            }
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