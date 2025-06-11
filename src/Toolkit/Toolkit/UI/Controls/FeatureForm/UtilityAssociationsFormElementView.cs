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

using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;

#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui;
using TextBlock = Microsoft.Maui.Controls.Label;
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
    /// used for rendering a <see cref="UtilityAssociationsFormElement"/> and picking the correct template for each Input type.
    /// </summary>
    public partial class UtilityAssociationsFormElementView
    {
        private WeakEventListener<UtilityAssociationsFormElementView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityAssociationsFormElementView"/> class.
        /// </summary>
        public UtilityAssociationsFormElementView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(UtilityAssociationsFormElementView);
#endif
        }

#if WINDOWS_XAML
        internal FeatureForm? FeatureForm => FeatureFormView.GetFeatureFormViewParent(this)?.FeatureForm;
#else
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
            BindableProperty.Create(nameof(FeatureForm), typeof(FeatureForm), typeof(UtilityAssociationsFormElementView), null);
#else
        public static readonly DependencyProperty FeatureFormProperty =
            DependencyProperty.Register(nameof(FeatureForm), typeof(FeatureForm), typeof(UtilityAssociationsFormElementView), new PropertyMetadata(null));
#endif
#endif
        /// <summary>
        /// Gets or sets the UtilityAssociationsFormElement.
        /// </summary>
        public UtilityAssociationsFormElement? Element
        {
            get => GetValue(ElementProperty) as UtilityAssociationsFormElement;
            set => SetValue(ElementProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty ElementProperty =
            BindableProperty.Create(nameof(Element), typeof(UtilityAssociationsFormElement), typeof(UtilityAssociationsFormElementView), null, propertyChanged: (s, oldValue, newValue) => ((UtilityAssociationsFormElementView)s).OnElementPropertyChanged(oldValue as UtilityAssociationsFormElement, newValue as UtilityAssociationsFormElement));
#else
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(UtilityAssociationsFormElement), typeof(UtilityAssociationsFormElementView), new PropertyMetadata(null, (s,e) => ((UtilityAssociationsFormElementView)s).OnElementPropertyChanged(e.OldValue as UtilityAssociationsFormElement, e.NewValue as UtilityAssociationsFormElement)));
#endif

        private void OnElementPropertyChanged(UtilityAssociationsFormElement? oldValue, UtilityAssociationsFormElement? newValue)
        {
            if (oldValue is INotifyPropertyChanged inpcOld)
            {
                _elementPropertyChangedListener?.Detach();
                _elementPropertyChangedListener = null;
            }
            if (newValue is INotifyPropertyChanged inpcNew)
            {
                _elementPropertyChangedListener = new WeakEventListener<UtilityAssociationsFormElementView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpcNew)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.UtilityAssociationsFormElement_PropertyChanged(source, eventArgs),
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                inpcNew.PropertyChanged += _elementPropertyChangedListener.OnEvent;
                RefreshAssociations();
                UpdateVisibility();
            }
        }

        private async void RefreshAssociations()
        {
            if (Element is not null)
            {
                try
                {
                    await Element.FetchAssociationsFilterResultsAsync();
                }
                catch { }
            }
        }

        private void UtilityAssociationsFormElement_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UtilityAssociationsFormElement.IsVisible))
            {
                this.Dispatch(UpdateVisibility);
            }
        }

        private void UpdateVisibility()
        {
            bool isVisible = (Element is not null && Element.IsVisible);
#if MAUI
            this.IsVisible = isVisible;
#else
            this.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
#endif
        }
    }
}