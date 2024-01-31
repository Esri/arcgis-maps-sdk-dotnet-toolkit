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
using System.Diagnostics;
using Esri.ArcGISRuntime.Mapping.FeatureForms;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.FeatureFormView"/> control,
    /// used for rendering a set of popup elements.
    /// </summary>
    public class FieldFormElementItemsControl : ItemsControl
    {
        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            if (element is ContentPresenter presenter)
            {
                if (item is BarcodeScannerFormInput)
                {
                    presenter.ContentTemplate = BarcodeScannerFormInputTemplate;
                }
                else if (item is ComboBoxFormInput)
                {
                    presenter.ContentTemplate = ComboBoxFormInputTemplate;
                }
                else if (item is DateTimePickerFormInput)
                {
                    presenter.ContentTemplate = DateTimePickerFormInputTemplate;
                }
                else if (item is RadioButtonsFormInput)
                {
                    presenter.ContentTemplate = RadioButtonsFormInputTemplate;
                }
                else if (item is SwitchFormInput)
                {
                    presenter.ContentTemplate = SwitchFormInputTemplate;
                }
                else if (item is TextAreaFormInput)
                {
                    presenter.ContentTemplate = TextAreaFormInputTemplate;
                }
                else if (item is TextBoxFormInput)
                {
                    presenter.ContentTemplate = TextBoxFormInputTemplate;
                }
            }
            base.PrepareContainerForItemOverride(element, item);
        }

        /// <summary>
        /// Template used for rendering a <see cref="BarcodeScannerFormInput"/>.
        /// </summary>
        /// <seealso cref="TextFormInputView"/>
        public DataTemplate BarcodeScannerFormInputTemplate
        {
            get { return (DataTemplate)GetValue(BarcodeScannerFormInputTemplateProperty); }
            set { SetValue(BarcodeScannerFormInputTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="BarcodeScannerFormInputTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BarcodeScannerFormInputTemplateProperty =
            DependencyProperty.Register(nameof(BarcodeScannerFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementItemsControl), new PropertyMetadata(null));

        /// <summary>
        /// Template used for rendering a <see cref="ComboBoxFormInput"/>.
        /// </summary>
        /// <seealso cref="ComboBoxFormInputView"/>
        public DataTemplate ComboBoxFormInputTemplate
        {
            get { return (DataTemplate)GetValue(ComboBoxFormInputTemplateProperty); }
            set { SetValue(ComboBoxFormInputTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ComboBoxFormInputTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ComboBoxFormInputTemplateProperty =
            DependencyProperty.Register(nameof(ComboBoxFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementItemsControl), new PropertyMetadata(null));

        /// <summary>
        /// Template used for rendering a <see cref="DateTimePickerFormInput"/>.
        /// </summary>
        /// <seealso cref="DateTimePickerFormInputView"/>
        public DataTemplate DateTimePickerFormInputTemplate
        {
            get { return (DataTemplate)GetValue(DateTimePickerFormInputTemplateProperty); }
            set { SetValue(DateTimePickerFormInputTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="DateTimePickerFormInputTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DateTimePickerFormInputTemplateProperty =
            DependencyProperty.Register(nameof(DateTimePickerFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementItemsControl), new PropertyMetadata(null));

        /// <summary>
        /// Template used for rendering a <see cref="RadioButtonsFormInput"/>.
        /// </summary>
        /// <seealso cref="RadioButtonsFormInputView"/>
        public DataTemplate RadioButtonsFormInputTemplate
        {
            get { return (DataTemplate)GetValue(RadioButtonsFormInputTemplateProperty); }
            set { SetValue(RadioButtonsFormInputTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="RadioButtonsFormInputTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RadioButtonsFormInputTemplateProperty =
            DependencyProperty.Register(nameof(RadioButtonsFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementItemsControl), new PropertyMetadata(null));

        /// <summary>
        /// Template used for rendering a <see cref="SwitchFormInput"/>.
        /// </summary>
        /// <seealso cref="SwitchFormInputView"/>
        public DataTemplate SwitchFormInputTemplate
        {
            get { return (DataTemplate)GetValue(SwitchFormInputTemplateProperty); }
            set { SetValue(SwitchFormInputTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SwitchFormInputTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SwitchFormInputTemplateProperty =
            DependencyProperty.Register(nameof(SwitchFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementItemsControl), new PropertyMetadata(null));

        /// <summary>
        /// Template used for rendering a <see cref="TextAreaFormInput"/>.
        /// </summary>
        /// <seealso cref="TextFormInputView"/>
        public DataTemplate TextAreaFormInputTemplate
        {
            get { return (DataTemplate)GetValue(TextAreaFormInputTemplateProperty); }
            set { SetValue(TextAreaFormInputTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TextAreaFormInputTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextAreaFormInputTemplateProperty =
            DependencyProperty.Register(nameof(TextAreaFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementItemsControl), new PropertyMetadata(null));


        /// <summary>
        /// Template used for rendering a <see cref="TextBoxFormInput"/>.
        /// </summary>
        /// <seealso cref="TextFormInputView"/>
        public DataTemplate TextBoxFormInputTemplate
        {
            get { return (DataTemplate)GetValue(TextBoxFormInputTemplateProperty); }
            set { SetValue(TextBoxFormInputTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TextBoxFormInputTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextBoxFormInputTemplateProperty =
            DependencyProperty.Register(nameof(TextBoxFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementItemsControl), new PropertyMetadata(null));
    }
}
#endif