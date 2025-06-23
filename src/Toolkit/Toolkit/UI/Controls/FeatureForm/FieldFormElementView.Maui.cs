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

#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.FeatureForms;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class FieldFormElementView : TemplatedView
    {
        private const string FieldInputName = "FieldInput";
        private const string ErrorLabelName = "ErrorLabel";
        private static readonly ControlTemplate DefaultControlTemplate;
        private static readonly DataTemplate DefaultComboBoxFormInputTemplate;
        private static readonly DataTemplate DefaultSwitchFormInputTemplate;
        private static readonly DataTemplate DefaultDateTimePickerFormInputTemplate;
        private static readonly DataTemplate DefaultRadioButtonsFormInputTemplate;
        private static readonly DataTemplate DefaultTextAreaFormInputTemplate;
        private static readonly DataTemplate DefaultTextBoxFormInputTemplate;
        private static readonly DataTemplate DefaultBarcodeScannerFormInputTemplate;


        static FieldFormElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
            DefaultComboBoxFormInputTemplate = new DataTemplate(BuildDefaultComboBoxFormInputTemplate);
            DefaultSwitchFormInputTemplate = new DataTemplate(BuildDefaultSwitchFormInputTemplate);
            DefaultDateTimePickerFormInputTemplate = new DataTemplate(BuildDefaultDateTimePickerFormInputTemplate);
            DefaultRadioButtonsFormInputTemplate = new DataTemplate(BuildDefaultRadioButtonsFormInputTemplate);
            DefaultTextAreaFormInputTemplate = new DataTemplate(BuildDefaultTextAreaFormInputTemplate);
            DefaultTextBoxFormInputTemplate = new DataTemplate(BuildDefaultTextBoxFormInputTemplate);
            DefaultBarcodeScannerFormInputTemplate = new DataTemplate(BuildDefaultBarcodeScannerFormInputTemplate);
        }

        private static object BuildDefaultComboBoxFormInputTemplate()
        {
            var input = new ComboBoxFormInputView();
            input.SetBinding(ComboBoxFormInputView.ElementProperty, static (FormElement element) => element);
            return input;
        }

        private static object BuildDefaultSwitchFormInputTemplate()
        {
            var input = new SwitchFormInputView();
            input.SetBinding(SwitchFormInputView.ElementProperty, static (FormElement element) => element);
            return input;
        }

        private static object BuildDefaultDateTimePickerFormInputTemplate()
        {
            var input = new DateTimePickerFormInputView();
            input.SetBinding(DateTimePickerFormInputView.ElementProperty, static (FormElement element) => element);
            return input;
        }

        private static object BuildDefaultRadioButtonsFormInputTemplate()
        {
            var input = new RadioButtonsFormInputView();
            input.SetBinding(RadioButtonsFormInputView.ElementProperty, static (FormElement element) => element);
            return input;
        }

        private static object BuildDefaultTextAreaFormInputTemplate()
        {
            var input = new TextFormInputView();
            input.SetBinding(TextFormInputView.ElementProperty, static (FormElement element) => element);
            return input;
        }

        private static object BuildDefaultTextBoxFormInputTemplate()
        {
            var input = new TextFormInputView();
            input.SetBinding(TextFormInputView.ElementProperty, static (FormElement element) => element);
            return input;
        }

        private static object BuildDefaultBarcodeScannerFormInputTemplate()
        {
            var input = new TextFormInputView();
            input.SetBinding(TextFormInputView.ElementProperty, static (FormElement element) => element);
            return input;
        }
        
        private static object BuildDefaultTemplate()
        {
            var root = new VerticalStackLayout();
            root.SetBinding(VerticalStackLayout.IsVisibleProperty, static (FieldFormElementView view) => view.Element?.IsVisible);
            var label = new Label();
            label.SetBinding(Label.TextProperty, static (FieldFormElementView view) => view.Element?.Label, source: RelativeBindingSource.TemplatedParent);
            label.SetBinding(View.IsVisibleProperty, static (Label lbl) => lbl.Text, source: RelativeBindingSource.Self, converter: EmptyStringToBoolConverter.Instance);
            label.Style = FeatureFormView.GetFeatureFormTitleStyle();
            root.Children.Add(label);
            label = new Label();
            label.SetBinding(Label.TextProperty, static (FieldFormElementView view) => view.Element?.Description, source: RelativeBindingSource.TemplatedParent);
            label.SetBinding(Label.IsVisibleProperty, static (Label lbl) => lbl.Text, source: RelativeBindingSource.Self, converter: EmptyStringToBoolConverter.Instance);
            label.Style = FeatureFormView.GetFeatureFormCaptionStyle();
            root.Children.Add(label);
            var content = new DataTemplatedContentPresenter();
            content.SetBinding(DataTemplatedContentPresenter.ContentDataProperty, static (FieldFormElementView view) => view.Element, source: RelativeBindingSource.TemplatedParent);
            root.Children.Add(content);
            var errorLabel = new Label() { Margin = new Thickness(0, 2), TextColor = Colors.Red };
            root.Children.Add(errorLabel);
            errorLabel.SetBinding(Label.IsVisibleProperty, static (Label lbl) => lbl.Text, source: RelativeBindingSource.Self, converter: EmptyStringToBoolConverter.Instance);

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(FieldInputName, content);
            nameScope.RegisterName(ErrorLabelName, errorLabel);
            return root;
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild(FieldInputName) is DataTemplatedContentPresenter content)
            {
                content.DataTemplateSelector = new FieldTemplateSelector(this);
            }
            UpdateErrorMessages();
        }

        private class DataTemplatedContentPresenter : ContentPresenter
        {
            public DataTemplatedContentPresenter()
            {                
            }

            private void RefreshContent()
            {
                var view = DataTemplateSelector?.CreateContent(ContentData, this) as View;
                if (view is not null)
                    view.BindingContext = ContentData;
                this.Content = view;
            }

            public DataTemplateSelector? DataTemplateSelector { get; set; }

            public object ContentData
            {
                get { return (object)GetValue(ContentDataProperty); }
                set { SetValue(ContentDataProperty, value); }
            }

            public static readonly BindableProperty ContentDataProperty =
                BindableProperty.Create(nameof(ContentData), typeof(object), typeof(DataTemplatedContentPresenter), null, 
                    propertyChanged: (s, o, n) => ((DataTemplatedContentPresenter) s).RefreshContent());
        }
    }
}
#endif