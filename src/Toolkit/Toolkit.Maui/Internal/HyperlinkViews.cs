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

#if WINDOWS || __IOS__
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

internal sealed class HyperlinkContentView : ContentView
{
    private readonly EventHandler<Uri> _clickHandler;

    internal HyperlinkContentView(View content, Uri uri, EventHandler<Uri> clickHandler)
    {
        Content = content;
        Padding = 0;
        Uri = uri;
        _clickHandler = clickHandler;
#if __IOS__
        SemanticProperties.SetDescription(this, SemanticProperties.GetDescription(content) ??
            (content is Label label ? label.Text ?? label.FormattedText?.ToString() : null) ?? uri.AbsoluteUri);
        SemanticProperties.SetHeadingLevel(this, SemanticProperties.GetHeadingLevel(content));
#endif
    }

    internal Uri Uri { get; }

    internal void Activate() => _clickHandler(this, Uri);
}
#endif
