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
using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;


#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
using TextBlock = Microsoft.Maui.Controls.Label;
#else
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives;
#endif

/// <summary>
/// Supporting control for the <see cref="FeatureFormView"/> control,
/// used for rendering a <see cref="FieldFormElement"/> and picking the correct template for each Input type.
/// </summary>
public partial class FieldFormElementView
{
    private DataTemplateSelector? InputTemplateSelector;

    /// <summary>
    /// Name of the FieldInput template part.
    /// </summary>
    public const string FieldInputName = "FieldInput";

    private WeakEventListener<FieldFormElementView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldFormElementView"/> class.
    /// </summary>
    public FieldFormElementView()
    {
#if MAUI
        ControlTemplate = DefaultControlTemplate;
#else
        DefaultStyleKey = typeof(FieldFormElementView);
#endif
    }
    
    /// <summary>
    /// Gets or sets the FeatureForm that the <see cref="Element"/> belongs to.
    /// </summary>
    public FeatureForm FeatureForm
    {
        get { return (FeatureForm)GetValue(FeatureFormProperty); }
        set { SetValue(FeatureFormProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="FeatureForm"/> dependency property.
    /// </summary>
#if MAUI
    public static readonly BindableProperty FeatureFormProperty =
        BindableProperty.Create(nameof(FeatureForm), typeof(FeatureForm), typeof(FieldFormElementView), null);
#else
    public static readonly DependencyProperty FeatureFormProperty =
        DependencyProperty.Register(nameof(FeatureForm), typeof(FeatureForm), typeof(FieldFormElementView), new PropertyMetadata(null));
#endif

    /// <summary>
    /// Gets or sets the FieldFormElement.
    /// </summary>
    public FieldFormElement? Element
    {
        get => GetValue(ElementProperty) as FieldFormElement;
        set => SetValue(ElementProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="Element"/> dependency property.
    /// </summary>
#if MAUI
    public static readonly BindableProperty ElementProperty =
        BindableProperty.Create(nameof(Element), typeof(FieldFormElement), typeof(FieldFormElementView), propertyChanged: (b,o,n) => ((FieldFormElementView)b).OnElementPropertyChanged(o as FieldFormElement, n as FieldFormElement));
#else
    public static readonly DependencyProperty ElementProperty =
        DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(FieldFormElementView), new PropertyMetadata(null, (s,e) => ((FieldFormElementView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));
#endif

    private void OnElementPropertyChanged(FieldFormElement? oldValue, FieldFormElement? newValue)
    {
        if (oldValue is INotifyPropertyChanged inpcOld)
        {
            _elementPropertyChangedListener?.Detach();
            _elementPropertyChangedListener = null;
        }
        if (newValue is INotifyPropertyChanged inpcNew)
        {
            _elementPropertyChangedListener = new WeakEventListener<FieldFormElementView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpcNew)
            {
                OnEventAction = static (instance, source, eventArgs) => instance.FieldFormElement_PropertyChanged(source, eventArgs),
                OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
            };
            inpcNew.PropertyChanged += _elementPropertyChangedListener.OnEvent;
#if MAUI
            var fin = GetTemplateChild(FieldInputName);
            if (fin is BindableObject inputBindable)
                BindableLayout.SetItemsSource(inputBindable, new[] { Element });
#endif
        }
    }

    private async void OnValuePropertyChanged()
    {
        if (Element is null || FeatureForm is null)
            return;
        try
        {
            await FeatureForm.EvaluateExpressionsAsync();
        }
        catch (System.Exception)
        {
        }
    }

    private void FieldFormElement_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FieldFormElement.Value))
        {
#if WPF
            if (Dispatcher.CheckAccess())
                OnValuePropertyChanged();
            else 
                Dispatcher.Invoke(OnValuePropertyChanged);
#elif MAUI
            if (Dispatcher.IsDispatchRequired)
                Dispatcher.Dispatch(OnValuePropertyChanged);
            else
                OnValuePropertyChanged();
#endif
        }
        else if (e.PropertyName == nameof(FieldFormElement.ValidationErrors))
        {
            UpdateErrorMessages();
        }
    }

    private void UpdateErrorMessages()
    {
        string? errMessage = null;
        if (Element is not null)
        {
            var errors = Element.ValidationErrors;

            if (errors != null && errors.Any())
            {
                errMessage = string.Join("\n", errors.Select(e => FeatureFormView.ValidationErrorToLocalizedString(Element, e)!));
            }
            else if (Element?.IsRequired == true && (Element.Value is null || Element?.Value is string str && string.IsNullOrEmpty(str)))
            {
                errMessage = Properties.Resources.GetString("FeatureFormFieldIsRequired");
            }
        }
        if (GetTemplateChild("ErrorLabel") is TextBlock tb)
        {
            tb.Text = errMessage;
        }
    }

    private class FieldTemplateSelector : DataTemplateSelector
    {
        private readonly FieldFormElementView _parent;

        public FieldTemplateSelector(FieldFormElementView parent)
        {
            _parent = parent;
        }

#if MAUI
        protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
#else
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
#endif
        {
            if (item is FieldFormElement ffe)
            {
                return ffe.Input switch
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
#endif