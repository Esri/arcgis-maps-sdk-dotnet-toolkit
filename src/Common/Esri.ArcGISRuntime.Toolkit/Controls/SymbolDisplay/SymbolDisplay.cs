// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;
using System.Diagnostics;
#else
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
    
    /// <summary>
    /// A control that displays a symbol.
    /// <para>
    /// The control creates internally the swatch by using <see cref="Symbology.Symbol.CreateSwatchAsync()"/>.
    /// </para>
    /// <para>
    /// If the symbol symbolizes a point feature, the symbol will be displayed at scale without any stretching except if the 
    /// Height and Width are set and don't allow displaying the symbol at scale without clipping. In this case the symbol is stretched to fill 
    /// the available space.
    /// </para>
    /// <para>If the symbol symbolizes a line or a polygon, the swatch is created with a default size that can be overridden
    /// by setting either the Height/Width or the MinHeight/MinWidth.
    /// </para>
    /// </summary>
    [TemplatePart(Name = "Image", Type = typeof(Image))]
    public class SymbolDisplay : Control
    {
        private Image _image; // image template part
        private double _swatchDpi; // private variable to know whether the SwatchDpi DP has been explicitly set

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDisplay"/> class.
        /// </summary>
        public SymbolDisplay()
        {
#if NETFX_CORE
            DefaultStyleKey = typeof(SymbolDisplay);
            SubscribeToDpiChanged();
#endif
        }

#if !NETFX_CORE
        static SymbolDisplay()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (SymbolDisplay),
                new FrameworkPropertyMetadata(typeof (SymbolDisplay)));
        }
#endif

        /// <summary>
        /// Finalizes an instance of the <see cref="SymbolDisplay"/> class.
        /// </summary>
        ~SymbolDisplay()
        {
            // Detach WeakEventListeners for avoiding small memory leaks
            UnsubscribeToDpiChanged();
        }


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
            get { return (Symbol) GetValue(SymbolProperty); }
            set { SetValue(SymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Symbol"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty SymbolProperty =
            DependencyProperty.Register("Symbol", typeof (Symbol), typeof (SymbolDisplay), new PropertyMetadata(null, UpdateImageSource));

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
            DependencyProperty.Register("BackgroundColor", typeof(Color), typeof(SymbolDisplay), new PropertyMetadata(Colors.Transparent, UpdateImageSource));

        #endregion

        #region GeometryType

        /// <summary>
        /// The symbol geometry type (<see cref="ArcGISRuntime.Geometry.GeometryType.Unknown"/> by default).
        /// <para>
        /// If the geometry type is <see cref="ArcGISRuntime.Geometry.GeometryType.Unknown"/>, the geometry type will be deduced from the symbol type.
        /// However, in some cases such as a MarkerSymbol used to symbolize a line or area object, it may be usefull tos et this parameter.
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
            DependencyProperty.Register("GeometryType", typeof(GeometryType), typeof(SymbolDisplay), new PropertyMetadata(GeometryType.Unknown, UpdateImageSource));

        #endregion

        #region SwatchDpi

        /// <summary>
        /// The DPI value used for creating the swatch (optional). If the SwatchDpi property is not set,
        /// the symbol swatch is generated with the current view DPI.
        /// </summary>
        public double SwatchDpi
        {
            get { return (double)GetValue(SwatchDpiProperty); }
            set { SetValue(SwatchDpiProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SwatchDpi"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty SwatchDpiProperty =
            DependencyProperty.Register("SwatchDpi", typeof(double), typeof(SymbolDisplay), new PropertyMetadata(0.0, OnSwatchDpiChanged));

        private static void OnSwatchDpiChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var symbolDisplay = (SymbolDisplay)d;
            if (symbolDisplay != null)
                symbolDisplay.OnSwatchDpiChanged((double)e.NewValue);
        }

        private void OnSwatchDpiChanged(double newValue)
        {
            _swatchDpi = newValue > 0.0 ? newValue : CompatUtility.LogicalDpi(this);
            UpdateImageSource();
        }

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

            // Measure the image without constraints
            _image.Stretch = Stretch.None;
            _image.Height = double.NaN;
            _image.Width = double.NaN;
            _image.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Size fullImageSize = _image.DesiredSize;

            // Set the image Height/Width in order to see the full image at raw resolution
#if NETFX_CORE
            _image.Height = fullImageSize.Height * 96.0 / _swatchDpi;
            _image.Width = fullImageSize.Width * 96.0 / _swatchDpi;
#else
            _image.Height = fullImageSize.Height;
            _image.Width = fullImageSize.Width;
#endif
            _image.Stretch = Stretch.Uniform;
            return base.MeasureOverride(availableSize);
        }

        /// <summary>
        /// Provides the behavior for the Arrange pass of layout.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this object should use to arrange itself and its children.</param>
        /// <returns>The actual size that is used after the element is arranged in layout.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // If the image is reduced due to the constraints, change the image size in order to see the whole image (no clipping)
            var desiredImageSize = _image.DesiredSize;
            _image.Width = desiredImageSize.Width;
            _image.Height = desiredImageSize.Height;
            return base.ArrangeOverride(finalSize);
        }

        // Private methods
        private static void UpdateImageSource(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var symbolDisplay = (SymbolDisplay)d;
            if (symbolDisplay != null)
            {
                symbolDisplay.SetPixelsSize();
                symbolDisplay.UpdateImageSource();
            }
        }

        private ThrottleTimer _updateImageSourceTimer;
        private void UpdateImageSource()
        {
            // wait for initialization of all properties
            if (_updateImageSourceTimer == null)
            {
                _updateImageSourceTimer = new ThrottleTimer(100) { Action = UpdateImageSourceImpl };
            }
            _updateImageSourceTimer.Invoke();
        }

        private void UpdateImageSourceImpl()
        {
            if (_image != null)
            {
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
                    UpdateImageSource();
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
                    UpdateImageSource();
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
            int heightPixels = 0;
            int widthPixels = 0;

            if (geometryType != GeometryType.Point) // for point, we need to keep 0 as expected image size so RTC will calculate it to avoid clipping
            {
                // For line and polygon, use Height/Width and MinHeight/MinWidth (may worth a specific DP)
                double height = double.IsNaN(Height) ? MinHeight : Math.Max(Height, MinHeight);
                double width = double.IsNaN(Width) ? MinWidth : Math.Max(Width, MinWidth);

                heightPixels = (int)Math.Ceiling(height * _swatchDpi / 96.0);
                widthPixels = (int)Math.Ceiling(width * _swatchDpi / 96.0);
            }
            HeightPixels = heightPixels;
            WidthPixels = widthPixels;
        }


        private void SubscribeToDpiChanged()
        {
#if NETFX_CORE // todo later for Desktop: subscribe to dpi changed in case of dpi-aware per monitor app
            if (!CompatUtility.IsDesignMode)
            {
                Debug.Assert(_dpiChangedWeakEventListener == null);
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
#endif
        }


        private void UnsubscribeToDpiChanged()
        {
#if NETFX_CORE
            if (_dpiChangedWeakEventListener != null)
            {
                var dpiChangedListener = _dpiChangedWeakEventListener;
                _dpiChangedWeakEventListener = null;
                CompatUtility.ExecuteOnUIThread(dpiChangedListener.Detach, Dispatcher);
            }
#endif
        }

#if NETFX_CORE
        // Listen for dpi changed by using a weak event listener since the view is a long lived object
        private WeakEventListener<SymbolDisplay, Windows.Graphics.Display.DisplayInformation, object> _dpiChangedWeakEventListener;

        private void OnLogicalDpiChanged(Windows.Graphics.Display.DisplayInformation info, object sender)
        {
            if (info.LogicalDpi != _swatchDpi && SwatchDpi == 0.0) // if SwatchDpi != 0.0, the value sets by the user takes precedence
            {
                OnSwatchDpiChanged(info.LogicalDpi);
            }
        }
#endif
    }
}
