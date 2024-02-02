#if WPF
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Radio button view for the <see cref="RadioButtonsFormInput"/>.
    /// </summary>
    [TemplatePart(Name ="Selector", Type = typeof(System.Windows.Controls.Primitives.Selector))]
    public class RadioButtonsFormInputView : System.Windows.Controls.Primitives.Selector // Control
    {
        private WeakEventListener<RadioButtonsFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;
        private static int s_formcounter = -1;
        private static int _formid = 0; // Used to ensure uniqueness of groupnames

        /// <summary>
        /// Initializes an instance of the <see cref="RadioButtonsFormInputView"/> class.
        /// </summary>
        public RadioButtonsFormInputView()
        {
            DefaultStyleKey = typeof(RadioButtonsFormInputView);
            _formid = Interlocked.Increment(ref s_formcounter);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateItems();
        }


        private class RadioButtonItem : RadioButton
        {
            protected override void OnChecked(RoutedEventArgs e)
            {
                base.OnChecked(e);
                ParentSelector?.RaiseCheckedEvent(this, true);
            }
            internal RadioButtonsFormInputView? ParentSelector => ItemsControl.ItemsControlFromItemContainer(this) as RadioButtonsFormInputView;
        }

        private void RaiseCheckedEvent(RadioButtonItem button, bool isChecked)
        {
            if (isChecked)
            {
                var selection = (button.DataContext as CodedValue)?.Code;
                Element?.UpdateValue(selection);
            }
        }

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new RadioButtonItem() { GroupName = Element?.FieldName + "_" + _formid };
        }

        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            if(element is RadioButtonItem radio)
            {
                bool isChecked = false;
                if(item is CodedValue cv)
                {
                    if (object.Equals(cv.Code, Element?.Value))
                        isChecked = true;
                }
                else if(item is RadioButtonNullValue && Element?.Value is null)
                {
                    isChecked = true;
                }
                radio.IsChecked = isChecked;
            }
            base.PrepareContainerForItemOverride(element, item);
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
            DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(RadioButtonsFormInputView), new PropertyMetadata(null, (s, e) => ((RadioButtonsFormInputView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));

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
                if (Dispatcher.CheckAccess())
                    UpdateSelection();
                else
                    Dispatcher.Invoke(UpdateSelection);
            }
        }

        private void UpdateItems()
        {
            if (Element?.Input is RadioButtonsFormInput input)
            {
                DisplayMemberPath = nameof(CodedValue.Name);
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
                    SelectedIndex = 0;
                else
                    SelectedItem = selection;
            }
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            if (Element is null) return;
            var value = (SelectedItem as CodedValue);
            foreach(var item in GetItemContainers(this))
            {
                item.IsChecked = (item.DataContext == SelectedItem);
            }
        }

        private static IEnumerable<RadioButtonItem> GetItemContainers(DependencyObject depObj)
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    var child = VisualTreeHelper.GetChild(depObj, i);
                    if (child is RadioButtonItem b)
                        yield return b;
                    else foreach (var cb in GetItemContainers(child))
                            yield return cb;
                }
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