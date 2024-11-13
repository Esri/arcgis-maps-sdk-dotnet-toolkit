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
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;

#if NET6_0_OR_GREATER
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
#endif
#if WINUI
using Microsoft.UI.Xaml.Media.Imaging;
#elif WINDOWS_UWP
using Windows.UI.Xaml.Media.Imaging;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a <see cref="PopupAttachment"/>.
    /// </summary>
    public partial class AttachmentThumbnailImage : Control
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentThumbnailImage"/> class.
        /// </summary>
        public AttachmentThumbnailImage()
        {
            DefaultStyleKey = typeof(AttachmentThumbnailImage);
        }

        /// <inheritdoc/>
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            
            UpdateVisualState(false);
            LoadThumbnail();
        }

        /// <summary>
        /// Gets or sets the attachment to display.
        /// </summary>
        public PopupAttachment? Attachment
        {
            get { return (PopupAttachment)GetValue(AttachmentProperty); }
            set { SetValue(AttachmentProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Attachment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AttachmentProperty =
            DependencyProperty.Register(nameof(Attachment), typeof(PopupAttachment), typeof(AttachmentThumbnailImage), new PropertyMetadata(null, (d, e) => ((AttachmentThumbnailImage)d).OnAttachmentChanged(e.OldValue as PopupAttachment, e.NewValue as PopupAttachment)));

        private void OnAttachmentChanged(PopupAttachment? oldAttachment, PopupAttachment? newAttachment)
        {
            var img = GetTemplateChild("PART_Image") as Image;
            if(img != null)
            {
                img.Source = null;
            }   
            if (oldAttachment != null)
            {
                oldAttachment.LoadStatusChanged -= Attachment_LoadStatusChanged;
            }
            if (newAttachment != null && newAttachment.LoadStatus != LoadStatus.Loaded)
            {
                newAttachment.LoadStatusChanged += Attachment_LoadStatusChanged;
            }
            LoadThumbnail();
            UpdateVisualState(false);
        }

        private void Attachment_LoadStatusChanged(object? sender, LoadStatusEventArgs e)
        {
            if (e.Status == LoadStatus.Loaded)
            {
                ((ILoadable)sender!).LoadStatusChanged -= Attachment_LoadStatusChanged;
                this.Dispatch(LoadThumbnail);
            }
            this.Dispatch(() => UpdateVisualState(true));
        }

        private void UpdateVisualState(bool useTransistions)
        {
            var status = Attachment?.LoadStatus ?? LoadStatus.NotLoaded;
            bool isLocal = Attachment?.IsLocal ?? false;
            if (isLocal)
                VisualStateManager.GoToState(this, "AttachmentIsLocal", useTransistions);
            else
            {
                switch (status)
                {
                    case LoadStatus.Loading:
                        VisualStateManager.GoToState(this, "AttachmentLoading", useTransistions);
                        break;
                    case LoadStatus.Loaded:
                        VisualStateManager.GoToState(this, "AttachmentLoaded", useTransistions);
                        break;
                    case LoadStatus.FailedToLoad:
                        VisualStateManager.GoToState(this, "AttachmentFailedToLoad", useTransistions);
                        break;
                    case LoadStatus.NotLoaded:
                        VisualStateManager.GoToState(this, "AttachmentNotLoaded", useTransistions);
                        break;
                }
            }
        }

        private async void LoadThumbnail()
        {
            var img = GetTemplateChild("PART_Image") as Image;
            if (img is null || ThumbnailSize <= 0)
                return;
            try
            {
#if WINUI
                var size = ThumbnailSize * XamlRoot?.RasterizationScale ?? 1;
#elif WINDOWS_UWP
                var size = ThumbnailSize * Windows.Graphics.Display.DisplayInformation.GetForCurrentView()?.RawPixelsPerViewPixel ?? 1;
#elif WPF
                var size = ThumbnailSize * VisualTreeHelper.GetDpi(this).PixelsPerDip;
#endif
#if NETFRAMEWORK
                if (Attachment != null && Attachment.Type == PopupAttachmentType.Image && Attachment.IsLocal)
                {
                    var thumb = await Attachment.CreateThumbnailAsync((int)size, (int)size);
                    img.Source = await thumb.ToImageSourceAsync();
                    return;
                }
#else
                if (Attachment != null && Attachment.IsLocal)
                {
                    if (!File.Exists(Attachment.Filename) && Attachment.LoadStatus == LoadStatus.NotLoaded)
                    {
                        await Attachment.LoadAsync();
                    }
                    if (File.Exists(Attachment.Filename))
                    {
                        var fs = await Windows.Storage.StorageFile.GetFileFromPathAsync(Attachment.Filename);

                        var thumb = await fs.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, (uint)size, Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail);
                        using var ms = new MemoryStream();
                        thumb.AsStreamForRead().CopyTo(ms);
                        ms.Seek(0, SeekOrigin.Begin);
#if WPF
                        img.Source = System.Windows.Media.Imaging.BitmapFrame.Create(ms, System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
#else
                        var source = new BitmapImage();
                        await source.SetSourceAsync(ms.AsRandomAccessStream());
                        img.Source = source;
#endif
                        return;
                    }
                }
#endif
            }
            catch { }
            img.Source = null;
        }

        /// <summary>
        /// Gets or sets the size of the thumbnail to display.
        /// </summary>
        public double ThumbnailSize
        {
            get { return (double)GetValue(ThumbnailSizeProperty); }
            set { SetValue(ThumbnailSizeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ThumbnailSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbnailSizeProperty =
            DependencyProperty.Register(nameof(ThumbnailSize), typeof(double), typeof(AttachmentThumbnailImage), new PropertyMetadata(30d));
    }
}
#endif