﻿#if MAUI
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Platform;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class DateTimePickerFormInputView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private DatePicker? _datePicker;
        private TimePicker? _timePicker;
#if !WINDOWS
        private Button? _clearButton;
#endif

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
            // Platform-specific workarounds are needed to allow selecting "no date",
            // because MAUI's own DatePicker does not support empty values.
            // See https://github.com/dotnet/maui/issues/1100
#if WINDOWS
            if (_datePicker is not null && _datePicker.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.CalendarDatePicker winPicker)
            {
                winPicker.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
                winPicker.DateChanged += (s, e) => UpdateValue();
                winPicker.PlaceholderText = GetNoValueText();
                if (Element?.Value is null)
                    winPicker.Date = null;
            }
#elif !NETSTANDARD
            if (_datePicker is not null && _datePicker.Handler?.PlatformView is Microsoft.Maui.Platform.MauiDatePicker nativePicker)
            {
                if (Element?.Value is null)
                {
                    nativePicker.Text = null;
#if ANDROID
                    nativePicker.Hint = GetNoValueText();
#else // IOS || MACCATALYST
                    nativePicker.Placeholder = GetNoValueText();
#endif
                }
            }
#endif
        }

        private static string GetNoValueText() => Properties.Resources.GetString("FeatureFormNoDateSelected") ?? "No date selected";

        private void TimePicker_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimePicker.Time))
                UpdateValue();
        }

        private void DatePicker_DateSelected(object? sender, DateChangedEventArgs e) => UpdateValue();
    }
}
#endif