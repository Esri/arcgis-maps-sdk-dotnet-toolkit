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
        internal readonly UI.Controls.Compass NativeCompass;
        private bool _headingSetByGeoView;

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

            var tapRecognizer = new TapGestureRecognizer();
            tapRecognizer.Command = new TapCommand(ResetRotation);
            this.GestureRecognizers.Add(tapRecognizer);
        }

        private class TapCommand : System.Windows.Input.ICommand
        {
            private Action _action;
            public TapCommand(Action action)
            {
                _action = action;
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
            {
                return _action != null;
            }

            public void Execute(object parameter)
            {
                _action?.Invoke();
            }
        }

        private void ResetRotation()
        {
            var view = GetGeoView(this);
            if (view is MapView)
            {
                ((MapView)view).SetViewpointRotationAsync(0);
            }
            else if (view is SceneView)
            {
                var sv = (SceneView)view;
                var c = sv.Camera;
                sv.SetViewpointCamera(c.RotateTo(0, c.Pitch, c.Roll));
            }
        }

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
                var compass = (Compass)bindable;
                if (compass.GeoView != null && !compass._headingSetByGeoView)
                    throw new InvalidOperationException("The Heading Property is read-only when the GeoView property has been assigned");
                 compass.NativeCompass.Heading = (double)newValue;
                 compass.InvalidateMeasure();
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
        
        /// <summary>
        /// Gets or sets the GeoView property that can be attached to a Compass control to accurately set the heading, instead of
        /// setting the <see cref="Compass.Heading"/> property directly.
        /// </summary>
        public GeoView GeoView
        {
            get { return GetValue(GeoViewProperty) as GeoView; }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> Dependency Property
        /// </summary>
        public static readonly BindableProperty GeoViewProperty =
            BindableProperty.Create(nameof(Compass.GeoView), typeof(GeoView), typeof(Compass), null, BindingMode.OneWay, null, OnGeoViewPropertyChanged);


        private static void OnGeoViewPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var compass = (Compass)bindable;
            var inpc = oldValue as INotifyPropertyChanged;
            if (inpc != null)
            {
                inpc.PropertyChanged -= compass.GeoView_PropertyChanged;
            }

            inpc = newValue as INotifyPropertyChanged;
            if (inpc != null)
            {
                inpc.PropertyChanged += compass.GeoView_PropertyChanged;
            }
            compass.UpdateCompassFromGeoView(newValue as GeoView);
        }

        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var view = GeoView;
            if (view is MapView && e.PropertyName == nameof(MapView.MapRotation) ||
                view is SceneView && e.PropertyName == nameof(SceneView.Camera))
                UpdateCompassFromGeoView(GeoView);
        }

        private void UpdateCompassFromGeoView(GeoView view)
        {
            _headingSetByGeoView = true;
            Heading = (view is MapView) ? ((MapView)view).MapRotation : (view is SceneView ? ((SceneView)view).Camera.Heading : 0);
            _headingSetByGeoView = false;
        }
    }
}
