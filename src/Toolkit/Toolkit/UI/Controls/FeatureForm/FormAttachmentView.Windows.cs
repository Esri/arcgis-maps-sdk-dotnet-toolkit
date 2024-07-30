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
using Esri.ArcGISRuntime.UI;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
#if WPF
using System.Windows.Controls.Primitives;
using System.Windows.Input;
#elif WINDOWS_XAML
using Windows.Foundation;
#endif
#if WINUI
using Microsoft.UI;
#elif WINDOWS_UWP
using Windows.UI;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class FormAttachmentView : ButtonBase
    {
        /// <inheritdoc />
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            UpdateLoadedState(true);
            UpdateThumbnail();
#if WINDOWS_XAML
            this.SizeChanged += OnSizeChanged;
#endif
        }

        private void UpdateLoadedState(bool useTransitions)
        {
            if (Attachment?.LoadStatus == LoadStatus.Loading)
                VisualStateManager.GoToState(this, "Loading", useTransitions);
            else if (Attachment?.LoadStatus == LoadStatus.Loaded)
            {
                UpdateThumbnail();
                VisualStateManager.GoToState(this, "Loaded", useTransitions);
            }
            else
                VisualStateManager.GoToState(this, "NotLoaded", useTransitions);
        }

#if WINDOWS_XAML
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
#else
        /// <inheritdoc/>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
#endif
            if (Attachment != null && (Attachment.LoadStatus == LoadStatus.Loaded && Attachment.Type == FormAttachmentType.Image || 
                (GetTemplateChild("ThumbnailImage") as Image)?.Source is null))
            {
                UpdateThumbnail();
            }
        }

        private void OnAttachmentContextMenu()
        {
#if WPF
            ContextMenu? contextMenu = new ContextMenu();
            contextMenu.Items.Add(new MenuItem()
            {
                Header = Properties.Resources.GetString("FeatureFormRemoveAttachmentMenuItem"),
                Icon = new TextBlock() { Text = "\uE74D", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontFamily = new FontFamily("Segoe MDL2 Assets") }
            });
            contextMenu.Items.Add(new MenuItem()
            {
                Header = Properties.Resources.GetString("FeatureFormRenameAttachmentMenuItem"),
                Icon = new TextBlock() { Text = "\uE70F", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontFamily = new FontFamily("Segoe MDL2 Assets") }
            });
            
            ((MenuItem)contextMenu.Items[0]).Click += (s, e) =>
            {
                DeleteAttachment();
            };
            
            ((MenuItem)contextMenu.Items[1]).Click += (s, e) =>
            {
                if (Attachment is not null && Element is not null)
                {
                    Window renameDialog = new Window()
                    {
                        SizeToContent = SizeToContent.Height,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = Window.GetWindow(this),
                        WindowStyle = WindowStyle.ToolWindow, Width = 250,
                        Title = Properties.Resources.GetString("FeatureFormRenameAttachmentWindowTitle")
                    };
                    StackPanel panel = new StackPanel() { Margin = new Thickness(10) };
                    TextBox textBox = new TextBox() { Text = Attachment.Name };
                    panel.Children.Add(textBox);

                    StackPanel panel2 = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
                    Button okButton = new Button() { Content = Properties.Resources.GetString("FeatureFormRenameAttachmentDialogOK"), MinWidth = 75, IsDefault = true, Margin = new Thickness(0, 0, 10, 0), IsEnabled = false };
                    okButton.Click += (s,e) => renameDialog.DialogResult = true;
                    Button cancelButton = new Button() { Content = Properties.Resources.GetString("FeatureFormRenameAttachmentDialogCancel"), MinWidth = 75, IsCancel = true };
                    panel2.Children.Add(okButton);
                    panel2.Children.Add(cancelButton);
                    panel.Children.Add(panel2);
                    renameDialog.Content = panel;

                    textBox.TextChanged += (s, e) =>
                    {
                        okButton.IsEnabled = !string.IsNullOrEmpty(textBox.Text.Trim()) && textBox.Text.Trim() != Attachment.Name;
                    };
                    bool? ok = renameDialog.ShowDialog();

                    if(ok.HasValue && ok.Value == true && !string.IsNullOrEmpty(textBox.Text.Trim()))
                    {
                        RenameAttachment(textBox.Text.Trim());
                    }
                }
            };
            contextMenu.PlacementTarget = this;
            contextMenu.IsOpen = true;
#elif WINDOWS_XAML
            MenuFlyout contextMenu = new();
            var delete = new MenuFlyoutItem()
            {
                Text = Properties.Resources.GetString("FeatureFormRemoveAttachmentMenuItem"),
                Icon = new SymbolIcon(Symbol.Delete),
            };
            delete.Click += (s, e) => DeleteAttachment();
            contextMenu.Items.Add(delete);
            var rename = new MenuFlyoutItem()
            {
                Text = Properties.Resources.GetString("FeatureFormRenameAttachmentMenuItem"),
                Icon = new SymbolIcon(Symbol.Rename),
            };
            contextMenu.Items.Add(rename);
            rename.Click += (s,e) => {
                if (Attachment is not null && Element is not null)
                {
                    var dialog = new ContentDialog()
                    {
                        Title = Properties.Resources.GetString("FeatureFormRenameAttachmentWindowTitle"),
                        PrimaryButtonText = Properties.Resources.GetString("FeatureFormRenameAttachmentDialogOK"),
                        SecondaryButtonText = Properties.Resources.GetString("FeatureFormRenameAttachmentDialogCancel"),
                        DefaultButton = ContentDialogButton.Primary,
                        Content = new TextBox() { Text = Attachment.Name },
#if WINDOWS_XAML
                        XamlRoot = this.XamlRoot
#endif
                    };
                    var textBox = (TextBox)dialog.Content;
                    textBox.TextChanged += (s, e) =>
                    {
                        dialog.IsPrimaryButtonEnabled = !string.IsNullOrEmpty(textBox.Text.Trim()) && textBox.Text.Trim() != Attachment.Name;
                    };
                    dialog.PrimaryButtonClick += (s, e) =>
                    {
                        RenameAttachment(textBox.Text.Trim());
                    };
                    _ = dialog.ShowAsync();
                }
            };

            contextMenu.ShowAt(this);
#endif
        }

#if WPF
        /// <inheritdoc />
        protected override void OnStylusSystemGesture(StylusSystemGestureEventArgs e)
        {
            base.OnStylusSystemGesture(e);
            if (e.SystemGesture == SystemGesture.HoldEnter && Element is not null && Element.IsEditable)
            {
                OnAttachmentContextMenu();
            }
        }
#endif

        /// <inheritdoc />
#if WINDOWS_XAML
        protected override void OnRightTapped(RightTappedRoutedEventArgs e)
        {
            base.OnRightTapped(e);
#elif WPF
        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);
#endif
            if (Element is not null && Element.IsEditable)
            {
                e.Handled = true;
                OnAttachmentContextMenu();
            }
        }

#if WINDOWS_XAML
        private void OnClick(object sender, RoutedEventArgs e)
        {
#elif WPF
        /// <inheritdoc />
        protected override void OnClick()
        {
            base.OnClick();
#endif
            OnAttachmentClicked();
        }

        private Size _thumbnailSize = default; // The size of the generated thumbnail - prevents regenerating the same thumbnail multiple times

        private async void UpdateThumbnail()
        {
            var image = GetTemplateChild("ThumbnailImage") as Image;
            if (image is null || this.ActualWidth == 0 || this.ActualHeight == 0) return;
            if (Attachment is null)
            {
                image.Source = null;
                return;
            }
            else if (Attachment.LoadStatus == LoadStatus.Loaded && Attachment.Type == FormAttachmentType.Image) // Attachment.LoadStatus == LoadStatus.Loaded)
            {
                try
                {
                    if (_thumbnailSize.Width == this.ActualWidth || _thumbnailSize.Height == this.ActualHeight)
                        return;
#if WINUI
                    var scale = XamlRoot?.RasterizationScale ?? 1;
#elif WINDOWS_UWP
                    var scale = Windows.Graphics.Display.DisplayInformation.GetForCurrentView()?.RawPixelsPerViewPixel ?? 1;
#elif WPF
                    var scale = VisualTreeHelper.GetDpi(this).PixelsPerDip;
#endif
                    _thumbnailSize = new Size(this.ActualWidth, this.ActualHeight);
                    var thumb = await Attachment.CreateThumbnailAsync((int)(this.ActualWidth * scale), (int)(this.ActualHeight * scale));
                    image.Source = await thumb.ToImageSourceAsync();
                    image.Stretch = Stretch.UniformToFill;
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
                FormAttachmentType.Image => "\uEB9F",
                FormAttachmentType.Audio => "\uEC4F",
                FormAttachmentType.Document => "\uF584",
                FormAttachmentType.Video => "\uE714",
                _ => "\uE160"
            };
            if (Attachment.LoadStatus == LoadStatus.FailedToLoad)
                glyph = "\uE783";
            var tb = new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Foreground = (Attachment.LoadStatus == LoadStatus.FailedToLoad) ? new SolidColorBrush(Colors.Red) : Foreground,
                FontSize = 32, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
#if WPF
            var geometryDrawing = new GeometryDrawing
            {
                Brush = new VisualBrush
                {
                    Visual = tb,
                    Stretch = Stretch.None
                },
                Geometry = new RectangleGeometry(new Rect(0, 0, 40, 40))
            };
            image.Source = new DrawingImage(geometryDrawing);
            image.Stretch = Stretch.None;
#endif
            
        }
    }
}
#endif
