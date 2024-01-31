#if WPF
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Checkbox switch for the <see cref="ComboBoxFormInput"/>.
    /// </summary>
    [TemplatePart(Name ="Selector", Type = typeof(System.Windows.Controls.Primitives.Selector))]
    public class FeatureFormComboBoxInputView : Control
    {
        private System.Windows.Controls.Primitives.Selector? _selector;

        /// <summary>
        /// Initializes an instance of the <see cref="FeatureFormComboBoxInputView"/> class.
        /// </summary>
        public FeatureFormComboBoxInputView()
        {
            DefaultStyleKey = typeof(FeatureFormComboBoxInputView);
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
            DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(FeatureFormComboBoxInputView), new PropertyMetadata(null, (s, e) => ((FeatureFormComboBoxInputView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));

        private void OnElementPropertyChanged(FieldFormElement? oldValue, FieldFormElement? newValue)
        {
            if (oldValue is INotifyPropertyChanged inpcOld)
            {
                inpcOld.PropertyChanged += FeatureFormTextInputView_PropertyChanged; //TODO: Weak
            }
            if (newValue is INotifyPropertyChanged inpcNew)
            {
                inpcNew.PropertyChanged += FeatureFormTextInputView_PropertyChanged;
            }
            UpdateItems();
        }

        private void FeatureFormTextInputView_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FieldFormElement.Value))
            {
                UpdateSelection();
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