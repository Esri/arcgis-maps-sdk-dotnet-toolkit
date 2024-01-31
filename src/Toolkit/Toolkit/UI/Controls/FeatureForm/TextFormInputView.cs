#if WPF
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Text input for the <see cref="TextAreaFormInput"/>, <see cref="TextBoxFormInput"/> and <see cref="BarcodeScannerFormInput"/> inputs.
    /// </summary>
    public class TextFormInputView : Control
    {
        private TextBox? _textInput;

        /// <summary>
        /// Initializes an instance of the <see cref="TextFormInputView"/> class.
        /// </summary>
        public TextFormInputView()
        {
            DefaultStyleKey = typeof(TextFormInputView);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_textInput != null)
            {
                _textInput.KeyDown -= TextInput_KeyDown;
                _textInput.LostFocus -= TextInput_LostFocus;
            }
            _textInput = GetTemplateChild("TextInput") as TextBox;
            if (_textInput != null)
            {
                _textInput.KeyDown += TextInput_KeyDown;
                _textInput.LostFocus += TextInput_LostFocus;
            }
            ConfigureTextBox();
            var barcodeButton = GetTemplateChild("BarcodeScannerButton") as ButtonBase;
            if (barcodeButton is not null)
            {
#if !NET6_0_OR_GREATER
                barcodeButton.Visibility = Visibility.Collapsed; // Barcode scanner API not available in NETFX without external dependency
#else
                barcodeButton.Click += BarcodeButton_Click;
#endif
            }
        }

        private void BarcodeButton_Click(object sender, RoutedEventArgs e)
        {
#if NET6_0_OR_GREATER
            //TODO...
            /*
            var scanner = await Windows.Devices.PointOfService.BarcodeScanner.GetDefaultAsync();
            var claimedScanner = await scanner.ClaimScannerAsync();

            if (claimedScanner != null)
            {
                claimedScanner.DataReceived += (s,e) =>
                {
                };
                await claimedScanner.EnableAsync();
            }*/
#endif
        }

        private void ConfigureTextBox()
        {
            if (_textInput != null)
            {
                if (Element?.Input is TextAreaFormInput area)
                {
                    _textInput.AcceptsReturn = true;
                    _textInput.MaxLength = (int)area.MaxLength;
                }
                else if (Element?.Input is TextBoxFormInput box)
                {
                    _textInput.AcceptsReturn = false;
                    _textInput.MaxLength = (int)box.MaxLength;
                }
                else if (Element?.Input is BarcodeScannerFormInput bar)
                {
                    _textInput.AcceptsReturn = false;
                    _textInput.MaxLength = (int)bar.MaxLength;
                }
                _textInput.Text = Element?.Value?.ToString();
            }
        }

        private void TextInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Element?.Input is TextBoxFormInput || Element?.Input is BarcodeScannerFormInput)
                {
                    Apply();
                }
            }
        }

        private void TextInput_LostFocus(object sender, RoutedEventArgs e)
        {
            Apply();
        }

        private void Apply()
        {
            if (_textInput is null || _textInput.Text == Element?.Value as string) return;
            try
            {
                Element?.UpdateValue(_textInput.Text);
                var err = Element?.GetValidationErrors();
                if(err != null && err.Any())
                {
                    VisualStateManager.GoToState(this, "InputError", true);
                }
                else
                {
                    VisualStateManager.GoToState(this, "InputValid", true);
                }
            }
            catch(System.Exception)
            {
                var err = Element?.GetValidationErrors();
            }
        }

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
        public static readonly DependencyProperty MinLinesProperty = 
            DependencyProperty.Register("MinLines", typeof(int), typeof(TextFormInputView), new PropertyMetadata(1));

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
        public static readonly DependencyProperty MaxLinesProperty =
            DependencyProperty.Register(nameof(MaxLines), typeof(int), typeof(TextFormInputView), new PropertyMetadata(1));

        /// <summary>
        /// Gets or sets a value indicating whether the barcode scanner button is available.
        /// </summary>
        public bool IsBarcodeScannerEnabled
        {
            get { return (bool)GetValue(IsBarcodeScannerEnabledProperty); }
            set { SetValue(IsBarcodeScannerEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsBarcodeScannerEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsBarcodeScannerEnabledProperty =
            DependencyProperty.Register(nameof(IsBarcodeScannerEnabled), typeof(bool), typeof(TextFormInputView), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the character count is visible.
        /// </summary>
        public bool ShowCharacterCount
        {
            get { return (bool)GetValue(ShowCharacterCountProperty); }
            set { SetValue(ShowCharacterCountProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ShowCharacterCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowCharacterCountProperty =
            DependencyProperty.Register(nameof(ShowCharacterCount), typeof(bool), typeof(TextFormInputView), new PropertyMetadata(false));

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
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(TextFormInputView), new PropertyMetadata(null, (s,e) => ((TextFormInputView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));

        private void OnElementPropertyChanged(FieldFormElement? oldValue, FieldFormElement? newValue)
        {
            if (newValue is not null)
            {
                ((System.ComponentModel.INotifyPropertyChanged)newValue).PropertyChanged += FeatureFormTextInputView_PropertyChanged;
            }
            ConfigureTextBox();
        }

        private void FeatureFormTextInputView_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FieldFormElement.Value))
            {
                ConfigureTextBox();
            }
        }
    }
}
#endif