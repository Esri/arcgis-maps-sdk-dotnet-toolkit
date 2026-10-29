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
#if __IOS__
using Microsoft.Maui.Handlers;
using UIKit;
using NativeContentView = Microsoft.Maui.Platform.ContentView;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    public partial class PopupViewer : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private static readonly Style DefaultPopupViewerHeaderStyle;
        private static readonly Style DefaultPopupViewerTitleStyle;
        private static readonly Style DefaultPopupViewerCaptionStyle;
        private static readonly IValueConverter? HeaderTextConverter =
#if WINDOWS
            new Toolkit.Internal.HtmlToPlainTextConverter();
#else
            null;
#endif
        private static readonly IValueConverter? PopupTitleConverter =
#if __IOS__
            new Toolkit.Internal.HtmlToPlainTextConverter();
#else
            HeaderTextConverter;
#endif

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
            DefaultPopupViewerCaptionStyle.Setters.Add(new Setter() { Property = Label.TextColorProperty, Value = Colors.Gray });
        }

        private static object BuildDefaultTemplate()
        {
            NavigationSubView root = new NavigationSubView();

            root.SetBinding(NavigationSubView.VerticalScrollBarVisibilityProperty, static (PopupViewer viewer) => viewer.VerticalScrollBarVisibility, source: RelativeBindingSource.TemplatedParent);
            root.HeaderTemplateSelector = BuildHeaderTemplateSelector();
            root.ContentTemplateSelector = BuildContentTemplateSelector();

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName("SubFrameView", root);

            return root;
        }

        private static DataTemplateSelector BuildHeaderTemplateSelector()
        {
            PopupContentTemplateSelector selector = new PopupContentTemplateSelector();
            selector.PopupTemplate = new DataTemplate(() =>
            {
                Label roottitle = new Label();
                roottitle.Style = GetPopupViewerHeaderStyle();
#if WINDOWS || __IOS__ || __ANDROID__
                SemanticProperties.SetHeadingLevel(roottitle, SemanticHeadingLevel.Level1);
#endif
                roottitle.SetBinding(Label.TextProperty, static (Popup popup) => popup?.Title, converter: PopupTitleConverter);
                roottitle.SetBinding(VisualElement.IsVisibleProperty, static (Popup popup) => popup?.Title, converter: Internal.EmptyToFalseConverter.Instance);
                return roottitle;
            });

            selector.UtilityAssociationsFilterResultTemplate = new DataTemplate(() =>
            {
                VerticalStackLayout root = new VerticalStackLayout() { VerticalOptions = LayoutOptions.Center };

                Label title = new Label() { LineBreakMode = LineBreakMode.TailTruncation };
                title.Style = GetPopupViewerHeaderStyle();
#if __IOS__
                SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level1);
#endif
                title.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result?.Filter.Title, converter: HeaderTextConverter);
                root.Children.Add(title);

                Label desc = new Label() { LineBreakMode = LineBreakMode.TailTruncation };
                desc.Style = GetPopupViewerCaptionStyle();
                desc.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result?.Filter.Description, converter: HeaderTextConverter);
                desc.SetBinding(VisualElement.IsVisibleProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result?.Filter.Description, converter: Internal.EmptyToFalseConverter.Instance);
                root.Children.Add(desc);

                return root;
            });

            selector.UtilityAssociationGroupResultTemplate = new DataTemplate(() =>
            {
                Label roottitle = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
                roottitle.Style = GetPopupViewerHeaderStyle();
#if __IOS__
                SemanticProperties.SetHeadingLevel(roottitle, SemanticHeadingLevel.Level1);
#endif
                roottitle.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationGroupResult result) => result?.Name);
                return roottitle;
            });

            return selector;
        }

        private static DataTemplateSelector BuildContentTemplateSelector()
        {
            PopupContentTemplateSelector selector = new PopupContentTemplateSelector();

            selector.PopupTemplate = new DataTemplate(() =>
            {
                VerticalStackLayout rootLayout = new VerticalStackLayout()
                {
                    Margin = new Thickness(0, 10),
                };
                VerticalStackLayout itemsView = new VerticalStackLayout();
                BindableLayout.SetItemTemplateSelector(itemsView, new PopupElementTemplateSelector());
                itemsView.SetBinding(BindableLayout.ItemsSourceProperty, static (Popup popup) => popup?.EvaluatedElements);
                rootLayout.Children.Add(itemsView);

                Label editSummaryLabel = new Label() { LineBreakMode = LineBreakMode.WordWrap, Style = GetPopupViewerCaptionStyle(), Margin = new Thickness(0, 10, 0, 0)};
                editSummaryLabel.SetBinding(Label.TextProperty, static (Popup popup) => popup?.EditSummary);
                editSummaryLabel.SetBinding(VisualElement.IsVisibleProperty, static (Popup popup) => popup?.PopupDefinition?.ShowEditSummary);
                rootLayout.Children.Add(editSummaryLabel);

                return rootLayout;
            });

            selector.UtilityAssociationsFilterResultTemplate = new DataTemplate(() =>
            {
                var view = new UtilityAssociationsFilterResultsPopupView();
                view.SetBinding(UtilityAssociationsFilterResultsPopupView.AssociationsFilterResultProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result);
                return view;
            });

            selector.UtilityAssociationGroupResultTemplate = new DataTemplate(() =>
            {
                var view = new UtilityAssociationGroupResultPopupView();
                view.SetBinding(UtilityAssociationGroupResultPopupView.GroupResultProperty, static (UtilityNetworks.UtilityAssociationGroupResult result) => result);
                return view;
            });

            return selector;
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

        internal static PopupViewer? GetPopupViewerParent(Element element) => GetParent<PopupViewer>(element);

        private static T? GetParent<T>(Element element) where T : Element
        {
            var parent = element.Parent;
            while (parent is not null and not T)
            {
                parent = parent.Parent;
            }
            return parent as T;
        }
    }

#if __IOS__
    internal sealed class PopupViewerHandler : ContentViewHandler
    {
        protected override NativeContentView CreatePlatformView() => new PopupViewerContentView
        {
            CrossPlatformLayout = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} must be set before creating the popup view."),
            FocusGroupIdentifier = $"{nameof(PopupViewer)}-{((PopupViewer)VirtualView).Id}",
            ShouldGroupAccessibilityChildren = true,
        };
    }

    internal sealed class PopupViewerContentView : NativeContentView
    {
        public override bool ShouldUpdateFocus(UIFocusUpdateContext context)
        {
            bool leavesPopup = context.FocusHeading is UIFocusHeading.Left or UIFocusHeading.Right &&
                context.PreviouslyFocusedItem is { } previous && UIFocusSystem.Contains(this, previous) &&
                context.NextFocusedItem is { } next && !UIFocusSystem.Contains(this, next);
            return !leavesPopup && base.ShouldUpdateFocus(context);
        }
    }
#endif
}
#endif