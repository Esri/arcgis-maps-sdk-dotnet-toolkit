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

#if NETFX_CORE
using System;
using System.Linq;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Windows.Devices.Sensors;
using Windows.Media.Capture;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Esri.ArcGISRuntime.ARToolkit
{
    public partial class ARSceneView : SceneView
    {
        private CaptureElement _cameraView;
        private OrientationSensor _sensor;
        private MediaCapture _mediaCapture;
        private bool _isLoaded;
        private bool _isTracking;

        private void Initialize()
        {
            _isTracking = true;
            // IsManualRendering = true;
            Loaded += ARSceneView_Loaded;
            Unloaded += ARSceneView_Unloaded;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var elm = GetTemplateChild("MapSurface") as FrameworkElement;
            if (elm != null && elm.Parent is Panel parent)
            {
                _cameraView = new CaptureElement() { Stretch = Windows.UI.Xaml.Media.Stretch.Uniform };
                parent.Children.Insert(parent.Children.IndexOf(elm), _cameraView);
            }
        }

        private void OnStartTracking()
        {
            if (_isTracking)
            {
                return;
            }

            if (_isLoaded)
            {
                InitializeTracker();
            }

            _isTracking = true;
        }

        private void OnStopTracking()
        {
            if (!_isTracking)
            {
                return;
            }

            DisposeTracking();
            _isTracking = false;
        }

        private void CompositionTarget_Rendering(object sender, object e)
        {
            // TODO: Use Camera frame sync instead
            RenderFrame();
        }

        private void ARSceneView_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            if (_isTracking)
            {
                InitializeTracker();
            }

            if (IsManualRendering)
            {
                Windows.UI.Xaml.Media.CompositionTarget.Rendering += CompositionTarget_Rendering;
            }
        }

        private void InitializeTracker()
        {
            _sensor = OrientationSensor.GetDefault();
            if (_sensor == null)
            {
                throw new NotSupportedException("No Orientation Sensor detected");
            }

            _sensor.ReadingChanged += Sensor_ReadingChanged;
            StartCapturing();
        }

        private void ARSceneView_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
            if (_isTracking)
            {
                DisposeTracking();
            }

            if (IsManualRendering)
            {
                Windows.UI.Xaml.Media.CompositionTarget.Rendering -= CompositionTarget_Rendering;
            }
        }

        private void DisposeTracking()
        {
            if (_sensor != null)
            {
                _sensor.ReadingChanged -= Sensor_ReadingChanged;
            }

            _sensor = null;
            // Windows.UI.Xaml.Media.CompositionTarget.Rendering -= CompositionTarget_Rendering;
            if (_cameraView != null)
            {
                _cameraView.Source = null;
            }

            _mediaCapture?.Dispose();
        }

        private void Sensor_ReadingChanged(OrientationSensor sender, OrientationSensorReadingChangedEventArgs args)
        {
            var c = Camera;
            if (c == null)
            {
                return;
            }

            var l = c.Transformation;
            var q = args.Reading.Quaternion;

            if (_controller.OriginCamera == null)
            {
                _controller.OriginCamera = new Esri.ArcGISRuntime.Mapping.Camera(c.Location, c.Heading, 90, 0);
                OriginCameraChanged?.Invoke(this, EventArgs.Empty);
            }

            _controller.TransformationMatrix = InitialTransformation + TransformationMatrix.Create(q.X, q.Y, q.Z, q.W, 0, 0, 0);
        }

        private async void StartCapturing()
        {
            if (_cameraView == null)
            {
                return;
            }

            var allVideoDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.VideoCapture);
            var desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null
                && x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);
            var cameraDevice = desiredDevice ?? allVideoDevices.FirstOrDefault();
            var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

            _mediaCapture = new MediaCapture();
            try
            {
                await _mediaCapture.InitializeAsync(settings);
            }
            catch (UnauthorizedAccessException)
            {
                // Access denied to media capture (requires webcam + microphone access
                return;
            }

            _cameraView.Source = _mediaCapture;
            await _mediaCapture.StartPreviewAsync();
        }

        private TransformationMatrix HitTest(Windows.Foundation.Point screenPoint)
        {
            throw new NotSupportedException();
        }
    }
}
#endif