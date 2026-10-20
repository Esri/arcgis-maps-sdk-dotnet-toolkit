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
using Esri.ArcGISRuntime.Toolkit.Internal;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal sealed class HyperlinkLabelHandler : ViewHandler<HyperlinkLabel, UITextView>, IDisposable
{
    private static readonly IPropertyMapper<HyperlinkLabel, HyperlinkLabelHandler> PropertyMapper =
        new PropertyMapper<HyperlinkLabel, HyperlinkLabelHandler>(ViewMapper)
        {
            [nameof(ILabel.Text)] = MapText,
            [nameof(ILabel.Font)] = MapText,
            [nameof(Label.TextColor)] = MapText,
            [nameof(Label.TextTransform)] = MapText,
            [nameof(Label.LineHeight)] = MapText,
            [nameof(Label.CharacterSpacing)] = MapText,
            [nameof(Label.TextDecorations)] = MapText,
            [nameof(Label.HorizontalTextAlignment)] = MapText,
        };

    private readonly List<(NSRange Range, Action<Element> Activate)> _links = new();
    private LinkDelegate? _delegate;

    public HyperlinkLabelHandler() : base(PropertyMapper)
    {
    }

    protected override UITextView CreatePlatformView()
    {
        using var linkAttributes = new NSDictionary();
        var textView = new HyperlinkTextView
        {
            Editable = false,
            Selectable = true,
            ScrollEnabled = false,
            // Rendered links should not form a separate text-input region in the Tab loop.
            FocusGroupIdentifier = null,
            BackgroundColor = UIColor.Clear,
            TextContainerInset = UIEdgeInsets.Zero,
            WeakLinkTextAttributes = linkAttributes,
        };
        textView.TextContainer.LineFragmentPadding = 0;
        return textView;
    }

    protected override void ConnectHandler(UITextView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.Delegate = _delegate = new LinkDelegate(this);
    }

    protected override void DisconnectHandler(UITextView platformView)
    {
        platformView.Delegate = null;
        Dispose();
        base.DisconnectHandler(platformView);
    }

    public void Dispose()
    {
        if (PlatformView is not null)
            PlatformView.Delegate = null;
        _delegate?.Dispose();
        _delegate = null;
        _links.Clear();
        GC.SuppressFinalize(this);
    }

    private static void MapText(HyperlinkLabelHandler handler, HyperlinkLabel view) => handler.UpdateText();

    private void UpdateText()
    {
        if (MauiContext is null)
            return;

        var label = VirtualView;
        var fontManager = MauiContext.Services.GetRequiredService<IFontManager>();
        var font = ((ILabel)label).Font;
        if (font.IsDefault)
            font = font.WithSize(fontManager.DefaultFontSize);
        using var text = new NSMutableAttributedString();
        _links.Clear();
        foreach (var span in label.InlineContent.Spans)
        {
            using var attributedSpan = span.ToNSAttributedString(fontManager, label.LineHeight,
                label.HorizontalTextAlignment, font, label.TextColor, label.TextTransform);
            var range = new NSRange(text.Length, attributedSpan.Length);
            text.Append(attributedSpan);
            if (range.Length == 0)
                continue;
            if (span.TextColor is null && label.TextColor is null)
                text.AddAttribute(UIStringAttributeKey.ForegroundColor, UIColor.Label, range);
            if (!span.IsSet(Span.CharacterSpacingProperty))
            {
                using var spacing = NSNumber.FromDouble(label.CharacterSpacing);
                text.AddAttribute(UIStringAttributeKey.KerningAdjustment, spacing, range);
            }
            if (!span.IsSet(Span.TextDecorationsProperty))
            {
                using var underline = NSNumber.FromInt32(label.TextDecorations.HasFlag(TextDecorations.Underline) ? 1 : 0);
                using var strikethrough = NSNumber.FromInt32(label.TextDecorations.HasFlag(TextDecorations.Strikethrough) ? 1 : 0);
                text.AddAttribute(UIStringAttributeKey.UnderlineStyle, underline, range);
                text.AddAttribute(UIStringAttributeKey.StrikethroughStyle, strikethrough, range);
            }
            if (span is HtmlToView.HyperlinkSpan link)
            {
                using var url = new NSUrl(link.Uri.AbsoluteUri);
                text.AddAttribute(UIStringAttributeKey.Link, url, range);
                _links.Add((range, link.Activate));
            }
        }
        PlatformView.AttributedText = text;
        PlatformView.InvalidateIntrinsicContentSize();
    }

    private sealed class LinkDelegate : UITextViewDelegate
    {
        private readonly WeakReference<HyperlinkLabelHandler> _handler;

        internal LinkDelegate(HyperlinkLabelHandler handler) => _handler = new(handler);

        public override UIAction? GetPrimaryAction(UITextView textView, UITextItem textItem, UIAction defaultAction)
        {
            if (_handler.TryGetTarget(out var handler))
            {
                foreach (var link in handler._links)
                {
                    if (textItem.Range.Location >= link.Range.Location && textItem.Range.Location < link.Range.Location + link.Range.Length)
                        return UIAction.Create(_ => link.Activate(handler.VirtualView));
                }
            }
            return defaultAction;
        }
    }
}

internal sealed class HyperlinkTextView : UITextView
{
    // This text renderer cannot scroll; its popup owns vertical accessibility scrolling.
    public override bool AccessibilityScroll(UIAccessibilityScrollDirection direction) =>
        NativePopupScrollView.ForwardVerticalScroll(this, direction) || base.AccessibilityScroll(direction);
}
#endif
