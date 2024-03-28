#if WPF || MAUI
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
            if (_textInput != null)
            {
                if (Element?.Input is TextAreaFormInput area)
                {
#if MAUI
                    if (_textLineInput != null) _textLineInput.IsVisible = false;
                    if (_textAreaInput != null)
                    {
                        _textAreaInput.IsVisible = Element.IsEditable;
                    }
#else
                    _textInput.AcceptsReturn = true;
#endif
                    _textInput.MaxLength = (int)area.MaxLength;
                }
                else if (Element?.Input is TextBoxFormInput box)
                {
#if MAUI
                    if (_textAreaInput != null) _textAreaInput.IsVisible = false;
                    if (_textLineInput != null)
                    {
                        _textLineInput.IsVisible = Element.IsEditable;
                    }
#else
                    _textInput.AcceptsReturn = false;
#endif
                    _textInput.MaxLength = (int)box.MaxLength;
                }
                _textInput.Text = Element?.Value?.ToString();
#if MAUI
                bool isNumericInput = Element?.FieldType == FieldType.Int32 ||
                    Element?.FieldType == FieldType.Int16 ||
                    Element?.FieldType == FieldType.Float64 ||
                    Element?.FieldType == FieldType.Float32;
                _textInput.Keyboard = isNumericInput ? Keyboard.Numeric : Keyboard.Default;
#endif
            }
            ShowCharacterCount = Element?.IsEditable == true && Element?.Input is TextAreaFormInput;
            if (_readonlyLabel is not null)
            {
#if MAUI
                _readonlyLabel.IsVisible = Element?.IsEditable == false;
#else
                _readonlyLabel.Visibility = Element?.IsEditable == false ? Visibility.Visible : Visibility.Collapsed;
#endif
                _readonlyLabel.Text = Element?.FormattedValue;
            }
#if !MAUI
            if(_textInput != null)
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

        /// <summary>
        /// Gets a value indicating whether the character count is visible.
        /// </summary>
        public bool ShowCharacterCount
        {
            get {
#if MAUI
                return _showCharacterCount;
#else
               return (bool)GetValue(ShowCharacterCountPropertyKey.DependencyProperty); 
#endif
            }
            private set
            {
#if MAUI
                if(_showCharacterCount != value)
                {
                    _showCharacterCount = value;
                    OnPropertyChanged(nameof(ShowCharacterCount));
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
                Dispatch(ConfigureTextBox);
            }
            else if (e.PropertyName == nameof(FieldFormElement.IsEditable))
            {
                Dispatch(ConfigureTextBox);
            }
            else if (e.PropertyName == nameof(FieldFormElement.ValidationErrors))
            {
                Dispatch(UpdateValidationState);
            }
        }

        private void Dispatch(Action action)
        {
#if WPF
            if (Dispatcher.CheckAccess())
                action();
            else
                Dispatcher.Invoke(action);
#elif MAUI
            if (Dispatcher.IsDispatchRequired)
                Dispatcher.Dispatch(action);
            else
                action();
#endif
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
#endif