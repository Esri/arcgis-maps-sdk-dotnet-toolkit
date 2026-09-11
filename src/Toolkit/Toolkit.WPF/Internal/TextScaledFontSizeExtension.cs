using System;
using System.Windows.Markup;
using Windows.UI.ViewManagement;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Provides a font size adjusted by the Windows text scale factor.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class TextScaledFontSizeExtension : MarkupExtension
    {
        /// <summary>
        /// Gets or sets the unscaled font size.
        /// </summary>
        public double FontSize { get; set; }

        /// <inheritdoc/>
        public override object ProvideValue(IServiceProvider serviceProvider) =>
            FontSize * new UISettings().TextScaleFactor;
    }
}