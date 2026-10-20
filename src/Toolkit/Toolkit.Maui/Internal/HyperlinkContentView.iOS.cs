// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  * Licensed under the Apache License, Version 2.0 (the "License");
//  * you may not use this file except in compliance with the License.
//  * You may obtain a copy of the License at
//  *
//  * http://www.apache.org/licenses/LICENSE-2.0
//  *
//  * Unless required by applicable law or agreed to in writing, software
//  * distributed under the License is distributed on an "AS IS" BASIS,
//  * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  * See the License for the specific language governing permissions and
//  * limitations under the License.
//  ******************************************************************************/

#if __IOS__
using CoreGraphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;
using NativeContentView = Microsoft.Maui.Platform.ContentView;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal sealed class HyperlinkContentViewHandler : ViewHandler<HyperlinkContentView, HyperlinkContentButton>
{
    private static readonly IPropertyMapper<HyperlinkContentView, HyperlinkContentViewHandler> PropertyMapper =
        new PropertyMapper<HyperlinkContentView, HyperlinkContentViewHandler>(ViewMapper)
        {
            [nameof(HyperlinkContentView.Content)] = (handler, _) => handler.UpdateContent(),
        };
    private View? _content;

    public HyperlinkContentViewHandler() : base(PropertyMapper)
    {
    }

    protected override HyperlinkContentButton CreatePlatformView() => new();

    protected override void ConnectHandler(HyperlinkContentButton platformView)
    {
        base.ConnectHandler(platformView);
        platformView.PrimaryActionTriggered += OnClick;
    }

    protected override void DisconnectHandler(HyperlinkContentButton platformView)
    {
        platformView.PrimaryActionTriggered -= OnClick;
        ClearContent(platformView);
        platformView.ContentPanel.CrossPlatformLayout = null;
        platformView.ContentPanel.View = null;
        base.DisconnectHandler(platformView);
    }

    private void OnClick(object? sender, EventArgs e) => VirtualView.Activate();

    private void UpdateContent()
    {
        ClearContent(PlatformView);
        PlatformView.ContentPanel.CrossPlatformLayout = VirtualView;
        PlatformView.ContentPanel.View = VirtualView;
        if (VirtualView.Content is View content && MauiContext is not null)
        {
            PlatformView.ContentPanel.AddSubview(content.ToPlatform(MauiContext));
            _content = content;
            content.HandlerChanging += Content_HandlerChanging;
            content.HandlerChanged += Content_HandlerChanged;
        }
        PlatformView.ContentPanel.SetNeedsLayout();
        PlatformView.SetNeedsLayout();
        PlatformView.InvalidateIntrinsicContentSize();
    }

    private void ClearContent(HyperlinkContentButton platformView)
    {
        if (_content is not null)
        {
            _content.HandlerChanging -= Content_HandlerChanging;
            _content.HandlerChanged -= Content_HandlerChanged;
            _content = null;
        }
        platformView.ContentPanel.ClearSubviews();
    }

    private void Content_HandlerChanging(object? sender, HandlerChangingEventArgs e) => PlatformView.ContentPanel.ClearSubviews();

    private void Content_HandlerChanged(object? sender, EventArgs e)
    {
        if (_content?.Handler is not null)
            UpdateContent();
    }
}

internal sealed class HyperlinkContentButton : UIButton
{
    public override bool AccessibilityScroll(UIAccessibilityScrollDirection direction) =>
        NativePopupScrollView.ForwardVerticalScroll(this, direction) || base.AccessibilityScroll(direction);

    internal NativeContentView ContentPanel { get; } = new()
    {
        UserInteractionEnabled = false,
        AccessibilityElementsHidden = true,
    };

    internal HyperlinkContentButton()
    {
        AccessibilityTraits = UIAccessibilityTrait.Link;
        AddSubview(ContentPanel);
    }

    public override CGSize SizeThatFits(CGSize size) => ContentPanel.SizeThatFits(size);

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        ContentPanel.Frame = Bounds;
        using var effect = UIFocusHaloEffect.Create(Bounds);
        FocusEffect = effect;
    }
}
#endif
