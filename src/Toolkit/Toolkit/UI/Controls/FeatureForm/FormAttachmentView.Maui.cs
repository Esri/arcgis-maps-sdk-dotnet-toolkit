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
using Microsoft.Maui.Controls.Shapes;
using System.Runtime.Versioning;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class FormAttachmentView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static FormAttachmentView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            Border background = new Border() { BackgroundColor = Color.FromRgba(0, 0, 0, 0x30), StrokeShape = new RoundRectangle() { CornerRadius = 4 }, StrokeThickness = 0 };
            var root = new Grid();
            background.Content = root;
            Image image = new Image();
            root.Children.Add(image);
            root.SetBinding(VerticalStackLayout.IsVisibleProperty, static (FormElement element) => element.IsVisible);
            Border nameBackground = new Border() { BackgroundColor = Colors.Transparent, Padding = new Thickness(2), VerticalOptions = LayoutOptions.End, StrokeThickness = 0 };
            var nameLabel = new Label() { HorizontalOptions = LayoutOptions.Center, FontSize = 10, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation };
            nameBackground.Content = nameLabel;
            nameLabel.SetBinding(Label.TextProperty, static (FormAttachmentView view) => view.Attachment?.Name, source: RelativeBindingSource.TemplatedParent);
            root.Children.Add(nameBackground);
            var sizeLabel = new Label() { VerticalOptions = LayoutOptions.Start, HorizontalOptions = LayoutOptions.Center, FontSize = 10, MaxLines = 1 };
            sizeLabel.SetBinding(Label.TextProperty, static (FormAttachmentView view) => view.Attachment?.Size, source: RelativeBindingSource.TemplatedParent, converter: new FileSizeConverter());
            root.Children.Add(sizeLabel);
            var downloadIcon = new Label() { VerticalOptions = LayoutOptions.Start, HorizontalOptions = LayoutOptions.End, FontFamily = ToolkitIcons.FontFamilyName, Text = ToolkitIcons.DownloadTo, Margin = new Thickness(2) };
            root.Children.Add(downloadIcon);
            var downloadIndicator = new ActivityIndicator() { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, IsRunning = false };
            root.Children.Add(downloadIndicator);

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(background, nameScope);
            nameScope.RegisterName("ThumbnailImage", image);
            nameScope.RegisterName("AttachmentName", nameLabel);
            nameScope.RegisterName("FileSizeText", sizeLabel);
            nameScope.RegisterName("DownloadIcon", downloadIcon);
            nameScope.RegisterName("NameBackground", nameBackground);
            nameScope.RegisterName("DownloadIndicator", downloadIndicator);
            return background;
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("maccatalyst")]
        private void ConfigureFlyout()
        {
            MenuFlyout flyout = new MenuFlyout();
            flyout.Add(new MenuFlyoutItem()
            {
                Text = Properties.Resources.GetString("FeatureFormRemoveAttachmentMenuItem"),
                IconImageSource = new FontImageSource { Glyph = ToolkitIcons.Trash, FontFamily = ToolkitIcons.FontFamilyName, Size = 32, Color = Colors.Gray }
            });
            flyout.Add(new MenuFlyoutItem()
            {
                Text = Properties.Resources.GetString("FeatureFormRenameAttachmentMenuItem"),
                IconImageSource = new FontImageSource { Glyph = ToolkitIcons.Pencil, FontFamily = ToolkitIcons.FontFamilyName, Size = 32, Color = Colors.Gray }
            });

            ((MenuFlyoutItem)flyout[0]).Clicked += (s, e) => DeleteAttachment();

            ((MenuFlyoutItem)flyout[1]).Clicked += async (s, e) =>
            {
                if (Attachment is not null && Element is not null && GetPage() is Page page)
                {
                    try
                    {
                        string result = await page.DisplayPromptAsync(Properties.Resources.GetString("FeatureFormRenameAttachmentWindowTitle"), "", initialValue: Attachment.Name);
                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            RenameAttachment(result.Trim());
                        }
                    }
                    catch { }
                }
            };
            FlyoutBase.SetContextFlyout(this, flyout);
        }

        /// <inheritdoc />
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            if (Attachment != null && (Attachment.LoadStatus == LoadStatus.Loaded && Attachment.Type == FormAttachmentType.Image ||
               (GetTemplateChild("ThumbnailImage") as Image)?.Source is null))
            {
                UpdateThumbnail();
            }
        }

        private void TapGestureRecognizer_Tapped(object? sender, TappedEventArgs e)
        {
            OnAttachmentClicked();
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateLoadedState(true);
            UpdateThumbnail();
        }

        private void UpdateLoadedState(bool useTransitions)
        {
            VisualElement? sizeLabel = GetTemplateChild("FileSizeText") as VisualElement;
            Label? nameLabel = GetTemplateChild("AttachmentName") as Label;
            VisualElement? downloadIcon = GetTemplateChild("DownloadIcon") as VisualElement;
            VisualElement? nameBackground = GetTemplateChild("NameBackground") as VisualElement;
            ActivityIndicator? downloadIndicator = GetTemplateChild("DownloadIndicator") as ActivityIndicator;
            

            if (Attachment?.LoadStatus == LoadStatus.Loading)
            {
                if(downloadIcon != null) downloadIcon.IsVisible = false;
                if (downloadIndicator != null) downloadIndicator.IsRunning = true;
            }
            else if (Attachment?.LoadStatus == LoadStatus.Loaded)
            {
                UpdateThumbnail();
                if (sizeLabel != null) sizeLabel.IsVisible = false;
                if (downloadIcon != null) downloadIcon.IsVisible = false;
                if (nameLabel != null) nameLabel.TextColor = Colors.White;
                if (nameBackground != null) nameBackground.BackgroundColor = Color.FromRgba(0, 0, 0, 0x30);
                if (downloadIndicator != null) downloadIndicator.IsRunning = false;
            }
            else if (Attachment?.LoadStatus == LoadStatus.FailedToLoad)
            {
                UpdateThumbnail();
                if (downloadIcon != null) downloadIcon.IsVisible = true;
                if (downloadIndicator != null) downloadIndicator.IsRunning = false;
            }
            else
            {
                if (sizeLabel != null) sizeLabel.IsVisible = true;
                if (downloadIcon != null) downloadIcon.IsVisible = true;
                if (nameLabel != null) nameLabel.TextColor = null;
                if (nameBackground != null) nameBackground.BackgroundColor = Colors.Transparent;
                if (downloadIndicator != null) downloadIndicator.IsRunning = false;
            }
        }

        private Page? GetPage()
        {
            var parent = this.Parent;
            while (parent is not null && parent is not Page page)
            {
                parent = parent.Parent;
            }
            return parent as Page;
        }

        private Size _thumbnailSize = default; // The size of the generated thumbnail - prevents regenerating the same thumbnail multiple times

        private async void UpdateThumbnail()
        {
            var image = GetTemplateChild("ThumbnailImage") as Image;
            if (image is null || this.Width <= 0 || this.Width <= 0) return;
            if (Attachment is null)
            {
                image.Source = null;
                return;
            }
            else if (Attachment.LoadStatus == LoadStatus.Loaded && Attachment.Type == FormAttachmentType.Image) // Attachment.LoadStatus == LoadStatus.Loaded)
            {
                try
                {
                    if (_thumbnailSize.Width == this.Width || _thumbnailSize.Height == this.Height)
                        return;
                    _thumbnailSize = new Size(this.Width, this.Height);
                    var thumb = await Attachment.CreateThumbnailAsync((int)(this.Width), (int)(this.Height));
                    image.Source = await thumb.ToImageSourceAsync();
                    image.Aspect = Aspect.AspectFill;
                    return;
                }
                catch { } // Fallback to default icon
                finally
                {
                    _thumbnailSize = default;
                }
            }
            // Fallback to file icon
            string glyph = Attachment.Type switch
            {
                FormAttachmentType.Image => ToolkitIcons.Image,
                FormAttachmentType.Audio => ToolkitIcons.FileSound,
                FormAttachmentType.Document => ToolkitIcons.Article,
                FormAttachmentType.Video => ToolkitIcons.VideoWeb,
                _ => ToolkitIcons.File
            };
            if (Attachment.LoadStatus == LoadStatus.FailedToLoad)
                glyph = ToolkitIcons.ExclamationMarkCircle;
            image.Source = new FontImageSource
            {
                Glyph = glyph,
                FontFamily = ToolkitIcons.FontFamilyName,
                Size = 32,
                Color = (Attachment.LoadStatus == LoadStatus.FailedToLoad) ? Colors.Red : IconColor
            };
            image.Aspect = Aspect.Center;
        }

        private async Task<bool> DisplayAttachmentActionSheet()
        {
            if (Attachment is null) return false;
            var page = GetPage();
            if (page is null) return false;

            string delete = Properties.Resources.GetString("FeatureFormRemoveAttachmentMenuItem")!;
            string rename = Properties.Resources.GetString("FeatureFormRenameAttachmentMenuItem")!;
            string open = Properties.Resources.GetString("FeatureFormOpenAttachmentMenuItem")!;
            var result = await page.DisplayActionSheet(Attachment.Name, null, null, delete, rename, open);
            if (result == delete)
            {
                DeleteAttachment();
            }
            else if(result == rename)
            {
                try
                {
                    string newName = await page.DisplayPromptAsync(Properties.Resources.GetString("FeatureFormRenameAttachmentWindowTitle"), "", initialValue: Attachment.Name);
                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        RenameAttachment(newName.Trim());
                    }
                }
                catch { }
            }
            else if (result == open)
            {
                return false; // Falls back to default action
            }

            return true;
        }

        /// <summary>
        /// Gets or sets the icon color.
        /// </summary>
        public Color? IconColor
        {
            get { return (Color)GetValue(IconColorProperty); }
            set { SetValue(IconColorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IconColor"/> dependency property.
        /// </summary>
        public static readonly BindableProperty IconColorProperty = BindableProperty.Create(nameof(IconColor), typeof(Color), typeof(FormAttachmentView), Colors.Black, 
            propertyChanged: (s, oldValue, newValue) => ((FormAttachmentView)s).OnColorPropertyChanged());

        private void OnColorPropertyChanged()
        {
            if (Attachment is not null && (Attachment.LoadStatus != LoadStatus.Loaded || Attachment.Type != FormAttachmentType.Image))
                UpdateThumbnail();
        }
    }
}
#endif