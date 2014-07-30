// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;
#if !WINDOWS_PHONE_APP
using Esri.ArcGISRuntime.Toolkit.Internal;
#endif
#else
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Controls.Primitives
{
    
    /// <summary>
    /// A control that displays a symbol.
    /// <para>
    /// The control creates internally the swatch by using <see cref="Symbology.Symbol.CreateSwatchAsync()"/>.
    /// </para>
    /// <para>
    /// If the symbol symbolizes a point feature, the symbol will be displayed at scale without any stretching except if the 
    /// Height/Width or MaxHeight/MaxWidth are set and don't allow displaying the symbol at scale without clipping. In this case the symbol is stretched to fill 
    /// the available space.
    /// </para>
    /// <para>If the symbol symbolizes a line or a polygon, the swatch size is based on the Height/Width or MaxHeight/MaxWidth.
    /// If these properties are not set, a default 32*32 size is used instead.
    /// </para>
    /// </summary>
    [TemplatePart(Name = "Image", Type = typeof(Image))]
    public class SymbolDisplay : Control
    {
        private Image _image; // image template part
        private double _swatchDpi;
        private bool _isDirty; // flag indicating if the ImageSource needs to be updated
        private const double DefaultWidth = 32; // Default width for line or polygon swatch
        private const double DefaultHeight = 32; // Default height for line or polygon swatch

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDisplay"/> class.
        /// </summary>
        public SymbolDisplay()
        {
#if NETFX_CORE
            DefaultStyleKey = typeof(SymbolDisplay);
#if !WINDOWS_PHONE_APP // Windows Phone DPI never changes
            Loaded += (sender, args) => SubscribeToDpiChanged();
            Unloaded +=(sender, args) => UnsubscribeToDpiChanged();
#endif
#endif
        }

#if !NETFX_CORE
        static SymbolDisplay()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (SymbolDisplay),
                new FrameworkPropertyMetadata(typeof (SymbolDisplay)));
        }
#endif

        #endregion Constructor

        #region OnApplyTemplate

        /// <summary>
        /// Invoked whenever application code or internal processes 
        /// (such as a rebuilding layout pass) call ApplyTemplate. 
        /// In simplest terms, this means the method is called just 
        /// before a UI element displays in your app. Override this 
        /// method to influence the default post-template logic of 
        /// a class.
        /// </summary>
#if NETFX_CORE
        protected
#else
        public
#endif
            override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Get a reference to the templated parts
            _image = GetTemplateChild("Image") as Image;
            if (_image == null)
                throw new Exception("Missing 'Image' Template part.");

            // Initialize default swatch dpi with the view dpi
            _swatchDpi = CompatUtility.IsDesignMode ? 96.0 : CompatUtility.LogicalDpi(this);
        }

        #endregion

        #region Symbol

        /// <summary>
        /// The symbol to display.
        /// </summary>
        public Symbol Symbol
        {
            get { return (Symbol)GetValue(SymbolProperty); }
            set { SetValue(SymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Symbol"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty SymbolProperty =
            DependencyProperty.Register("Symbol", typeof(Symbol), typeof(SymbolDisplay), new PropertyMetadata(null, OnPropertyChanged));
        #endregion

        #region BackgroundColor

        /// <summary>
        /// The swatch background (transparent by default)
        /// </summary>
        public Color BackgroundColor
        {
            get { return (Color) GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="BackgroundColor"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Color), typeof(SymbolDisplay), new PropertyMetadata(Colors.Transparent, OnPropertyChanged));

        #endregion

        #region GeometryType

        /// <summary>
        /// The symbol geometry type (<see cref="ArcGISRuntime.Geometry.GeometryType.Unknown"/> by default).
        /// <para>
        /// If the geometry type is <see cref="ArcGISRuntime.Geometry.GeometryType.Unknown"/>, the geometry type will be deduced from the symbol type.
        /// However, in some cases such as a MarkerSymbol used to symbolize a line or an area object, it may be useful to set this parameter.
        /// </para>
        /// </summary>
        public GeometryType GeometryType
        {
            get { return (GeometryType) GetValue(GeometryTypeProperty); }
            set { SetValue(GeometryTypeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeometryType"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty GeometryTypeProperty =
            DependencyProperty.Register("GeometryType", typeof(GeometryType), typeof(SymbolDisplay), new PropertyMetadata(GeometryType.Unknown, OnPropertyChanged));

        #endregion

        /// <summary>
        /// Provides the behavior for the Measure pass of the layout cycle.
        /// </summary>
        /// <param name="availableSize">The available size that this object can give to child objects.</param>
        /// <returns>The size that this object determines it needs during layout.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            // Update HeightPixels and WidthPixels as they can have changed due to a control Height or Width change (we don't subscribe to these changes)
            SetPixelsSize();
            
            if (_isDirty)
            {
                UpdateImageSource();
            }
            else
            {
                // Measure the image without constraints
                _image.Stretch = Stretch.None;
                _image.MaxHeight = double.PositiveInfinity;
                _image.MaxWidth = double.PositiveInfinity;
                _image.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Size fullImageSize = _image.DesiredSize;

                // Set the image Height/Width in order to see the full image at raw resolution
                // There is a main difference between Desktop and WinStore on how Image control deals with dpi.
                // For desktop, the dpi info is part of the bitmap and the Image control takes care of the dpi automatically.
                // For WinStore, the Image control does not take care of the dpi the swatch has been generated for. It always display the image as if it was 96dpi.
                // The symbolDisplay control hides this difference to users.

                // Note that we set the MaxHeight and not the Height, so if the available space is smaller, the symbol will be displayed without clipping inside the available space
#if NETFX_CORE
                _image.MaxHeight = fullImageSize.Height * 96.0 / _swatchDpi;
                _image.MaxWidth = fullImageSize.Width * 96.0 / _swatchDpi;
#else
                _image.MaxHeight = fullImageSize.Height;
                _image.MaxWidth = fullImageSize.Width;
#endif
                _image.Stretch = Stretch.Uniform;
            }
            return base.MeasureOverride(availableSize);
        }


        // Private methods
        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var symbolDisplay = (SymbolDisplay)d;
            if (symbolDisplay != null)
            {
                symbolDisplay.SetDirty();
            }
        }

        private void SetDirty()
        {
            _isDirty = true;
            InvalidateMeasure(); // measure will recreate the swatch
        }

        private void UpdateImageSource()
        {
            if (_image != null)
            {
                _isDirty = false;
                if (Symbol == null)
                {
                    _image.Source = null;
                }
                else
                {
                    var task = UpdateImageSourceAsync();
                }
            }
        }

        private async Task UpdateImageSourceAsync()
        {
            try
            {
                _image.Source = await Symbol.CreateSwatchAsync(WidthPixels, HeightPixels, _swatchDpi, BackgroundColor, GeometryType);
            }
            catch
            {
                _image.Source = null;
            }
            InvalidateMeasure();
        }


        private int _heightPixels;
        private int HeightPixels
        {
            get { return _heightPixels; }
            set
            {
                if (_heightPixels != value)
                {
                    _heightPixels = value;
                    SetDirty();
                }
            }
        }

        private int _widthPixels;
        private int WidthPixels
        {
            get { return _widthPixels; }
            set
            {
                if (_widthPixels != value)
                {
                    _widthPixels = value;
                    SetDirty();
                }
            }
        }

        // set expected swatch size (except for point)
        private void SetPixelsSize()
        {
            var geometryType = GeometryType;
            if (geometryType == GeometryType.Unknown)
            {
                // try to retrieve the geometry type from the symbol type
                if (Symbol is FillSymbol)
                    geometryType = GeometryType.Polygon;
                else if (Symbol is LineSymbol)
                    geometryType = GeometryType.Polyline;
                else
                    geometryType = GeometryType.Point;
            }
            int heightPixels;
            int widthPixels;

            if (geometryType == GeometryType.Point) 
            {
                // for point, we need to keep 0 as expected image size so RTC will calculate it to avoid clipping
                heightPixels = 0;
                widthPixels = 0;
            }
            else
            {
                // For line and polygon, use Height/Width, MaxHeight/MaxWidth or  DefaultHeight/DefaultWidth
                double height = double.IsNaN(Height) ? double.PositiveInfinity : Height;
                double width = double.IsNaN(Width) ? double.PositiveInfinity : Width;

                if (!double.IsNaN(MaxWidth))
                    width = Math.Min(width, MaxWidth);

                if (!double.IsNaN(MaxHeight))
                    height = Math.Min(height, MaxHeight);

                if (double.IsPositiveInfinity(width))
                    width = DefaultWidth;

                if (double.IsPositiveInfinity(height))
                    height = DefaultHeight;

                heightPixels = (int)Math.Ceiling(height * _swatchDpi / 96.0);
                widthPixels = (int)Math.Ceiling(width * _swatchDpi / 96.0);
            }
            HeightPixels = heightPixels;
            WidthPixels = widthPixels;
        }

        // DPI Changed management
#if NETFX_CORE && !WINDOWS_PHONE_APP // todo later for Desktop: subscribe to dpi changed in case of dpi-aware per monitor app

        // Listen for dpi changed by using a weak event listener since the view is a long lived object
        private WeakEventListener<SymbolDisplay, Windows.Graphics.Display.DisplayInformation, object> _dpiChangedWeakEventListener;

        private void OnLogicalDpiChanged(Windows.Graphics.Display.DisplayInformation info, object sender)
        {
            if (info.LogicalDpi != _swatchDpi)
            {
                OnSwatchDpiChanged(info.LogicalDpi);
            }
        }

        private void OnSwatchDpiChanged(double newValue)
        {
            _swatchDpi = newValue > 0.0 ? newValue : CompatUtility.LogicalDpi(this);
            SetDirty();
        }

        private void SubscribeToDpiChanged()
        {
            if (!CompatUtility.IsDesignMode && _dpiChangedWeakEventListener == null)
            {
                var viewInfo = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
                if (viewInfo != null)
                {
                    _dpiChangedWeakEventListener = new WeakEventListener<SymbolDisplay, Windows.Graphics.Display.DisplayInformation, object>(this)
                    {
                        OnEventAction = (instance, source, eventArgs) => instance.OnLogicalDpiChanged(source, eventArgs),
                        OnDetachAction = weakEventListener => viewInfo.DpiChanged -= weakEventListener.OnEvent
                    };
                    viewInfo.DpiChanged += _dpiChangedWeakEventListener.OnEvent;
                }
            }
        }

        private void UnsubscribeToDpiChanged()
        {
            if (_dpiChangedWeakEventListener != null)
            {
                var dpiChangedListener = _dpiChangedWeakEventListener;
                _dpiChangedWeakEventListener = null;
                CompatUtility.ExecuteOnUIThread(dpiChangedListener.Detach, Dispatcher);
            }
        }
#endif
    }
}
