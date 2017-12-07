using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;
using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Internal;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// The Compass Control showing the heading on the map when the rotation is not North up / 0.
    /// </summary>
    public class Compass : View
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Compass"/> class
        /// </summary>
        public Compass() : this(new UI.Controls.Compass()) { }

        internal Compass(UI.Controls.Compass nativeCompass)
        {
            NativeCompass = nativeCompass;

#if NETFX_CORE
            nativeCompass.SizeChanged += (o, e) => InvalidateMeasure();
#endif
        }

        internal readonly UI.Controls.Compass NativeCompass;

        /// <summary>
        /// Identifies the <see cref="Heading"/> bindable property.
        /// </summary>
        public static readonly BindableProperty HeadingProperty =
            BindableProperty.Create(nameof(Heading), typeof(double), typeof(Compass), 0, BindingMode.OneWay, null, OnHeadingPropertyChanged);

        /// <summary>
        /// Gets or sets the Heading for the compass.
        /// </summary>
        public double Heading
        {
            get { return (double)GetValue(HeadingProperty); }
            set { SetValue(HeadingProperty, value); }
        }

        private static void OnHeadingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var Compass = (Compass)bindable;
                Compass.NativeCompass.Heading = (double)newValue;
                Compass.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Identifies the <see cref="AutoHide"/> bindable property.
        /// </summary>
        public static readonly BindableProperty AutoHideProperty =
            BindableProperty.Create(nameof(AutoHide), typeof(bool), typeof(Compass), false, BindingMode.OneWay, null, OnAutoHidePropertyChanged);

        /// <summary>
        /// Gets or sets a value indicating whether to auto-hide the control when Heading is 0
        // </summary>
        public bool AutoHide
        {
            get { return (bool)GetValue(AutoHideProperty); }
            set { SetValue(AutoHideProperty, value); }
        }

        private static void OnAutoHidePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var Compass = (Compass)bindable;
                Compass.NativeCompass.AutoHide = (bool)newValue;
                Compass.InvalidateMeasure();
            }
        }
    }
}
