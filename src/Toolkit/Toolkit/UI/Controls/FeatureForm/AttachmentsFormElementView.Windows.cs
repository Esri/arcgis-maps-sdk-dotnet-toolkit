#if WPF
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    // [TemplatePart(Name ="Selector", Type = typeof(System.Windows.Controls.Primitives.Selector))]
    public partial class AttachmentsFormElementView : Control
    {
        /// <inheritdoc />
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
        }
    }
}
#endif