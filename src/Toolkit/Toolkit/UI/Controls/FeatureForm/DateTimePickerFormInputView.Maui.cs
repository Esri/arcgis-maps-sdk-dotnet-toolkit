#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui.Platform;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class DateTimePickerFormInputView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private DatePicker? _datePicker;
        private TimePicker? _timePicker;
        private Button? _clearButton;

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
            container.AddColumnDefinition(new ColumnDefinition() { Width = GridLength.Star });
            container.AddColumnDefinition(new ColumnDefinition() { Width = GridLength.Auto });
            container.SetBinding(IsEnabledProperty, "Element.IsEditable");

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(container, nameScope);

            var datePicker = new DatePicker();
            datePicker.HorizontalOptions = LayoutOptions.Fill;
            container.Children.Add(datePicker);
            nameScope.RegisterName("DatePickerInput", datePicker);

#if !WINDOWS
            var clearButton = new Button();
            clearButton.Text = "\u2716";
            clearButton.SetAppThemeColor(Button.TextColorProperty, Colors.Black, Colors.White);
            clearButton.HorizontalOptions = LayoutOptions.End;
            clearButton.Background = new SolidColorBrush(Colors.Transparent);
            clearButton.BorderWidth = 0;
            container.Children.Add(clearButton);
            Grid.SetColumn(clearButton, 1);
            nameScope.RegisterName("ClearButton", clearButton);
#endif

            var timePicker = new TimePicker();
            container.Children.Add(timePicker);
            Grid.SetRow(timePicker, 1);
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
            _clearButton = GetTemplateChild("ClearButton") as Button;
            if (_clearButton is not null)
            {
                _clearButton.Clicked += ClearButton_Clicked;
                _clearButton.IsVisible = Element?.Value != null;
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

#if !WINDOWS
        private void ClearButton_Clicked(object? sender, EventArgs e)
        {
            Element?.UpdateValue(null); // UI will be synced by triggering Element_PropertyChanged
        }
#endif

        private void DatePicker_HandlerChanged(object? sender, EventArgs e)
        {
#if WINDOWS
            if (_datePicker is not null && _datePicker.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.CalendarDatePicker winPicker)
            {
                winPicker.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
                winPicker.DateChanged += (s, e) => UpdateValue();
                winPicker.PlaceholderText = "No date selected";
                if (Element?.Value is null)
                    winPicker.Date = null;
            }
#endif
#if ANDROID
            if (_datePicker is not null && _datePicker.Handler?.PlatformView is Microsoft.Maui.Platform.MauiDatePicker androidPicker)
            {
                if (Element?.Value is null)
                {
                    androidPicker.Text = null;
                    androidPicker.Hint = "No date selected";
                }
            }
#endif
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