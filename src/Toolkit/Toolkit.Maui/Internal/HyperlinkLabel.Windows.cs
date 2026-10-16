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
using Esri.ArcGISRuntime.Toolkit.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Documents;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal sealed class HyperlinkLabel : Label
{
    internal HyperlinkLabel(FormattedString text)
    {
        InlineContent = text;
        Text = text.ToString();
    }

    // Native hyperlinks replace MAUI's formatted-span hit testing, so don't assign Label.FormattedText.
    internal FormattedString InlineContent { get; }
}

internal sealed class HyperlinkLabelHandler : LabelHandler
{
    public override void SetVirtualView(IView view)
    {
        base.SetVirtualView(view);
        UpdateInlines();
    }

    public override void UpdateValue(string property)
    {
        base.UpdateValue(property);
        if (property is nameof(ILabel.Text) or nameof(ILabel.Font) or nameof(Label.TextColor) or
            nameof(Label.TextTransform) or nameof(Label.LineHeight) or nameof(Label.CharacterSpacing) or
            nameof(Label.TextDecorations) or nameof(Label.HorizontalTextAlignment))
        {
            UpdateInlines();
        }
    }

    private void UpdateInlines()
    {
        if (VirtualView is not HyperlinkLabel label || PlatformView is null || MauiContext is null)
            return;

        PlatformView.IsTabStop = false;
        PlatformView.TextHighlighters.Clear();
        PlatformView.UpdateInlines(MauiContext.Services.GetRequiredService<IFontManager>(), label.InlineContent,
            label.LineHeight, label.HorizontalTextAlignment, ((ILabel)label).Font, label.TextColor, label.TextTransform);
        var inlines = PlatformView.Inlines.ToArray();
        PlatformView.Inlines.Clear();

        Action<Element>? previousLink = null;
        Hyperlink? hyperlink = null;
        for (int i = 0; i < inlines.Length; i++)
        {
            var activate = (label.InlineContent.Spans[i] as HtmlToView.HyperlinkSpan)?.Activate;
            if (activate is null)
            {
                PlatformView.Inlines.Add(inlines[i]);
                hyperlink = null;
            }
            else
            {
                // Styled spans belonging to one HTML anchor share a single keyboard stop.
                if (hyperlink is null || !ReferenceEquals(activate, previousLink))
                {
                    hyperlink = new Hyperlink { IsTabStop = true };
                    hyperlink.Click += (_, _) => activate(label);
                    PlatformView.Inlines.Add(hyperlink);
                }
                hyperlink.Inlines.Add(inlines[i]);
            }
            previousLink = activate;
        }
    }
}
#endif
