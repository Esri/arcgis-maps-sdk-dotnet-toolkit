#if __ANDROID__
using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Runtime;
using Android.Views;

namespace Esri.ArcGISRuntime.ARToolkit
{
    internal sealed class CompassOrientationHelper : Java.Lang.Object, ISensorEventListener
    {
        private SensorManager? _sensorManager;
        private readonly IWindowManager? _windowManager;

        private Sensor? _rotationSensor;
        private Sensor? _accelerometer;
        private Sensor? _magnetometer;

        public CompassOrientationHelper(Context? context)
        {
            _sensorManager = (SensorManager?)context?.GetSystemService(Context.SensorService);
            if (_sensorManager != null)
            {
                _rotationSensor = _sensorManager.GetDefaultSensor(SensorType.RotationVector);
                _accelerometer = _sensorManager.GetDefaultSensor(SensorType.Accelerometer);
                _magnetometer = _sensorManager.GetDefaultSensor(SensorType.MagneticField);
                _windowManager = Application.Context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            }
        }

        public static bool IsSupported(Context context)
        {
            var sensorManager = (SensorManager?)context.GetSystemService(Context.SensorService);
            if (sensorManager != null)
            {
                if (sensorManager.GetDefaultSensor(SensorType.RotationVector) != null)
                    return true;
                if (sensorManager.GetDefaultSensor(SensorType.Accelerometer) != null && sensorManager.GetDefaultSensor(SensorType.MagneticField) != null)
                    return true;
            }
            return false;
        }

        public void Resume()
        {
            if (_isStarted)
                return;
            _currentAccuracy = SensorStatus.NoContact;

            if (_sensorManager != null)
            {
                if (_rotationSensor != null)
                    _sensorManager.RegisterListener(this, _rotationSensor, SensorDelay.Game);
                else
                {
                    if (_accelerometer != null)
                        _sensorManager.RegisterListener(this, _accelerometer, SensorDelay.Ui);
                    if (_magnetometer != null)
                        _sensorManager.RegisterListener(this, _magnetometer, SensorDelay.Ui);
                }
            }

            _isStarted = true;
        }
        private bool _isStarted;


        public void Pause()
        {
            if (!_isStarted)
                return;

            if (_sensorManager != null)
            {
                _sensorManager.UnregisterListener(this);
            }

            _isStarted = false;
        }

        private float[]? _lastAccelerometer;
        private float[]? _lastMagnetometer;

        SensorStatus _currentAccuracy;

        void ISensorEventListener.OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy)
        {
            _currentAccuracy = accuracy;
        }

        void ISensorEventListener.OnSensorChanged(SensorEvent? e)
        {
            if (e is null)
                return;
            float[]? rotationMatrix = null;

            if (e.Sensor == _rotationSensor)
            {
                var rotationVector = e.Values.ToArray();
                rotationMatrix = new float[9];
                SensorManager.GetRotationMatrixFromVector(rotationMatrix, rotationVector);
            }
            else if (e.Sensor == _accelerometer)
            {
                _lastAccelerometer = e.Values.ToArray();
            }
            else if (e.Sensor == _magnetometer)
            {
                _lastMagnetometer = e.Values.ToArray();
            }
            if (_lastAccelerometer != null && _lastMagnetometer != null)
            {
                rotationMatrix = new float[9];
                float[] magnetic = new float[9];
                SensorManager.GetRotationMatrix(rotationMatrix, magnetic, _lastAccelerometer, _lastMagnetometer);
                _lastMagnetometer = null;
                _lastAccelerometer = null;
            }
            if(rotationMatrix != null)
            {
                Android.Hardware.Axis ax, ay;
                switch (_windowManager?.DefaultDisplay?.Rotation)
                {
                    case SurfaceOrientation.Rotation90:
                        ax = Android.Hardware.Axis.Z;
                        ay = Android.Hardware.Axis.MinusX;
                        break;
                    case SurfaceOrientation.Rotation180:
                        ax = Android.Hardware.Axis.MinusX;
                        ay = Android.Hardware.Axis.MinusZ;
                        break;
                    case SurfaceOrientation.Rotation270:
                        ax = Android.Hardware.Axis.MinusZ;
                        ay = Android.Hardware.Axis.X;
                        break;
                    case SurfaceOrientation.Rotation0:
                    default:
                        ax = Android.Hardware.Axis.X;
                        ay = Android.Hardware.Axis.Z;
                        break;
                }
                float[] adjustedRotationMatrix = new float[9];
                SensorManager.RemapCoordinateSystem(rotationMatrix, ax, ay, adjustedRotationMatrix);
                // Transform rotation matrix into azimuth/pitch/roll
                float[] orientation = new float[3];
                SensorManager.GetOrientation(adjustedRotationMatrix, orientation);

                OrientationChanged?.Invoke(this, new CompassOrientationEventArgs()
                {
                    Transformation = adjustedRotationMatrix,
                    Orientation = orientation,
                    Accuracy = _currentAccuracy
                });
            }
        }
        public event EventHandler<CompassOrientationEventArgs>? OrientationChanged;
    }

    internal struct CompassOrientationEventArgs
    {
        public float[] Transformation { get; set; }

        public float[] Orientation { get; set; }

        public SensorStatus Accuracy { get; set; }
        /// <summary>
        /// Azimuth, angle of rotation about the -z axis. This value represents the angle between the device's y axis and the magnetic
        /// north pole. When facing north, this angle is 0, when facing south, this angle is π. Likewise, when facing east, this angle
        /// is π/2, and when facing west, this angle is -π/2. The range of values is -π to π.
        /// </summary>
        public float Azimuth => (Orientation[0] * 57.2957795f);
        /// <summary>
        ///  Pitch, angle of rotation about the x axis. This value represents the angle between a plane parallel to the device's 
        ///  screen and a plane parallel to the ground. Assuming that the bottom edge of the device faces the user and that the
        ///  screen is face-up, tilting the top edge of the device toward the ground creates a positive pitch angle. The range
        ///  of values is -π to π.
        /// </summary>
        public float Pitch => Orientation[1] * 57.2957795f;
        /// <summary>
        /// Roll, angle of rotation about the y axis. This value represents the angle between a plane perpendicular to the device's screen and a
        /// plane perpendicular to the ground. Assuming that the bottom edge of the device faces the user and that the screen is face-up, tilting
        /// the left edge of the device toward the ground creates a positive roll angle. The range of values is -π/2 to π/2.
        /// </summary>
        public float Roll => Orientation[2] * 57.2957795f;
    }
}
#endif