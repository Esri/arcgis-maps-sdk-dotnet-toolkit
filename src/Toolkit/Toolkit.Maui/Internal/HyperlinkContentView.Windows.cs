// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *   http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

#if WINDOWS
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using NativeAutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using NativeHyperlinkButton = Microsoft.UI.Xaml.Controls.HyperlinkButton;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal sealed class HyperlinkContentViewHandler : ViewHandler<HyperlinkContentView, NativeHyperlinkButton>
{
    private static readonly IPropertyMapper<HyperlinkContentView, HyperlinkContentViewHandler> PropertyMapper =
        new PropertyMapper<HyperlinkContentView, HyperlinkContentViewHandler>(ViewMapper)
        {
            [nameof(ContentView.Content)] = (handler, _) => handler.UpdateContent(),
            [nameof(IView.Background)] = (handler, view) => handler.PlatformView.Background = ((IView)view).Background?.ToPlatform(),
        };
    private readonly ContentPanel _contentPanel = new() { IsTabStop = false };
    private View? _content;

    public HyperlinkContentViewHandler() : base(PropertyMapper)
    {
    }

    protected override NativeHyperlinkButton CreatePlatformView() => new()
    {
        Content = _contentPanel,
        Padding = new Microsoft.UI.Xaml.Thickness(0),
        BorderThickness = new Microsoft.UI.Xaml.Thickness(0),
        Background = null,
        MinWidth = 0,
        MinHeight = 0,
        HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
        VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch,
        HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
        VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch,
    };

    protected override void ConnectHandler(NativeHyperlinkButton platformView)
    {
        base.ConnectHandler(platformView);
        platformView.Click += OnClick;
    }

    protected override void DisconnectHandler(NativeHyperlinkButton platformView)
    {
        platformView.Click -= OnClick;
        ClearContent(platformView);
        _contentPanel.CrossPlatformLayout = null;
        platformView.Content = null;
        base.DisconnectHandler(platformView);
    }

    private void OnClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => VirtualView.Activate();

    private void UpdateContent()
    {
        ClearContent(PlatformView);
        // Keep MAUI's measure/arrange behavior for the existing block tree inside the native link.
        _contentPanel.CrossPlatformLayout = VirtualView;
        if (VirtualView.Content is View content && MauiContext is not null)
        {
            _contentPanel.Children.Add(content.ToPlatform(MauiContext));
            // The layout panel must not hide the original content's native accessible name.
            NativeAutomationProperties.SetLabeledBy(PlatformView, content.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement);
            _content = content;
            content.HandlerChanging += Content_HandlerChanging;
            content.HandlerChanged += Content_HandlerChanged;
        }
    }

    private void ClearContent(NativeHyperlinkButton platformView)
    {
        if (_content is not null)
        {
            _content.HandlerChanging -= Content_HandlerChanging;
            _content.HandlerChanged -= Content_HandlerChanged;
            _content = null;
        }
        _contentPanel.Children.Clear();
        NativeAutomationProperties.SetLabeledBy(platformView, null);
    }

    private void Content_HandlerChanging(object? sender, HandlerChangingEventArgs e)
    {
        _contentPanel.Children.Clear();
        NativeAutomationProperties.SetLabeledBy(PlatformView, null);
    }

    private void Content_HandlerChanged(object? sender, EventArgs e)
    {
        if (_content?.Handler is not null)
            UpdateContent();
    }
}
#endif
