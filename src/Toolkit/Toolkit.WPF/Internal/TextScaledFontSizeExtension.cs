using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;
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
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding(nameof(TextScaleFactorSource.TextScaleFactor))
            {
                Source = TextScaleFactorSource.Instance,
                Converter = new TextScaleFactorConverter(FontSize),
                Mode = BindingMode.OneWay,
            };

            return binding.ProvideValue(serviceProvider);
        }

        private sealed class TextScaleFactorSource : INotifyPropertyChanged
        {
            private readonly Dispatcher _dispatcher;
            private readonly UISettings _uiSettings;
            private double _textScaleFactor;

            private TextScaleFactorSource()
            {
                _dispatcher = Dispatcher.CurrentDispatcher;
                _uiSettings = new UISettings();
                _textScaleFactor = _uiSettings.TextScaleFactor;
                _uiSettings.TextScaleFactorChanged += OnTextScaleFactorChanged;
            }

            public static TextScaleFactorSource Instance { get; } = new TextScaleFactorSource();

            public event PropertyChangedEventHandler? PropertyChanged;

            public double TextScaleFactor => _textScaleFactor;

            private void OnTextScaleFactorChanged(UISettings sender, object args)
            {
                if (_dispatcher.CheckAccess())
                {
                    UpdateTextScaleFactor();
                }
                else
                {
                    _dispatcher.BeginInvoke((Action)UpdateTextScaleFactor);
                }
            }

            private void UpdateTextScaleFactor()
            {
                _textScaleFactor = _uiSettings.TextScaleFactor;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextScaleFactor)));
            }
        }

        private sealed class TextScaleFactorConverter : IValueConverter
        {
            private readonly double _fontSize;

            public TextScaleFactorConverter(double fontSize) => _fontSize = fontSize;

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
                _fontSize * (double)value;

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
                throw new NotSupportedException();
        }
    }
}