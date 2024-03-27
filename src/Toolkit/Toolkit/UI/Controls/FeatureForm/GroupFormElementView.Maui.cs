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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public class GroupFormElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private const string CollapsibleViewName = "CollapsibleView";
        private const string ClickableAreaName = "ClickableArea";        
        private const string GroupFormElementViewStyleName = "GroupFormElementViewStyle";
        private static readonly Style DefaultGroupFormElementViewStyle;
        private WeakEventListener<GroupFormElementView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;

        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement.IsVisible), "Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement.Label), "Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement.Description), "Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.GroupFormElement.Elements), "Esri.ArcGISRuntime.Mapping.FeatureForms.GroupFormElement", "Esri.ArcGISRuntime")]
        static GroupFormElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
            DefaultGroupFormElementViewStyle = new Style(typeof(FieldFormElementView));
            DefaultGroupFormElementViewStyle.Setters.Add(new Setter() { Property = BackgroundColorProperty, Value = Colors.White });
        }

        public GroupFormElementView()
        {
            ControlTemplate = DefaultControlTemplate;
        }
        private class InvertBoolConverter : IValueConverter
        {
            public static InvertBoolConverter Instance { get; } = new InvertBoolConverter();
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value is bool b) ? !b : value;

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
        }

        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.GroupFormElement.Label), "Esri.ArcGISRuntime.Mapping.FeatureForms.GroupFormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.GroupFormElement.Description), "Esri.ArcGISRuntime.Mapping.FeatureForms.GroupFormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.GroupFormElement.Elements), "Esri.ArcGISRuntime.Mapping.FeatureForms.GroupFormElement", "Esri.ArcGISRuntime")]
        private static object BuildDefaultTemplate()
        {
            
            var layout = new VerticalStackLayout();

            var clickAreaContent = new Grid() { VerticalOptions = new LayoutOptions(LayoutAlignment.Center, true) };
            clickAreaContent.RowDefinitions.Add(new RowDefinition());
            clickAreaContent.RowDefinitions.Add(new RowDefinition());
            clickAreaContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            clickAreaContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            var label = new Label() { Style = FeatureFormView.GetFeatureFormTitleStyle() };
            label.SetBinding(Label.TextProperty, new Binding("Element.Label", source: RelativeBindingSource.TemplatedParent));
            clickAreaContent.Children.Add(label);
            label = new Label() { Style = FeatureFormView.GetFeatureFormCaptionStyle() };
            label.SetBinding(Label.TextProperty, new Binding("Element.Description", source: RelativeBindingSource.TemplatedParent));
            Grid.SetRow(label, 1);
            clickAreaContent.Children.Add(label);

            Label collapsedChevron = new Label() { VerticalOptions = new LayoutOptions(LayoutAlignment.Center, true), FontFamily = "calcite-ui-icons-24" };
#if IOS
            collapsedChevron.Text = ""; // iOS use right-chevron for expand
#else
            collapsedChevron.Text = "";
#endif
            collapsedChevron.SetBinding(VisualElement.IsVisibleProperty, new Binding(nameof(IsExpanded), converter: InvertBoolConverter.Instance, converterParameter: "Inverse", source: RelativeBindingSource.TemplatedParent));
            Grid.SetRowSpan(collapsedChevron, 2);
            Grid.SetColumn(collapsedChevron, 1);
            clickAreaContent.Children.Add(collapsedChevron);
            
            Label expandedChevron = new Label() { Text = "", VerticalOptions = new LayoutOptions(LayoutAlignment.Center, true), FontFamily = "calcite-ui-icons-24" };
            expandedChevron.SetBinding(VisualElement.IsVisibleProperty, new Binding(nameof(IsExpanded), source: RelativeBindingSource.TemplatedParent));
            Grid.SetRowSpan(expandedChevron, 2);
            Grid.SetColumn(expandedChevron, 1);
            clickAreaContent.Children.Add(expandedChevron);

            layout.Children.Add(clickAreaContent);
            VerticalStackLayout itemsView = new VerticalStackLayout();
            BindableLayout.SetItemTemplateSelector(itemsView, new FeatureFormElementTemplateSelector());
            itemsView.SetBinding(BindableLayout.ItemsSourceProperty, new Binding("Element.Elements", source: RelativeBindingSource.TemplatedParent));
            layout.Children.Add(itemsView);
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(layout, nameScope);
            nameScope.RegisterName(ClickableAreaName, clickAreaContent);
            nameScope.RegisterName(CollapsibleViewName, itemsView);
            Border bottomDivider = new Border() { HeightRequest = 1 };
            bottomDivider.SetAppThemeColor(Border.BackgroundProperty, Colors.Black, Colors.White);
            layout.Children.Add(bottomDivider);
            layout.Style = FeatureFormView.GetStyle(GroupFormElementViewStyleName, DefaultGroupFormElementViewStyle);
            return layout;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild(CollapsibleViewName) is View view)
            {
                view.IsVisible = IsExpanded;
            }
            if(GetTemplateChild(ClickableAreaName) is View clickableArea)
            {
                clickableArea.GestureRecognizers.Add(
                    new TapGestureRecognizer() { Command = new Command(() => IsExpanded = !IsExpanded) });
            }
        }

        /// <summary>
        /// Gets or sets the GroupFormElement.
        /// </summary>
        public GroupFormElement? Element
        {
            get => GetValue(ElementProperty) as GroupFormElement;
            set => SetValue(ElementProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ElementProperty =
            BindableProperty.Create(nameof(Element), typeof(GroupFormElement), typeof(GroupFormElementView), null, propertyChanged: (s, oldValue, newValue) => ((GroupFormElementView)s).OnElementPropertyChanged(oldValue as GroupFormElement, newValue as GroupFormElement));

        /// <summary>
        /// Gets or sets a value indicating whether this expander is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsExpanded"/> dependency property.
        /// </summary>
        public static readonly BindableProperty IsExpandedProperty =
            BindableProperty.Create(nameof(IsExpanded), typeof(bool), typeof(GroupFormElementView), false, propertyChanged: (s, oldValue, newValue) => ((GroupFormElementView)s).OnIsExpandedPropertyChanged((bool)newValue));

        private void OnIsExpandedPropertyChanged(bool isExpanded)
        {
            if (GetTemplateChild(CollapsibleViewName) is View view)
            {
                view.IsVisible = isExpanded;
            }
        }

        private void OnElementPropertyChanged(GroupFormElement? oldValue, GroupFormElement? newValue)
        {
            if (oldValue is INotifyPropertyChanged inpcOld)
            {
                _elementPropertyChangedListener?.Detach();
                _elementPropertyChangedListener = null;
            }
            if (newValue is INotifyPropertyChanged inpcNew)
            {
                _elementPropertyChangedListener = new WeakEventListener<GroupFormElementView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpcNew)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.GroupFormElement_PropertyChanged(source, eventArgs),
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                inpcNew.PropertyChanged += _elementPropertyChangedListener.OnEvent;
                UpdateVisibility();
                UpdateState();
            }
        }

        private void UpdateState()
        {
            IsExpanded = Element?.InitialState == FormGroupState.Expanded;
        }

        private void GroupFormElement_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FormElement.IsVisible))
            {
                Dispatch(UpdateVisibility);
            }
        }

        private void UpdateVisibility()
        {
            this.IsVisible = Element!= null && Element.IsVisible && Element.Elements.Count > 0;
        }

        private void Dispatch(Action action)
        {
            if (Dispatcher.IsDispatchRequired)
                Dispatcher.Dispatch(action);
            else
                action();
        }
    }
}
#endif