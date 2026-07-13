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
            root.SetBinding(NavigationSubView.VerticalScrollBarVisibilityProperty, static (FeatureFormView viewer) => viewer.VerticalScrollBarVisibility, source: RelativeBindingSource.TemplatedParent);
            root.HeaderTemplateSelector = BuildHeaderTemplateSelector(root);
            root.ContentTemplateSelector = BuildContentTemplateSelector();
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName("SubFrameView", root);
            return root;
        }

        private static DataTemplateSelector BuildHeaderTemplateSelector(NavigationSubView subFrameView)
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
                desc.Style = GetFeatureFormTitleStyle();
                desc.SetBinding(Label.TextProperty, static (NavigationSubView view) => (view.AdditionalContent as FeatureForm)?.Title, source: subFrameView);
                desc.SetBinding(VisualElement.IsVisibleProperty, static (NavigationSubView view) => (view.AdditionalContent as FeatureForm)?.Title, source: subFrameView, converter: Internal.EmptyToFalseConverter.Instance);
                root.Children.Add(desc);
                Label description = new Label() { LineBreakMode = LineBreakMode.TailTruncation };
                description.Style = GetFeatureFormCaptionStyle();
                description.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result?.Filter.Description);
                description.SetBinding(VisualElement.IsVisibleProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result?.Filter.Description, converter: Internal.EmptyToFalseConverter.Instance);
                root.Children.Add(description);
                return root;
            });
            selector.UtilityAssociationGroupResultTemplate = new DataTemplate(() =>
            {
                VerticalStackLayout root = new VerticalStackLayout() { VerticalOptions = LayoutOptions.Center };
                Label roottitle = new Label() { LineBreakMode = LineBreakMode.TailTruncation };
                roottitle.Style = GetFeatureFormHeaderStyle();
                roottitle.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationGroupResult result) => result?.Name);
                root.Children.Add(roottitle);
                Label desc = new Label() { LineBreakMode = LineBreakMode.TailTruncation };
                desc.Style = GetFeatureFormTitleStyle();
                desc.SetBinding(Label.TextProperty, static (NavigationSubView view) => (view.AdditionalContent as UtilityNetworks.UtilityAssociationsFilterResult)?.Filter.Title, source: subFrameView);
                desc.SetBinding(VisualElement.IsVisibleProperty, static (NavigationSubView view) => (view.AdditionalContent as UtilityNetworks.UtilityAssociationsFilterResult)?.Filter.Title, source: subFrameView, converter: Internal.EmptyToFalseConverter.Instance);
                root.Children.Add(desc);
                return root;
            });
            selector.UtilityAssociationResultTemplate = new DataTemplate(() =>
            {
                Label roottitle = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
                roottitle.Style = GetFeatureFormHeaderStyle();
                roottitle.Text = Properties.Resources.GetString("FeatureFormUtilityAssociationSettings");
                return roottitle;
            });
            var workflowHeaderTemplate = new DataTemplate(() =>
            {
                VerticalStackLayout root = new VerticalStackLayout() { VerticalOptions = LayoutOptions.Center };
                Label title = new Label() { LineBreakMode = LineBreakMode.TailTruncation };
                title.Style = GetFeatureFormHeaderStyle();
                title.SetBinding(Label.TextProperty, static (UtilityAssociationWorkflowPage page) => page.Title);
                root.Add(title);
                Label subtitle = new Label() { LineBreakMode = LineBreakMode.TailTruncation };
                subtitle.Style = GetFeatureFormTitleStyle();
                subtitle.SetBinding(Label.TextProperty, static (UtilityAssociationWorkflowPage page) => page.Subtitle);
                subtitle.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationWorkflowPage page) => page.Subtitle, converter: Internal.EmptyToFalseConverter.Instance);
                root.Add(subtitle);
                return root;
            });
            selector.UtilityAssociationFeatureSourceSelectionTemplate = workflowHeaderTemplate;
            selector.UtilityAssociationAssetTypeSelectionTemplate = workflowHeaderTemplate;
            selector.UtilityAssociationFeatureCandidateSelectionTemplate = workflowHeaderTemplate;
            selector.UtilityAssociationCreationTemplate = new DataTemplate(() =>
            {
                Label title = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
                title.Style = GetFeatureFormHeaderStyle();
                title.SetBinding(Label.TextProperty, static (UtilityAssociationCreation creation) => creation.Title);
                return title;
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
            selector.UtilityAssociationResultTemplate = new DataTemplate(() =>
            {
                var view = new UtilityAssociationResultDetailsView();
                view.SetBinding(UtilityAssociationResultDetailsView.AssociationResultProperty, static (UtilityNetworks.UtilityAssociationResult result) => result);
                return view;
            });
            selector.UtilityAssociationFeatureSourceSelectionTemplate = BuildFeatureSourceSelectionTemplate();
            selector.UtilityAssociationAssetTypeSelectionTemplate = BuildAssetTypeSelectionTemplate();
            selector.UtilityAssociationFeatureCandidateSelectionTemplate = BuildFeatureCandidateSelectionTemplate();
            selector.UtilityAssociationCreationTemplate = BuildAssociationCreationTemplate();
            return selector;
        }

        private static DataTemplate BuildFeatureSourceSelectionTemplate()
        {
            return new DataTemplate(() =>
            {
                var root = new VerticalStackLayout() { Spacing = 8 };
                var search = new Entry() { Placeholder = Properties.Resources.GetString("FeatureFormUtilityAssociationsSearch") };
                search.SetBinding(Entry.TextProperty, static (UtilityAssociationFeatureSourceSelection selection) => selection.SearchText, mode: BindingMode.TwoWay);
                root.Add(search);
                var count = new Label() { HorizontalOptions = LayoutOptions.End };
                count.Style = GetFeatureFormCaptionStyle();
                count.SetBinding(Label.TextProperty, static (UtilityAssociationFeatureSourceSelection selection) => selection.CountText);
                root.Add(count);
                var list = new CollectionView() { SelectionMode = SelectionMode.Single };
                list.SetBinding(ItemsView.ItemsSourceProperty, static (UtilityAssociationFeatureSourceSelection selection) => selection.FilteredFeatureSources, mode: BindingMode.OneWay);
                list.SetBinding(SelectableItemsView.SelectedItemProperty, static (UtilityAssociationFeatureSourceSelection selection) => selection.SelectedFeatureSource, mode: BindingMode.TwoWay);
                list.ItemTemplate = new DataTemplate(() =>
                {
                    var label = new Label() { Padding = new Thickness(12), LineBreakMode = LineBreakMode.TailTruncation };
                    label.Style = GetFeatureFormTitleStyle();
                    label.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationFeatureSource source) => source.Name);
                    return label;
                });
                root.Add(list);
                var empty = new Label()
                {
                    Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsNoFeatureSources"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(12),
                };
                empty.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationFeatureSourceSelection selection) => selection.HasNoResults);
                root.Add(empty);
                var error = new Label() { TextColor = Colors.Red, Margin = new Thickness(12) };
                error.SetBinding(Label.TextProperty, static (UtilityAssociationFeatureSourceSelection selection) => selection.ErrorMessage, mode: BindingMode.OneWay);
                root.Add(error);
                return root;
            });
        }

        private static DataTemplate BuildAssetTypeSelectionTemplate()
        {
            return new DataTemplate(() =>
            {
                var root = new VerticalStackLayout() { Spacing = 8 };
                var search = new Entry() { Placeholder = Properties.Resources.GetString("FeatureFormUtilityAssociationsSearch") };
                search.SetBinding(Entry.TextProperty, static (UtilityAssociationAssetTypeSelection selection) => selection.SearchText, mode: BindingMode.TwoWay);
                root.Add(search);
                var heading = new Grid();
                heading.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                heading.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                var available = new Label() { Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsAvailableAssetTypes") };
                available.Style = GetFeatureFormCaptionStyle();
                heading.Add(available);
                var count = new Label();
                count.Style = GetFeatureFormCaptionStyle();
                count.SetBinding(Label.TextProperty, static (UtilityAssociationAssetTypeSelection selection) => selection.CountText);
                Grid.SetColumn(count, 1);
                heading.Add(count);
                root.Add(heading);
                var list = new CollectionView() { SelectionMode = SelectionMode.Single };
                list.SetBinding(ItemsView.ItemsSourceProperty, static (UtilityAssociationAssetTypeSelection selection) => selection.FilteredAssetTypes, mode: BindingMode.OneWay);
                list.SetBinding(SelectableItemsView.SelectedItemProperty, static (UtilityAssociationAssetTypeSelection selection) => selection.SelectedAssetType, mode: BindingMode.TwoWay);
                list.ItemTemplate = new DataTemplate(() =>
                {
                    var layout = new VerticalStackLayout() { Padding = new Thickness(12, 8) };
                    var name = new Label() { LineBreakMode = LineBreakMode.TailTruncation };
                    name.Style = GetFeatureFormTitleStyle();
                    name.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssetType assetType) => assetType.Name);
                    layout.Add(name);
                    var group = new Label() { LineBreakMode = LineBreakMode.TailTruncation };
                    group.Style = GetFeatureFormCaptionStyle();
                    group.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssetType assetType) => assetType.AssetGroup.Name);
                    layout.Add(group);
                    return layout;
                });
                root.Add(list);
                var empty = new Label()
                {
                    Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsNoAssetTypes"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(12),
                };
                empty.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationAssetTypeSelection selection) => selection.HasNoResults);
                root.Add(empty);
                return root;
            });
        }

        private static DataTemplate BuildFeatureCandidateSelectionTemplate()
        {
            return new DataTemplate(() =>
            {
                var root = new VerticalStackLayout() { Spacing = 8 };
                var search = new Entry() { Placeholder = Properties.Resources.GetString("FeatureFormUtilityAssociationsSearchFeatures") };
                search.SetBinding(Entry.TextProperty, static (UtilityAssociationFeatureCandidateSelection selection) => selection.SearchText, mode: BindingMode.TwoWay);
                root.Add(search);
                var heading = new Label() { Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsChooseToAdd") };
                heading.Style = GetFeatureFormCaptionStyle();
                root.Add(heading);
                var list = new CollectionView() { SelectionMode = SelectionMode.Single };
                list.SetBinding(ItemsView.ItemsSourceProperty, static (UtilityAssociationFeatureCandidateSelection selection) => selection.FilteredCandidates, mode: BindingMode.OneWay);
                list.SetBinding(SelectableItemsView.SelectedItemProperty, static (UtilityAssociationFeatureCandidateSelection selection) => selection.SelectedCandidate, mode: BindingMode.TwoWay);
                list.ItemTemplate = new DataTemplate(() =>
                {
                    var label = new Label() { Padding = new Thickness(12), LineBreakMode = LineBreakMode.TailTruncation };
                    label.Style = GetFeatureFormTitleStyle();
                    label.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationFeatureCandidate candidate) => candidate.Title);
                    return label;
                });
                root.Add(list);
                var loadMore = new Button() { Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsLoadMore") };
                loadMore.SetBinding(Button.CommandProperty, static (UtilityAssociationFeatureCandidateSelection selection) => selection.LoadMoreCommand);
                loadMore.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationFeatureCandidateSelection selection) => selection.HasMore);
                root.Add(loadMore);
                var empty = new Label()
                {
                    Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsNoCandidates"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(12),
                };
                empty.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationFeatureCandidateSelection selection) => selection.HasNoResults);
                root.Add(empty);
                var error = new Label() { TextColor = Colors.Red, Margin = new Thickness(12) };
                error.SetBinding(Label.TextProperty, static (UtilityAssociationFeatureCandidateSelection selection) => selection.ErrorMessage, mode: BindingMode.OneWay);
                root.Add(error);
                return root;
            });
        }

        private static DataTemplate BuildAssociationCreationTemplate()
        {
            return new DataTemplate(() =>
            {
                var root = new VerticalStackLayout() { Spacing = 12 };
                root.Add(BuildAssociationValueRow("FeatureFormUtilityAssociationsAssociationType", UtilityAssociationValue.AssociationType));
                var visibility = new HorizontalStackLayout() { Spacing = 8 };
                visibility.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationCreation creation) => creation.ShowContentVisibility);
                visibility.Add(new Label() { Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsContentVisibility"), VerticalOptions = LayoutOptions.Center });
                var visibilitySwitch = new Switch();
                visibilitySwitch.SetBinding(Switch.IsToggledProperty, static (UtilityAssociationCreation creation) => creation.ContentIsVisible, mode: BindingMode.TwoWay);
                visibility.Add(visibilitySwitch);
                root.Add(visibility);
                root.Add(BuildAssociationValueRow("FeatureFormUtilityAssociationsFromElement", UtilityAssociationValue.FromElement));
                var fromTerminal = new Picker() { Title = Properties.Resources.GetString("FeatureFormUtilityAssociationsCurrentFeatureTerminal") };
                fromTerminal.SetBinding(Picker.ItemsSourceProperty, static (UtilityAssociationCreation creation) => creation.FromTerminalNames);
                fromTerminal.SetBinding(Picker.SelectedIndexProperty, static (UtilityAssociationCreation creation) => creation.SelectedFromTerminalIndex, mode: BindingMode.TwoWay);
                fromTerminal.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationCreation creation) => creation.ShowFromTerminals);
                root.Add(fromTerminal);
                root.Add(BuildAssociationValueRow("FeatureFormUtilityAssociationsToElement", UtilityAssociationValue.ToElement));
                var toTerminal = new Picker() { Title = Properties.Resources.GetString("FeatureFormUtilityAssociationsOtherFeatureTerminal") };
                toTerminal.SetBinding(Picker.ItemsSourceProperty, static (UtilityAssociationCreation creation) => creation.ToTerminalNames);
                toTerminal.SetBinding(Picker.SelectedIndexProperty, static (UtilityAssociationCreation creation) => creation.SelectedToTerminalIndex, mode: BindingMode.TwoWay);
                toTerminal.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationCreation creation) => creation.ShowToTerminals);
                root.Add(toTerminal);
                var fraction = new VerticalStackLayout();
                fraction.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationCreation creation) => creation.ShowFractionAlongEdge);
                var fractionLabel = new Label() { Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsFractionAlongEdgePercent") };
                fraction.Add(fractionLabel);
                var slider = new Slider() { Minimum = 0, Maximum = 100 };
                slider.SetBinding(Slider.ValueProperty, static (UtilityAssociationCreation creation) => creation.FractionAlongEdgePercent, mode: BindingMode.TwoWay);
                fraction.Add(slider);
                root.Add(fraction);
                var error = new Label() { TextColor = Colors.Red };
                error.SetBinding(Label.TextProperty, static (UtilityAssociationCreation creation) => creation.ErrorMessage, mode: BindingMode.OneWay);
                root.Add(error);
                var add = new Button() { Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsAdd") };
                add.SetBinding(Button.CommandProperty, static (UtilityAssociationCreation creation) => creation.AddCommand);
                root.Add(add);
                return root;
            });
        }

        private enum UtilityAssociationValue
        {
            AssociationType,
            FromElement,
            ToElement,
        }

        private static View BuildAssociationValueRow(string labelResourceKey, UtilityAssociationValue value)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.Add(new Label() { Text = Properties.Resources.GetString(labelResourceKey), FontAttributes = FontAttributes.Bold });
            var valueLabel = new Label() { HorizontalTextAlignment = TextAlignment.End };
            if (value == UtilityAssociationValue.AssociationType)
            {
                valueLabel.SetBinding(Label.TextProperty, static (UtilityAssociationCreation creation) => creation.AssociationType, mode: BindingMode.OneWay);
            }
            else if (value == UtilityAssociationValue.FromElement)
            {
                valueLabel.SetBinding(Label.TextProperty, static (UtilityAssociationCreation creation) => creation.FromElement, mode: BindingMode.OneWay);
            }
            else
            {
                valueLabel.SetBinding(Label.TextProperty, static (UtilityAssociationCreation creation) => creation.ToElement, mode: BindingMode.OneWay);
            }
            Grid.SetColumn(valueLabel, 1);
            grid.Add(valueLabel);
            return grid;
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