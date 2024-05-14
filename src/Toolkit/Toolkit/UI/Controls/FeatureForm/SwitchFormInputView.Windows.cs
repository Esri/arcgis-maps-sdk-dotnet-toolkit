#if WPF
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class SwitchFormInputView : CheckBox
    {
        /// <inheritdoc/>
        protected override void OnChecked(RoutedEventArgs e)
        {
            OnChecked();
            base.OnChecked(e);
        }

        /// <inheritdoc/>
        protected override void OnUnchecked(RoutedEventArgs e)
        {
            OnUnchecked();
            base.OnUnchecked(e);
        }
    }
}
#endif