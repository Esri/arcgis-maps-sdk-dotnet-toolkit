using Esri.ArcGISRuntime.Symbology;
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// A control that renders a <see cref="Symbology.Symbol"/>.
    /// </summary>
    public class SymbolDisplay : View
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDisplay"/> class
        /// </summary>
        public SymbolDisplay() : this(new UI.Controls.SymbolDisplay()) { }

        internal SymbolDisplay(UI.Controls.SymbolDisplay nativeSymbolDisplay)
        {
            NativeSymbolDisplay = nativeSymbolDisplay;

#if NETFX_CORE
            nativeSymbolDisplay.SizeChanged += (o, e) => InvalidateMeasure();
#endif
        }

        internal readonly UI.Controls.SymbolDisplay NativeSymbolDisplay;
        
        /// <summary>
        /// Identifies the <see cref="Symbol"/> bindable property.
        /// </summary>
        public static readonly BindableProperty SymbolProperty =
            BindableProperty.Create(nameof(Symbol), typeof(Symbol), typeof(SymbolDisplay), null, BindingMode.OneWay, null, OnSymbolPropertyChanged);

        /// <summary>
        /// Gets or sets the symbol to render.
        /// </summary>
        /// <seealso cref="SymbolProperty"/>
        public Symbol Symbol
        {
            get { return (Symbol)GetValue(SymbolProperty); }
            set { SetValue(SymbolProperty, value); }
        }

        private static void OnSymbolPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SymbolDisplay)
            {
                var symbolDisplay = (SymbolDisplay)bindable;
                symbolDisplay.NativeSymbolDisplay.Symbol = newValue as Symbol;
                symbolDisplay.InvalidateMeasure();
            }
        }        
    }
}
