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

#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Microsoft.Win32;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if WPF
using System.Windows.Controls.Primitives;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    [TemplatePart(Name = "AddAttachmentButton", Type = typeof(ButtonBase))]
    public partial class AttachmentsFormElementView : Control
    {
        private ButtonBase? _addAttachmentButton;
        private FrameworkElement? _minAttachmentBadge;
        private TextBlock? _minAttachmentBadgeText;
        private FrameworkElement? _maxAttachmentBadge;
        private TextBlock? _maxAttachmentBadgeText;
        private TextBlock? _captureMethodUnsupportedLabel;
        private TextBlock? _attachmentErrorLabel;
        private bool _scrollToEnd;

        /// <inheritdoc />
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            if (_addAttachmentButton is not null)
            {
                _addAttachmentButton.Click -= AddAttachmentButton_Click;
            }
            _addAttachmentButton = GetTemplateChild("AddAttachmentButton") as ButtonBase;
            if(_addAttachmentButton is not null)
            {
                _addAttachmentButton.Click += AddAttachmentButton_Click;
            }
            _minAttachmentBadge = GetTemplateChild("MinAttachmentBadge") as FrameworkElement;
            _minAttachmentBadgeText = GetTemplateChild("MinAttachmentBadgeText") as TextBlock;
            _maxAttachmentBadge = GetTemplateChild("MaxAttachmentBadge") as FrameworkElement;
            _maxAttachmentBadgeText = GetTemplateChild("MaxAttachmentBadgeText") as TextBlock;
            _captureMethodUnsupportedLabel = GetTemplateChild("CaptureMethodUnsupportedLabel") as TextBlock;
            _attachmentErrorLabel = GetTemplateChild("AttachmentErrorLabel") as TextBlock;
            UpdateCaptureMethodUnsupportedState();
            UpdateAddAttachmentButtonState();
            UpdateMinMaxAttachmentText();
            if (GetTemplateChild("ItemsScrollView") is ScrollViewer scrollViewer)
            {
#if WPF
                scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
#elif WINDOWS_XAML
                if(scrollViewer.Content is FrameworkElement element)
                {
                    element.SizeChanged += AttachmentsFormElementView_SizeChanged;
                }
#endif
            }
            UpdateVisibility();
        }


#if WPF
        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if(_scrollToEnd)
            {
                (sender as ScrollViewer)?.ScrollToRightEnd();
                _scrollToEnd = false;
            }
        }
#elif WINDOWS_XAML

        private void AttachmentsFormElementView_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            if (_scrollToEnd && GetTemplateChild("ItemsScrollView") is ScrollViewer scrollViewer)
            {
                scrollViewer.ChangeView(scrollViewer.ScrollableWidth, null, null);
            }
        }
#endif

        private async void AddAttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CanAddAttachment()) return;
            try
            {
#if WPF
                OpenFileDialog openFileDialog = new OpenFileDialog();
                var allowedExtensions = GetAllowedFileExtensionsForCurrentInputs();
                if (allowedExtensions.Count > 0)
                {
                    string filterName = Properties.Resources.GetString("FeatureFormAttachmentPickerSupportedFiles") ?? "Supported files";
                    openFileDialog.Filter = $"{filterName}|{string.Join(";", allowedExtensions.Select(static extension => $"*{extension}"))}";
                }
                if (openFileDialog.ShowDialog() == true)
                {
                    var fileInfo = new FileInfo(openFileDialog.FileName);
                    if (fileInfo.Exists && CanAddAttachment())
                    {
                        var element = Element;
                        if (element is null)
                        {
                            return;
                        }
                        _scrollToEnd = true;
                        await element.AddAttachmentAsync(fileInfo.Name, MimeTypeMap.GetMimeType(fileInfo.Extension), File.ReadAllBytes(fileInfo.FullName));
                        EvaluateExpressions();
                        UpdateAddAttachmentButtonState();
                    }
                }
#elif WINDOWS_XAML
                await ShowWinUiAttachmentActionsAsync(sender as ButtonBase);
#endif
            }
            catch (System.Exception ex)
            {
                if (!TryHandleAttachmentValidationException(ex))
                {
                    System.Diagnostics.Trace.WriteLine("Failed to add attachment: " + ex.Message);
                }
            }
        }

#if WINDOWS_XAML
        private sealed class WinUiAttachmentAction
        {
            public required string Title { get; init; }

            public required Func<Task> ExecuteAsync { get; init; }
        }

        private sealed class WinUiAttachmentCapabilities
        {
            public bool CanCaptureImage { get; set; }

            public bool CanCaptureVideo { get; set; }

            public bool CanCaptureAudio { get; set; }

            public bool CanChooseFromFiles { get; set; }

            public bool SupportsCapture => CanCaptureImage || CanCaptureVideo || CanCaptureAudio;
        }

        private WinUiAttachmentCapabilities GetWinUiAttachmentCapabilities()
        {
            var capabilities = new WinUiAttachmentCapabilities();
            var element = Element;
            if (element is null)
            {
                return capabilities;
            }

            foreach (var input in element.Inputs)
            {
                switch (input)
                {
                    case ImageFormInput imageInput:
                        if (imageInput.InputMethod is AttachmentInputMethod.Any or AttachmentInputMethod.Capture)
                        {
                            capabilities.CanCaptureImage = true;
                        }

                        if (imageInput.InputMethod is AttachmentInputMethod.Any or AttachmentInputMethod.Upload)
                        {
                            capabilities.CanChooseFromFiles = true;
                        }
                        break;

                    case VideoFormInput videoInput:
                        if (videoInput.InputMethod is AttachmentInputMethod.Any or AttachmentInputMethod.Capture)
                        {
                            capabilities.CanCaptureVideo = true;
                        }

                        if (videoInput.InputMethod is AttachmentInputMethod.Any or AttachmentInputMethod.Upload)
                        {
                            capabilities.CanChooseFromFiles = true;
                        }
                        break;

                    case AudioFormInput audioInput:
                        if (audioInput.InputMethod is AttachmentInputMethod.Any or AttachmentInputMethod.Capture)
                        {
                            capabilities.CanCaptureAudio = true;
                        }

                        if (audioInput.InputMethod is AttachmentInputMethod.Any or AttachmentInputMethod.Upload)
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

        private List<WinUiAttachmentAction> BuildWinUiAttachmentActions()
        {
            var actions = new List<WinUiAttachmentAction>();
            var capabilities = GetWinUiAttachmentCapabilities();

            if (capabilities.SupportsCapture)
            {
                if (capabilities.CanCaptureImage)
                {
                    actions.Add(new WinUiAttachmentAction
                    {
                        Title = Properties.Resources.GetString("FeatureFormAddAttachmentMenuWithCamera")!,
                        ExecuteAsync = CapturePhotoWinUiAsync,
                    });
                }

                if (capabilities.CanCaptureVideo)
                {
                    actions.Add(new WinUiAttachmentAction
                    {
                        Title = Properties.Resources.GetString("FeatureFormAddAttachmentMenuWithVideoCamera")!,
                        ExecuteAsync = CaptureVideoWinUiAsync,
                    });
                }

                if (capabilities.CanCaptureAudio)
                {
                    actions.Add(new WinUiAttachmentAction
                    {
                        Title = Properties.Resources.GetString("FeatureFormAddAttachmentMenuWithMicrophone")!,
                        ExecuteAsync = CaptureAudioWinUiAsync,
                    });
                }
            }

            if (capabilities.CanChooseFromFiles)
            {
                actions.Add(new WinUiAttachmentAction
                {
                    Title = Properties.Resources.GetString("FeatureFormAddAttachmentMenuChooseFromFiles")!,
                    ExecuteAsync = ChooseFromFilesWinUiAsync,
                });
            }

            return actions;
        }

        private async Task ShowWinUiAttachmentActionsAsync(ButtonBase? sourceButton)
        {
            var actions = BuildWinUiAttachmentActions();
            if (actions.Count == 0)
            {
                return;
            }

            if (sourceButton is null)
            {
                await actions[0].ExecuteAsync();
                return;
            }

            var flyout = new MenuFlyout();
            foreach (var action in actions)
            {
                var item = new MenuFlyoutItem { Text = action.Title };
                item.Click += async (_, __) => await action.ExecuteAsync();
                flyout.Items.Add(item);
            }

            flyout.ShowAt(sourceButton);
        }

        private async Task<Windows.Storage.StorageFile?> PickSingleWindowsFileAsync()
        {
            var hwnd = this.XamlRoot?.ContentIslandEnvironment?.AppWindowId.Value ?? 0;
            if (hwnd == 0)
            {
                return null;
            }

            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, (nint)hwnd);

            var allowedExtensions = GetAllowedFileExtensionsForCurrentInputs();
            if (allowedExtensions.Count > 0)
            {
                foreach (var extension in allowedExtensions)
                {
                    openPicker.FileTypeFilter.Add(extension);
                }
            }
            else
            {
                openPicker.FileTypeFilter.Add("*");
            }

            return await openPicker.PickSingleFileAsync();
        }

        private async Task ChooseFromFilesWinUiAsync()
        {
            var file = await PickSingleWindowsFileAsync();
            if (file is not null)
            {
                await AddPickedWindowsFileAsync(file);
            }
        }

        private async Task AddPickedWindowsFileAsync(Windows.Storage.StorageFile file)
        {
            var fileInfo = new FileInfo(file.Path);
            _scrollToEnd = true;
            if (!CanAddAttachment())
            {
                return;
            }

            var element = Element;
            if (element is null)
            {
                return;
            }

            await element.AddAttachmentAsync(fileInfo.Name, MimeTypeMap.GetMimeType(fileInfo.Extension), File.ReadAllBytes(fileInfo.FullName));
            EvaluateExpressions();
            UpdateAddAttachmentButtonState();
        }

        private async Task CapturePhotoWinUiAsync()
        {
            var hwnd = this.XamlRoot?.ContentIslandEnvironment?.AppWindowId.Value ?? 0;
            if (hwnd == 0)
            {
                return;
            }

            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow((nint)hwnd);
            var captureUi = new Microsoft.Windows.Media.Capture.CameraCaptureUI(windowId);
            var file = await captureUi.CaptureFileAsync(Microsoft.Windows.Media.Capture.CameraCaptureUIMode.Photo);
            if (file is not null)
            {
                await AddPickedWindowsFileAsync(file);
            }
        }

        private async Task CaptureVideoWinUiAsync()
        {
            var hwnd = this.XamlRoot?.ContentIslandEnvironment?.AppWindowId.Value ?? 0;
            if (hwnd == 0)
            {
                return;
            }

            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow((nint)hwnd);
            var captureUi = new Microsoft.Windows.Media.Capture.CameraCaptureUI(windowId);
            var file = await captureUi.CaptureFileAsync(Microsoft.Windows.Media.Capture.CameraCaptureUIMode.Video);
            if (file is not null)
            {
                await AddPickedWindowsFileAsync(file);
            }
        }

        private async Task CaptureAudioWinUiAsync()
        {
            Windows.Media.Capture.MediaCapture? mediaCapture = null;
            Windows.Storage.StorageFile? file = null;
            bool isRecording = false;
            bool shouldSave = false;

            try
            {
                mediaCapture = new Windows.Media.Capture.MediaCapture();
                await mediaCapture.InitializeAsync(new Windows.Media.Capture.MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.Audio,
                });

                file = await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFileAsync(
                    $"audio-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.m4a",
                    Windows.Storage.CreationCollisionOption.GenerateUniqueName);

                var dialog = new ContentDialog
                {
                    Title = Properties.Resources.GetString("FeatureFormAddAttachmentMenuWithMicrophone")!,
                    Content = Properties.Resources.GetString("FeatureFormAudioCaptureReady")!,
                    PrimaryButtonText = Properties.Resources.GetString("FeatureFormAudioCaptureRecord")!,
                    CloseButtonText = Properties.Resources.GetString("FeatureFormRenameAttachmentDialogCancel")!,
                    XamlRoot = this.XamlRoot,
                };

                dialog.PrimaryButtonClick += async (_, args) =>
                {
                    var deferral = args.GetDeferral();
                    try
                    {
                        if (!isRecording)
                        {
                            var profile = Windows.Media.MediaProperties.MediaEncodingProfile.CreateM4a(Windows.Media.MediaProperties.AudioEncodingQuality.Auto);
                            await mediaCapture.StartRecordToStorageFileAsync(profile, file);
                            isRecording = true;
                            args.Cancel = true;
                            dialog.Content = Properties.Resources.GetString("FeatureFormAudioCaptureInProgress")!;
                            dialog.PrimaryButtonText = Properties.Resources.GetString("FeatureFormAudioCaptureStop")!;
                        }
                        else
                        {
                            await mediaCapture.StopRecordAsync();
                            isRecording = false;
                            shouldSave = true;
                        }
                    }
                    finally
                    {
                        deferral.Complete();
                    }
                };

                var result = await dialog.ShowAsync();

                if (isRecording)
                {
                    await mediaCapture.StopRecordAsync();
                    isRecording = false;
                }

                if (result == ContentDialogResult.Primary && shouldSave && file is not null)
                {
                    await AddPickedWindowsFileAsync(file);
                }
                else if (file is not null)
                {
                    await file.DeleteAsync();
                }
            }
            finally
            {
                mediaCapture?.Dispose();
            }
        }
#endif

        private partial void UpdateAddAttachmentButtonState()
        {
            if (_addAttachmentButton is not null)
            {
                _addAttachmentButton.IsEnabled = CanAddAttachment();
            }
        }

        private partial void UpdateCaptureMethodUnsupportedTextCore(string warningText, bool warningVisible)
        {
            if (_captureMethodUnsupportedLabel is not null)
            {
                _captureMethodUnsupportedLabel.Text = warningText;
                _captureMethodUnsupportedLabel.Visibility = warningVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async partial Task ShowAttachmentValidationAlertAsync(string message)
        {
            string title = Properties.Resources.GetString("FeatureFormAttachmentValidationErrorTitle")!;
#if WPF
            System.Windows.MessageBox.Show(message, title);
            await Task.CompletedTask;
#elif WINDOWS_XAML
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = Properties.Resources.GetString("FeatureFormRenameAttachmentDialogOK")!
            };
            dialog.XamlRoot = this.XamlRoot;
            await dialog.ShowAsync();
#endif
        }

        private partial void UpdateMinMaxAttachmentTextCore(string minAttachmentText, bool minVisible, string maxAttachmentText, bool maxVisible, string errorText, bool errorVisible)
        {
            if (_minAttachmentBadge is not null)
            {
                _minAttachmentBadge.Visibility = minVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            if (_minAttachmentBadgeText is not null)
            {
                _minAttachmentBadgeText.Text = minAttachmentText;
            }

            if (_maxAttachmentBadge is not null)
            {
                _maxAttachmentBadge.Visibility = maxVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            if (_maxAttachmentBadgeText is not null)
            {
                _maxAttachmentBadgeText.Text = maxAttachmentText;
            }
            if (_attachmentErrorLabel is not null)
            {
                _attachmentErrorLabel.Text = errorText;
                _attachmentErrorLabel.Visibility = errorVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
#endif