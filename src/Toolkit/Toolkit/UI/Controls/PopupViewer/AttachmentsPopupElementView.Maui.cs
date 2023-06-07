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
using Esri.ArcGISRuntime.Mapping.Popups;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.InteropServices;
#if WINDOWS
using WinRT;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.PopupViewer"/> control,
    /// used for rendering a <see cref="AttachmentsPopupElement"/>.
    /// </summary>
    public partial class AttachmentsPopupElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        /// <summary>
        /// Template name of the <see cref="CollectionView"/> attachment list.
        /// </summary>
        public const string AttachmentListName = "AttachmentList";

        static AttachmentsPopupElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            StackLayout root = new StackLayout();
            Label roottitle = new Label();
            roottitle.SetBinding(Label.TextProperty, new Binding("Element.Title", source: RelativeBindingSource.TemplatedParent));
            roottitle.SetBinding(VisualElement.IsVisibleProperty, new Binding("Element.Title", source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance));
            roottitle.Style = PopupViewer.GetPopupViewerTitleStyle();
            root.Add(roottitle);
            Label rootcaption = new Label();
            rootcaption.SetBinding(Label.TextProperty, new Binding("Element.Description", source: RelativeBindingSource.TemplatedParent));
            rootcaption.SetBinding(VisualElement.IsVisibleProperty, new Binding("Element.Description", source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance));
            rootcaption.Style = PopupViewer.GetPopupViewerCaptionStyle();
            root.Add(rootcaption);
            root.Add(new Border() { StrokeThickness = 0, HeightRequest = 1, BackgroundColor = Colors.Gray, Margin = new Thickness(0,5) });
            CollectionView cv = new CollectionView() { SelectionMode = SelectionMode.Single };
            cv.SetBinding(CollectionView.ItemsSourceProperty, new Binding("Element.Attachments", source: RelativeBindingSource.TemplatedParent));
            cv.ItemTemplate = new DataTemplate(BuildDefaultItemTemplate);
            root.Add(cv);
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(AttachmentListName, cv);
            return root;
        }

        private static object BuildDefaultItemTemplate()
        {
            Grid layout = new Grid();
            layout.ColumnDefinitions.Add(new ColumnDefinition(30));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.SetBinding(Grid.BindingContextProperty, Binding.SelfPath, converter: AttachmentViewModelConverter.Instance);
            Image image = new Image() { WidthRequest = 24, HeightRequest = 24, HorizontalOptions = LayoutOptions.Start, Aspect = Aspect.Fill };
            layout.Add(image);
            image.SetBinding(Image.SourceProperty, new Binding(nameof(AttachmentViewModel.Thumbnail)));
            Grid.SetRowSpan(image, 2);
            
            Label name = new Label() { VerticalOptions = LayoutOptions.End };
            name.SetBinding(Label.TextProperty, nameof(AttachmentViewModel.Name));
            Grid.SetColumn(name, 1);
            layout.Add(name);

            Label size = new Label() { VerticalOptions = LayoutOptions.Start, TextColor = Colors.Gray, FontSize = 12 };
            size.SetBinding(Label.TextProperty, nameof(AttachmentViewModel.Size));
            Grid.SetColumn(size, 1);
            Grid.SetRow(size, 1);
            layout.Add(size);

            Image image2 = new Image() { WidthRequest = 18, HeightRequest = 18 };
            image2.SetBinding(Image.IsVisibleProperty, new Binding(nameof(AttachmentViewModel.IsDownloadButtonVisible)));
            image2.Source = new FontImageSource() { Glyph = ((char)0xE16C).ToString(), Color = Colors.Gray, FontFamily = "calcite-ui-icons-24", Size = 18 };
            TapGestureRecognizer tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += TapGesture_Tapped;
            image2.GestureRecognizers.Add(tapGesture);
            Grid.SetColumn(image2, 2);
            Grid.SetRowSpan(image2, 2);
            layout.Add(image2);
            
            ActivityIndicator indicator = new ActivityIndicator() { WidthRequest = 24, HeightRequest = 24, IsRunning = true };
            //indicator.SetBinding(ActivityIndicator.IsVisibleProperty, new Binding(nameof(AttachmentViewModel.IsDownloading)));
            indicator.SetBinding(ActivityIndicator.IsRunningProperty, new Binding(nameof(AttachmentViewModel.IsDownloading)));
            Grid.SetColumn(indicator, 2);
            Grid.SetRowSpan(indicator, 2);
            layout.Add(indicator);
            var divider = new Border() { StrokeThickness = 0, HeightRequest = 1, BackgroundColor = Colors.Gray, Margin = new Thickness(0,5) };
            divider.VerticalOptions = LayoutOptions.End;
            Grid.SetRow(divider, 2);
            Grid.SetColumnSpan(divider, 3);
            layout.Add(divider);
            Border root = new Border() { StrokeThickness = 0, Content = layout };
            return root;
        }

        private static void TapGesture_Tapped(object? sender, EventArgs e)
        {
            var img = sender as Image;
            if(img?.BindingContext is AttachmentViewModel vm)
            {
                vm.Load();
            }
        }

        /// <summary>
        /// Occurs when an attachment is clicked.
        /// </summary>
        /// <remarks>Override this to prevent the default "save to file dialog" action.</remarks>
        /// <param name="attachment">Attachment clicked.</param>
        public virtual async void OnAttachmentClicked(PopupAttachment attachment)
        {
            if (attachment.LoadStatus == LoadStatus.Loaded && attachment.Attachment != null)
            {
#if WINDOWS
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                var initializeWithWindow = savePicker.As<IInitializeWithWindow>();
                var hwnd = this.Window.Handler.PlatformView.As<IWindowNative>().WindowHandle;
                initializeWithWindow.Initialize(hwnd);
                savePicker.SuggestedFileName = attachment.Name;
                var storage = await savePicker.PickSaveFileAsync();
                using (var stream = await storage.OpenStreamForWriteAsync())
                {
                    using var dataStream = await attachment.Attachment.GetDataAsync();
                    await dataStream.CopyToAsync(stream);
                }
#elif IOS
                // TODO: Use QuickLook Previewer
                //QuickLook.QLPreviewController controller = new QuickLook.QLPreviewController();
                //controller.DataSource = new QuickLookSource(source.Documents);
                //new QuickLook.QLOpenUrl(controller, new Foundation.NSUrl(attachment.Filename!));
                //var view = this.Handler.PlatformView as View;
                //view.Quick
#elif ANDROID
                // TODO
#endif
            }
        }

#if WINDOWS
        [ComImport]
        [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IInitializeWithWindow
        {
            void Initialize(IntPtr hwnd);
        }
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }
#endif

        private class AttachmentViewModel : System.ComponentModel.INotifyPropertyChanged
        {
            private readonly PopupAttachment _attachment;
            private string? _filename;

            public AttachmentViewModel(PopupAttachment attachment)
            {
                _attachment = attachment;
                _attachment.PropertyChanged += Attachment_PropertyChanged;
            }

            public string Name => _attachment.Name;

            public string Size
            {
                get
                {
                    var size = _attachment.Size;
                    if (size < 1024)
                        return $"{size} B";
                    else if (size < 1024 * 1024)
                        return $"{size / 1024} kB";
                    else
                        return $"{Math.Round(size / 1024d / 1024d, 1)} MB";
                }
            } 

            private void Attachment_PropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(PopupAttachment.LoadStatus))
                {
                    OnPropertyChanged(nameof(IsDownloadButtonVisible));
                    OnPropertyChanged(nameof(IsDownloading));
                    if (_attachment.LoadStatus == LoadStatus.Loaded)
                    {
                        CreateThumbnail();
                    }
                }
            }

            public void Load()
            {
                _ = _attachment?.LoadAsync();
            }

            private ImageSource? _thumbnail;

            public ImageSource? Thumbnail
            {
                get
                {
                    if (_thumbnail is null)
                        CreateThumbnail();
                    return _thumbnail;
                }
            }
            
            public bool IsDownloadButtonVisible => !_attachment.IsLocal && _attachment.LoadStatus != LoadStatus.Loaded && _attachment.LoadStatus != LoadStatus.Loading;
            
            public bool IsDownloading => !_attachment.IsLocal && _attachment.LoadStatus == LoadStatus.Loading;

            private void CreateThumbnail()
            {
                if (_attachment.IsLocal || _attachment.LoadStatus == LoadStatus.Loaded)
                {
                    _thumbnail = ImageSource.FromStream(async (token) =>
                    {
                        var img = await _attachment.CreateThumbnailAsync(40, 40);
                        return await img!.GetEncodedBufferAsync();
                    });
                }
                else
                {
                    _thumbnail = new FontImageSource() { Glyph = ContentTypeToCalciteGlyph(_attachment.ContentType).ToString(), Color = Colors.Gray, FontFamily = "calcite-ui-icons-24", Size = 18 };
                }
                OnPropertyChanged(nameof(Thumbnail));
            }

            private static char ContentTypeToCalciteGlyph(string contentType)
            {
                contentType = contentType.ToLowerInvariant();
                if (contentType.StartsWith("image/"))
                    return(char)0xE169;
                if (contentType.StartsWith("video/"))
                    return (char)0xE10B;
                if (contentType.StartsWith("audio/"))
                    return (char)0xE109;
                switch (contentType)
                {
                    case "application/pdf": return (char)0xE105;
                    case "text/csv":
                    case "application/vnd.ms-excel": return (char)0xE0FD;
                    case "application/msword":
                    case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":
                        return (char)0xE10C;
                    default: return (char)0xE10E;
                }
            }

            private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            public event PropertyChangedEventHandler? PropertyChanged;
        }

        private class AttachmentViewModelConverter : IValueConverter
        {
            internal static AttachmentViewModelConverter Instance { get; } = new AttachmentViewModelConverter();
            public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is PopupAttachment attachment)
                    return new AttachmentViewModel(attachment);
                return value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
        }
    }
}
#endif