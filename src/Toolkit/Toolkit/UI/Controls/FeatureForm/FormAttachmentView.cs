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

using System.ComponentModel;
using System.Diagnostics;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if WPF
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
#endif

#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui;
#else
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Displays a <see cref="FormAttachment"/> object.
    /// </summary>
    public partial class FormAttachmentView
    {
        private WeakEventListener<FormAttachmentView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>? _elementPropertyChangedListener;

        /// <summary>
        /// Initializes an instance of the <see cref="FormAttachmentView"/> class.
        /// </summary>
        public FormAttachmentView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
            WidthRequest = 92;
            HeightRequest = 75;
            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += TapGestureRecognizer_Tapped;
            GestureRecognizers.Add(tapGestureRecognizer);
#if WINDOWS || MACCATALYST
            ConfigureFlyout();
#endif
#else
            DefaultStyleKey = typeof(FormAttachmentView);
#endif
#if WINDOWS_XAML
            this.Click += OnClick;
#endif
        }

        private async void OnAttachmentClicked()
        {
            if (Attachment is null || Attachment.LoadStatus == LoadStatus.Loading) return;
            if (Attachment.LoadStatus == LoadStatus.FailedToLoad)
            {
                try
                {
                    await Attachment.RetryLoadAsync().ConfigureAwait(false);
                }
                catch (System.Exception ex)
                {
                    Trace.WriteLine("Failed to retry loading attachment: " + ex.Message, "ArcGIS Maps SDK Toolkit");
                }
            }
            else if (Attachment.LoadStatus == LoadStatus.NotLoaded)
            {
                try
                {
                    await Attachment.LoadAsync().ConfigureAwait(false);
                }
                catch (System.Exception ex)
                {
                    Trace.WriteLine("Failed to load attachment: " + ex.Message, "ArcGIS Maps SDK Toolkit");
                }
            }
            else if (Attachment.LoadStatus == LoadStatus.Loaded)
            {
                OnLoadedAttachmentClicked();
            }
        }

        private void DeleteAttachment()
        {
            if (Attachment is not null && Element is not null)
            {
                var view = FeatureFormView.GetFeatureFormViewParent(this);
                var form = view?.FeatureForm; // Get form before delete, or we won't be able to get to the parent since this instance will be removed from the tree
                Element.DeleteAttachment(Attachment);
                _ = view?.EvaluateExpressions(form);
            }
        }

        private void RenameAttachment(string newName)
        {
            if (Attachment != null && Attachment.Name != newName)
            {
                Attachment.Name = newName;
                var view = FeatureFormView.GetFeatureFormViewParent(this);
                _ = view?.EvaluateExpressions(view?.FeatureForm);
            }
        }

        /// <summary>
        /// Gets or sets the AttachmentsFormElement.
        /// </summary>
        public AttachmentsFormElement? Element
        {
            get { return (AttachmentsFormElement)GetValue(ElementProperty); }
            set { SetValue(ElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty ElementProperty = BindableProperty.Create(nameof(Element), typeof(AttachmentsFormElement), typeof(FormAttachmentView), null);
#else
        public static readonly DependencyProperty ElementProperty = DependencyProperty.Register(nameof(Element), typeof(AttachmentsFormElement), typeof(FormAttachmentView), null);
#endif

        /// <summary>
        /// Gets or sets the Attachment.
        /// </summary>
        public FormAttachment? Attachment
        {
            get { return (FormAttachment)GetValue(AttachmentProperty); }
            set { SetValue(AttachmentProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Attachment"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty AttachmentProperty =
            BindableProperty.Create(nameof(Attachment), typeof(FormAttachment), typeof(FormAttachmentView), null, propertyChanged: (s, oldValue, newValue) => ((FormAttachmentView)s).OnAttachmentPropertyChanged(oldValue as FormAttachment, newValue as FormAttachment));
#else
        public static readonly DependencyProperty AttachmentProperty =
            DependencyProperty.Register(nameof(Attachment), typeof(FormAttachment), typeof(FormAttachmentView), new PropertyMetadata(null, (s, e) => ((FormAttachmentView)s).OnAttachmentPropertyChanged(e.OldValue as FormAttachment, e.NewValue as FormAttachment)));
#endif

        private void OnAttachmentPropertyChanged(FormAttachment? oldValue, FormAttachment? newValue)
        {
            if (oldValue is INotifyPropertyChanged inpcOld)
            {
                _elementPropertyChangedListener?.Detach();
                _elementPropertyChangedListener = null;
            }
#if WPF
            _thumbnailSize = default;
#endif
            if (newValue is INotifyPropertyChanged inpcNew)
            {
                _elementPropertyChangedListener = new WeakEventListener<FormAttachmentView, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpcNew)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.Attachment_PropertyChanged(source, eventArgs),
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                inpcNew.PropertyChanged += _elementPropertyChangedListener.OnEvent;
            }
            if (newValue is not null)
            {
                if (newValue.LoadStatus == LoadStatus.NotLoaded && newValue.IsLocal) // Load local attachments immediately
                    _ = newValue.LoadAsync();
            }
            UpdateThumbnail();
            UpdateLoadedState(false);
        }

        private void Attachment_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FormAttachment.LoadStatus))
            {
                this.Dispatch(() => UpdateLoadedState(true));
            }
        }

        private async void OnLoadedAttachmentClicked()
        {
            FormAttachment? attachment = Attachment;
            if (attachment != null && attachment.IsLocal && attachment.LoadStatus == LoadStatus.Loaded)
            {
                var viewer = FeatureFormView.GetFeatureFormViewParent(this);
                if (viewer is not null)
                {
                    bool handled = viewer.OnFormAttachmentClicked(attachment);
                    if (handled)
                        return;
                }
#if MAUI
#if !WINDOWS && !MACCATALYST // Windows and Catalyst supports context menus for the additional options
                if (await DisplayAttachmentActionSheet())
                    return;
#endif
                try
                {
                    await Microsoft.Maui.ApplicationModel.Launcher.Default.OpenAsync(
                        new Microsoft.Maui.ApplicationModel.OpenFileRequest(attachment.Name, new ReadOnlyFile(attachment.FilePath, attachment.ContentType)));
                }
                catch(System.Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Failed to open attachment: " + ex.Message, "ArcGIS Maps SDK Toolkit");
                }
#else
#if WPF
                var saveFileDialog = new SaveFileDialog();
                var fileInfo = new System.IO.FileInfo(attachment.FilePath);
                saveFileDialog.FileName = fileInfo.Name;
                saveFileDialog.DefaultExt = fileInfo.Extension;
                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        var stream = System.IO.File.OpenRead(attachment.FilePath);
                        using var outfile = saveFileDialog.OpenFile();
                        await stream.CopyToAsync(outfile);
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine($"Failed to save file to disk: " + ex.Message);
                    }
                }
#elif WINDOWS_XAML
                Windows.Storage.StorageFile? file = null;
#if WINUI
                var hwnd = this.XamlRoot?.ContentIslandEnvironment?.AppWindowId.Value ?? 0;
                if (hwnd == 0)
                    return; // Can't show dialog without a root window
#endif
                var fileInfo = new FileInfo(attachment.FilePath);
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
#if WINUI
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, (nint)hwnd);
#endif
                var ext = fileInfo.Extension;
                savePicker.FileTypeChoices.Add("*" + ext, new List<string>() { ext });
                savePicker.SuggestedFileName = fileInfo.Name;
                file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    Windows.Storage.CachedFileManager.DeferUpdates(file);
                    using var stream = await attachment.Attachment!.GetDataAsync();
                    using var filestream = await file.OpenStreamForWriteAsync();
                    await stream.CopyToAsync(filestream);
                }
#endif
#endif
            }
        }
    }
}