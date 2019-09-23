// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

#if __ANDROID__
using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Opengl;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Google.AR.Core;
using Javax.Microedition.Khronos.Opengles;

namespace Esri.ArcGISRuntime.ARToolkit
{
    [Android.Runtime.Register("Esri.ArcGISRuntime.ARToolkit.ARSceneView")]
    public partial class ARSceneView : SceneView
    {
        private class UpdateListener : Java.Lang.Object, Google.AR.Sceneform.Scene.IOnUpdateListener
        {
            private readonly Action<Google.AR.Sceneform.FrameTime> _onUpdate;

            public UpdateListener(Action<Google.AR.Sceneform.FrameTime> onUpdate)
            {
                _onUpdate = onUpdate ?? throw new ArgumentNullException(nameof(onUpdate));
            }

            void Google.AR.Sceneform.Scene.IOnUpdateListener.OnUpdate(Google.AR.Sceneform.FrameTime p0) => _onUpdate(p0);
        }

        private class OrientationListener : Android.Views.OrientationEventListener
        {
            private readonly Context _context;
            private Android.Views.IWindowManager _windowManager;
            private UI.DeviceOrientation _currentOrientation = UI.DeviceOrientation.Portrait;
            
            public UI.DeviceOrientation CurrentOrientation
            {
                get => _currentOrientation;
                private set
                {
                    if (value != _currentOrientation)
                    {
                        _currentOrientation = value;
                        OrientationChanged?.Invoke(this, _currentOrientation);
                    }
                }
            }

            public OrientationListener(Context context) : base(context, Android.Hardware.SensorDelay.Normal)
            {
                _context = context;
            }

            public override void Enable()
            {
                _windowManager = _context.GetSystemService(Context.WindowService).JavaCast<Android.Views.IWindowManager>();
                CurrentOrientation = ToDeviceOrientation(WindowOrientation);
                base.Enable();
            }

            public override void OnOrientationChanged(int orientation)
            {
                // We are ignoring the orientation value supplied by it as it doesn't
                // reflect the orientation of the Window at all times. Instead we are making a call to the WindowManager to retrieve
                // the orientation it reports.
                CurrentOrientation = ToDeviceOrientation(WindowOrientation);
            }
            private Android.Views.SurfaceOrientation WindowOrientation
            {
                get => _windowManager?.DefaultDisplay?.Rotation ?? Android.Views.SurfaceOrientation.Rotation0;
            }


            private static UI.DeviceOrientation ToDeviceOrientation(Android.Views.SurfaceOrientation orientation)
            {
                switch (orientation)
                {
                    case Android.Views.SurfaceOrientation.Rotation90:
                        return UI.DeviceOrientation.LandscapeRight;
                    case Android.Views.SurfaceOrientation.Rotation180:
                        return UI.DeviceOrientation.ReversePortrait; 
                    case Android.Views.SurfaceOrientation.Rotation270:
                        return UI.DeviceOrientation.LandscapeRight;
                    case Android.Views.SurfaceOrientation.Rotation0:
                    default:
                        return UI.DeviceOrientation.Portrait;
                }
                
            }

            public event EventHandler<UI.DeviceOrientation> OrientationChanged;
        }

        private OrientationListener _orientationListener;
        private UpdateListener _updateListener;
        private UI.DeviceOrientation _screenOrientation;
        private CompassOrientationHelper _compassListener; //Used for getting heading, and orientation if ARCore is disabled
        private double? _initialHeading;
        private Android.Views.View _sceneviewSurface;

        /// <summary>
        /// Initializes a new instance of the <see cref="ARSceneView"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        public ARSceneView(Android.Content.Context context)
            : base(context)
        {
            InitializeCommon();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ARSceneView"/> class.
        /// </summary>
        /// <param name="context"> The Context the view is running in, through which it can access resources, themes, etc.</param>
        /// <param name="attr">The attributes of the AXML element declaring the view.</param>
        public ARSceneView(Context context, Android.Util.IAttributeSet attr)
            : base(context, attr)
        {
            using (var style = context.Theme.ObtainStyledAttributes(attr, Resource.Styleable.ArcGISARSceneView, 0, 0))
            {
                RenderVideoFeed = style.GetBoolean(Resource.Styleable.ArcGISARSceneView_renderVideoFeed, true);
                NorthAlign = style.GetBoolean(Resource.Styleable.ArcGISARSceneView_northAlign, true);
            }
            InitializeCommon();
        }

        private bool IsDesignTime => IsInEditMode;

        Google.AR.Sceneform.ArSceneView _arSceneView;
        private void Initialize()
        {
            if (IsDesignTime)
                return;
            CheckArCoreAvailability();
            _updateListener = new UpdateListener(OnFrameUpdated);
            _orientationListener = new OrientationListener(Context);
            _orientationListener.OrientationChanged += (s, orientation) => _screenOrientation = orientation;
            _compassListener = new CompassOrientationHelper(Context);
            _compassListener.OrientationChanged += OrientationHelper_OrientationChanged;

            _sceneviewSurface = GetChildAt(0);

            _arSceneView = new Google.AR.Sceneform.ArSceneView(Context);
            AddViewInLayout(_arSceneView, 0, new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent));
            // Tell the SceneView we will be calling `RenderFrame()` manually if we're using ARCore.
            IsManualRendering = IsUsingARCore;
        }

        private void OrientationHelper_OrientationChanged(object sender, CompassOrientationEventArgs e)
        {
            if (e.Accuracy == Android.Hardware.SensorStatus.NoContact)
                return;
            // Keep track of initial heading to oriente scene correctly as ARCore doesn't provide global heading accuracy.
            if (!_initialHeading.HasValue)
            {
                _initialHeading = e.Azimuth;

                if (IsUsingARCore && NorthAlign)
                {
                    var camera = OriginCamera;
                    if (camera != null)
                    {
                        OriginCamera = new Mapping.Camera(camera.Location, _initialHeading.Value, camera.Pitch, camera.Roll);
                        _initialHeading = e.Azimuth;
                    }
                }
            }

            if (!IsUsingARCore)
            {
                // Use orientation sensor instead of ARCore
                var m = e.Transformation;
                var qw = Math.Sqrt(1 + m[0] + m[4] + m[8]) / 2;
                var qx = (m[7] - m[5]) / (4 * qw);
                var qy = (m[2] - m[6]) / (4 * qw);
                var qz = (m[3] - m[1]) / (4 * qw);
                _controller.TransformationMatrix = InitialTransformation + TransformationMatrix.Create(qx, qz, -qy, qw, 0, 0, 0);
            }
        }

        /// <inheritdoc />
        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            _arSceneView.Layout(left, top, right, bottom);
            base.OnLayout(changed, left, top, right, bottom);
        }

        /// <summary>
        /// Gets a value indicating whether ARCore should be used for tracking the device movements
        /// </summary>
        public bool IsUsingARCore { get; private set; } = true;

        /// <summary>
        /// Gets the AR SurfaceView that integrates with ARCore and renders a scene.
        /// </summary>
        public Google.AR.Sceneform.ArSceneView ArSceneView => _arSceneView;
        
        /// <summary>
        /// Gets or sets a value indicating whether the scene should attempt to use the device compass to align the scene towards north.
        /// </summary>
        /// <remarks>
        /// Note that the accuracy of the compass can heavily affect the quality of a good world alignment.
        /// </remarks>
        public bool NorthAlign { get; set; } = true;

        private bool RequestLocationPermission()
        {
            if (!(Context is Android.App.Activity activity))
            {
                // Throw exception if Context is not an instance of Activity as it's required for permission request
                throw new NotSupportedException("Context must be an instance of Activity");
            }
            if (ContextCompat.CheckSelfPermission(activity, Android.Manifest.Permission.AccessFineLocation) != Android.Content.PM.Permission.Granted)
            {
                ActivityCompat.RequestPermissions(activity, new string[] { Android.Manifest.Permission.AccessFineLocation }, 0);
                return false;
            }
            return true;
        }

        private void StartArCoreSession()
        {
            if(!(Context is Android.App.Activity activity))
            {
                // Throw exception if Context is not an instance of Activity as it's required for permission request
                throw new NotSupportedException("Context must be an instance of Activity");
            }

            if(IsUsingARCore && RenderVideoFeed && ContextCompat.CheckSelfPermission(activity, Android.Manifest.Permission.Camera) != Android.Content.PM.Permission.Granted)
            {
                ActivityCompat.RequestPermissions(activity, new string[] { Android.Manifest.Permission.Camera }, 0);
                return;
            }

            if (IsUsingARCore)
            {
                if (_arSceneView?.Session == null)
                {
                    // Create the session
                    var session = new Session(Context);
                    var config = new Config(session);
                    config.SetUpdateMode(Config.UpdateMode.LatestCameraImage);
                    config.SetFocusMode(Config.FocusMode.Auto);
                    session.Configure(config);
                    _arSceneView.SetupSession(session);
                }

                // ensure that OnUpdateListener is added on the UI thread to prevent threading issues with ARCore
                Post(() => _arSceneView.Scene.AddOnUpdateListener(_updateListener));

                _arSceneView.Resume();
            }
        }

        private void OnStopTracking()
        {
            _orientationListener.Disable();
            _compassListener?.Pause();
            if (_arSceneView != null)
            {
                Post(() => _arSceneView.Scene.RemoveOnUpdateListener(_updateListener));
                _arSceneView.Pause();
            }
            _initialHeading = null;
        }

        private void OnStartTracking()
        {
            _initialHeading = null;
            _orientationListener.Enable();
            _compassListener.Resume();
            if (IsUsingARCore)
            {
                StartArCoreSession();
            }
        }

        private void OnResetTracking()
        {
            if (IsUsingARCore)
            {
                StartArCoreSession();
            }
        }

        private void OnFrameUpdated(Google.AR.Sceneform.FrameTime obj)
        {
            var arCamera = _arSceneView?.ArFrame?.Camera;
            if (arCamera != null && IsTracking)
            {
                var rot = arCamera.DisplayOrientedPose.GetRotationQuaternion();
                var tr = arCamera.DisplayOrientedPose.GetTranslation();
                var arCoreTransMatrix = TransformationMatrix.Create(rot[0], rot[1], rot[2], rot[3], tr[0], tr[1], tr[2]);
                _controller.TransformationMatrix = InitialTransformation + arCoreTransMatrix;
                var it = arCamera.ImageIntrinsics;
                var fl = it.GetFocalLength();
                var pp = it.GetPrincipalPoint();
                var id = it.GetImageDimensions();
                SetFieldOfView(fl[0], fl[1], pp[0], pp[1], id[0], id[1], _screenOrientation);
                if (IsManualRendering)
                {
                    RenderFrame();
                }
            }
        }

        private TransformationMatrix HitTest(Android.Graphics.PointF screenPoint)
        {
            if (!IsUsingARCore)
                throw new InvalidOperationException("HitTest not supported when ARCore is disabled");
            var frame = _arSceneView?.ArFrame;
            var camera = frame?.Camera;
            if (camera != null && camera.TrackingState == TrackingState.Tracking)
            {
                var hitResults = frame.HitTest(screenPoint.X, screenPoint.Y);
                foreach (var item in hitResults)
                {
                    if (item.Trackable is Plane pl && pl.IsPoseInPolygon(item.HitPose) ||
                        item.Trackable is Point pnt && pnt.GetOrientationMode() == Point.OrientationMode.EstimatedSurfaceNormal)
                    {
                        var q = item.HitPose.GetRotationQuaternion();
                        var t = item.HitPose.GetTranslation();
                        return TransformationMatrix.Create(q[0], q[1], q[2], q[3], t[0], t[1], t[2]);
                    }
                }
            }

            return null;
        }

#region ARCore Checkers/Install


        private void CheckArCoreAvailability()
        {
            ArCoreAvailability = ArCoreApk.Instance.CheckAvailability(Context);
        }

        /// <summary>
        /// A value defining whether a request for ARCore has been made. Used when requesting installation of ARCore.
        /// </summary>
        private bool _arCoreInstallRequested;

        /// <summary>
        /// Requests installation of ARCore using ArCoreApk. Should only be called once we know the device is supported by ARCore.
        /// </summary>
        private void RequestArCoreInstall(Android.App.Activity activity)
        {
            try
            {
                if (ArCoreApk.Instance.RequestInstall(activity, !_arCoreInstallRequested) == ArCoreApk.InstallStatus.InstallRequested)
                {
                    _arCoreInstallRequested = true;
                    return;
                }
            }
            catch (Exception)
            {
            }
        }

        private ArCoreApk.Availability _ArCoreAvailability;

        private ArCoreApk.Availability ArCoreAvailability
        {
            get => _ArCoreAvailability;
            set
            {
                if (value != _ArCoreAvailability)
                {
                    var newValue = _ArCoreAvailability = value;
                    if (newValue == ArCoreApk.Availability.UnknownChecking)
                    {
                        _ = CheckArCoreJob();
                    }
                    else if (newValue == ArCoreApk.Availability.SupportedInstalled)
                    {
                        IsUsingARCore = true;
                    }
                    else if (newValue == ArCoreApk.Availability.SupportedNotInstalled ||
                        newValue == ArCoreApk.Availability.SupportedApkTooOld)
                    {
                        RequestArCoreInstall(Context as Android.App.Activity);
                    }
                    else
                    {
                        IsUsingARCore = false;
                    }
                }
            }
        }

        private Task _checkArCoreJob;

        /// <summary>
        /// A background task used to poll ArCoreApk to set the value of ARCore availability for the current device.
        /// </summary>
        /// <remarks>
        /// The ArCoreApk.getInstance().checkAvailability() function may initiate a query to a remote service to determine compatibility, in which case
        /// it immediately returns ArCoreApk.Availability.UNKNOWN_CHECKING.This leaves us unable to determine if the device
        /// is compatible with ARCore until the value is retrieved.See: https://developers.google.com/ar/reference/java/arcore/reference/com/google/ar/core/ArCoreApk#checkAvailability(android.content.Context)        ///
        /// We should not be calling ArCoreApk.getInstance().requestInstall() until we've received one of the SUPPORTED_...
        /// values so this job allows us to do this. See: https://developers.google.com/ar/reference/java/arcore/reference/com/google/ar/core/ArCoreApk#requestInstall(android.app.Activity,%20boolean)
        /// </remarks>
        /// <returns></returns>
        private Task CheckArCoreJob()
        {
            if (_checkArCoreJob == null)
            {
                _checkArCoreJob = Task.Run(async () =>
                {
                    while (ArCoreApk.Instance.CheckAvailability(Context) == ArCoreApk.Availability.UnknownChecking)
                    {
                        await Task.Delay(100);
                    }
                    ArCoreAvailability = ArCoreApk.Instance.CheckAvailability(Context);
                });
            }

            return _checkArCoreJob;
        }

#endregion
    }
}
#endif