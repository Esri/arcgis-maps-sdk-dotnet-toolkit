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
                if (GetTemplateChild("ItemsPanel") is Layout layout)
                {
                    layout.Children.Clear();
                    if (_itemsSource is not null)
                    {
                        foreach (var item in _itemsSource)
                        {
#if __IOS__
                            var button = new RadioButtonFix();
#else
                            var button = new RadioButton();
#endif
                            button.SetBinding(RadioButton.ContentProperty, static (CodedValue item) => item.Name);
                            button.BindingContext = item;
                            button.IsChecked = item == SelectedItem;
                            button.IsEnabled = Element?.IsEditable == true;
                            button.GroupName = Element?.FieldName + "_" + _formid.ToString();
                            button.CheckedChanged += Button_CheckedChanged;
                            button.Focused += static (s, e) => FeatureFormView.GetParent<FieldFormElementView>(s as Element)?.OnGotFocus();
                            layout.Children.Add(button);
                        }
                    }
                }
            }
        }
#if __IOS__
        private class RadioButtonFix : RadioButton
        {
            // Workaround for MAUI arrange bug when the collection of radio buttons
            // are inside a collapsed group expander.
            protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
            {
                var size = base.MeasureOverride(widthConstraint, heightConstraint);
                if (size.Height > 0 && this.Bounds.Height == 0)
                    this.Handler?.PlatformArrange(new Rect(0, 0, size.Width, size.Height));
                return size;
            }
        }
#endif

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

        private void UpdateEditableState()
        {
#if __IOS__
            // Workaround for https://github.com/dotnet/maui/issues/18668
            ItemsSource = _itemsSource; // Rebuilds the radiobutton layout
#else
            if (GetTemplateChild("ItemsPanel") is Layout layout)
            {
                foreach (var button in layout.Children.OfType<RadioButton>())
                {
                    button.IsEnabled = Element?.IsEditable == true;
                }
            }
#endif
        }
    }
}
#endif