// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
#if WPF
using System.Windows.Controls.Primitives;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    [TemplatePart(Name ="Selector", Type = typeof(Selector))]
    public partial class RadioButtonsFormInputView :
#if WPF
        Selector
#else
        ItemsControl
#endif
    {

        /// <inheritdoc />
#if WPF
        public override void OnApplyTemplate()
#else
        protected override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            UpdateItems();
        }

        private class RadioButtonItem : RadioButton
        {
#if WINDOWS_XAML
            public RadioButtonItem()
            {
                this.Checked += (s,e) => ParentSelector?.RaiseCheckedEvent(this, true);
            }
#else
            protected override void OnChecked(RoutedEventArgs e)
            {
                base.OnChecked(e);
                ParentSelector?.RaiseCheckedEvent(this, true);
            }
#endif
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
            if (element is RadioButtonItem radio)
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
#if WINDOWS_XAML
                radio.ContentTemplate = ItemTemplate;
#endif
            }
#if WPF
            base.PrepareContainerForItemOverride(element, item);
#endif
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
#if WINDOWS_XAML
        private void OnSelectionChanged()
        {
#else
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
#endif
            if (Element is null) return;
            var value = (SelectedItem as CodedValue);
            foreach(var item in GetItemContainers(this))
            {
                item.IsChecked = (item.DataContext == SelectedItem);
            }
        }

#if WINDOWS_XAML
        private object? _selectedItem;

        /// <summary>
        /// Gets or sets the currently selected item
        /// </summary>
        private object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value; OnSelectionChanged();
                }
            }
        }
#endif
    }
}
#endif