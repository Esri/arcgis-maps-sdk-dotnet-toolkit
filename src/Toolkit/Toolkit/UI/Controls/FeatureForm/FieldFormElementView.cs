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
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;

#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui;
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
        /// <summary>
        /// Initializes a new instance of the <see cref="FieldFormElementView"/> class.
        /// </summary>
        public FieldFormElementView()
        {
#if MAUI
            //ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(FieldFormElementView);
#endif
        }

        /// <inheritdoc />
#if WINDOWS_XAML || MAUI
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
#if !MAUI
            var content = GetTemplateChild(FieldInputName) as ContentControl;
            if (content != null)
            {
                if (InputTemplateSelector == null)
                    InputTemplateSelector = new FieldTemplateSelector(this);
                content.ContentTemplateSelector = InputTemplateSelector;
            }
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
            BindableProperty.Create(nameof(Element), typeof(FieldFormElement), typeof(FieldFormElementView), null);
#else
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(FieldFormElementView), new PropertyMetadata(null, (s,e) => ((FieldFormElementView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));
#endif

        private void OnElementPropertyChanged(FieldFormElement? oldValue, FieldFormElement? newValue)
        {
            if (oldValue is not null)
            {
                ((System.ComponentModel.INotifyPropertyChanged)oldValue).PropertyChanged += FieldFormElement_PropertyChanged; //TODO: Weak
            }
            if (newValue is not null)
            {
                ((System.ComponentModel.INotifyPropertyChanged)newValue).PropertyChanged += FieldFormElement_PropertyChanged;
            }
        }

        private async void OnValuePropertyChanged()
        {
            if (Element is null || FeatureForm is null)
                return;
            try
            {
                await FeatureForm.EvaluateExpressionsAsync();
                var errors = Element?.GetValidationErrors();

                string? errMessage = null;
                if (errors != null && errors.Any())
                {
                    errMessage = string.Join("\n", errors.Select(e => e.Message));
                }
                else if (Element?.IsRequired == true && (Element.Value is null || Element?.Value is string str && string.IsNullOrEmpty(str)))
                {
                    errMessage = "Required";
                }
#if WPF
                if (GetTemplateChild("ErrorLabel") is TextBlock tb)
                {
                    tb.Text = errMessage;
                }
#endif

            }
            catch (System.Exception)
            {
                //var err = Element?.GetValidationErrors();
            }
        }

        private void FieldFormElement_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FieldFormElement.Value))
            {
                OnValuePropertyChanged();
            }
        }
    }
}
#endif