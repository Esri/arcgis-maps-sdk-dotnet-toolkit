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
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if WPF
using System.Windows.Documents;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a <see cref="FieldsPopupElement"/>.
    /// </summary>
    [TemplatePart(Name = TableAreaContentName, Type = typeof(ContentPresenter))]
    public partial class FieldsPopupElementView : Control
    {
        private const string TableAreaContentName = "TableAreaContent";

#if WPF
        private static bool IsStyleCompatible(Type elementType, Style? style)
        {
            // A Style can be applied if its TargetType is null or a base type of the element.
            return style is null || style.TargetType is null || style.TargetType.IsAssignableFrom(elementType);
        }

        private FrameworkElement CreateTextCell(string? text, bool wrap)
        {
            bool canApplyBlockStyle = IsStyleCompatible(typeof(TextBlock), FieldTextStyle);
            bool canApplyBoxStyle = IsStyleCompatible(typeof(TextBox), FieldTextStyle);

            if (canApplyBlockStyle && !canApplyBoxStyle)
            {
                // Fallback to TextBlock if user specifically provided a custom style for it.
                // Previous versions of this control used TextBlock, so this allows users to keep their existing styling.
                return new TextBlock
                {
                    Style = FieldTextStyle,
                    Text = text ?? "",
                    TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
                };
            }

            // Default: Use a TextBox styled to look like a TextBlock, which allows text selection and copying.
            // Requested for WPF by https://github.com/Esri/arcgis-maps-sdk-dotnet-toolkit/issues/710
            var tb = new TextBox
            {
                Text = text ?? "",

                IsReadOnly = true,
                IsReadOnlyCaretVisible = false,
                IsTabStop = false,

                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),

                FocusVisualStyle = null,
                VerticalContentAlignment = VerticalAlignment.Top,
            };

            if (canApplyBoxStyle)
                tb.Style = FieldTextStyle;

            if (wrap)
            {
                tb.AcceptsReturn = true; // enables multi-line text
                tb.TextWrapping = TextWrapping.Wrap;
                tb.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                tb.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }

            return tb;
        }

        private TextBlock CreateHyperlinkCell(Uri uri)
        {
            var t = new TextBlock { TextWrapping = TextWrapping.Wrap };

            // Apply FieldTextStyle if it also works for TextBlock
            if (IsStyleCompatible(typeof(TextBlock), FieldTextStyle))
                t.Style = FieldTextStyle;

            var hl = new Hyperlink { NavigateUri = uri };
            hl.Click += Hyperlink_Click;
            hl.Inlines.Add(Properties.Resources.GetString("PopupViewerViewHyperlinkText"));
            t.Inlines.Add(hl);
            return t;
        }
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink link)
                UI.Controls.PopupViewer.GetPopupViewerParent(this)?.OnHyperlinkClicked(link.NavigateUri);
        }
#endif
    }
}
#endif
