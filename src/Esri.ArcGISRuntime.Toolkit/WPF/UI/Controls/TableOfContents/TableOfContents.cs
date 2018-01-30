using Esri.ArcGISRuntime.UI.Controls;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// A control that creates a table of content tree view from a <see cref="GeoView"/>.
    /// </summary>
    [TemplatePart(Name = "List", Type = typeof(ItemsControl))]
    public class TableOfContents : LayerList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableOfContents"/> class.
        /// </summary>
        public TableOfContents() : base()
        {
            DefaultStyleKey = typeof(TableOfContents);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show a legend for the layers in the tree view
        /// </summary>
        public bool ShowLegend
        {
            get { return (bool)GetValue(ShowLegendProperty); }
            set { SetValue(ShowLegendProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ShowLegend"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowLegendProperty =
            DependencyProperty.Register(nameof(ShowLegend), typeof(bool), typeof(TableOfContents), new PropertyMetadata(true, OnShowLegendPropertyChanged));

        private static void OnShowLegendPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TableOfContents)d).ShowLegendInternal = (bool)e.NewValue;
        }
    }
}