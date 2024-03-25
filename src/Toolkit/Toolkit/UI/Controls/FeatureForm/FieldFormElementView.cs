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
using Esri.ArcGISRuntime.Toolkit.Maui;
using TextBlock = Microsoft.Maui.Controls.Label;
using DependencyProperty = Microsoft.Maui.Controls.BindableProperty;
#else
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Supporting control for the <see cref="FeatureFormView"/> control,
    /// used for rendering a <see cref="FieldFormElement"/> and picking the correct template for each Input type.
    /// </summary>
    public partial class FieldFormElementView
    {
        private WeakEventListener<FieldFormElementView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldFormElementView"/> class.
        /// </summary>
        public FieldFormElementView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
            ComboBoxFormInputTemplate = DefaultComboBoxFormInputTemplate;
            SwitchFormInputTemplate = DefaultSwitchFormInputTemplate;
            DateTimePickerFormInputTemplate = DefaultDateTimePickerFormInputTemplate;
            RadioButtonsFormInputTemplate = DefaultRadioButtonsFormInputTemplate;
            TextAreaFormInputTemplate = DefaultTextAreaFormInputTemplate;
            TextBoxFormInputTemplate = DefaultTextBoxFormInputTemplate;
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
            BindableProperty.Create(nameof(Element), typeof(FieldFormElement), typeof(FieldFormElementView), null, propertyChanged: (s, oldValue, newValue) => ((FieldFormElementView)s).OnElementPropertyChanged(oldValue as FieldFormElement, newValue as FieldFormElement));
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
                UpdateVisibility();
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
                Dispatch(OnValuePropertyChanged);
            }
            else if (e.PropertyName == nameof(FieldFormElement.ValidationErrors))
            {
                Dispatch(UpdateErrorMessages);
            }
            else if (e.PropertyName == nameof(FieldFormElement.IsVisible))
            {
                Dispatch(UpdateVisibility);
            }
        }

        private void UpdateVisibility()
        {
            bool isVisible =
                Element is not null && Element.IsVisible &&
                (Element.Input is ComboBoxFormInput ||
                Element.Input is SwitchFormInput ||
                Element.Input is DateTimePickerFormInput ||
                Element.Input is RadioButtonsFormInput ||
                Element.Input is TextAreaFormInput ||
                Element.Input is TextBoxFormInput);
#if MAUI
            this.IsVisible = isVisible;
#else
            this.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
#endif
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

        private void Dispatch(Action action)
        {
#if WPF
            if (Dispatcher.CheckAccess())
                action();
            else
                Dispatcher.Invoke(action);
#elif MAUI
                if (Dispatcher.IsDispatchRequired)
                    Dispatcher.Dispatch(action);
                else
                    action();
#endif
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
#if MAUI
            BindableProperty.Create(nameof(ComboBoxFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), null);
#else
            DependencyProperty.Register(nameof(ComboBoxFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));
#endif

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
#if MAUI
            BindableProperty.Create(nameof(SwitchFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), null);
#else
            DependencyProperty.Register(nameof(SwitchFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));
#endif

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
#if MAUI
            BindableProperty.Create(nameof(DateTimePickerFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), null);
#else
            DependencyProperty.Register(nameof(DateTimePickerFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));
#endif

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
#if MAUI
            BindableProperty.Create(nameof(RadioButtonsFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), null);
#else
            DependencyProperty.Register(nameof(RadioButtonsFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));
#endif

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
#if MAUI
            BindableProperty.Create(nameof(TextAreaFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), null);
#else
            DependencyProperty.Register(nameof(TextAreaFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));
#endif

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
#if MAUI
            BindableProperty.Create(nameof(TextBoxFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), null);
#else
            DependencyProperty.Register(nameof(TextBoxFormInputTemplate), typeof(DataTemplate), typeof(FieldFormElementView), new PropertyMetadata(null));
#endif
    }
}
#endif