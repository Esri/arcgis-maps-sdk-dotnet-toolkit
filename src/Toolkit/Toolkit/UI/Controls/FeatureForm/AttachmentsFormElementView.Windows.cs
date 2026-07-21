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
#if WINUI
                var hwnd = this.XamlRoot?.ContentIslandEnvironment?.AppWindowId.Value ?? 0;
                if (hwnd == 0)
                    return; // Can't show dialog without a root window
#endif
                var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
#if WINUI
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, (nint)hwnd);
#endif
                var allowedExtensions = GetAllowedFileExtensionsForCurrentInputs();
                if (allowedExtensions.Count > 0)
                {
#if WINUI
                    openPicker.FileTypeFilter.Add("*");
#else
                    foreach (var extension in allowedExtensions)
                    {
                        openPicker.FileTypeFilter.Add(extension);
                    }
#endif
                }
                else
                {
                    openPicker.FileTypeFilter.Add("*");
                }
                var file = await openPicker.PickSingleFileAsync();
                if (file != null)
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
#if WINDOWS_UWP
                    using var ms = new MemoryStream();
                    using var filestream = await file.OpenStreamForReadAsync();
                    await filestream.CopyToAsync(ms);
                    await element.AddAttachmentAsync(fileInfo.Name, MimeTypeMap.GetMimeType(fileInfo.Extension), ms.ToArray());
#else
                    await element.AddAttachmentAsync(fileInfo.Name, MimeTypeMap.GetMimeType(fileInfo.Extension), File.ReadAllBytes(fileInfo.FullName));
#endif
                    EvaluateExpressions();
                    UpdateAddAttachmentButtonState();
                }
#endif
            }
            catch (System.Exception ex)
            {
                if (!TryHandleAttachmentValidationException(ex))
                {
                    System.Diagnostics.Trace.WriteLine("Failed to add attachment: " + ex.Message, "ArcGIS Maps SDK Toolkit");
                }
            }
        }

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