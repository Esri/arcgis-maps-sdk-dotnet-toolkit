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
using System.Linq;
#if IOS || MACCATALYST
using UniformTypeIdentifiers;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class AttachmentsFormElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private const string AttachmentsListViewName = "AttachmentsListView";
        private const string AddAttachmentButtonName = "AddAttachmentButton";
        private const string MinAttachmentBadgeName = "MinAttachmentBadge";
        private const string MaxAttachmentBadgeName = "MaxAttachmentBadge";
        private const string CaptureMethodUnsupportedLabelName = "CaptureMethodUnsupportedLabel";
        private const string AttachmentErrorLabelName = "AttachmentErrorLabel";
        private static readonly Color EnabledAddAttachmentColor = Colors.CornflowerBlue;
        private static readonly Color DisabledAddAttachmentColor = Colors.Gray;

        private Button? _addAttachmentButton;
        private Label? _minAttachmentBadge;
        private Label? _maxAttachmentBadge;
        private Label? _captureMethodUnsupportedLabel;
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
            var captureMethodUnsupportedLabel = new Label() { Style = FeatureFormView.GetFeatureFormCaptionStyle(), IsVisible = false, Margin = new Thickness(0, 2, 0, 0), LineBreakMode = LineBreakMode.WordWrap };
            captureMethodUnsupportedLabel.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#B16800"), Color.FromArgb("#FFC900"));
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
            root.Children.Add(captureMethodUnsupportedLabel);
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
            nameScope.RegisterName(CaptureMethodUnsupportedLabelName, captureMethodUnsupportedLabel);
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
            _captureMethodUnsupportedLabel = GetTemplateChild(CaptureMethodUnsupportedLabelName) as Label;
            _attachmentErrorLabel = GetTemplateChild(AttachmentErrorLabelName) as Label;
            UpdateCaptureMethodUnsupportedState();
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
            if (page is null)
            {
                return;
            }

            var actions = BuildMobileAttachmentActions();
            if (actions.Count == 0)
            {
                return;
            }

            string? result = await page.DisplayActionSheetAsync(
                Properties.Resources.GetString("FeatureFormAddAttachmentMenuFromFile"),
                Properties.Resources.GetString("FeatureFormRenameAttachmentDialogCancel"),
                null,
                actions.Select(static a => a.Title).ToArray());

            if (string.IsNullOrEmpty(result))
            {
                return;
            }

            var selectedAction = actions.FirstOrDefault(action => string.Equals(action.Title, result, StringComparison.Ordinal));
            if (selectedAction is not null)
            {
                await selectedAction.ExecuteAsync();
            }
        }

        private async Task AddSelectedMediaAsync(Task<FileResult?> mediaTask)
        {
            if (!CanAddAttachment())
            {
                return;
            }

            try
            {
                var file = await mediaTask;
                if (file is null || Element is null || !CanAddAttachment())
                {
                    return;
                }

                await AddAttachmentFromResultAsync(file);
            }
            catch (System.Exception ex)
            {
                if (!TryHandleAttachmentValidationException(ex))
                {
                    Trace.WriteLine("Failed to add attachment: " + ex.Message, "ArcGIS Maps SDK Toolkit");
                }
            }
        }

        private async Task AddAttachmentFromResultAsync(FileResult result)
        {
            if (Element is null || !CanAddAttachment())
            {
                return;
            }

            await using var stream = await result.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            var extension = System.IO.Path.GetExtension(result.FileName);
            var contentType = string.IsNullOrWhiteSpace(result.ContentType)
                ? MimeTypeMap.GetMimeType(extension)
                : result.ContentType;

#if IOS
            // Workaround https://github.com/dotnet/maui/issues/15562
            if (!string.IsNullOrEmpty(contentType) && !contentType.Contains('/'))
            {
                contentType = "image/" + contentType;
            }
#endif

            await Element.AddAttachmentAsync(result.FileName, contentType, memoryStream.ToArray());
            EvaluateExpressions();
            UpdateAddAttachmentButtonState();
            (GetTemplateChild(AttachmentsListViewName) as CollectionView)?.ScrollTo(Element.Attachments.Last());
        }

        private sealed class MobileAttachmentAction
        {
            public required string Title { get; init; }

            public required Func<Task> ExecuteAsync { get; init; }
        }

        private List<MobileAttachmentAction> BuildMobileAttachmentActions()
        {
            var actions = new List<MobileAttachmentAction>();
            var capabilities = GetMobileAttachmentCapabilities();

            if (capabilities.SupportsCapture && MediaPicker.IsCaptureSupported)
            {
                if (capabilities.CanCaptureImage)
                {
                    actions.Add(new MobileAttachmentAction
                    {
                        Title = Properties.Resources.GetString("FeatureFormAddAttachmentMenuWithCamera")!,
                        ExecuteAsync = CapturePhotoAsync,
                    });
                }

                if (capabilities.CanCaptureVideo)
                {
                    actions.Add(new MobileAttachmentAction
                    {
                        Title = Properties.Resources.GetString("FeatureFormAddAttachmentMenuWithVideoCamera")!,
                        ExecuteAsync = CaptureVideoAsync,
                    });
                }
            }

            if (capabilities.SupportsLibrary)
            {
#if !WINDOWS
                if (capabilities.CanPickImageFromLibrary)
                {
                    actions.Add(new MobileAttachmentAction
                    {
                        Title = capabilities.CanPickVideoFromLibrary
                            ? Properties.Resources.GetString("FeatureFormAddAttachmentMenuChoosePhotoFromLibrary")!
                            : Properties.Resources.GetString("FeatureFormAddAttachmentMenuFromLibrary")!,
                        ExecuteAsync = () => AddSelectedMediaAsync(MediaPicker.PickPhotoAsync()),
                    });
                }

                if (capabilities.CanPickVideoFromLibrary)
                {
                    actions.Add(new MobileAttachmentAction
                    {
                        Title = capabilities.CanPickImageFromLibrary
                            ? Properties.Resources.GetString("FeatureFormAddAttachmentMenuChooseVideoFromLibrary")!
                            : Properties.Resources.GetString("FeatureFormAddAttachmentMenuFromLibrary")!,
                        ExecuteAsync = () => AddSelectedMediaAsync(MediaPicker.PickVideoAsync()),
                    });
                }
#endif
            }

            if (capabilities.CanChooseFromFiles)
            {
                actions.Add(new MobileAttachmentAction
                {
                    Title = Properties.Resources.GetString("FeatureFormAddAttachmentMenuChooseFromFiles")!,
                    ExecuteAsync = () =>
                    {
                        AddAttachmentFromFile();
                        return Task.CompletedTask;
                    },
                });
            }

            return actions;
        }

        private async Task CapturePhotoAsync()
        {
            if (!MediaPicker.IsCaptureSupported)
            {
                return;
            }

#if ANDROID
            if (!Permissions.IsDeclaredInManifest("android.permission.CAMERA"))
            {
                Trace.WriteLine("**Microsoft.Maui.ApplicationModel.PermissionException:** 'You need to declare using the permission: `android.permission.CAMERA` in your AndroidManifest.xml'", "ArcGIS Maps SDK Toolkit");
                return;
            }

#elif IOS
            // Check if manifest allows camera access.
            if (!Permissions.IsKeyDeclaredInInfoPlist("NSCameraUsageDescription"))
            {
                Trace.WriteLine("You must set `NSCameraUsageDescription` in your Info.plist file to use the Permission: Camera.", "ArcGIS Maps SDK Toolkit");
                return;
            }

            if (!Permissions.IsKeyDeclaredInInfoPlist("NSPhotoLibraryAddUsageDescription"))
            {
                Trace.WriteLine("You must set `NSPhotoLibraryAddUsageDescription` in your Info.plist file to use the Permission: PhotosAddOnly.", "ArcGIS Maps SDK Toolkit");
                return;
            }
#endif

            await AddSelectedMediaAsync(MediaPicker.CapturePhotoAsync());
        }

        private async Task CaptureVideoAsync()
        {
            if (!MediaPicker.IsCaptureSupported)
            {
                return;
            }

#if ANDROID
            if (!Permissions.IsDeclaredInManifest("android.permission.CAMERA"))
            {
                Trace.WriteLine("**Microsoft.Maui.ApplicationModel.PermissionException:** 'You need to declare using the permission: `android.permission.CAMERA` in your AndroidManifest.xml'", "ArcGIS Maps SDK Toolkit");
                return;
            }
#elif IOS
            if (!Permissions.IsKeyDeclaredInInfoPlist("NSCameraUsageDescription"))
            {
                Trace.WriteLine("You must set `NSCameraUsageDescription` in your Info.plist file to use the Permission: Camera.", "ArcGIS Maps SDK Toolkit");
                return;
            }

            if (!Permissions.IsKeyDeclaredInInfoPlist("NSMicrophoneUsageDescription"))
            {
                Trace.WriteLine("You must set `NSMicrophoneUsageDescription` in your Info.plist file to use the Permission: Microphone.", "ArcGIS Maps SDK Toolkit");
                return;
            }

            if (!Permissions.IsKeyDeclaredInInfoPlist("NSPhotoLibraryAddUsageDescription"))
            {
                Trace.WriteLine("You must set `NSPhotoLibraryAddUsageDescription` in your Info.plist file to use the Permission: PhotosAddOnly.", "ArcGIS Maps SDK Toolkit");
                return;
            }
#endif

            await AddSelectedMediaAsync(MediaPicker.CaptureVideoAsync());
        }

        private MobileAttachmentCapabilities GetMobileAttachmentCapabilities()
        {
            var capabilities = new MobileAttachmentCapabilities();
            if (Element is null)
            {
                return capabilities;
            }

            foreach (var input in Element.Inputs)
            {
                switch (input)
                {
                    case ImageFormInput image:
                        ApplyImageInputMethod(capabilities, image.InputMethod);
                        break;
                    case VideoFormInput video:
                        ApplyVideoInputMethod(capabilities, video.InputMethod);
                        break;
                    case AudioFormInput audio:
                        if (audio.InputMethod is AttachmentInputMethod.Any or AttachmentInputMethod.Upload)
                        {
                            capabilities.CanChooseFromFiles = true;
                        }
                        break;
                    case DocumentFormInput:
                        capabilities.CanChooseFromFiles = true;
                        break;
                }
            }

            return capabilities;
        }

        private static void ApplyImageInputMethod(MobileAttachmentCapabilities capabilities, AttachmentInputMethod method)
        {
            if (method is AttachmentInputMethod.Any or AttachmentInputMethod.Capture)
            {
                capabilities.CanCaptureImage = true;
            }

            if (method is AttachmentInputMethod.Any or AttachmentInputMethod.Upload)
            {
                capabilities.CanPickImageFromLibrary = true;
                capabilities.CanChooseFromFiles = true;
            }
        }

        private static void ApplyVideoInputMethod(MobileAttachmentCapabilities capabilities, AttachmentInputMethod method)
        {
            if (method is AttachmentInputMethod.Any or AttachmentInputMethod.Capture)
            {
                capabilities.CanCaptureVideo = true;
            }

            if (method is AttachmentInputMethod.Any or AttachmentInputMethod.Upload)
            {
                capabilities.CanPickVideoFromLibrary = true;
                capabilities.CanChooseFromFiles = true;
            }
        }

        private sealed class MobileAttachmentCapabilities
        {
            public bool CanCaptureImage { get; set; }

            public bool CanCaptureVideo { get; set; }

            public bool CanPickImageFromLibrary { get; set; }

            public bool CanPickVideoFromLibrary { get; set; }

            public bool CanChooseFromFiles { get; set; }

            public bool SupportsCapture => CanCaptureImage || CanCaptureVideo;

            public bool SupportsLibrary => CanPickImageFromLibrary || CanPickVideoFromLibrary;
        }

        private async void AddAttachmentFromFile()
        {
            if (!CanAddAttachment()) return;
            try
            {
                var result = await FilePicker.Default.PickAsync(CreatePickOptionsForCurrentInputs());
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
                if (!TryHandleAttachmentValidationException(ex))
                {
                    System.Diagnostics.Trace.WriteLine("Failed to add attachment: " + ex.Message);
                }
            }
        }

        private PickOptions CreatePickOptionsForCurrentInputs()
        {
            var options = new PickOptions();
            var allowedMimeTypes = GetAllowedMimeTypesForCurrentInputs();
            if (allowedMimeTypes.Count == 0)
            {
                return options;
            }

            var allowedExtensions = GetAllowedFileExtensionsForCurrentInputs();

#if WINDOWS
            if (allowedExtensions.Count > 0)
            {
                options.FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, allowedExtensions },
                });
            }
#elif ANDROID
            options.FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, allowedMimeTypes },
            });
#elif IOS
            var iosFileTypes = GetApplePickerTypes(allowedExtensions, allowedMimeTypes);
            options.FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, iosFileTypes },
            });
#elif MACCATALYST
            var macFileTypes = GetApplePickerTypes(allowedExtensions, allowedMimeTypes);
            options.FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.MacCatalyst, macFileTypes },
            });
#endif

            return options;
        }

        private static IReadOnlyList<string> GetApplePickerTypes(IReadOnlyList<string> allowedExtensions, IReadOnlyList<string> allowedMimeTypes)
        {
#if IOS || MACCATALYST
            var pickerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var extension in allowedExtensions)
            {
                if (string.IsNullOrWhiteSpace(extension))
                {
                    continue;
                }

                var extensionWithoutDot = extension.StartsWith('.') ? extension[1..] : extension;
                var type = UTType.CreateFromExtension(extensionWithoutDot) ?? UTType.CreateFromExtension(extension);
                if (type is not null && !string.IsNullOrWhiteSpace(type.Identifier))
                {
                    pickerTypes.Add(type.Identifier);
                }
            }

            if (pickerTypes.Count > 0)
            {
                // Keep extension-based filters strict. Add only narrow text fallback for plain text files.
                if (allowedMimeTypes.Contains("text/*", StringComparer.OrdinalIgnoreCase))
                {
                    pickerTypes.Add("public.plain-text");
                    pickerTypes.Add("public.text");
                }

                return pickerTypes.ToList();
            }

            foreach (var uti in GetIosUtiTypesForMimeTypes(allowedMimeTypes))
            {
                pickerTypes.Add(uti);
            }

            if (pickerTypes.Count > 0)
            {
                return pickerTypes.ToList();
            }
#endif

            return GetIosUtiTypesForMimeTypes(allowedMimeTypes).ToList();
        }

        private static IEnumerable<string> GetIosUtiTypesForMimeTypes(IReadOnlyList<string> mimeTypes)
        {
            var utiTypes = new List<string>();
            foreach (var mimeType in mimeTypes)
            {
                switch (mimeType)
                {
                    case "image/*":
                        if (!utiTypes.Contains("public.image", StringComparer.OrdinalIgnoreCase))
                        {
                            utiTypes.Add("public.image");
                        }
                        break;
                    case "video/*":
                        if (!utiTypes.Contains("public.movie", StringComparer.OrdinalIgnoreCase))
                        {
                            utiTypes.Add("public.movie");
                        }
                        break;
                    case "audio/*":
                        if (!utiTypes.Contains("public.audio", StringComparer.OrdinalIgnoreCase))
                        {
                            utiTypes.Add("public.audio");
                        }
                        break;
                    case "text/*":
                        if (!utiTypes.Contains("public.plain-text", StringComparer.OrdinalIgnoreCase))
                        {
                            utiTypes.Add("public.plain-text");
                        }
                        if (!utiTypes.Contains("public.text", StringComparer.OrdinalIgnoreCase))
                        {
                            utiTypes.Add("public.text");
                        }
                        break;
                    case "application/*":
                        if (!utiTypes.Contains("com.adobe.pdf", StringComparer.OrdinalIgnoreCase))
                        {
                            utiTypes.Add("com.adobe.pdf");
                        }
                        if (!utiTypes.Contains("public.composite-content", StringComparer.OrdinalIgnoreCase))
                        {
                            utiTypes.Add("public.composite-content");
                        }
                        break;
                    default:
                        if (!utiTypes.Contains("public.data", StringComparer.OrdinalIgnoreCase))
                        {
                            utiTypes.Add("public.data");
                        }
                        break;
                }
            }

            return utiTypes;
        }

        private async partial Task ShowAttachmentValidationAlertAsync(string message)
        {
            if (GetParent<Page>() is Page page)
            {
                await page.DisplayAlertAsync(
                    Properties.Resources.GetString("FeatureFormAttachmentValidationErrorTitle")!,
                    message,
                    Properties.Resources.GetString("FeatureFormRenameAttachmentDialogOK")!);
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

        private partial void UpdateCaptureMethodUnsupportedTextCore(string warningText, bool warningVisible)
        {
            if (_captureMethodUnsupportedLabel is not null)
            {
                _captureMethodUnsupportedLabel.Text = warningText;
                _captureMethodUnsupportedLabel.IsVisible = warningVisible;
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