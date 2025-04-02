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
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.ApplicationModel;

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

        private T? GetParent<T>() where T : Element
        {
            var parent = this.Parent;
            while (parent is not null && parent is not T page)
            {
                parent = parent.Parent;
            }
            return parent as T;
        }

        private static object BuildDefaultTemplate()
        {
            var root = new VerticalStackLayout();
            root.SetBinding(VerticalStackLayout.IsVisibleProperty, static (FormElement element) => element.IsVisible);

            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            header.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            header.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var label = new Label();
            label.SetBinding(Label.TextProperty, static (AttachmentsFormElementView view) => view.Element?.Label, source: RelativeBindingSource.TemplatedParent);
            label.SetBinding(View.IsVisibleProperty, static (Label label) => label.Text, source: RelativeBindingSource.Self, converter: new EmptyStringToBoolConverter());
            label.Style = FeatureFormView.GetFeatureFormTitleStyle();
            header.Children.Add(label);
            label = new Label();
            label.SetBinding(Label.TextProperty, static (AttachmentsFormElementView view) => view.Element?.Description, source: RelativeBindingSource.TemplatedParent);
            label.SetBinding(Label.IsVisibleProperty, static (Label label) => label.Text, source: RelativeBindingSource.Self, converter: new EmptyStringToBoolConverter());
            label.Style = FeatureFormView.GetFeatureFormCaptionStyle();
            Grid.SetRow(label, 1);
            header.Children.Add(label);
            Button addButton = new Button()
            {
                Margin = new Thickness(0, -5, 0, 0),
                Text = "\uE21B",
                FontFamily = "calcite-ui-icons-24",
                BorderWidth = 0,
                FontSize = 24,
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.CornflowerBlue,
                HorizontalOptions = new LayoutOptions(LayoutAlignment.Start, true),
                VerticalOptions = new LayoutOptions(LayoutAlignment.Start, true),
                Padding = new Thickness(5)
            };
           
            
            Grid.SetColumn(addButton, 1);
            Grid.SetRowSpan(addButton, 2);
            addButton.SetBinding(VisualElement.IsVisibleProperty, static (AttachmentsFormElementView view) => view.Element?.IsEditable, source: RelativeBindingSource.TemplatedParent);
            header.Children.Add(addButton);

            root.Children.Add(header);

            CollectionView itemsView = new CollectionView()
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Default,
                EmptyView = new Label() { Text = Properties.Resources.GetString("FeatureFormNoAttachments"), TextColor = Colors.Gray },
                ItemsLayout = new GridItemsLayout(1, ItemsLayoutOrientation.Horizontal) { HorizontalItemSpacing = 4 },
                ItemTemplate = new DataTemplate(() =>
                {
                    var view = new FormAttachmentView();
                    view.SetBinding(FormAttachmentView.AttachmentProperty, static (FormAttachmentView view) => view.Attachment);
                    view.SetBinding(FormAttachmentView.ElementProperty, static (AttachmentsFormElementView view) => view.Element, source: RelativeBindingSource.TemplatedParent);
                    view.SetAppThemeColor(FormAttachmentView.IconColorProperty, Colors.Black, Colors.White);
                    view.SetBinding(ToolTipProperties.TextProperty, static (FormAttachmentView view) => view.Attachment?.Name);
                    return view;
                }),
#if IOS
                HeightRequest = 75,
                ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem,
#else
                MinimumHeightRequest = 75,
#endif
            };
            itemsView.SetBinding(CollectionView.ItemsSourceProperty, static (AttachmentsFormElementView view) => view.Element?.Attachments, source: RelativeBindingSource.TemplatedParent);
            root.Children.Add(itemsView);
            
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(AddAttachmentButtonName, addButton);
            nameScope.RegisterName(AttachmentsListViewName, itemsView);
            return root;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_addAttachmentButton is not null)
            {
                _addAttachmentButton.Clicked -= AddAttachmentButton_Click;
                FlyoutBase.SetContextFlyout(_addAttachmentButton, null);
            }
            _addAttachmentButton = GetTemplateChild("AddAttachmentButton") as Button;
            if (_addAttachmentButton is not null)
            {
                _addAttachmentButton.Clicked += AddAttachmentButton_Click;
            }
            UpdateVisibility();
        }

        private async void AddAttachmentButton_Click(object? sender, EventArgs e)
        {
            var page = GetParent<Page>();
            if(page != null && MediaPicker.IsCaptureSupported)
            {
#if ANDROID
                // Check if manifest allows camera access.
                if (!Permissions.IsDeclaredInManifest("android.permission.CAMERA"))
                {
                    Trace.WriteLine("**Microsoft.Maui.ApplicationModel.PermissionException:** 'You need to declare using the permission: `android.permission.CAMERA` in your AndroidManifest.xml'", "ArcGIS Maps SDK Toolkit");
                    // Fallback to just adding a file
                    AddAttachmentFromFile();
                    return;
                }
#elif IOS
                // Check if manifest allows camera access.
                if (!Permissions.IsKeyDeclaredInInfoPlist("NSCameraUsageDescription"))
                {
                    Trace.WriteLine("You must set `NSCameraUsageDescription` in your Info.plist file to use the Permission: Camera.", "ArcGIS Maps SDK Toolkit");
                    // Fallback to just adding a file
                    AddAttachmentFromFile();
                    return;
                }
#endif
                var addAttachment = Properties.Resources.GetString("FeatureFormAddAttachmentMenuFromFile");
                var camera = Properties.Resources.GetString("FeatureFormAddAttachmentMenuWithCamera");
                
                var result = await page.DisplayActionSheet(addAttachment, null, null, camera, addAttachment);
                if (result == camera)
                {
                    try
                    {
                        var status = await Permissions.RequestAsync<Permissions.Camera>();
                        if (status != PermissionStatus.Granted)
                        {
                            return;
                        }
                        // Note: iOS returns a PNG image. See https://github.com/dotnet/maui/issues/8251
                        var photo = await MediaPicker.CapturePhotoAsync();
                        if (photo != null && Element != null)
                        {
                            using (var stream = await photo.OpenReadAsync())
                            {
                                using var sr = new BinaryReader(stream);
                                var data = sr.ReadBytes((int)stream.Length);
                                var contentType = photo.ContentType;
#if IOS                         // Workaround https://github.com/dotnet/maui/issues/15562
                                if (!contentType.Contains('/'))
                                    contentType = "image/" + contentType;
#endif
                                Element.AddAttachment(photo.FileName, contentType, data);
                            }
                            EvaluateExpressions();
                            (GetTemplateChild(AttachmentsListViewName) as CollectionView)?.ScrollTo(Element.Attachments.Last());
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Trace.WriteLine("Failed to add attachment: " + ex.Message, "ArcGIS Maps SDK Toolkit");
                    }
                }
                if (result == addAttachment)
                {
                    AddAttachmentFromFile();
                }
            }
            else
                AddAttachmentFromFile();
        }

        private async void AddAttachmentFromFile()
        {
            if (Element is null) return;
            try
            {
                var result = await FilePicker.Default.PickAsync(new());
                if (result != null)
                {
                    Element.AddAttachment(result.FileName, MimeTypeMap.GetMimeType(new FileInfo(result.FileName).Extension), File.ReadAllBytes(result.FullPath));
                    EvaluateExpressions();
                    (GetTemplateChild(AttachmentsListViewName) as CollectionView)?.ScrollTo(Element.Attachments.Last());
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Failed to add attachment: " + ex.Message);
            }
        }
    }
}
#endif