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
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    public partial class FeatureFormView : TemplatedView, IBorderElement
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private static readonly Style DefaultFeatureFormTitleStyle;
        private static readonly DataTemplate DefaultFieldFormElementTemplate;
        private static readonly DataTemplate DefaultGroupFormElementTemplate;

        private const string FeatureFormTitleStyleName = "FeatureFormTitleStyle";

        /// <summary>Template name of the <see cref="ScrollView"/> that contains the items view.</summary>
        public const string ContentScrollViewName = "ContentScrollView";

        /// <summary>Template name of the <see cref="IBindableLayout"/> that presents the form's elements.</summary>
        public const string ItemsViewName = "ElementsView";

        static FeatureFormView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);

            DefaultFeatureFormTitleStyle = new Style(typeof(Label));
            DefaultFeatureFormTitleStyle.Setters.Add(new Setter { Property = Label.FontSizeProperty, Value = 16 });
            DefaultFeatureFormTitleStyle.Setters.Add(new Setter { Property = Label.FontAttributesProperty, Value = FontAttributes.Bold });

            DefaultFieldFormElementTemplate = new DataTemplate(() =>
            {
                FieldFormElementView view = new();
                view.SetBinding(FieldFormElementView.ElementProperty, Binding.SelfPath);
                return view;
            });
            DefaultGroupFormElementTemplate = new DataTemplate(() =>
            {
                VerticalStackLayout itemsView = new();
                itemsView.Margin = new Thickness(0, 10);
                BindableLayout.SetItemTemplateSelector(itemsView, new FormElementTemplateSelector());
                itemsView.SetBinding(BindableLayout.ItemsSourceProperty, nameof(GroupFormElement.Elements));
                return itemsView;
            });
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(FeatureForm))]
        private static object BuildDefaultTemplate()
        {
            Border root = new();
            root.SetBinding(Border.StrokeProperty,
                new Binding(nameof(BorderColor), source: RelativeBindingSource.TemplatedParent));
            root.SetBinding(Border.StrokeShapeProperty,
                new Binding(nameof(CornerRadius), source: RelativeBindingSource.TemplatedParent, converter: Internal.BorderRadiusConverter.Instance));
            root.SetBinding(Border.StrokeThicknessProperty,
                new Binding(nameof(BorderWidth), source: RelativeBindingSource.TemplatedParent));

            Grid grid = new();
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            root.Content = grid;

            Label title = new();
            title.Style = GetFeatureFormTitleStyle();
            title.SetBinding(Label.TextProperty, new Binding("FeatureForm.Title", source: RelativeBindingSource.TemplatedParent));
            title.SetBinding(IsVisibleProperty, new Binding("FeatureForm.Title", source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance));
            grid.Add(title, row: 0);

            ScrollView scrollView = new();
            grid.Add(scrollView, row: 1);

            VerticalStackLayout itemsView = new();
            itemsView.Margin = new Thickness(0, 10);
            itemsView.SetBinding(BindableLayout.ItemTemplateSelectorProperty, new Binding(nameof(FormElementTemplateSelector), source: RelativeBindingSource.TemplatedParent));
            itemsView.SetBinding(BindableLayout.ItemsSourceProperty, new Binding("FeatureForm.Elements", source: RelativeBindingSource.TemplatedParent));
            scrollView.Content = itemsView;

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(FeatureFormTitleStyleName, scrollView);
            nameScope.RegisterName(ItemsViewName, itemsView);

            return root;
        }

        internal static Style GetStyle(string resourceKey, Style defaultStyle)
        {
            if (Application.Current?.Resources?.TryGetValue(resourceKey, out var value) == true && value is Style style)
            {
                return style;
            }
            return defaultStyle;
        }

        internal static Style GetFeatureFormTitleStyle() => GetStyle(FeatureFormTitleStyleName, DefaultFeatureFormTitleStyle);

        /// <summary>Bindable property for <see cref="IBorderElement.BorderWidth"/>.</summary>
        public static readonly BindableProperty FormElementTemplateSelectorProperty = BindableProperty.Create(nameof(FormElementTemplateSelector), typeof(FormElementTemplateSelector), typeof(FeatureFormView), defaultValueCreator: (bindable) => new FormElementTemplateSelector());

        /// <summary>
        /// Gets or sets the template selector used for rendering the form's elements.
        /// </summary>
        public FormElementTemplateSelector FormElementTemplateSelector
        {
            get { return (FormElementTemplateSelector)GetValue(FormElementTemplateSelectorProperty); }
            set { SetValue(FormElementTemplateSelectorProperty, value); }
        }

        #region IBorderElement

        /// <summary>Bindable property for <see cref="IBorderElement.BorderWidth"/>.</summary>
        public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(nameof(IBorderElement.BorderWidth), typeof(double), typeof(IBorderElement), -1d);

        /// <summary>Bindable property for <see cref="IBorderElement.BorderColor"/>.</summary>
        public static readonly BindableProperty BorderColorProperty =
            BindableProperty.Create(nameof(IBorderElement.BorderColor), typeof(Color), typeof(IBorderElement), null);

        /// <summary>Bindable property for <see cref="IBorderElement.CornerRadius"/>.</summary>
        public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(nameof(IBorderElement.CornerRadius), typeof(int), typeof(IBorderElement), -1);

        /// <inheritdoc/>
        public double BorderWidth
        {
            get { return (double)GetValue(BorderWidthProperty); }
            set { SetValue(BorderWidthProperty, value); }
        }

        /// <inheritdoc/>
        public Color BorderColor
        {
            get { return (Color)GetValue(BorderColorProperty); }
            set { SetValue(BorderColorProperty, value); }
        }

        /// <inheritdoc/>
        public int CornerRadius
        {
            get { return (int)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

#pragma warning disable CA1033 // Interface methods should be callable by child types
        int IBorderElement.CornerRadiusDefaultValue => (int)CornerRadiusProperty.DefaultValue;
        Color? IBorderElement.BorderColorDefaultValue => (Color?)BorderColorProperty.DefaultValue;
        double IBorderElement.BorderWidthDefaultValue => (double)BorderWidthProperty.DefaultValue;
        bool IBorderElement.IsCornerRadiusSet() => IsSet(CornerRadiusProperty);
        bool IBorderElement.IsBackgroundColorSet() => IsSet(BackgroundColorProperty);
        bool IBorderElement.IsBackgroundSet() => IsSet(BackgroundProperty);
        bool IBorderElement.IsBorderColorSet() => IsSet(BorderColorProperty);
        bool IBorderElement.IsBorderWidthSet() => IsSet(BorderWidthProperty);
        void IBorderElement.OnBorderColorPropertyChanged(Color oldValue, Color newValue) { }
#pragma warning restore CA1033 // Interface methods should be callable by child types

        #endregion
    }
}
#endif