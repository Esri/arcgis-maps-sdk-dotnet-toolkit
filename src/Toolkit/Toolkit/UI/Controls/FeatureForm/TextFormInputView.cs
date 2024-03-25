#if WPF
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
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
    /// Text input for the <see cref="TextAreaFormInput"/> and <see cref="TextBoxFormInput"/> inputs.
    /// </summary>
    public class TextFormInputView : Control
    {
        private WeakEventListener<TextFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;
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
                _textInput.LostFocus -= TextInput_LostFocus;
                _textInput.TextChanged -= TextInput_TextChanged;
            }
            _textInput = GetTemplateChild("TextInput") as TextBox;
            if (_textInput != null)
            {
                _textInput.LostFocus += TextInput_LostFocus;
                _textInput.TextChanged += TextInput_TextChanged;
            }
            ConfigureTextBox();
            UpdateValidationState();
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
                _textInput.Text = Element?.Value?.ToString();
            }
        }

        private void TextInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            Apply();
        }

        private void TextInput_LostFocus(object sender, RoutedEventArgs e)
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
            DependencyProperty.Register(nameof(MinLines), typeof(int), typeof(TextFormInputView), new PropertyMetadata(1));

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
                if (Dispatcher.CheckAccess())
                    ConfigureTextBox();
                else 
                    Dispatcher.Invoke(ConfigureTextBox);
            }
            else if (e.PropertyName == nameof(FieldFormElement.ValidationErrors))
            {
                if (Dispatcher.CheckAccess())
                    UpdateValidationState();
                else
                    Dispatcher.Invoke(UpdateValidationState);
            }
        }

        private void UpdateValidationState()
        {
            var err = Element?.ValidationErrors;
            if (err != null && err.Any())
            {
                VisualStateManager.GoToState(this, "InputError", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "InputValid", true);
            }
        }
    }
}
#endif