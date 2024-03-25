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
using System.Diagnostics.CodeAnalysis;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.FeatureFormView"/> control,
    /// used for rendering a <see cref="FieldFormElement"/>.
    /// </summary>
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


        static FieldFormElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
            DefaultComboBoxFormInputTemplate = new DataTemplate(BuildDefaultComboBoxFormInputTemplate);
            DefaultSwitchFormInputTemplate = new DataTemplate(BuildDefaultSwitchFormInputTemplate);
            DefaultDateTimePickerFormInputTemplate = new DataTemplate(BuildDefaultDateTimePickerFormInputTemplate);
            DefaultRadioButtonsFormInputTemplate = new DataTemplate(BuildDefaultRadioButtonsFormInputTemplate);
            DefaultTextAreaFormInputTemplate = new DataTemplate(BuildDefaultTextAreaFormInputTemplate);
            DefaultTextBoxFormInputTemplate = new DataTemplate(BuildDefaultTextBoxFormInputTemplate);
        }

        private static object BuildDefaultComboBoxFormInputTemplate()
        {
            return new Label() { Text = "ComboBox goes here" };
        }

        private static object BuildDefaultSwitchFormInputTemplate()
        {
            return new Label() { Text = "Switch goes here" };
        }

        private static object BuildDefaultDateTimePickerFormInputTemplate()
        {
            return new Label() { Text = "DateTimePicker goes here" };
        }

        private static object BuildDefaultRadioButtonsFormInputTemplate()
        {
            return new Label() { Text = "RadioButtons goes here" };
        }

        private static object BuildDefaultTextAreaFormInputTemplate()
        {
            return new Label() { Text = "TextArea goes here" };
        }

        private static object BuildDefaultTextBoxFormInputTemplate()
        {
            return new Label() { Text = "TextBox goes here" };
        }

        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement.Label), "Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement.Description), "Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement", "Esri.ArcGISRuntime")]
        private static object BuildDefaultTemplate()
        {
            var root = new VerticalStackLayout();
            root.SetBinding(VerticalStackLayout.IsVisibleProperty, nameof(FormElement.IsVisible));
            var label = new Label() { Margin = new Thickness(0, 10) };
            label.SetBinding(Label.TextProperty, new Binding("Element.Label", source: RelativeBindingSource.TemplatedParent));
            label.SetBinding(View.IsVisibleProperty, new Binding("Text", source: RelativeBindingSource.Self, converter: new EmptyStringToBoolConverter()));
            root.Children.Add(label);
            label = new Label() { Margin = new Thickness(0, 10) };
            label.SetBinding(Label.TextProperty, new Binding("Element.Description", source: RelativeBindingSource.TemplatedParent));
            label.SetBinding(Label.IsVisibleProperty, new Binding("Text", source: RelativeBindingSource.Self, converter: new EmptyStringToBoolConverter()));
            root.Children.Add(label);
            var content = new DataTemplatedContentPresenter();
            content.SetBinding(DataTemplatedContentPresenter.ContentDataProperty, new Binding(nameof(Element), source: RelativeBindingSource.TemplatedParent));
            root.Children.Add(content);
            label = new Label() { Margin = new Thickness(0, 10), TextColor = Colors.Red };
            label.SetBinding(Label.IsVisibleProperty, new Binding("Text", source: RelativeBindingSource.Self, converter: new EmptyStringToBoolConverter()));

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(FieldInputName, content);
            nameScope.RegisterName(ErrorLabelName, label);
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
        }

        private class DataTemplatedContentPresenter : ContentPresenter
        {
            public DataTemplatedContentPresenter()
            {                
            }

            private void RefreshContent()
            {
                this.Content = DataTemplateSelector?.SelectTemplate(ContentData, this)?.CreateContent() as View;
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