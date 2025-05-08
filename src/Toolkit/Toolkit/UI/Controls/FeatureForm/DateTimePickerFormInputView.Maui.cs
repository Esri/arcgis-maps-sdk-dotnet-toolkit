#if MAUI
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Platform;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class DateTimePickerFormInputView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private DatePicker? _datePicker;
        private TimePicker? _timePicker;
#if IOS || ANDROID
        private Button? _clearButton;
#elif MACCATALYST
        private Switch? _hasValueSwitch;
#endif

        static DateTimePickerFormInputView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            var container = new Grid();
            container.AddRowDefinition(new RowDefinition() { Height = GridLength.Auto });
            container.AddRowDefinition(new RowDefinition() { Height = GridLength.Auto });

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(container, nameScope);

            var datePicker = new DatePicker();
            datePicker.HorizontalOptions = LayoutOptions.Fill;
            datePicker.Focused += static (s, e) => FeatureFormView.GetParent<FieldFormElementView>(s as Element)?.OnGotFocus();
            container.Children.Add(datePicker);
            nameScope.RegisterName("DatePickerInput", datePicker);

#if IOS || ANDROID
            // Add a button that clears the date picker, overlayed on top of it
            var clearButton = new Button();
            clearButton.Text = "\u2716"; // Unicode "Heavy Multiplication X" character
            clearButton.SetAppThemeColor(Button.TextColorProperty, Colors.Black, Colors.White);
            clearButton.HorizontalOptions = LayoutOptions.End;
            clearButton.VerticalOptions = LayoutOptions.Center;
            clearButton.Background = new SolidColorBrush(Colors.Transparent);
            clearButton.BorderWidth = 0;
            clearButton.Margin = new Thickness(0);
            container.Children.Add(clearButton);
            nameScope.RegisterName("ClearButton", clearButton);
#endif

            var timePicker = new TimePicker();
            timePicker.Focused += static (s, e) => FeatureFormView.GetParent<FieldFormElementView>(s as Element)?.OnGotFocus();
            container.Children.Add(timePicker);
            Grid.SetRow(timePicker, 1);
            nameScope.RegisterName("TimePickerInput", timePicker);

#if MACCATALYST
            // On Mac, we need to add a switch to allow selecting "no date",
            // because MAUI's DatePicker on Mac always has a value with no way to clear it.

            var hasValueSwitch = new Switch();
            container.Children.Add(hasValueSwitch);
            nameScope.RegisterName("HasValueSwitch", hasValueSwitch);
            
            container.AddColumnDefinition(new ColumnDefinition { Width = GridLength.Auto });
            container.AddColumnDefinition(new ColumnDefinition { Width = GridLength.Star });
            Grid.SetColumn(datePicker, 1);
            Grid.SetColumn(timePicker, 1);
#endif

            return container;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _datePicker = GetTemplateChild("DatePickerInput") as DatePicker;
            _timePicker = GetTemplateChild("TimePickerInput") as TimePicker;
#if IOS || ANDROID
            _clearButton = GetTemplateChild("ClearButton") as Button;
            if (_clearButton is not null)
            {
                _clearButton.Clicked += ClearButton_Clicked;
                _clearButton.IsVisible = Element?.Value != null;
            }
#elif MACCATALYST
            _hasValueSwitch = GetTemplateChild("HasValueSwitch") as Switch;
            if (_hasValueSwitch is not null)
            {
                _hasValueSwitch.Toggled += HasValueSwitch_Toggled;
                _hasValueSwitch.IsToggled = Element?.Value != null;
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

#if IOS || ANDROID
        private void ClearButton_Clicked(object? sender, EventArgs e)
        {
            Element?.UpdateValue(null); // UI will be synced by triggering Element_PropertyChanged
        }
#elif MACCATALYST
        private void HasValueSwitch_Toggled(object? sender, ToggledEventArgs e)
        {
            UpdateValue();
        }
#endif

        private void DatePicker_HandlerChanged(object? sender, EventArgs e)
        {
            // Platform-specific workarounds are needed to allow selecting "no date",
            // because MAUI's own DatePicker does not support empty values.
            // See https://github.com/dotnet/maui/issues/1100
#if WINDOWS
            if (_datePicker is not null && _datePicker.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.CalendarDatePicker winPicker)
            {
                winPicker.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
                winPicker.DateChanged += (s, e) => UpdateValue();
                if (Element?.Value is null)
                    winPicker.Date = null;
            }
#elif IOS || ANDROID
            if (_datePicker is not null && _datePicker.Handler?.PlatformView is Microsoft.Maui.Platform.MauiDatePicker nativePicker)
            {
                if (Element?.Value is null)
                {
                    nativePicker.Text = null;
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