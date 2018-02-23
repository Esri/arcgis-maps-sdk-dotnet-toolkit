using Esri.ArcGISRuntime.Mapping;
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// A control that renders a <see cref="Symbology.Symbol"/>.
    /// </summary>
    public class LayerLegend : View
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LayerLegend"/> class
        /// </summary>
        public LayerLegend() : this(new UI.Controls.LayerLegend()) { }

        internal LayerLegend(UI.Controls.LayerLegend nativeLayerLegend)
        {
            NativeLayerLegend = nativeLayerLegend;

#if NETFX_CORE
            nativeLayerLegend.SizeChanged += (o, e) => InvalidateMeasure();
#endif
        }

        internal readonly UI.Controls.LayerLegend NativeLayerLegend;

        /// <summary>
        /// Identifies the <see cref="LayerContent"/> bindable property.
        /// </summary>
        public static readonly BindableProperty LayerContentProperty =
            BindableProperty.Create(nameof(LayerContent), typeof(ILayerContent), typeof(LayerLegend), null, BindingMode.OneWay, null, OnLayerContentPropertyChanged);

        /// <summary>
        /// Gets or sets the layer to display the legend for.
        /// </summary>
        /// <seealso cref="LayerContentProperty"/>
        public ILayerContent LayerContent
        {
            get { return (ILayerContent)GetValue(LayerContentProperty); }
            set { SetValue(LayerContentProperty, value); }
        }

        private static void OnLayerContentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is LayerLegend)
            {
                var LayerLegend = (LayerLegend)bindable;
                LayerLegend.NativeLayerLegend.LayerContent = newValue as ILayerContent;
                LayerLegend.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="ShowEntireTreeHierarchy"/> bindable property.
        /// </summary>
        public static readonly BindableProperty ShowEntireTreeHierarchyProperty =
            BindableProperty.Create(nameof(ShowEntireTreeHierarchy), typeof(bool), typeof(LayerLegend), null, BindingMode.OneWay, null, OnShowEntireTreeHierarchyPropertyChanged);

        /// <summary>
        /// Gets or sets a value indicating whether the entire <see cref="ILayerContent"/> tree hierarchy should be rendered
        /// </summary>
        /// <seealso cref="ShowEntireTreeHierarchyProperty"/>
        public bool ShowEntireTreeHierarchy
        {
            get { return (bool)GetValue(ShowEntireTreeHierarchyProperty); }
            set { SetValue(ShowEntireTreeHierarchyProperty, value); }
        }

        private static void OnShowEntireTreeHierarchyPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is LayerLegend && newValue is bool)
            {
                var LayerLegend = (LayerLegend)bindable;
                LayerLegend.NativeLayerLegend.ShowEntireTreeHierarchy = (bool)newValue;
                LayerLegend.InvalidateMeasure();
            }
        }
    }
}
