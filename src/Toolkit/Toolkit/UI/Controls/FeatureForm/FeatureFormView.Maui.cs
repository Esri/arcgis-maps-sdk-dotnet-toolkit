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
            FeatureFormContentTemplateSelector selector = new FeatureFormContentTemplateSelector();
            selector.FeatureFormTemplate = new DataTemplate(() =>
            {
                Label roottitle = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
                roottitle.Style = GetFeatureFormHeaderStyle();
                roottitle.SetBinding(Label.TextProperty, static (FeatureForm form) => form?.Title);
                return roottitle;
            });
            selector.UtilityAssociationsFilterResultTemplate = new DataTemplate(() =>
            {
                VerticalStackLayout root = new VerticalStackLayout() { VerticalOptions = LayoutOptions.Center };
                Label title = new Label() { LineBreakMode = LineBreakMode.TailTruncation };
                title.Style = GetFeatureFormHeaderStyle();
                title.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result?.Filter.Title);
                root.Children.Add(title);
                Label desc = new Label() { LineBreakMode = LineBreakMode.TailTruncation };
                desc.Style = GetFeatureFormCaptionStyle();
                desc.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result?.Filter.Description); // TODO: This needs to be the FeatureForm.Title
                desc.SetBinding(VisualElement.IsVisibleProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result?.Filter.Description, converter: Internal.EmptyToFalseConverter.Instance);
                root.Children.Add(desc);
                return root;
            });
            selector.UtilityAssociationGroupResultTemplate = new DataTemplate(() =>
            {
                Label roottitle = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
                roottitle.Style = GetFeatureFormHeaderStyle();
                roottitle.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationGroupResult result) => result?.Name);
                return roottitle;
            });
            return selector;
        }

        private static DataTemplateSelector BuildContentTemplateSelector()
        {
            FeatureFormContentTemplateSelector selector = new FeatureFormContentTemplateSelector();

            selector.FeatureFormTemplate = new DataTemplate(() =>
            {
                var layout = new VerticalStackLayout();
                VerticalStackLayout itemsView = new VerticalStackLayout();
                BindableLayout.SetItemTemplateSelector(itemsView, new FeatureFormElementTemplateSelector());
                itemsView.SetBinding(BindableLayout.ItemsSourceProperty, static (FeatureForm form) => form?.Elements);
                layout.Add(itemsView);

                AttachmentsFormElementView attachmentsView = new AttachmentsFormElementView();
                attachmentsView.SetBinding(AttachmentsFormElementView.ElementProperty, static (FeatureForm form) => form?.DefaultAttachmentsElement);
                layout.Add(attachmentsView);
                return layout;
            });
            selector.UtilityAssociationsFilterResultTemplate = new DataTemplate(() =>
            {
                var view = new UtilityAssociationsFilterResultsView();
                view.SetBinding(UtilityAssociationsFilterResultsView.AssociationsFilterResultProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result);
                return view;
            });

            selector.UtilityAssociationGroupResultTemplate = new DataTemplate(() =>
            {
                var view = new UtilityAssociationGroupResultView();
                view.SetBinding(UtilityAssociationGroupResultView.GroupResultProperty, static (UtilityNetworks.UtilityAssociationGroupResult result) => result);
                return view;
            });
            selector.UtilityAssociationsFilterResultTemplate = new DataTemplate(() =>
            {
                var view = new UtilityAssociationsFilterResultsView();
                view.SetBinding(UtilityAssociationsFilterResultsView.AssociationsFilterResultProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result);
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
            return root.GetVisualTreeDescendants().OfType<T>();
        }

        private FeatureForm? _currentFeatureForm;

        private FeatureForm? GetCurrentFeatureForm() => _currentFeatureForm;

        private void SetCurrentFeatureForm(FeatureForm? value)
        {
            if(_currentFeatureForm != value)
            {
                var oldValue = _currentFeatureForm;
                _currentFeatureForm = value;
                OnCurrentFeatureFormPropertyChanged(oldValue, value);
                OnPropertyChanged(nameof(CurrentFeatureForm));
            }
        }
    }
}
#endif