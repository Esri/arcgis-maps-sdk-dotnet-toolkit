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

using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Checkbox switch for the <see cref="SwitchFormInput"/>.
    /// </summary>
    public partial class SwitchFormInputView
    {
        private WeakEventListener<SwitchFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;

        /// <summary>
        /// Initializes an instance of the <see cref="SwitchFormInputView"/> class.
        /// </summary>
        public SwitchFormInputView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(SwitchFormInputView);
#endif
        }

        /// <summary>
        /// Gets or sets the FieldFormElement.
        /// </summary>
        public FieldFormElement? Element
        {
            get { return (FieldFormElement)GetValue(ElementProperty); }
            set { SetValue(ElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty ElementProperty =
            BindableProperty.Create(nameof(Element), typeof(FieldFormElement), typeof(SwitchFormInputView), null, propertyChanged: (s, oldValue, newValue) => ((SwitchFormInputView)s).OnElementPropertyChanged(oldValue as FieldFormElement, newValue as FieldFormElement));
#else
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(FieldFormElement), typeof(SwitchFormInputView), new PropertyMetadata(null, (s, e) => ((SwitchFormInputView)s).OnElementPropertyChanged(e.OldValue as FieldFormElement, e.NewValue as FieldFormElement)));
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
                _elementPropertyChangedListener = new WeakEventListener<SwitchFormInputView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpcNew)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.Element_PropertyChanged(source, eventArgs),
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                inpcNew.PropertyChanged += _elementPropertyChangedListener.OnEvent;
            }
            UpdateCheckState();
#if MAUI && WINDOWS
            UpdateOnOffContent();
#endif
        }

        private void Element_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FieldFormElement.Value))
            {
#if MAUI
                this.Dispatch(UpdateCheckState);
#else
                this.Dispatch(UpdateCheckState);
#endif
            }

#if MAUI
            if (e.PropertyName == nameof(FieldFormElement.IsEditable))
            {
                Dispatcher.Dispatch(UpdateEditableState);
            }
#endif
        }

#if !WPF
        /// <summary>
        /// Gets or sets a value indicating whether this switch is checked or not.
        /// </summary>
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FeatureForm"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty =
            PropertyHelper.CreateProperty<bool, SwitchFormInputView>(nameof(Element), false, (s, oldValue, newValue) => s.OnIsCheckedPropertyChanged(newValue));

        private void OnIsCheckedPropertyChanged(bool newValue)
        {
            if (newValue)
                OnChecked();
            else
                OnUnchecked();
        }
#endif

        private void OnChecked()
        {
            if (Element is not null && Element.Input is SwitchFormInput input && !object.Equals(input.OnValue.Code, Element.Value))
            {
                Element.UpdateValue(input.OnValue.Code);
            }
        }

        private void OnUnchecked()
        {
            if (Element is not null && Element.Input is SwitchFormInput input && !object.Equals(input.OffValue.Code, Element.Value))
            {
                Element.UpdateValue(input.OffValue.Code);
            }
        }

        private void UpdateCheckState()
        {
            if (Element is not null && Element.Input is SwitchFormInput input)
            {
                IsChecked = object.Equals(input.OnValue.Code, Element.Value);
            }
            else
            {
#if MAUI || WINDOWS_XAML
                IsChecked = false;
#else
                IsChecked = null;
#endif
            }
        }
    }
}