﻿#if WPF
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Checkbox switch for the <see cref="ComboBoxFormInput"/>.
    /// </summary>
    [TemplatePart(Name ="Selector", Type = typeof(System.Windows.Controls.Primitives.Selector))]
    public class ComboBoxFormInputView : Control
    {
        private WeakEventListener<ComboBoxFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;
        private System.Windows.Controls.Primitives.Selector? _selector;

        /// <summary>
        /// Initializes an instance of the <see cref="ComboBoxFormInputView"/> class.
        /// </summary>
        public ComboBoxFormInputView()
        {
            DefaultStyleKey = typeof(ComboBoxFormInputView);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_selector != null)
            {
                _selector.SelectionChanged -= Selector_SelectionChanged;
            }
            _selector = GetTemplateChild("Selector") as System.Windows.Controls.Primitives.Selector;
            if(_selector != null)
            {
                _selector.SelectionChanged += Selector_SelectionChanged;
            }
            UpdateItems();
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
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(ComboBoxFormInputView), new PropertyMetadata(null, (s, e) => ((ComboBoxFormInputView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));

        private void OnElementPropertyChanged(FieldFormElement? oldValue, FieldFormElement? newValue)
        {
            if (oldValue is INotifyPropertyChanged inpcOld)
            {
                _elementPropertyChangedListener?.Detach();
                _elementPropertyChangedListener = null;
            }
            if (newValue is INotifyPropertyChanged inpcNew)
            {
                _elementPropertyChangedListener = new WeakEventListener<ComboBoxFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpcNew)
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
                if (Dispatcher.CheckAccess())
                    UpdateSelection();
                else
                    Dispatcher.Invoke(UpdateSelection);
            }
        }

        private void UpdateItems()
        {
            if (_selector is not null)
            {
                if (Element?.Input is ComboBoxFormInput input)
                {
                    _selector.DisplayMemberPath = nameof(CodedValue.Name);
                    List<object> items = new List<object>();
                    if (input.NoValueOption == FormInputNoValueOption.Show)
                    {
                        items.Add(new ComboBoxNullValue() { Name = input.NoValueLabel });
                    }
                    items.AddRange(input.CodedValues);
                    _selector.ItemsSource = items;
                    UpdateSelection();
                }
                else
                {
                    _selector.SelectedItem = null;
                    _selector.ItemsSource = null;
                }
            }
        }

        private void UpdateSelection()
        {
            if (_selector is not null)
            {
                if (Element?.Input is ComboBoxFormInput input)
                {
                    var selection = input.CodedValues.Where(a => object.Equals(a.Code, Element?.Value)).FirstOrDefault();
                    if (selection is null && input.NoValueOption == FormInputNoValueOption.Show)
                        _selector.SelectedIndex = 0;
                    else
                        _selector.SelectedItem = selection;
                }
            }
        }

        private void Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var value = (_selector?.SelectedItem as CodedValue)?.Code;
            Element?.UpdateValue(value);
        }

        private class ComboBoxNullValue
        {
            public object? Code { get; set; }
            public string? Name { get; set; }
            public override string ToString() => Name!;
        }
    }
}
#endif