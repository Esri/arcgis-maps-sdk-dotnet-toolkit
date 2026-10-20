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

#if MAUI
using Esri.ArcGISRuntime.Mapping.Popups;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.PopupViewer"/> control,
    /// used for rendering a <see cref="Esri.ArcGISRuntime.Mapping.Popups.PopupMedia"/>.
    /// </summary>
    public partial class PopupMediaView : ContentView
    {
#if __IOS__
        private void UpdateImageContent(Image image)
        {
            image.GestureRecognizers.Clear();
            if (Uri.TryCreate(PopupMedia?.Value?.LinkUrl, UriKind.Absolute, out var uri))
            {
                var link = Content as Internal.HyperlinkContentView;
                if (link is null || link.Uri != uri)
                {
                    // Detach the image before moving it from a previous link wrapper.
                    if (link is not null)
                        link.Content = null;
                    Content = null;
                    link = new Internal.HyperlinkContentView(image, uri,
                        (_, target) => _ = Toolkit.Internal.Launcher.LaunchUriAsync(target));
                }
                SemanticProperties.SetDescription(link, GetAltText() ?? uri.AbsoluteUri);
                Content = link;
            }
            else
            {
                if (Content is Internal.HyperlinkContentView link)
                    link.Content = null;
                SemanticProperties.SetDescription(image, GetAltText());
                Content = image;
            }
        }
#endif
#if WINDOWS
        private Image? _image;
        private Microsoft.UI.Xaml.UIElement? _imagePlatformView;

        /// <inheritdoc />
        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == nameof(Content) && _image != Content)
                DisconnectImage();
        }

        /// <inheritdoc />
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            UpdateChartTabStop();
        }

        private void UpdateChartTabStop()
        {
            if (PopupMedia?.Type != PopupMediaType.Image && Content is Image image)
                UpdateImageAccessibility(image);

            if (Handler?.PlatformView is Microsoft.UI.Xaml.UIElement platformView)
            {
                bool isChart = PopupMedia is not null && PopupMedia.Type != PopupMediaType.Image;
                platformView.IsTabStop = isChart;
                platformView.UseSystemFocusVisuals = isChart;
            }
        }

        private void Image_HandlerChanged(object? sender, EventArgs e)
        {
            if (sender is Image image)
                UpdateImageAccessibility(image);
        }

        private void Image_HandlerChanging(object? sender, HandlerChangingEventArgs e) => SetImageKeyboardTarget(null);

        private void UpdateImageAccessibility(Image image)
        {
            if (_image != image)
            {
                DisconnectImage();
                _image = image;
                image.HandlerChanging += Image_HandlerChanging;
                image.HandlerChanged += Image_HandlerChanged;
            }

            bool hasLink = TryGetImageLink(out _);
            SemanticProperties.SetDescription(image, PopupMedia?.Type == PopupMediaType.Image ? GetAltText() : null);
            if (!hasLink)
                image.GestureRecognizers.Clear();

            var platformView = image.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
            if (platformView is not null)
            {
                platformView.IsTabStop = hasLink;
                platformView.UseSystemFocusVisuals = hasLink;
            }
            SetImageKeyboardTarget(hasLink && IsLoaded ? platformView : null);
        }

        private void DisconnectImage()
        {
            SetImageKeyboardTarget(null);
            if (_image is not null)
            {
                _image.HandlerChanging -= Image_HandlerChanging;
                _image.HandlerChanged -= Image_HandlerChanged;
                _image = null;
            }
        }

        private void SetImageKeyboardTarget(Microsoft.UI.Xaml.UIElement? platformView)
        {
            if (_imagePlatformView == platformView)
                return;

            if (_imagePlatformView is not null)
            {
                _imagePlatformView.KeyDown -= Image_KeyDown;
                _imagePlatformView.KeyUp -= Image_KeyUp;
            }
            _imagePlatformView = platformView;
            if (platformView is not null)
            {
                platformView.KeyDown += Image_KeyDown;
                platformView.KeyUp += Image_KeyUp;
            }
        }

        private bool TryGetImageLink(out Uri? link)
        {
            link = null;
            return PopupMedia?.Type == PopupMediaType.Image && Uri.TryCreate(PopupMedia.Value?.LinkUrl, UriKind.Absolute, out link);
        }

        private bool OpenImageLink()
        {
            if (_image is not null && Content == _image && TryGetImageLink(out var link))
            {
                _ = Esri.ArcGISRuntime.Toolkit.Internal.Launcher.LaunchUriAsync(link);
                return true;
            }
            return false;
        }

        private void Image_Tapped(object? sender, TappedEventArgs e) => OpenImageLink();

        private void Image_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            // Consume Space before the popup's ScrollView can scroll; activate only once on key release.
            if (e.Key is Windows.System.VirtualKey.Enter or Windows.System.VirtualKey.Space && TryGetImageLink(out _))
                e.Handled = true;
        }

        private void Image_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key is Windows.System.VirtualKey.Enter or Windows.System.VirtualKey.Space && OpenImageLink())
                e.Handled = true;
        }
#endif

        /// <inheritdoc />
        protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
        {
            if (PopupMedia != null && PopupMedia.Type != PopupMediaType.Image)
            {
                UpdateChart(new Size(widthConstraint, heightConstraint));
            }
            return base.MeasureOverride(widthConstraint, heightConstraint);
        }

        private class RuntimeStreamImageSource : StreamImageSource
        {
            public RuntimeStreamImageSource(Uri source)
            {
                Source = source;
                Stream = (token) => new ArcGISRuntime.UI.RuntimeImage(source).GetEncodedBufferAsync(token);
            }

            public Uri Source { get; }
        }
    }
}
#endif