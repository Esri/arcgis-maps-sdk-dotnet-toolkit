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
using Microsoft.Maui.Controls.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Groups a set of <see cref="FieldFormElementView"/> views in a collapsible group.
    /// </summary>
    public class GroupFormElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        //private static readonly Style DefaultGroupFormElementViewStyle;
        private const string CollapsibleViewName = "CollapsibleView";
        private const string ClickableAreaName = "ClickableArea";        
        private WeakEventListener<GroupFormElementView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;

        static GroupFormElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupFormElementView"/> class.
        /// </summary>
        public GroupFormElementView()
        {
            this.SetAppThemeColor(BorderStrokeProperty, Colors.Black, Colors.White);
            this.SetAppThemeColor(HeaderBackgroundProperty, Color.FromRgb(225,225,225), Colors.DarkGray);
            ControlTemplate = DefaultControlTemplate;
        }

        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement.Label), "Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement.Description), "Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.GroupFormElement.Elements), "Esri.ArcGISRuntime.Mapping.FeatureForms.GroupFormElement", "Esri.ArcGISRuntime")]
        private static object BuildDefaultTemplate()
        {
            Border root = new Border() { StrokeShape = new RoundRectangle() { CornerRadius = 2 } };
            root.SetBinding(Border.StrokeProperty, new Binding(nameof(BorderStroke), source: RelativeBindingSource.TemplatedParent));
            root.SetBinding(Border.StrokeThicknessProperty, new Binding(nameof(BorderStrokeThickness), source: RelativeBindingSource.TemplatedParent));

            var layout = new VerticalStackLayout();
            var clickAreaContent = new Grid() { VerticalOptions = new LayoutOptions(LayoutAlignment.Center, true) };
            clickAreaContent.SetBinding(Grid.BackgroundProperty, new Binding(nameof(HeaderBackground), source: RelativeBindingSource.TemplatedParent));
            clickAreaContent.SetBinding(Grid.PaddingProperty, new Binding(nameof(ContentPadding), source: RelativeBindingSource.TemplatedParent));
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
            collapsedChevron.Text = "\uE078"; // iOS use right-chevron for expand
#else
            collapsedChevron.Text = "\uE076";
#endif
            collapsedChevron.SetBinding(VisualElement.IsVisibleProperty, new Binding(nameof(IsExpanded), converter: Internal.InvertBoolConverter.Instance, source: RelativeBindingSource.TemplatedParent));
            Grid.SetRowSpan(collapsedChevron, 2);
            Grid.SetColumn(collapsedChevron, 1);
            clickAreaContent.Children.Add(collapsedChevron);
            
            Label expandedChevron = new Label() { Text = "\uE079", VerticalOptions = new LayoutOptions(LayoutAlignment.Center, true), FontFamily = "calcite-ui-icons-24" };
            expandedChevron.SetBinding(VisualElement.IsVisibleProperty, new Binding(nameof(IsExpanded), source: RelativeBindingSource.TemplatedParent));
            Grid.SetRowSpan(expandedChevron, 2);
            Grid.SetColumn(expandedChevron, 1);
            clickAreaContent.Children.Add(expandedChevron);

            layout.Children.Add(clickAreaContent);
            VerticalStackLayout itemsView = new VerticalStackLayout();
            itemsView.SetBinding(VerticalStackLayout.MarginProperty, new Binding(nameof(ContentPadding), source: RelativeBindingSource.TemplatedParent));
            BindableLayout.SetItemTemplateSelector(itemsView, new FeatureFormElementTemplateSelector());
            itemsView.SetBinding(BindableLayout.ItemsSourceProperty, new Binding("Element.Elements", source: RelativeBindingSource.TemplatedParent));
            layout.Children.Add(itemsView);
            root.Content = layout;
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(ClickableAreaName, clickAreaContent);
            nameScope.RegisterName(CollapsibleViewName, itemsView);
            return root;
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

        /// <summary>
        /// Gets or sets the brush used for the header background
        /// </summary>
        public Brush HeaderBackground
        {
            get => (Brush)GetValue(HeaderBackgroundProperty);
            set => SetValue(HeaderBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderBackground"/> dependency property.
        /// </summary>
        public static readonly BindableProperty HeaderBackgroundProperty =
            BindableProperty.Create(nameof(HeaderBackground), typeof(Brush), typeof(GroupFormElementView), null);

        /// <summary>
        /// Gets or sets the border stroke brush for the group
        /// </summary>
        public Brush BorderStroke
        {
            get => (Brush)GetValue(BorderStrokeProperty);
            set => SetValue(BorderStrokeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BorderStroke"/> dependency property.
        /// </summary>
        public static readonly BindableProperty BorderStrokeProperty =
            BindableProperty.Create(nameof(BorderStroke), typeof(Brush), typeof(GroupFormElementView), null);

        /// <summary>
        /// Gets or sets the stroke thickness of the border
        /// </summary>
        public double BorderStrokeThickness
        {
            get => (double)GetValue(BorderStrokeThicknessProperty);
            set => SetValue(BorderStrokeThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BorderStrokeThickness"/> dependency property.
        /// </summary>
        public static readonly BindableProperty BorderStrokeThicknessProperty =
            BindableProperty.Create(nameof(BorderStrokeThickness), typeof(double), typeof(GroupFormElementView), .5d);

        /// <summary>
        /// Gets or sets the padding inside the group's border
        /// </summary>
        public double ContentPadding
        {
            get => (double)GetValue(ContentPaddingProperty);
            set => SetValue(ContentPaddingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ContentPadding"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ContentPaddingProperty =
            BindableProperty.Create(nameof(ContentPadding), typeof(double), typeof(GroupFormElementView), 5d);

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
                this.Dispatch(UpdateVisibility);
            }
        }

        private void UpdateVisibility()
        {
            this.IsVisible = Element!= null && Element.IsVisible && Element.Elements.Count > 0; // TODO: Consider also hiding if all elements are not visible
        }
    }
}
#endif