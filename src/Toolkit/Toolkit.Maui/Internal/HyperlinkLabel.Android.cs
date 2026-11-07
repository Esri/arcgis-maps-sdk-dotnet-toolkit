// /*******************************************************************************
//  * Copyright 2026 Esri
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

#if __ANDROID__
using Android.Content;
using Android.Text;
using Android.Text.Method;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using NativeRect = Android.Graphics.Rect;
using NativeView = Android.Views.View;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal sealed class HyperlinkLabelHandler : LabelHandler
{
    private bool _settingVirtualView;

    protected override AppCompatTextView CreatePlatformView() => new HyperlinkTextView(Context);

    public override void SetVirtualView(IView view)
    {
        _settingVirtualView = true;
        try
        {
            base.SetVirtualView(view);
        }
        finally
        {
            _settingVirtualView = false;
        }
        UpdateLinks();
    }

    public override void UpdateValue(string property)
    {
        base.UpdateValue(property);
        if (!_settingVirtualView && (property is nameof(ILabel.Text) or nameof(Label.FormattedText) or nameof(ILabel.Font) or
            nameof(Label.TextColor) or nameof(Label.TextTransform) or nameof(Label.LineHeight) or
            nameof(Label.CharacterSpacing) or nameof(Label.TextDecorations)))
        {
            UpdateLinks();
        }
    }

    protected override void DisconnectHandler(AppCompatTextView platformView)
    {
        if (platformView is HyperlinkTextView textView)
            textView.SetLinks(Array.Empty<LinkSpan>());
        platformView.Text = null;
        base.DisconnectHandler(platformView);
    }

    private void UpdateLinks()
    {
        if (VirtualView is not HyperlinkLabel label || PlatformView is not HyperlinkTextView platformView ||
            platformView.TextFormatted is not { } formattedText)
            return;

        var text = new SpannableString(formattedText);
        foreach (var span in text.GetSpans(0, text.Length(), Java.Lang.Class.FromType(typeof(ClickableSpan))))
            text.RemoveSpan(span);

        var links = new List<LinkSpan>();
        Action<Element>? previousAction = null;
        int offset = 0;
        foreach (var span in label.InlineContent.Spans)
        {
            var transform = span.TextTransform == TextTransform.Default ? label.TextTransform : span.TextTransform;
            int end = offset + TextTransformUtilities.GetTransformedText(span.Text ?? string.Empty, transform).Length;
            var activate = (span as HtmlToView.HyperlinkSpan)?.Activate;
            if (activate is not null && end > offset)
            {
                if (ReferenceEquals(activate, previousAction) && links.Count > 0 && links[^1].End == offset)
                    links[^1].End = end;
                else
                    links.Add(new LinkSpan(() => activate(label)) { Start = offset, End = end });
            }
            previousAction = activate;
            offset = end;
        }

        foreach (var link in links)
            text.SetSpan(link, link.Start, link.End, SpanTypes.ExclusiveExclusive);

        platformView.SetText(text, TextView.BufferType.Spannable);
        platformView.SetLinks(links);
    }

    private sealed class LinkSpan : ClickableSpan
    {
        private readonly Action _activate;

        internal LinkSpan(Action activate) => _activate = activate;

        internal int Start { get; init; }

        internal int End { get; set; }

        internal void Activate() => _activate();

        public override void OnClick(NativeView widget)
        {
            if (widget.Enabled)
                _activate();
        }

        public override void UpdateDrawState(TextPaint ds)
        {
            // Keep the colors and decorations supplied by the popup's formatted spans.
        }
    }

    private sealed class HyperlinkTextView : MauiTextView
    {
        private IReadOnlyList<LinkSpan> _links = Array.Empty<LinkSpan>();
        private int _selectedLink = -1;
        private int _pendingActivation = -1;

        internal HyperlinkTextView(Context context) : base(context)
        {
        }

        internal void SetLinks(IReadOnlyList<LinkSpan> links)
        {
            _links = links;
            _pendingActivation = -1;
            MovementMethod = LinkMovementMethod.Instance;
            LinksClickable = links.Count > 0;
            Focusable = links.Count > 0;
            DefaultFocusHighlightEnabled = true;
            if (HasFocus && links.Count > 0)
                SelectLink(Math.Clamp(_selectedLink, 0, links.Count - 1));
        }

        protected override void OnFocusChanged(bool gainFocus, FocusSearchDirection direction, NativeRect? previouslyFocusedRect)
        {
            base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);
            _pendingActivation = -1;
            if (gainFocus && _links.Count > 0)
            {
                SelectLink(direction is FocusSearchDirection.Backward or FocusSearchDirection.Up ? _links.Count - 1 : 0);
            }
            else
            {
                _selectedLink = -1;
                if (TextFormatted is ISpannable text)
                    Selection.RemoveSelection(text);
            }
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
        {
            SynchronizeSelection();
            if (e is not null)
            {
                if (PopupKeyboardNavigation.IsTabNavigation(keyCode, e))
                {
                    int next = _selectedLink < 0
                        ? (e.IsShiftPressed ? _links.Count - 1 : 0)
                        : _selectedLink + (e.IsShiftPressed ? -1 : 1);
                    if (next >= 0 && next < _links.Count)
                    {
                        SelectLink(next);
                        return true;
                    }
                    return false;
                }
                if (e.HasNoModifiers && PopupKeyboardNavigation.IsActivationKey(keyCode) && _selectedLink >= 0 && _selectedLink < _links.Count)
                {
                    if (e.RepeatCount == 0)
                        _pendingActivation = _selectedLink;
                    return true;
                }
            }

            bool handled = base.OnKeyDown(keyCode, e);
            SynchronizeSelection();
            return handled;
        }

        private void SynchronizeSelection()
        {
            int selected = -1;
            if (TextFormatted is ISpannable buffer)
            {
                int selectionStart = Selection.GetSelectionStart(buffer);
                int selectionEnd = Selection.GetSelectionEnd(buffer);
                int start = Math.Min(selectionStart, selectionEnd);
                int end = Math.Max(selectionStart, selectionEnd);
                for (int index = 0; index < _links.Count; index++)
                {
                    if (_links[index].Start == start && _links[index].End == end)
                    {
                        selected = index;
                        break;
                    }
                }
            }
            if (_selectedLink != selected)
            {
                _selectedLink = selected;
                _pendingActivation = -1;
            }
        }

        public override bool OnKeyUp(Keycode keyCode, KeyEvent? e)
        {
            if (PopupKeyboardNavigation.IsActivationKey(keyCode) && _pendingActivation >= 0)
            {
                int index = _pendingActivation;
                _pendingActivation = -1;
                if (e is not null && e.HasNoModifiers && !e.IsCanceled && Enabled && index < _links.Count)
                    _links[index].Activate();
                return true;
            }
            return base.OnKeyUp(keyCode, e);
        }

        private void SelectLink(int index)
        {
            _pendingActivation = -1;
            _selectedLink = index;
            if (TextFormatted is ISpannable text)
            {
                Selection.SetSelection(text, _links[index].Start, _links[index].End);
                BringPointIntoView(_links[index].Start);
            }
        }
    }
}
#endif
