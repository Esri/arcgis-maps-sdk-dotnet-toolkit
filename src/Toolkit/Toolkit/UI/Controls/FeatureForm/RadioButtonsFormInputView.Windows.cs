#if WPF
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    [TemplatePart(Name ="Selector", Type = typeof(System.Windows.Controls.Primitives.Selector))]
    public partial class RadioButtonsFormInputView : System.Windows.Controls.Primitives.Selector
    {

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
    }
}
#endif