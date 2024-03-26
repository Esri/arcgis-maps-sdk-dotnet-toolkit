#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class RadioButtonsFormInputView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static RadioButtonsFormInputView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            VerticalStackLayout views = new VerticalStackLayout();
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(views, nameScope);
            nameScope.RegisterName("ItemsPanel", views);
            return views;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateItems();
        }

        private System.Collections.IEnumerable? _itemsSource;

        private System.Collections.IEnumerable? ItemsSource
        {
            get => _itemsSource;
            set
            {
                _itemsSource = value;
                if(GetTemplateChild("ItemsPanel") is Layout layout)
                {
                    layout.Children.Clear();
                    if (_itemsSource is not null)
                    {
                        foreach (var item in _itemsSource)
                        {
                            var button = new RadioButton();
                            button.SetBinding(RadioButton.ContentProperty, "Name");
                            button.BindingContext = item;
                            button.IsChecked = item == SelectedItem;
                            button.SetBinding(RadioButton.IsEnabledProperty, new Binding() { Path = "Element.IsEditable", Source = RelativeBindingSource.TemplatedParent });
                            button.GroupName = Element?.FieldName + "_" + _formid.ToString();
                            button.CheckedChanged += Button_CheckedChanged;
                            layout.Children.Add(button);
                        }
                    }
                }
            }
        }

        private void Button_CheckedChanged(object? sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                var button = (RadioButton)sender!;
                SelectedItem = button.BindingContext;
                var selection = (button.BindingContext as CodedValue)?.Code;
                Element?.UpdateValue(selection);
            }
        }

        private object? _selectedItem;

        private object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                if (GetTemplateChild("ItemsPanel") is Layout layout)
                {
                    foreach (var item in layout.Children.OfType<RadioButton>())
                    {
                        if(item.BindingContext is RadioButtonNullValue && value is null)
                            item.IsChecked = true;
                        else if (item.BindingContext == value)
                            item.IsChecked = true;
                        else 
                            item.IsChecked = false;
                    }
                }
            }
        }
    }
}
#endif