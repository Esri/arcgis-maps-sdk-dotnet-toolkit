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
using Esri.ArcGISRuntime.UtilityNetworks;

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
    public partial class UtilityAssociationsFilterResultsView
    {
        private Button? _addAssociationButton;
        private UtilityAssociationsFormElement? _associationsFormElement;
        private WeakEventListener<UtilityAssociationsFilterResultsView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityAssociationsFilterResultsView"/> class.
        /// </summary>
        public UtilityAssociationsFilterResultsView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(UtilityAssociationsFilterResultsView);
#endif
            Loaded += UtilityAssociationsFilterResultsView_Loaded;
            Unloaded += UtilityAssociationsFilterResultsView_Unloaded;
        }

        /// <summary>
        /// Gets or sets the AssociationsFilterResults.
        /// </summary>
        public UtilityAssociationsFilterResult? AssociationsFilterResult
        {
            get => GetValue(AssociationsFilterResultProperty) as UtilityAssociationsFilterResult;
            set => SetValue(AssociationsFilterResultProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AssociationsFilterResult"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty AssociationsFilterResultProperty =
            BindableProperty.Create(
                nameof(AssociationsFilterResult),
                typeof(UtilityAssociationsFilterResult),
                typeof(UtilityAssociationsFilterResultsView),
                null,
                propertyChanged: static (bindable, oldValue, newValue) => ((UtilityAssociationsFilterResultsView)bindable).UpdateAddAssociationSupport());
#else
        public static readonly DependencyProperty AssociationsFilterResultProperty =
            DependencyProperty.Register(
                nameof(AssociationsFilterResult),
                typeof(UtilityAssociationsFilterResult),
                typeof(UtilityAssociationsFilterResultsView),
                new PropertyMetadata(null, static (sender, args) => ((UtilityAssociationsFilterResultsView)sender).UpdateAddAssociationSupport()));
#endif

        private void UpdateAddAssociationButton()
        {
            if (_addAssociationButton is not null)
            {
#if MAUI
                _addAssociationButton.Clicked -= AddAssociationButton_Clicked;
#else
                _addAssociationButton.Click -= AddAssociationButton_Clicked;
#endif
            }

            _addAssociationButton = GetTemplateChild("AddAssociationButton") as Button;
            if (_addAssociationButton is not null)
            {
#if MAUI
                _addAssociationButton.Clicked += AddAssociationButton_Clicked;
#else
                _addAssociationButton.Click += AddAssociationButton_Clicked;
#endif
            }

            UpdateAddAssociationSupport();
        }

        private void UpdateAddAssociationSupport()
        {
            var parent = FeatureFormView.GetFeatureFormViewParent(this);
            var form = parent?.CurrentFeatureForm;
            var result = AssociationsFilterResult;
            var element = form is null || result is null
                ? null
                : form.Elements.OfType<UtilityAssociationsFormElement>().FirstOrDefault(candidate => candidate.AssociationsFilterResults.Contains(result));

            if (!ReferenceEquals(element, _associationsFormElement))
            {
                _elementPropertyChangedListener?.Detach();
                _elementPropertyChangedListener = null;
                _associationsFormElement = element;

                if (element is INotifyPropertyChanged notifyPropertyChanged)
                {
                    _elementPropertyChangedListener = new WeakEventListener<UtilityAssociationsFilterResultsView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, notifyPropertyChanged)
                    {
                        OnEventAction = static (instance, source, eventArgs) => instance.AssociationsFormElement_PropertyChanged(eventArgs),
                        OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                    };
                    notifyPropertyChanged.PropertyChanged += _elementPropertyChangedListener.OnEvent;
                }
            }

            SetAddAssociationButtonVisibility(element?.IsEditable == true);
        }

        private void AssociationsFormElement_PropertyChanged(PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName == nameof(UtilityAssociationsFormElement.IsEditable))
            {
                this.Dispatch(UpdateAddAssociationSupport);
            }
        }

        private void SetAddAssociationButtonVisibility(bool isVisible)
        {
            if (_addAssociationButton is null)
            {
                return;
            }

#if MAUI
            _addAssociationButton.IsVisible = isVisible;
#else
            _addAssociationButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
#endif
        }

#if MAUI
        private void UtilityAssociationsFilterResultsView_Loaded(object? sender, EventArgs e)
#else
        private void UtilityAssociationsFilterResultsView_Loaded(object sender, RoutedEventArgs e)
#endif
        {
            UpdateAddAssociationSupport();
        }

#if MAUI
        private void UtilityAssociationsFilterResultsView_Unloaded(object? sender, EventArgs e)
#else
        private void UtilityAssociationsFilterResultsView_Unloaded(object sender, RoutedEventArgs e)
#endif
        {
            _elementPropertyChangedListener?.Detach();
            _elementPropertyChangedListener = null;
            _associationsFormElement = null;
        }

#if MAUI
        private void AddAssociationButton_Clicked(object? sender, EventArgs e)
#else
        private void AddAssociationButton_Clicked(object sender, RoutedEventArgs e)
#endif
        {
            ShowAddAssociationMenu(sender);
        }

        private bool CanSelectAssociationOnMap()
        {
            var parent = FeatureFormView.GetFeatureFormViewParent(this);
            return parent?.GeoView is MapView mapView && mapView.Map is not null;
        }

        private void SelectFromNetworkDataSource()
        {
            var parent = FeatureFormView.GetFeatureFormViewParent(this);
            var form = parent?.CurrentFeatureForm;
            var element = _associationsFormElement;
            var filter = AssociationsFilterResult?.Filter;
            if (parent is null || form is null || element is null || filter is null)
            {
                return;
            }

            parent.NavigateToItem(new UtilityAssociationFeatureSourceSelection(
                form,
                element,
                filter,
                parent.NavigateToItem));
        }

        private partial void ShowAddAssociationMenu(object? flyoutTarget);
    }
}