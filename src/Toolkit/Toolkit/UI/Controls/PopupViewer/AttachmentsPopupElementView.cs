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

using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Microsoft.Win32;
#if WPF
using System.Windows.Controls.Primitives;
#elif MAUI
using ListBox = Microsoft.Maui.Controls.CollectionView;
using Selector = Microsoft.Maui.Controls.SelectableItemsView;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    public partial class AttachmentsPopupElementView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentsPopupElementView"/> class.
        /// </summary>
        public AttachmentsPopupElementView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(AttachmentsPopupElementView);
#endif
        }

        /// <inheritdoc />
#if WINDOWS_XAML || MAUI
        protected override void OnApplyTemplate()
#elif WPF
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            var itemsList = GetTemplateChild(AttachmentListName) as Selector;
            if (itemsList != null)
            {
                itemsList.SelectionChanged += ItemsList_SelectionChanged;
                LoadAttachments();
            }
        }

        private void ItemsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
#if MAUI
            if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
            {
                var attachment = e.CurrentSelection[0] as PopupAttachment;
#else
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                var attachment = e.AddedItems[0] as PopupAttachment;
#endif
                if (attachment?.Attachment != null)
                {
                    OnAttachmentClicked(attachment);
                }
                if (sender is Selector s)
                    s.SelectedItem = null;
            }
        }

        private async void LoadAttachments()
        {
            var itemsList = GetTemplateChild(AttachmentListName) as Selector;
            if (itemsList is null) return;
#if MAUI
            IsVisible = false;
#else
            Visibility = Visibility.Collapsed;
#endif
            itemsList.ItemsSource = null;
            if (Element is not null)
            {
                try
                {
                    await Element.GetAttachmentsAsync();
                }
                catch
                {

                }
                itemsList.ItemsSource = Element?.Attachments;
            }
            bool isVisible = (Element?.Attachments?.Count ?? 0) > 0;
#if MAUI
            IsVisible = isVisible;
#else
            Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
#endif

        }

        /// <summary>
        /// Gets or sets the AttachmentsPopupElement.
        /// </summary>
        public AttachmentsPopupElement? Element
        {
            get { return GetValue(ElementProperty) as AttachmentsPopupElement; }
            set { SetValue(ElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ElementProperty =
            PropertyHelper.CreateProperty<AttachmentsPopupElement, AttachmentsPopupElementView>(nameof(Element), null, (s, oldValue, newValue) => s.LoadAttachments());


        /// <summary>
        /// Occurs when an attachment is clicked.
        /// </summary>
        /// <remarks>
        /// <para>Override this to prevent the default open action.</para></remarks>
        /// <param name="attachment">Attachment clicked.</param>
        public virtual async void OnAttachmentClicked(PopupAttachment attachment)
        {
            if (attachment.Attachment != null)
            {
                var viewer = GetPopupViewerParent();
                if (viewer is not null)
                {
                    bool handled = viewer.OnPopupAttachmentClicked(attachment);
                    if (handled)
                        return;
                }
#if MAUI
                try
                {
                    if (attachment.LoadStatus == LoadStatus.NotLoaded)
                        await attachment.LoadAsync();
                    await Microsoft.Maui.ApplicationModel.Launcher.Default.OpenAsync(
                        new Microsoft.Maui.ApplicationModel.OpenFileRequest(attachment.Name, new ReadOnlyFile(attachment.Filename!, attachment.ContentType)));
                }
                catch(System.Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Failed to open attachment: " + ex.Message);
                }
#elif WPF
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = attachment.Name;
                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        using var stream = await attachment.Attachment!.GetDataAsync();
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
                try
                {
                    if (attachment.LoadStatus == LoadStatus.NotLoaded)
                        await attachment.LoadAsync();
                    var fileInfo = new FileInfo(attachment.Filename!);
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
                        using var filestream= await file.OpenStreamForWriteAsync();
                        await stream.CopyToAsync(filestream);
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Failed to open attachment: " + ex.Message);
                }
                finally
                {
                    if (file != null)
                        _ = Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                }
#endif
            }
        }
    }
}