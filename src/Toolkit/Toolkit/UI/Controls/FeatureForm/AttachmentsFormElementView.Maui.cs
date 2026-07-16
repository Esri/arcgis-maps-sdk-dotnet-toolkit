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
using Microsoft.Maui.Controls.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class AttachmentsFormElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private const string AttachmentsListViewName = "AttachmentsListView";
        private const string AddAttachmentButtonName = "AddAttachmentButton";
        private const string MinAttachmentBadgeName = "MinAttachmentBadge";
        private const string MaxAttachmentBadgeName = "MaxAttachmentBadge";
        private const string AttachmentErrorLabelName = "AttachmentErrorLabel";
        private static readonly Color EnabledAddAttachmentColor = Colors.CornflowerBlue;
        private static readonly Color DisabledAddAttachmentColor = Colors.Gray;

        private Button? _addAttachmentButton;
        private Label? _minAttachmentBadge;
        private Label? _maxAttachmentBadge;
        private Label? _attachmentErrorLabel;

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
            root.SetBinding(VerticalStackLayout.IsVisibleProperty, static (AttachmentsFormElementView view) => view.Element?.IsVisible, source: RelativeBindingSource.TemplatedParent, converter: BoolOrNullToBoolConverter.Instance);

            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            header.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            header.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var label = new Label();
            label.SetBinding(Label.TextProperty, static (AttachmentsFormElementView view) => view.Element?.Label, source: RelativeBindingSource.TemplatedParent);
            label.SetBinding(View.IsVisibleProperty, static (Label label) => label.Text, source: RelativeBindingSource.Self, converter: EmptyStringToBoolConverter.Instance);
            label.Style = FeatureFormView.GetFeatureFormTitleStyle();
            header.Children.Add(label);

            label = new Label();
            label.SetBinding(Label.TextProperty, static (AttachmentsFormElementView view) => view.Element?.Description, source: RelativeBindingSource.TemplatedParent);
            label.SetBinding(Label.IsVisibleProperty, static (Label label) => label.Text, source: RelativeBindingSource.Self, converter: EmptyStringToBoolConverter.Instance);
            label.Style = FeatureFormView.GetFeatureFormCaptionStyle();
            label.Opacity = 0.7;

            var chipRow = new HorizontalStackLayout() { Spacing = 6, Margin = new Thickness(0, 2, 0, 0) };
            var minBadge = new Label() { Style = FeatureFormView.GetFeatureFormCaptionStyle(), IsVisible = false, Opacity = .7 };
            var maxBadge = new Label() { Style = FeatureFormView.GetFeatureFormCaptionStyle(), IsVisible = false, Opacity = .7 };
            var errorLabel = new Label() { Style = FeatureFormView.GetFeatureFormCaptionStyle(), IsVisible = false, TextColor = Colors.Red, Margin = new Thickness(0, 2, 0, 0), LineBreakMode = LineBreakMode.WordWrap };
            var minBadgeBorder = new Border()
            {
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle() { CornerRadius = 8 },
                Padding = new Thickness(8, 2),
                BackgroundColor = Colors.Transparent,
                Content = minBadge,
            };
            minBadgeBorder.SetBinding(IsVisibleProperty, static (Label value) => value.IsVisible, source: minBadge);

            var maxBadgeBorder = new Border()
            {
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle() { CornerRadius = 8 },
                Padding = new Thickness(8, 2),
                BackgroundColor = Colors.Transparent,
                Content = maxBadge,
            };
            maxBadgeBorder.SetBinding(IsVisibleProperty, static (Label value) => value.IsVisible, source: maxBadge);

            chipRow.Children.Add(minBadgeBorder);
            chipRow.Children.Add(maxBadgeBorder);
            Button addButton = new Button()
            {
                Margin = new Thickness(0, -5, 0, 0),
                Text = ToolkitIcons.Plus,
                FontFamily = ToolkitIcons.FontFamilyName,
                BorderWidth = 0,
                FontSize = 24,
                BackgroundColor = Colors.Transparent,
                TextColor = EnabledAddAttachmentColor,
                HorizontalOptions = new LayoutOptions(LayoutAlignment.Start, true),
                VerticalOptions = new LayoutOptions(LayoutAlignment.Start, true),
                Padding = new Thickness(5)
            };
           
            
            Grid.SetColumn(addButton, 1);
            addButton.SetBinding(VisualElement.IsVisibleProperty, static (AttachmentsFormElementView view) => view.Element?.IsEditable, source: RelativeBindingSource.TemplatedParent, converter: BoolOrNullToBoolConverter.Instance);
            header.Children.Add(addButton);

            root.Children.Add(header);
            root.Children.Add(label);
            root.Children.Add(chipRow);
            root.Children.Add(errorLabel);

            CollectionView itemsView = new CollectionView()
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Default,
                EmptyView = new Label() { Text = Properties.Resources.GetString("FeatureFormNoAttachments"), TextColor = Colors.Gray },
                ItemsLayout = new GridItemsLayout(1, ItemsLayoutOrientation.Horizontal) { HorizontalItemSpacing = 4 },
                ItemTemplate = new DataTemplate(() =>
                {
                    var view = new FormAttachmentView();
                    view.SetBinding(FormAttachmentView.AttachmentProperty, static (FormAttachment formAttachment) => formAttachment);
                    view.SetBinding(FormAttachmentView.ElementProperty, static (AttachmentsFormElementView view) => view.Element, source: RelativeBindingSource.TemplatedParent);
                    view.SetAppThemeColor(FormAttachmentView.IconColorProperty, Colors.Black, Colors.White);
                    view.SetBinding(ToolTipProperties.TextProperty, static (FormAttachment formAttachment) => formAttachment.Name);
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
            nameScope.RegisterName(MinAttachmentBadgeName, minBadge);
            nameScope.RegisterName(MaxAttachmentBadgeName, maxBadge);
            nameScope.RegisterName(AttachmentErrorLabelName, errorLabel);
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
            _minAttachmentBadge = GetTemplateChild(MinAttachmentBadgeName) as Label;
            _maxAttachmentBadge = GetTemplateChild(MaxAttachmentBadgeName) as Label;
            _attachmentErrorLabel = GetTemplateChild(AttachmentErrorLabelName) as Label;
            UpdateAddAttachmentButtonState();
            UpdateMinMaxAttachmentText();
            UpdateVisibility();
        }

        private async void AddAttachmentButton_Click(object? sender, EventArgs e)
        {
            if (!CanAddAttachment())
            {
                return;
            }

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
                
                var result = await page.DisplayActionSheetAsync(addAttachment, null, null, camera, addAttachment);
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
                            if (!CanAddAttachment())
                            {
                                return;
                            }

                            using (var stream = await photo.OpenReadAsync())
                            {
                                using var sr = new BinaryReader(stream);
                                var data = sr.ReadBytes((int)stream.Length);
                                var contentType = photo.ContentType;
#if IOS                         // Workaround https://github.com/dotnet/maui/issues/15562
                                if (!contentType.Contains('/'))
                                    contentType = "image/" + contentType;
#endif
                                await Element.AddAttachmentAsync(photo.FileName, contentType, data);
                            }
                            EvaluateExpressions();
                            UpdateAddAttachmentButtonState();
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
            if (!CanAddAttachment()) return;
            try
            {
                var result = await FilePicker.Default.PickAsync(new());
                if (result != null && CanAddAttachment())
                {
                    await Element.AddAttachmentAsync(result.FileName, MimeTypeMap.GetMimeType(new FileInfo(result.FileName).Extension), File.ReadAllBytes(result.FullPath));
                    EvaluateExpressions();
                    UpdateAddAttachmentButtonState();
                    (GetTemplateChild(AttachmentsListViewName) as CollectionView)?.ScrollTo(Element.Attachments.Last());
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Failed to add attachment: " + ex.Message);
            }
        }

        private partial void UpdateAddAttachmentButtonState()
        {
            if (_addAttachmentButton is not null)
            {
                bool canAddAttachment = CanAddAttachment();
                _addAttachmentButton.IsEnabled = canAddAttachment;
                _addAttachmentButton.Opacity = canAddAttachment ? 1.0 : 0.45;
                _addAttachmentButton.TextColor = canAddAttachment ? EnabledAddAttachmentColor : DisabledAddAttachmentColor;
            }
        }

        private partial void UpdateMinMaxAttachmentTextCore(string minAttachmentText, bool minVisible, string maxAttachmentText, bool maxVisible, string errorText, bool errorVisible)
        {
            if (_minAttachmentBadge is not null)
            {
                _minAttachmentBadge.Text = minAttachmentText;
                _minAttachmentBadge.IsVisible = minVisible;
            }

            if (_maxAttachmentBadge is not null)
            {
                _maxAttachmentBadge.Text = maxAttachmentText;
                _maxAttachmentBadge.IsVisible = maxVisible;
            }

            if (_attachmentErrorLabel is not null)
            {
                _attachmentErrorLabel.Text = errorText;
                _attachmentErrorLabel.IsVisible = errorVisible;
            }
        }
    }
}
#endif