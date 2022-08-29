#if __ANDROID__
using Android.Content;
using Android.Locations;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Google.AR.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using LocationX = Esri.ArcGISRuntime.Location.Location;

namespace Esri.ArcGISRuntime.ARToolkit
{
    public class VPSLocationDataSource : LocationDataSource, INotifyPropertyChanged
    {
        private double _geoidalSeparation;
        private double _lastSetAltitude;
        private double _lastEarthAltitude;
        public double Separation
        {
            get => _geoidalSeparation;
            set
            {
                if (value != _geoidalSeparation)
                {
                    _geoidalSeparation = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Separation)));
                }
            }
        }
        public double EarthAltitude
        {
            get => _lastEarthAltitude;
             set
            {
                if (value != _lastEarthAltitude)
                {
                    _lastEarthAltitude = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EarthAltitude)));
                }
            }
        }
        public double AppliedAltitude
        {
            get => _lastSetAltitude;
            set
            {
                if (value != _lastSetAltitude)
                {
                    _lastSetAltitude = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AppliedAltitude)));
                }
            }
        }

        private ARSceneView _view;
        public VPSLocationDataSource(Context context, ARSceneView originatingView)
        {
            _view = originatingView;
            _view.NotifyRenderFrame += _view_NotifyRenderFrame;

            _context = context;

            // Create and listen for updates from a new system location data source.
            _baseSource = new SystemLocationDataSource();
            System.Diagnostics.Debug.WriteLine($"NXC: created location data source");

            // Listen for altitude change events from the onboard GNSS.
            _listener.NmeaAltitudeChanged += HandleNMEA;
            GetLocationManager().AddNmeaListener(_listener, null);
            System.Diagnostics.Debug.WriteLine($"NXC: Added NMEA listener");
        }

        private void _view_NotifyRenderFrame(object sender, EventArgs e)
        {
            ApplyEarth();
        }

        private void ApplyEarth()
        {
            if (_view.TrackingMode == ARLocationTrackingMode.ContinuousWithVPS && _view.ArSceneView?.Session?.Earth?.TrackingState == TrackingState.Tracking)
            {
                try
                {
                    GeospatialPose earthRelativePose = _view.ArSceneView.Session.Earth.CameraGeospatialPose;
                    EarthAltitude = earthRelativePose.Altitude;

                    // Set origin camera using Earth pose
                    _view.OriginCamera = new Mapping.Camera(earthRelativePose.Latitude, earthRelativePose.Longitude, earthRelativePose.Altitude - _geoidalSeparation, 0, 90, 0);
                    AppliedAltitude = earthRelativePose.Altitude - _geoidalSeparation + 2;

                    // Get the old origin camera.
                    Mapping.Camera uncorrectedCamera = _view.OriginCamera;

                    // Get the actual camera after ARCore transformation
                    Mapping.Camera actualCamera = _view.Camera;

                    // Calculate the new heading by applying the offset to the old camera's heading.
                    double desiredheading = earthRelativePose.Heading;
                    double actualHeading = actualCamera.Heading;
                    var difference = desiredheading - actualHeading;

                    // Create a new camera by rotating the old camera to the difference between the desired and actual heading.
                    Mapping.Camera newCamera = uncorrectedCamera.RotateTo(difference, uncorrectedCamera.Pitch, uncorrectedCamera.Roll);

                    // Use the new camera as the origin camera.
                    _view.OriginCamera = newCamera;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AR Toolkit: failed to apply position from ARCore Geospatial API - {ex}");
                }
            }
        }
        #region MSL adjustment

        // Object to handle NMEA messages from the onboard GNSS device.
        private readonly NmeaListener _listener = new NmeaListener();

        public IntPtr Handle => throw new NotImplementedException();

        // Use the underlying system location data source.
        private readonly SystemLocationDataSource _baseSource;

        private readonly Context _context;

        private void HandleNMEA(object sender, AltitudeEventArgs e)
        {
            //_listener.NmeaAltitudeChanged -= HandleNMEA;
            //GetLocationManager().RemoveNmeaListener(_listener);
            Separation = e.Altitude;
            System.Diagnostics.Debug.WriteLine($"NXC: NMEA geoid separation changed");

        }

        protected override Task OnStartAsync()
        {
            System.Diagnostics.Debug.WriteLine("NXC4: VPS location data source started");
            return _baseSource.StartAsync();
        }

        protected override Task OnStopAsync()
        {
            System.Diagnostics.Debug.WriteLine("NXC4: VPS location data source ");
            return _baseSource.StopAsync();
        }

        private LocationManager _locationManager;

        public event PropertyChangedEventHandler PropertyChanged;

        private LocationManager GetLocationManager()
        {
            return _locationManager ?? (_locationManager = (LocationManager)_context.GetSystemService("location"));
        }

        private class NmeaListener : Java.Lang.Object, IOnNmeaMessageListener
        {
            private long _lastTimestamp;

            public event EventHandler<AltitudeEventArgs> NmeaAltitudeChanged;

            public void OnNmeaMessage(string message, long timestamp)
            {
                System.Diagnostics.Debug.WriteLine($"NXC2: {message}");
                //if (message.StartsWith("$GPGGA") || message.StartsWith("$GNGNS") || message.StartsWith("$GNGGA"))
                if (message.StartsWith("$GNGNS") || message.StartsWith("$GPGNS"))
                {
                    var parts = message.Split(',');

                    if (parts.Length < 10)
                    {
                        return; // not enough
                    }

                    string geoidalseparation = parts[10];

                    if (string.IsNullOrEmpty(geoidalseparation))
                    {
                        return;
                    }


                    if (double.TryParse(geoidalseparation, NumberStyles.Float, CultureInfo.InvariantCulture,
                        out double geoidDiffDouble))
                    {
                        if (timestamp > _lastTimestamp)
                        {
                            _lastTimestamp = timestamp;
                            NmeaAltitudeChanged?.Invoke(this, new AltitudeEventArgs { Altitude = geoidDiffDouble });
                        }
                    }
                }
            }
        }
        public class AltitudeEventArgs
        {
            public double Altitude { get; set; }
        }
        #endregion MLS Adjustment
    }
}
#endif
