#if WPF || MAUI
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.Collections.ObjectModel;
using System.ComponentModel;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Combobox picker for the <see cref="ComboBoxFormInput"/>.
    /// </summary>
    public partial class ComboBoxFormInputView
    {
        private WeakEventListener<ComboBoxFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;

        /// <summary>
        /// Initializes an instance of the <see cref="ComboBoxFormInputView"/> class.
        /// </summary>
        public ComboBoxFormInputView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(ComboBoxFormInputView);
#endif
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
            BindableProperty.Create(nameof(Element), typeof(FieldFormElement), typeof(ComboBoxFormInputView), null, propertyChanged: (s, oldValue, newValue) => ((ComboBoxFormInputView)s).OnElementPropertyChanged(oldValue as FieldFormElement, newValue as FieldFormElement));
#else
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(ComboBoxFormInputView), new PropertyMetadata(null, (s, e) => ((ComboBoxFormInputView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));
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
                this.Dispatch(UpdateSelection);
            }
        }

        private void UpdateItems()
        {
            if (_selector is not null)
            {
                if (Element?.Input is ComboBoxFormInput input)
                {
#if !MAUI
                    _selector.DisplayMemberPath = nameof(CodedValue.Name);
#endif
                    var items = new ObservableCollection<object>();
                    if (input.NoValueOption == FormInputNoValueOption.Show)
                    {
                        items.Add(new ComboBoxNullValue() { Name = input.NoValueLabel });
                    }
                    foreach (var value in input.CodedValues)
                        items.Add(value);
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
                    else if (selection is null && Element?.Value is not null) // Attribute value not available in the domain
                    {
                        var missingValue = new ComboBoxPlaceHolderValue() { Name = Element.Value.ToString() };
                        var items = (IList<object>)_selector.ItemsSource;
                        items.Add(missingValue);
                        _selector.SelectedItem = missingValue;
                    }
                    else
                        _selector.SelectedItem = selection;
                }
            }
        }

        private class ComboBoxNullValue
        {
            public object? Code { get; set; }
            public string? Name { get; set; }
            public override string ToString() => Name!;
        }

        private class ComboBoxPlaceHolderValue : ComboBoxNullValue
        {
        }
    }
}
#endif
