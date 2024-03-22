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

        /// <inheritdoc />
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            var content = GetTemplateChild(FieldInputName) as ContentControl;
            if (content != null)
            {
                if (InputTemplateSelector == null)
                    InputTemplateSelector = new FieldTemplateSelector(this);
                content.ContentTemplateSelector = InputTemplateSelector;
            }
            UpdateErrorMessages();
        }

        /// <summary>
        /// Gets or sets the template for the <see cref="ComboBoxFormInput"/> element.
        /// </summary>
        public DataTemplate? ComboBoxFormInputTemplate
        {
            get { return (DataTemplate)GetValue(ComboBoxFormInputTemplateProperty); }
            set { SetValue(ComboBoxFormInputTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ComboBoxFormInputTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ComboBoxFormInputTemplateProperty =
            DependencyProperty.Register(nameof(ComboBoxFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the template for the <see cref="SwitchFormInput"/> element.
        /// </summary>
        public DataTemplate? SwitchFormInputTemplate
        {
            get { return (DataTemplate)GetValue(SwitchFormInputTemplateProperty); }
            set { SetValue(SwitchFormInputTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SwitchFormInputTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SwitchFormInputTemplateProperty =
            DependencyProperty.Register(nameof(SwitchFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the template for the <see cref="DateTimePickerFormInput"/> element.
        /// </summary>
        public DataTemplate? DateTimePickerFormInputTemplate
        {
            get { return (DataTemplate)GetValue(DateTimePickerFormInputTemplateProperty); }
            set { SetValue(DateTimePickerFormInputTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="DateTimePickerFormInputTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DateTimePickerFormInputTemplateProperty =
            DependencyProperty.Register(nameof(DateTimePickerFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the template for the <see cref="RadioButtonsFormInput"/> element.
        /// </summary>
        public DataTemplate? RadioButtonsFormInputTemplate
        {
            get { return (DataTemplate)GetValue(RadioButtonsFormInputTemplateProperty); }
            set { SetValue(RadioButtonsFormInputTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="RadioButtonsFormInputTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RadioButtonsFormInputTemplateProperty =
            DependencyProperty.Register(nameof(RadioButtonsFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the template for the <see cref="TextAreaFormInput"/> element.
        /// </summary>
        public DataTemplate? TextAreaFormInputTemplate
        {
            get { return (DataTemplate)GetValue(TextAreaFormInputTemplateProperty); }
            set { SetValue(TextAreaFormInputTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TextAreaFormInputTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextAreaFormInputTemplateProperty =
            DependencyProperty.Register(nameof(TextAreaFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the template for the <see cref="TextBoxFormInputTemplate"/> element.
        /// </summary>
        public DataTemplate? TextBoxFormInputTemplate
        {
            get { return (DataTemplate)GetValue(TextBoxFormInputTemplateProperty); }
            set { SetValue(TextBoxFormInputTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TextBoxFormInputTemplate"/> dependency property.
        /// </summary>
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