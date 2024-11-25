#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Markdig;
using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class TextFormElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private View? _textContainer;

        static TextFormElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            var textArea = new StackLayout();
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(textArea, nameScope);
            nameScope.RegisterName(TextAreaName, textArea);
            return textArea;
        }

        private void UpdateText()
        {
            var container = _textContainer as StackLayout;
            if (container is null)
                return;
            var text = Element?.Text ?? string.Empty;

            container.Children.Clear();
            if (Element?.Format == FormTextFormat.Markdown)
            {
                try
                {
                    var pipeline = new Markdig.MarkdownPipelineBuilder().Build();
                    var result = Markdig.Markdown.ToHtml(text ?? string.Empty, pipeline);
                    foreach (var block in HtmlToView.ToView(result, (s, e) => FeatureFormView.GetFeatureFormViewParent(this)?.OnHyperlinkClicked(e)))
                        container.Children.Add(block);

                    return;
                }
                catch
                {
                    text = RemoveMarkdown(text); // Fallback
                }
            }
            var label = new Label() { Text = text };
            if (text != null)
                text = StringExtensions.ToPlainText(text);
            label.Text = text;
            container.Children.Add(label);
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _textContainer = GetTemplateChild(TextAreaName) as StackLayout;
            UpdateText();
            UpdateVisibility();
        }
    }
}
#endif
