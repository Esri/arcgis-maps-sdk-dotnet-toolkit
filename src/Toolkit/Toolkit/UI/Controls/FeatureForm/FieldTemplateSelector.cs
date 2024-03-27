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

#if WPF || MAUI

using Esri.ArcGISRuntime.Mapping.FeatureForms;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    internal class FieldTemplateSelector : DataTemplateSelector
    {
        private FieldFormElementView _parent;
        public FieldTemplateSelector(FieldFormElementView parent)
        {
            _parent = parent;
        }
#if MAUI
        protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
#else
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
#endif
        {
            if (item is FieldFormElement elm)
            {
                return elm.Input switch
                {
                    ComboBoxFormInput => _parent.ComboBoxFormInputTemplate,
                    SwitchFormInput => _parent.SwitchFormInputTemplate,
                    DateTimePickerFormInput => _parent.DateTimePickerFormInputTemplate,
                    RadioButtonsFormInput => _parent.RadioButtonsFormInputTemplate,
                    TextAreaFormInput => _parent.TextAreaFormInputTemplate,
                    TextBoxFormInput => _parent.TextBoxFormInputTemplate,
                    _ => base.SelectTemplate(item, container)
                };
            }
            return base.SelectTemplate(item, container);
        }
    }
}
#endif