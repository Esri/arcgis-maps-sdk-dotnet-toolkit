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


#if WPF
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class FormAttachmentView : ButtonBase
    {
        private ButtonBase? _addAttachmentButton;

        private UI.Controls.FeatureFormView? GetFeatureFormViewParent()
        {
            var parent = VisualTreeHelper.GetParent(this);
            while (parent is not null && parent is not UI.Controls.FeatureFormView popup)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as UI.Controls.FeatureFormView;
        }

        /// <inheritdoc />
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            //if (_addAttachmentButton is not null)
            //{
            //    _addAttachmentButton.Click -= AddAttachmentButton_Click;
            //}
            //_addAttachmentButton = GetTemplateChild("AddAttachmentButton") as ButtonBase;
            //if(_addAttachmentButton is not null)
            //{
            //    _addAttachmentButton.Click += AddAttachmentButton_Click;
            //}
            UpdateLoadedState(true);
            UpdateThumbnail();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (Attachment != null && (Attachment.LoadStatus == LoadStatus.Loaded && Attachment.Type == FormAttachmentType.Image || 
                (GetTemplateChild("ThumbnailImage") as Image)?.Source is null))
            {
                UpdateThumbnail();
            }
        }

        private void OnAttachmentContextMenu()
        {

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
                if (Attachment is not null && Element is not null)
                {
                    Element.DeleteAttachment(Attachment);
                }
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
                    Button okButton = new Button() { Content = "OK", MinWidth = 75, IsDefault = true, Margin = new Thickness(0, 0, 10, 0) };
                    okButton.Click += (s,e) => renameDialog.DialogResult = true;
                    Button cancelButton = new Button() { Content = "Cancel", MinWidth = 75, IsCancel = true };
                    panel2.Children.Add(okButton);
                    panel2.Children.Add(cancelButton);
                    panel.Children.Add(panel2);
                    renameDialog.Content = panel;

                    textBox.TextChanged += (s, e) =>
                    {
                        okButton.IsEnabled = !string.IsNullOrEmpty(textBox.Text.Trim());
                    };
                    bool? ok = renameDialog.ShowDialog();

                    if(ok.HasValue && ok.Value == true && !string.IsNullOrEmpty(textBox.Text.Trim()))
                    {
                        Attachment.Name = textBox.Text.Trim();
                    }
                }
            };
            contextMenu.PlacementTarget = this;
            contextMenu.IsOpen = true;
        }

        /// <inheritdoc />
        protected override void OnStylusSystemGesture(StylusSystemGestureEventArgs e)
        {
            base.OnStylusSystemGesture(e);
            if (e.SystemGesture == SystemGesture.HoldEnter)
            {
                OnAttachmentContextMenu();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);
            e.Handled = true;
            OnAttachmentContextMenu();
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            base.OnClick();
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
                    _thumbnailSize = new Size(this.ActualWidth, this.ActualHeight);
                    var source = PresentationSource.FromVisual(this);
                    var thumb = await Attachment.CreateThumbnailAsync((int)(this.ActualWidth * source.CompositionTarget.TransformToDevice.M11), (int)(this.ActualHeight * source.CompositionTarget.TransformToDevice.M22));
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
        }
    }
}
#endif