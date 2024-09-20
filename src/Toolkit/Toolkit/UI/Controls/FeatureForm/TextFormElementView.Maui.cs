#if MAUI
using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class TextFormElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private Label? _readonlyLabel;

        static TextFormElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.FeatureForms.TextFormElement.Text), "Esri.ArcGISRuntime.Mapping.FeatureForms.FieldFormElement", "Esri.ArcGISRuntime")]
        private static object BuildDefaultTemplate()
        {
            Label readonlyText = new Label() { LineBreakMode = LineBreakMode.WordWrap };
            readonlyText.SetBinding(View.IsVisibleProperty, new Binding("Element.IsVisible", source: RelativeBindingSource.Self));
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(readonlyText, nameScope);
            nameScope.RegisterName("ReadOnlyText", readonlyText);
            return readonlyText;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _readonlyLabel = GetTemplateChild("ReadOnlyText") as Label;
            UpdateText();
            UpdateVisibility();
        }
    }
}
#endif
