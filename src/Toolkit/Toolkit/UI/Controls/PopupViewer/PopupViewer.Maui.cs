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
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    public partial class PopupViewer : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private static readonly Style DefaultPopupViewerHeaderStyle;
        private static readonly Style DefaultPopupViewerTitleStyle;
        private static readonly Style DefaultPopupViewerCaptionStyle;

        /// <summary>
        /// Template name of the <see cref="IBindableLayout"/> items layout view.
        /// </summary>
        public const string ItemsViewName = "ItemsView";

        /// <summary>
        /// Template name of the popup content's <see cref="ScrollView"/>.
        /// </summary>
        public const string PopupContentScrollViewerName = "PopupContentScrollViewer";

        private const string PopupViewerHeaderStyleName = "PopupViewerHeaderStyle";
        private const string PopupViewerTitleStyleName = "PopupViewerTitleStyle";
        private const string PopupViewerCaptionStyleName = "PopupViewerCaptionStyle";

        static PopupViewer()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
            DefaultPopupViewerHeaderStyle = new Style(typeof(Label));
            DefaultPopupViewerHeaderStyle.Setters.Add(new Setter() { Property = Label.FontSizeProperty, Value = 16 });
            DefaultPopupViewerHeaderStyle.Setters.Add(new Setter() { Property = Label.FontAttributesProperty, Value = FontAttributes.Bold });
            DefaultPopupViewerHeaderStyle.Setters.Add(new Setter() { Property = Label.LineBreakModeProperty, Value = LineBreakMode.WordWrap });

            DefaultPopupViewerTitleStyle = new Style(typeof(Label));
            DefaultPopupViewerTitleStyle.Setters.Add(new Setter() { Property = Label.FontSizeProperty, Value = 16 });
            DefaultPopupViewerTitleStyle.Setters.Add(new Setter() { Property = Label.LineBreakModeProperty, Value = LineBreakMode.WordWrap });

            DefaultPopupViewerCaptionStyle = new Style(typeof(Label));
            DefaultPopupViewerCaptionStyle.Setters.Add(new Setter() { Property = Label.FontSizeProperty, Value = 12 });
            DefaultPopupViewerCaptionStyle.Setters.Add(new Setter() { Property = Label.LineBreakModeProperty, Value = LineBreakMode.WordWrap });
        }

        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.Popups.Popup.Title), "Esri.ArcGISRuntime.Mapping.Popups.Popup", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.Popups.Popup.EvaluatedElements), "Esri.ArcGISRuntime.Mapping.Popups.Popup", "Esri.ArcGISRuntime")]
        private static object BuildDefaultTemplate()
        {
            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            Label roottitle = new Label();
            roottitle.Style = GetPopupViewerHeaderStyle();
            roottitle.SetBinding(Label.TextProperty, new Binding("Popup.Title", source: RelativeBindingSource.TemplatedParent));
            roottitle.SetBinding(VisualElement.IsVisibleProperty, new Binding("Popup.Title", source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance));
            root.Add(roottitle);
            ScrollView scrollView = new ScrollView() { HorizontalScrollBarVisibility = ScrollBarVisibility.Never };
#if WINDOWS
            scrollView.Padding = new Thickness(0, 0, 10, 0);
#endif
            scrollView.SetBinding(ScrollView.VerticalScrollBarVisibilityProperty, new Binding(nameof(VerticalScrollBarVisibility), source: RelativeBindingSource.TemplatedParent));
            Grid.SetRow(scrollView, 1);
            root.Add(scrollView);
            VerticalStackLayout itemsView = new VerticalStackLayout()
            {
                Margin = new Thickness(0, 10),
            };
            BindableLayout.SetItemTemplateSelector(itemsView, new PopupElementTemplateSelector());
            itemsView.SetBinding(BindableLayout.ItemsSourceProperty, new Binding("Popup.EvaluatedElements", source: RelativeBindingSource.TemplatedParent));
            scrollView.Content = itemsView;
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(PopupContentScrollViewerName, scrollView);
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

        internal static Style GetPopupViewerHeaderStyle() => GetStyle(PopupViewerHeaderStyleName, DefaultPopupViewerHeaderStyle);

        internal static Style GetPopupViewerTitleStyle() => GetStyle(PopupViewerTitleStyleName, DefaultPopupViewerTitleStyle);

        internal static Style GetPopupViewerCaptionStyle() => GetStyle(PopupViewerCaptionStyleName, DefaultPopupViewerCaptionStyle);
    }
}
#endif