﻿#if WPF || MAUI
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Radio button view for the <see cref="RadioButtonsFormInput"/>.
    /// </summary>
    public partial class RadioButtonsFormInputView 
    {
        private WeakEventListener<RadioButtonsFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;
        private static int s_formcounter = -1;
        private static int _formid = 0; // Used to ensure uniqueness of groupnames

        /// <summary>
        /// Initializes an instance of the <see cref="RadioButtonsFormInputView"/> class.
        /// </summary>
        public RadioButtonsFormInputView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(RadioButtonsFormInputView);
#endif
            _formid = Interlocked.Increment(ref s_formcounter);
        }

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
            BindableProperty.Create(nameof(Element), typeof(FieldFormElement), typeof(RadioButtonsFormInputView), null, propertyChanged: (s, oldValue, newValue) => ((RadioButtonsFormInputView)s).OnElementPropertyChanged(oldValue as FieldFormElement, newValue as FieldFormElement));
#else
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(RadioButtonsFormInputView), new PropertyMetadata(null, (s, e) => ((RadioButtonsFormInputView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));
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
                _elementPropertyChangedListener = new WeakEventListener<RadioButtonsFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpcNew)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.Element_PropertyChanged(source, eventArgs),
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                inpcNew.PropertyChanged += _elementPropertyChangedListener.OnEvent;
            }
            UpdateItems();
        }

        private void Element_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FieldFormElement.Value))
            {
                Dispatch(UpdateSelection);
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

        private void UpdateItems()
        {
            if (Element?.Input is RadioButtonsFormInput input)
            {
#if !MAUI
                DisplayMemberPath = nameof(CodedValue.Name);
#endif
                List<object> items = new List<object>();
                if (input.NoValueOption == FormInputNoValueOption.Show)
                {
                    items.Add(new RadioButtonNullValue() { Name = input.NoValueLabel });
                }
                items.AddRange(input.CodedValues);
                ItemsSource = items;
                UpdateSelection();
            }
            else
            {
                SelectedItem = null;
                ItemsSource = null;
            }
        }

        private void UpdateSelection()
        {
            if (Element?.Input is RadioButtonsFormInput input)
            {
                var selection = input.CodedValues.Where(a => object.Equals(a.Code, Element?.Value)).FirstOrDefault();
                if (selection is null && input.NoValueOption == FormInputNoValueOption.Show)
                {
#if MAUI
                    SelectedItem = null;
#else
                    SelectedIndex = 0;
#endif
                }
                else
                    SelectedItem = selection;
            }
        }


        private class RadioButtonNullValue
        {
            public object? Code { get; set; }
            public string? Name { get; set; }
            public override string ToString() => Name!;
        }
    }
}
#endif