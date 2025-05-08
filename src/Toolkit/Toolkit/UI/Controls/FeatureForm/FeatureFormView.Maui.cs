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

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    /// <summary>
    /// A visual feature editor form controlled by a <see cref="FeatureForm"/> definition.
    /// </summary>
    /// <remarks>
    /// <para>To use the camera to capture images for attachments, the corerct permissions must be set on your application.</para>
    /// <para><b>Android:</b><br/>Add the following to Android's AndroidManifest.xml:</para>
    /// <code>
    /// &lt;uses-permission android:name="android.permission.CAMERA" />
    /// &lt;queries>
    ///     &lt;intent>
    ///         &lt;action android:name="android.media.action.IMAGE_CAPTURE" />
    ///     &lt;/intent>
    /// &lt;/queries>
    /// </code>
    /// <para><b>iOS:</b><br/>Add the following to iOS's Info.plist:</para>
    /// <code>
    /// &lt;key>NSCameraUsageDescription&lt;/key>
    /// &lt;string>Adding attachments&lt;/string>
    /// </code>
    /// <para>If these settings are not added, only file browsing will be enabled.</para>
    /// </remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device-media/picker?#get-started">MAUI: Media picker for photos and videos</seealso>
    /// <seealso cref="Esri.ArcGISRuntime.Data.ArcGISFeatureTable.FeatureFormDefinition"/>
    /// <seealso cref="Esri.ArcGISRuntime.Mapping.FeatureLayer.FeatureFormDefinition"/>
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

        private static object BuildDefaultTemplate()
        {
            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            Label roottitle = new Label();
            roottitle.Style = GetFeatureFormHeaderStyle();
            roottitle.SetBinding(Label.TextProperty, static (FeatureFormView view) => view.FeatureForm?.Title, source: RelativeBindingSource.TemplatedParent);
            roottitle.SetBinding(VisualElement.IsVisibleProperty, static (FeatureFormView view) => view.FeatureForm?.Title, source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance);
            root.Add(roottitle);
            ScrollView scrollView = new ScrollView() { HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Margin = new Thickness(0, 5, 0, 0) };
#if WINDOWS
            scrollView.Padding = new Thickness(0, 0, 10, 0);
#endif
            scrollView.SetBinding(ScrollView.VerticalScrollBarVisibilityProperty, static (FeatureFormView view) => view.VerticalScrollBarVisibility, source: RelativeBindingSource.TemplatedParent);
            var scrollableContent = new VerticalStackLayout();
            scrollView.Content = scrollableContent;
            Grid.SetRow(scrollView, 1);
            root.Add(scrollView);
            VerticalStackLayout itemsView = new VerticalStackLayout();
            BindableLayout.SetItemTemplateSelector(itemsView, new FeatureFormElementTemplateSelector());
            itemsView.SetBinding(BindableLayout.ItemsSourceProperty, static (FeatureFormView view) => view.FeatureForm?.Elements, source: RelativeBindingSource.TemplatedParent);
            scrollableContent.Add(itemsView);

            AttachmentsFormElementView attachmentsView = new AttachmentsFormElementView();
            attachmentsView.SetBinding(AttachmentsFormElementView.ElementProperty, static (FeatureFormView view) => view.FeatureForm?.DefaultAttachmentsElement, source: RelativeBindingSource.TemplatedParent);
            scrollableContent.Add(attachmentsView);

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

        internal static FeatureFormView? GetFeatureFormViewParent(Element child) => GetParent<FeatureFormView>(child);

        internal static T? GetParent<T>(Element? child) where T : Element
        {
            var parent = child?.Parent;
            while (parent is not null && parent is not T page)
            {
                parent = parent.Parent;
            }
            return parent as T;
        }

        internal static IEnumerable<T> GetDescendentsOfType<T>(Element root)
        {
            if (root is null)
                yield break;

            
            foreach (var child in root.GetVisualTreeDescendants())
            {
                if (child == root) continue;
                if (child is Element frameworkElement)
                {
                    if (frameworkElement is T targetElement)
                        yield return targetElement;

                    foreach (var descendant in GetDescendentsOfType<T>(frameworkElement))
                        yield return descendant;
                }
            }
        }
    }
}
#endif