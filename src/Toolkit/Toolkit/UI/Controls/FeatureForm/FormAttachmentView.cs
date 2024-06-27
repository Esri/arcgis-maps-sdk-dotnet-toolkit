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


#if WPF || MAUI
using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if WPF
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
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
            Margin = new Thickness(4, 0);
            ConfigureFlyout();
#else
            DefaultStyleKey = typeof(FormAttachmentView);
#endif
        }

        private void OnAttachmentClicked()
        {
            if (Attachment is null || Attachment.LoadStatus == LoadStatus.Loading) return;
            if (Attachment.LoadStatus == LoadStatus.FailedToLoad)
            {
                _ = Attachment.RetryLoadAsync();
            }
            else if (Attachment.LoadStatus == LoadStatus.NotLoaded)
            {
                _ = Attachment.LoadAsync();
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
                var form = GetFeatureFormViewParent()?.FeatureForm; // Get form before delete, or we won't be able to get to the parent since this instance will be removed from the tree
                Element.DeleteAttachment(Attachment);
                _ = form?.EvaluateExpressionsAsync();
            }
        }

        private void RenameAttachment(string newName)
        {
            if (Attachment != null && Attachment.Name != newName)
            {
                Attachment.Name = newName;
                _ = GetFeatureFormViewParent()?.FeatureForm?.EvaluateExpressionsAsync();
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
                var viewer = GetFeatureFormViewParent();
                if (viewer is not null)
                {
                    bool handled = viewer.OnFormAttachmentClicked(attachment);
                    if (handled)
                        return;
                }
#if MAUI
                try
                {
                    await Microsoft.Maui.ApplicationModel.Launcher.Default.OpenAsync(
                        new Microsoft.Maui.ApplicationModel.OpenFileRequest(attachment.Name, new ReadOnlyFile(attachment.FilePath, attachment.ContentType)));
                }
                catch(System.Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Failed to open attachment: " + ex.Message);
                }
#else
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
#endif
            }
        }
    }
}
#endif