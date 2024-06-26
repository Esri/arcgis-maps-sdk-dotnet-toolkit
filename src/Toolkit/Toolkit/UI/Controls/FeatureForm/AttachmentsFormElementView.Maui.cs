﻿// /*******************************************************************************
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
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class AttachmentsFormElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private const string AttachmentsListViewName = "AttachmentsListView";
        private const string AddAttachmentButtonName = "AddAttachmentButton";

        private Button? _addAttachmentButton;

        static AttachmentsFormElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.AttachmentsFormElement.Attachments), "Esri.ArcGISRuntime.Mapping.FeatureForms.AttachmentsFormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.AttachmentsFormElement.IsEditable), "Esri.ArcGISRuntime.Mapping.FeatureForms.AttachmentsFormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement.IsVisible), "Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement.Label), "Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement", "Esri.ArcGISRuntime")]
        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement.Description), "Esri.ArcGISRuntime.Mapping.FeatureForms.FormElement", "Esri.ArcGISRuntime")]
        private static object BuildDefaultTemplate()
        {
            var root = new VerticalStackLayout();
            root.SetBinding(VerticalStackLayout.IsVisibleProperty, nameof(FormElement.IsVisible));
            var label = new Label();
            label.SetBinding(Label.TextProperty, new Binding("Element.Label", source: RelativeBindingSource.TemplatedParent));
            label.SetBinding(View.IsVisibleProperty, new Binding("Element.Label", source: RelativeBindingSource.Self, converter: new EmptyStringToBoolConverter()));
            label.Style = FeatureFormView.GetFeatureFormTitleStyle();
            root.Children.Add(label);
            label = new Label();
            label.SetBinding(Label.TextProperty, new Binding("Element.Description", source: RelativeBindingSource.TemplatedParent));
            label.SetBinding(Label.IsVisibleProperty, new Binding("Element.Description", source: RelativeBindingSource.Self, converter: new EmptyStringToBoolConverter()));
            label.Style = FeatureFormView.GetFeatureFormCaptionStyle();
            root.Children.Add(label);

            CollectionView itemsView = new CollectionView()
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Default,
                EmptyView = new Label() { Text = Properties.Resources.GetString("FeatureFormNoAttachments"), TextColor = Colors.Gray },
                ItemsLayout = new GridItemsLayout(1, ItemsLayoutOrientation.Horizontal),
            };
            itemsView.SetBinding(CollectionView.ItemsSourceProperty, new Binding("Element.Attachments", source: RelativeBindingSource.TemplatedParent));
            root.Children.Add(itemsView);
            Button addButton = new Button()
            {
                Text = "+ ",
                BorderWidth = 0,
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.CornflowerBlue,
                HorizontalOptions = new LayoutOptions(LayoutAlignment.Start, true), Padding = new Thickness(0, 3, 50, 3)
            };
            addButton.SetBinding(VisualElement.IsVisibleProperty, new Binding("Element.IsEditable", source: RelativeBindingSource.TemplatedParent));
            root.Children.Add(addButton);

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(AttachmentsListViewName, itemsView);
            nameScope.RegisterName(AddAttachmentButtonName, addButton);
            return root;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_addAttachmentButton is not null)
            {
                _addAttachmentButton.Clicked -= AddAttachmentButton_Click;
            }
            _addAttachmentButton = GetTemplateChild("AddAttachmentButton") as Button;
            if (_addAttachmentButton is not null)
            {
                _addAttachmentButton.Clicked += AddAttachmentButton_Click;
            }
        }

        private async void AddAttachmentButton_Click(object? sender, EventArgs e)
        {
            if (Element is null) return;
            try
            {
                var result = await FilePicker.Default.PickAsync(new());
                if (result != null)
                {
                    Element.AddAttachment(result.FileName, MimeTypeMap.GetMimeType(result.FileName), File.ReadAllBytes(result.FullPath));
                }
            }
            catch(System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Failed to add attachment: " + ex.Message);
            }
        }
    }
}
#endif