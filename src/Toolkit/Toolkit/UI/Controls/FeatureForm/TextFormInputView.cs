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

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
#if MAUI
using TextBox = Microsoft.Maui.Controls.InputView;
#elif WPF
using System.Windows.Controls.Primitives;
using System.Windows.Input;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Text input for the <see cref="TextAreaFormInput"/> and <see cref="TextBoxFormInput"/> inputs.
    /// </summary>
    public partial class TextFormInputView
    {
        private WeakEventListener<TextFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;

        /// <summary>
        /// Initializes an instance of the <see cref="TextFormInputView"/> class.
        /// </summary>
        public TextFormInputView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(TextFormInputView);
#endif
        }

        private void ConfigureTextBox()
        {
#if WINDOWS_XAML
            VisualStateManager.GoToState(this, Element?.IsEditable == false ? "Disabled" : "Enabled", true);
            VisualStateManager.GoToState(this, Element?.Input is BarcodeScannerFormInput && Element.IsEditable ? "ShowBarcode" : "HideBarcode", true);
            VisualStateManager.GoToState(this, Element?.IsEditable == true && Element?.Input is TextAreaFormInput ? "MultiLineText" : "SingleLineText", true);
            VisualStateManager.GoToState(this, Element?.IsEditable == true && Element?.Input is TextAreaFormInput ? "ShowCharacterCount" : "HideCharacterCount", true);
#endif
            if (_textInput != null)
            {
                if (Element?.Input is TextAreaFormInput area)
                {
#if MAUI
                    if (_textLineInput != null) _textLineInput.IsVisible = false;
                    if (_textAreaInput != null) _textAreaInput.IsVisible = Element.IsEditable;
#elif WPF
                    _textInput.AcceptsReturn = true;
#endif
                    _textInput.MaxLength = (int)area.MaxLength;
                }
                else if (Element?.Input is TextBoxFormInput || Element?.Input is BarcodeScannerFormInput)
                {
#if MAUI
                    if (_textAreaInput != null) _textAreaInput.IsVisible = false;
                    if (_textLineInput != null) _textLineInput.IsVisible = Element.IsEditable;
#elif WPF
                    _textInput.AcceptsReturn = false;
#endif
                    int maxLength = 0;
                    if (Element?.Input is TextBoxFormInput box)
                        maxLength = (int)box.MaxLength;
                    else if (Element?.Input is BarcodeScannerFormInput bar)
                        maxLength = (int)bar.MaxLength;

                    _textInput.MaxLength = maxLength == 0 ? int.MaxValue : maxLength;
                }
                _textInput.Text = Element?.Value?.ToString();
#if MAUI || WINDOWS_XAML
                bool isNumericInput = Element?.FieldType == FieldType.Int32 ||
                    Element?.FieldType == FieldType.Int64 ||
                    Element?.FieldType == FieldType.Int16 ||
                    Element?.FieldType == FieldType.Float64 ||
                    Element?.FieldType == FieldType.Float32;
#if MAUI
                _textInput.Keyboard = isNumericInput ? Keyboard.Numeric : Keyboard.Default;
#else
                VisualStateManager.GoToState(this, isNumericInput ? "Numeric" : "Text", true);
#endif
#endif
            }
#if !WINDOWS_XAML
            ShowCharacterCount = Element?.IsEditable == true && Element?.Input is TextAreaFormInput;
#endif
            if (_readonlyLabel is not null)
            {
#if MAUI
                _readonlyLabel.IsVisible = Element?.IsEditable == false;
#elif WPF
                _readonlyLabel.Visibility = Element?.IsEditable == false ? Visibility.Visible : Visibility.Collapsed;
#endif
                _readonlyLabel.Text = Element?.FormattedValue;
            }

#if !WINDOWS_XAML
            ShowBarcodeScanner = Element?.Input is BarcodeScannerFormInput && Element.IsEditable;
#endif

#if !MAUI
            if (_textInput != null)
                _textInput.Visibility = Element?.IsEditable == false ? Visibility.Collapsed : Visibility.Visible;
#endif

        }

        private void TextInput_TextChanged(object? sender, TextChangedEventArgs e)
        {
            Apply();
        }

#if MAUI
        private void TextInput_Unfocused(object? sender, FocusEventArgs e)
#else
        private void TextInput_LostFocus(object sender, RoutedEventArgs e)
#endif
        {
            Apply();
        }

        private void Apply()
        {
            if (_textInput is null || _textInput.Text == Element?.Value as string) return;
            string strvalue = _textInput.Text;
            object? value = strvalue;
            if (string.IsNullOrEmpty(strvalue))
            {
                value = null;
            }
            else if (Element?.FieldType == FieldType.Int32 && int.TryParse(strvalue, out var intvalue))
                value = intvalue;
            else if (Element?.FieldType == FieldType.Int64 && long.TryParse(strvalue, out var longvalue))
                value = longvalue;
            else if (Element?.FieldType == FieldType.Int16 && short.TryParse(strvalue, out var shortvalue))
                value = shortvalue;
            else if (Element?.FieldType == FieldType.Float64 && double.TryParse(strvalue, out var doublevalue))
                value = doublevalue;
            else if (Element?.FieldType == FieldType.Float32 && float.TryParse(strvalue, out var floatvalue))
                value = floatvalue;
            else if (Element?.FieldType == FieldType.Date && DateTime.TryParse(strvalue, out var datevalue))
                value = datevalue;
            try
            {
                Element?.UpdateValue(value);
            }
            catch (System.Exception) // Unexpected error setting value
            {
                _textInput.Text = Element?.Value?.ToString();  //Reset input to previous valid value
            }
        }

#if !MAUI
        /// <summary>
        /// Gets or sets the minimum number of visible lines.
        /// </summary>
        public int MinLines
        {
            get { return (int)GetValue(MinLinesProperty); }
            set { SetValue(MinLinesProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MinLines"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty MinLinesProperty =
            BindableProperty.Create(nameof(MinLines), typeof(int), typeof(TextFormInputView), 1);
#else
        public static readonly DependencyProperty MinLinesProperty = 
            DependencyProperty.Register(nameof(MinLines), typeof(int), typeof(TextFormInputView), new PropertyMetadata(1));
#endif

        /// <summary>
        /// Gets or sets the maximum number of visible lines.
        /// </summary>
        public int MaxLines
        {
            get { return (int)GetValue(MaxLinesProperty); }
            set { SetValue(MaxLinesProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MaxLines"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty MaxLinesProperty =
            BindableProperty.Create(nameof(MaxLines), typeof(int), typeof(TextFormInputView), 1);
#else
        public static readonly DependencyProperty MaxLinesProperty =
            DependencyProperty.Register(nameof(MaxLines), typeof(int), typeof(TextFormInputView), new PropertyMetadata(1));
#endif
#endif

#if !WINDOWS_XAML
        /// <summary>
        /// Gets a value indicating whether the character count is visible.
        /// </summary>
        public bool ShowCharacterCount
        {
            get
            {
#if MAUI
                return _showCharacterCount;
#else
               return (bool)GetValue(ShowCharacterCountPropertyKey.DependencyProperty); 
#endif
            }
            private set
            {
#if MAUI
                if (_showCharacterCount != value)
                {
                    _showCharacterCount = value;
#if MAUI
                    OnPropertyChanged(nameof(ShowCharacterCount));
#endif
                }
#else
                SetValue(ShowCharacterCountPropertyKey, value); 
#endif
            }
        }

#if MAUI
        private bool _showCharacterCount = false;
#else
        private static readonly DependencyPropertyKey ShowCharacterCountPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(ShowCharacterCount), typeof(bool), typeof(TextFormInputView), new PropertyMetadata(false));
#endif

        /// <summary>
        /// Gets a value indicating whether the bar code scanner button is visible.
        /// </summary>
        public bool ShowBarcodeScanner
        {
            get
            {
#if MAUI
                return _ShowBarcodeScanner;
#else
                return (bool)GetValue(ShowBarcodeScannerPropertyKey.DependencyProperty);
#endif
            }
            private set
            {
#if MAUI || WINDOWS_XAML
                if (_ShowBarcodeScanner != value)
                {
                    _ShowBarcodeScanner = value;
#if MAUI
                    OnPropertyChanged(nameof(ShowBarcodeScanner));
#endif
                }
#else
                SetValue(ShowBarcodeScannerPropertyKey, value);
#endif
            }
        }


#if MAUI
        private bool _ShowBarcodeScanner = false;
#else
        private static readonly DependencyPropertyKey ShowBarcodeScannerPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(ShowBarcodeScanner), typeof(bool), typeof(TextFormInputView), new PropertyMetadata(false));
#endif
#endif

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
            BindableProperty.Create(nameof(Element), typeof(FieldFormElement), typeof(TextFormInputView), null, propertyChanged: (s, oldValue, newValue) => ((TextFormInputView)s).OnElementPropertyChanged(oldValue as FieldFormElement, newValue as FieldFormElement));
#else
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(TextFormInputView), new PropertyMetadata(null, (s,e) => ((TextFormInputView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));
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
                _elementPropertyChangedListener = new WeakEventListener<TextFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpcNew)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.Element_PropertyChanged(source, eventArgs),
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                inpcNew.PropertyChanged += _elementPropertyChangedListener.OnEvent;
            }
            ConfigureTextBox();
        }

        private void Element_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FieldFormElement.Value))
            {
                this.Dispatch(ConfigureTextBox);
            }
            else if (e.PropertyName == nameof(FieldFormElement.IsEditable))
            {
                this.Dispatch(ConfigureTextBox);
            }
            else if (e.PropertyName == nameof(FieldFormElement.ValidationErrors))
            {
                this.Dispatch(UpdateValidationState);
            }
        }

        private void UpdateValidationState()
        {
            var err = Element?.ValidationErrors;
            if (err != null && err.Any() && Element?.IsEditable == true)
            {
#if MAUI
                if (GetTemplateChild("ErrorBorder") is Border border)
                {
                    border.IsVisible = true;
                }
#else
                VisualStateManager.GoToState(this, "InputError", true);
#endif
            }
            else
            {
#if MAUI
                if (GetTemplateChild("ErrorBorder") is Border border)
                {
                    border.IsVisible = false;
                }
#else
                VisualStateManager.GoToState(this, "InputValid", true);
#endif
            }
        }
    }
}