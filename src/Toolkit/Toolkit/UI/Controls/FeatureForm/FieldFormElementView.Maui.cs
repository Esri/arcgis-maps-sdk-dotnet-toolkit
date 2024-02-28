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
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Microsoft.Maui.Controls.Internals;
using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

/// <summary>
/// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.FeatureFormView"/> control,
/// used for rendering a <see cref="FieldFormElement"/>.
/// </summary>
public partial class FieldFormElementView : ContentView
{
    private static readonly ControlTemplate DefaultControlTemplate;

    static FieldFormElementView()
    {
        DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);

        DefaultBarcodeScannerFormInputTemplate = new DataTemplate(() =>
        {
            var view = new Entry();
            view.SetBinding(Entry.TextProperty, new Binding("Value", source: RelativeBindingSource.TemplatedParent));
            view.SetBinding(Entry.IsReadOnlyProperty, new Binding("IsEditable", BindingMode.OneWay, InvertBooleanConverter.Instance, source: RelativeBindingSource.TemplatedParent));
            return view;
        });
        DefaultTextBoxFormInputTemplate = DefaultBarcodeScannerFormInputTemplate;

        DefaultComboBoxFormInputTemplate = new DataTemplate(() =>
        {
            var view = new Picker();
            view.SetBinding(Picker.ItemsSourceProperty, new Binding("Domain.CodedValues", source: RelativeBindingSource.TemplatedParent));
            view.SetBinding(Picker.SelectedItemProperty, new Binding("Value", source: RelativeBindingSource.TemplatedParent));
            view.SetBinding(Picker.IsEnabledProperty, new Binding("IsEditable", BindingMode.OneWay, source: RelativeBindingSource.TemplatedParent));
            return view;
        });

        DefaultSwitchFormInputTemplate = new DataTemplate(() =>
        {
            var view = new Switch();
            view.SetBinding(Switch.IsToggledProperty, new Binding("Value", source: RelativeBindingSource.TemplatedParent));
            view.SetBinding(Switch.IsEnabledProperty, new Binding("IsEditable", BindingMode.OneWay, source: RelativeBindingSource.TemplatedParent));
            return view;
        });

        DefaultDateTimePickerFormInputTemplate = new DataTemplate(() =>
        {
            var view = new DatePicker();
            view.SetBinding(DatePicker.DateProperty, new Binding("Value", source: RelativeBindingSource.TemplatedParent));
            view.SetBinding(DatePicker.IsEnabledProperty, new Binding("IsEditable", BindingMode.OneWay, source: RelativeBindingSource.TemplatedParent));
            return view;
        });

        DefaultRadioButtonsFormInputTemplate = new DataTemplate(() =>
        {
            var group = new VerticalStackLayout();
            BindableLayout.SetItemTemplate(group, new DataTemplate(() =>
            {
                var radio = new RadioButton();
                radio.SetBinding(RadioButton.ContentProperty, new Binding("Name", source: RelativeBindingSource.TemplatedParent));
                return radio;
            }));
            group.SetBinding(BindableLayout.ItemsSourceProperty, new Binding("Domain.CodedValues", source: RelativeBindingSource.TemplatedParent));
            return group;
        });
        
        DefaultTextAreaFormInputTemplate = new DataTemplate(() =>
        {
            var view = new Editor();
            view.SetBinding(Editor.TextProperty, new Binding("Value", source: RelativeBindingSource.TemplatedParent));
            view.SetBinding(Editor.IsReadOnlyProperty, new Binding("IsEditable", BindingMode.OneWay, InvertBooleanConverter.Instance, source: RelativeBindingSource.TemplatedParent));
            return view;
        });
    }

    private static object BuildDefaultTemplate()
    {
        VerticalStackLayout stack = new();
        var label = new Label();
        label.SetBinding(Label.TextProperty, new Binding("Element.Label", source: RelativeBindingSource.TemplatedParent));
        stack.Add(label);

        var inputView = new Grid(); // has to be a layout so that we can use BindableLayout item templating
        inputView.SetBinding(ContentProperty, new Binding("Element", source: RelativeBindingSource.TemplatedParent));
        stack.Add(inputView);

        var description = new Label();
        description.SetBinding(Label.TextProperty, new Binding("Element.Description", source: RelativeBindingSource.TemplatedParent));
        // TODO toggle visibility based on whether the description is empty
        stack.Add(description);

        var errors = new Label();
        errors.TextColor = Colors.Red;
        // TODO hide validation errors when text is empty
        stack.Add(errors);
        
        INameScope nameScope = new NameScope();
        NameScope.SetNameScope(stack, nameScope);
        nameScope.RegisterName(FieldInputName, inputView);
        nameScope.RegisterName("ErrorLabel", errors);

        return stack;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (GetTemplateChild(FieldInputName) is Grid content)
        {
            InputTemplateSelector ??= new FieldTemplateSelector(this);
            BindableLayout.SetItemTemplateSelector(content, InputTemplateSelector);
        }
    }

    public static DataTemplate DefaultBarcodeScannerFormInputTemplate { get; }
    public static DataTemplate DefaultComboBoxFormInputTemplate { get; }
    public static DataTemplate DefaultSwitchFormInputTemplate { get; }
    public static DataTemplate DefaultDateTimePickerFormInputTemplate { get; }
    public static DataTemplate DefaultRadioButtonsFormInputTemplate { get; }
    public static DataTemplate DefaultTextAreaFormInputTemplate { get; }
    public static DataTemplate DefaultTextBoxFormInputTemplate { get; }

    public DataTemplate? BarcodeScannerFormInputTemplate { get; set; } = DefaultBarcodeScannerFormInputTemplate;
    public DataTemplate? ComboBoxFormInputTemplate { get; set; } = DefaultComboBoxFormInputTemplate;
    public DataTemplate? SwitchFormInputTemplate { get; set; } = DefaultSwitchFormInputTemplate;
    public DataTemplate? DateTimePickerFormInputTemplate { get; set; } = DefaultDateTimePickerFormInputTemplate;
    public DataTemplate? RadioButtonsFormInputTemplate { get; set; } = DefaultRadioButtonsFormInputTemplate;
    public DataTemplate? TextAreaFormInputTemplate { get; set; } = DefaultTextAreaFormInputTemplate;
    public DataTemplate? TextBoxFormInputTemplate { get; set; } = DefaultTextBoxFormInputTemplate;

    private class InvertBooleanConverter : IValueConverter
    {
        public static InvertBooleanConverter Instance { get; } = new();
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is false;
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is true;
    }
}
#endif