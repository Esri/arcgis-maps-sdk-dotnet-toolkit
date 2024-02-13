using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
#if WPF
using System.Windows.Markup;
#elif MAUI
using MarkupExtension = Microsoft.Maui.Controls.Xaml.IMarkupExtension<string?>;
#elif WINUI
using Microsoft.UI.Xaml.Markup;
#elif WINDOWS_UWP
using Windows.UI.Xaml.Markup;
#endif
namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Markup extension for providing localized Toolkit strings
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class LocalizedStringExtension : MarkupExtension
    {
        /// <inheritdoc />
#if WPF
        public override object? ProvideValue(IServiceProvider serviceProvider)
#elif MAUI
        object? IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
#elif WINUI || WINDOWS_UWP
        protected override object? ProvideValue()
#endif
        {
            if (Key is null)
                return null;
            return Properties.Resources.GetString(Key);
        }

#if MAUI
        string? IMarkupExtension<string?>.ProvideValue(IServiceProvider serviceProvider) => ((IMarkupExtension)this).ProvideValue(serviceProvider) as string;
#endif

        /// <summary>
        /// Gets or sets the resource Key Name
        /// </summary>
        public string? Key { get; set; }
    }
}
