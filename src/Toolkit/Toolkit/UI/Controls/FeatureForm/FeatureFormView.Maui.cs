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

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    public partial class FeatureFormView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        
        private static readonly Style DefaultFeatureFormHeaderStyle;
        private static readonly Style DefaultFeatureFormTitleStyle;
        private static readonly Style DefaultFeatureFormCaptionStyle;

        /// <summary>
        /// Template name of the <see cref="IBindableLayout"/> items layout view.
        /// </summary>
        public const string ItemsViewName = "ItemsView";

        private const string FeatureFormHeaderStyleName = "FeatureFormHeaderStyle";
        private const string FeatureFormTitleStyleName = "FeatureFormTitleStyle";
        private const string FeatureFormCaptionStyleName = "FeatureFormCaptionStyle";

         /// <summary>
        /// Template name of the form's content's <see cref="ScrollView"/>.
        /// </summary>
        public const string FeatureFormContentScrollViewerName = "FeatureFormContentScrollViewer";

        static FeatureFormView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);

            DefaultFeatureFormHeaderStyle = new Style(typeof(Label));
            DefaultFeatureFormHeaderStyle.Setters.Add(new Setter() { Property = Label.FontSizeProperty, Value = 16 });
            DefaultFeatureFormHeaderStyle.Setters.Add(new Setter() { Property = Label.FontAttributesProperty, Value = FontAttributes.Bold });
            DefaultFeatureFormHeaderStyle.Setters.Add(new Setter() { Property = Label.LineBreakModeProperty, Value = LineBreakMode.WordWrap });

            DefaultFeatureFormTitleStyle = new Style(typeof(Label));
            DefaultFeatureFormTitleStyle.Setters.Add(new Setter() { Property = Label.FontSizeProperty, Value = 16 });
            DefaultFeatureFormTitleStyle.Setters.Add(new Setter() { Property = Label.LineBreakModeProperty, Value = LineBreakMode.WordWrap });

            DefaultFeatureFormCaptionStyle = new Style(typeof(Label));
            DefaultFeatureFormCaptionStyle.Setters.Add(new Setter() { Property = Label.FontSizeProperty, Value = 12 });
            DefaultFeatureFormCaptionStyle.Setters.Add(new Setter() { Property = Label.LineBreakModeProperty, Value = LineBreakMode.WordWrap });
        }

        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FeatureForm.Title), "Esri.ArcGISRuntime.Mapping.FeatureForms.FeatureForm", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FeatureForm.Elements), "Esri.ArcGISRuntime.Mapping.FeatureForms.FeatureForm", "Esri.ArcGISRuntime")]
        private static object BuildDefaultTemplate()
        {
            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            Label roottitle = new Label();
            roottitle.Style = GetFeatureFormHeaderStyle();
            roottitle.SetBinding(Label.TextProperty, new Binding("FeatureForm.Title", source: RelativeBindingSource.TemplatedParent));
            roottitle.SetBinding(VisualElement.IsVisibleProperty, new Binding("FeatureForm.Title", source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance));
            root.Add(roottitle);
            ScrollView scrollView = new ScrollView() { HorizontalScrollBarVisibility = ScrollBarVisibility.Never };
#if WINDOWS
            scrollView.Padding = new Thickness(0, 0, 10, 0);
#endif
            scrollView.SetBinding(ScrollView.VerticalScrollBarVisibilityProperty, new Binding(nameof(VerticalScrollBarVisibility), source: RelativeBindingSource.TemplatedParent));
            Grid.SetRow(scrollView, 1);
            root.Add(scrollView);
            VerticalStackLayout itemsView = new VerticalStackLayout();            
            BindableLayout.SetItemTemplateSelector(itemsView, new FeatureFormElementTemplateSelector());
            itemsView.SetBinding(BindableLayout.ItemsSourceProperty, new Binding("FeatureForm.Elements", source: RelativeBindingSource.TemplatedParent));
            scrollView.Content = itemsView;
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(FeatureFormContentScrollViewerName, scrollView);
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

        internal static Style GetFeatureFormHeaderStyle() => GetStyle(FeatureFormHeaderStyleName, DefaultFeatureFormHeaderStyle);

        internal static Style GetFeatureFormTitleStyle() => GetStyle(FeatureFormTitleStyleName, DefaultFeatureFormTitleStyle);

        internal static Style GetFeatureFormCaptionStyle() => GetStyle(FeatureFormCaptionStyleName, DefaultFeatureFormCaptionStyle);
    }
}
#endif