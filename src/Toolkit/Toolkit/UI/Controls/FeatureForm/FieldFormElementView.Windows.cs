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

#if WPF
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;


namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.FeatureFormView"/> control,
    /// used for rendering a <see cref="FieldFormElement"/>.
    /// </summary>
    [TemplatePart(Name = FieldInputName, Type = typeof(ContentControl))]
    public partial class FieldFormElementView : Control
    {
        private const string FieldInputName = "FieldInput";
        private DataTemplateSelector? InputTemplateSelector;

        private void OnElementPropertyChanged()
        {
        }

        public DataTemplate? BarcodeScannerFormInputTemplate
        {
            get { return (DataTemplate)GetValue(BarcodeScannerFormInputTemplateProperty); }
            set { SetValue(BarcodeScannerFormInputTemplateProperty, value); }
        }

        public static readonly DependencyProperty BarcodeScannerFormInputTemplateProperty =
            DependencyProperty.Register(nameof(BarcodeScannerFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));

        public DataTemplate? ComboBoxFormInputTemplate
        {
            get { return (DataTemplate)GetValue(ComboBoxFormInputTemplateProperty); }
            set { SetValue(ComboBoxFormInputTemplateProperty, value); }
        }

        public static readonly DependencyProperty ComboBoxFormInputTemplateProperty =
            DependencyProperty.Register(nameof(ComboBoxFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));

        
        public DataTemplate? SwitchFormInputTemplate
        {
            get { return (DataTemplate)GetValue(SwitchFormInputTemplateProperty); }
            set { SetValue(SwitchFormInputTemplateProperty, value); }
        }

        public static readonly DependencyProperty SwitchFormInputTemplateProperty =
            DependencyProperty.Register(nameof(SwitchFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));


        public DataTemplate? DateTimePickerFormInputTemplate
        {
            get { return (DataTemplate)GetValue(DateTimePickerFormInputTemplateProperty); }
            set { SetValue(DateTimePickerFormInputTemplateProperty, value); }
        }

        public static readonly DependencyProperty DateTimePickerFormInputTemplateProperty =
            DependencyProperty.Register(nameof(DateTimePickerFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));

        public DataTemplate? RadioButtonsFormInputTemplate
        {
            get { return (DataTemplate)GetValue(RadioButtonsFormInputTemplateProperty); }
            set { SetValue(RadioButtonsFormInputTemplateProperty, value); }
        }

        public static readonly DependencyProperty RadioButtonsFormInputTemplateProperty =
            DependencyProperty.Register(nameof(RadioButtonsFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));
        public DataTemplate? TextAreaFormInputTemplate
        {
            get { return (DataTemplate)GetValue(TextAreaFormInputTemplateProperty); }
            set { SetValue(TextAreaFormInputTemplateProperty, value); }
        }

        public static readonly DependencyProperty TextAreaFormInputTemplateProperty =
            DependencyProperty.Register(nameof(TextAreaFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));


        public DataTemplate? TextBoxFormInputTemplate
        {
            get { return (DataTemplate)GetValue(TextBoxFormInputTemplateProperty); }
            set { SetValue(TextBoxFormInputTemplateProperty, value); }
        }

        public static readonly DependencyProperty TextBoxFormInputTemplateProperty =
            DependencyProperty.Register(nameof(TextBoxFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));

        private class FieldTemplateSelector : DataTemplateSelector
        {
            private FieldFormElementView _parent;
            public FieldTemplateSelector(FieldFormElementView parent)
            {
                _parent = parent;
            }
            public override DataTemplate? SelectTemplate(object item, DependencyObject container)
            {
                if (item is FieldFormElement elm)
                {
                    return elm.Input switch
                    {
                        BarcodeScannerFormInput => _parent.BarcodeScannerFormInputTemplate,
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
}
#endif