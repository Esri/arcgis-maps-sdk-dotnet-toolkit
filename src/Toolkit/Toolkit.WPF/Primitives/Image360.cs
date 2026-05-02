using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Esri.ArcGISRuntime.Toolkit
{
    /// <summary>
    /// 360 Panorama Image Control
    /// </summary>
    public class Image360 : Control
    {
        private readonly GeometryModel3D _model;
        private Point _lastMousePosition;
        private double _yaw;
        private double _pitch;
        private PerspectiveCamera _camera = new PerspectiveCamera
            {
                Position = new Point3D(0, 0, 0),
                LookDirection = new Vector3D(0, 0, 1),
                UpDirection = new Vector3D(0, 1, 0),
                FieldOfView = 90
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="Image360"/> class.
        /// </summary>
        public Image360()
        {
            DefaultStyleKey = typeof(Image360);
            _model = new GeometryModel3D()
            {
                Geometry = CreateSphereMesh()
            };
            InitializeAnglesFromLookDirection();
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild("Viewport") is Viewport3D viewport)
            {
                InitializeScene(viewport);
            }
        }

        private void InitializeScene(Viewport3D viewport)
        {
            viewport.Camera = _camera;
            var ambientLight = new AmbientLight(Colors.White);
            viewport.Children.Add(new ModelVisual3D { Content = ambientLight });
            var modelVisual3D = new ModelVisual3D { Content = _model };
            viewport.Children.Add(modelVisual3D);
        }

        private static MeshGeometry3D CreateSphereMesh()
        {
            const int size = 30;
            var mesh = new MeshGeometry3D();

            for (int i = 0; i <= size; i++)
            {
                double phi = Math.PI / size * i;
                for (int j = 0; j <= size * 2; j++)
                {
                    double theta = 2 * Math.PI / (size * 2d) * j;
                    double x = Math.Sin(phi) * Math.Cos(theta);
                    double y = Math.Cos(phi);
                    double z = Math.Sin(phi) * Math.Sin(theta);
                    mesh.Positions.Add(new Point3D(x, y, z));
                    mesh.TextureCoordinates.Add(new Point(j / (size * 2d), phi / Math.PI));
                }
            }
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size * 2; y++)
                {
                    int v0 = x * (size * 2 + 1) + y;
                    int v1 = (x + 1) * (size * 2 + 1) + y;
                    int v2 = x * (size * 2 + 1) + y + 1;
                    int v3 = (x + 1) * (size * 2 + 1) + y + 1;
                    mesh.TriangleIndices.Add(v0);
                    mesh.TriangleIndices.Add(v2);
                    mesh.TriangleIndices.Add(v1);
                    mesh.TriangleIndices.Add(v2);
                    mesh.TriangleIndices.Add(v3);
                    mesh.TriangleIndices.Add(v1);
                }
            }
            return mesh;

        }

        /// <summary>
        /// Gets or sets the source image displayed by the control.
        /// </summary>
        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Identifies the Source dependency property for the Image360 control.
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(Image360), new PropertyMetadata(null, (s, e) => ((Image360)s).OnSourcePropertyChanged()));

        private void OnSourcePropertyChanged()
        {
            if (Source is not null)
            {
                var brush = new ImageBrush(Source)
                {
                    ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
                    TileMode = TileMode.None,
                    Stretch = Stretch.Fill
                };
                _model.BackMaterial = new DiffuseMaterial(brush);
            }
            else
            {
                _model.BackMaterial = null;
            }
        }

        /// <inheritdoc />
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(this);
                _lastMousePosition = pos;
                Mouse.Capture(this);
            }
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var mousePos = e.GetPosition(this);
                double deltaX = mousePos.X - _lastMousePosition.X;
                double deltaY = _lastMousePosition.Y - mousePos.Y;
                Pan(deltaX * .25, deltaY * .25);
                _lastMousePosition = mousePos;
            }
        }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            Mouse.Capture(null);
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            double rotationDelta = 2.0;
            switch (e.Key)
            {
                case Key.Left:
                    Pan(rotationDelta, 0);
                    break;
                case Key.Right:
                    Pan(-rotationDelta, 0);
                    break;
                case Key.Up:
                    Pan(0, -rotationDelta);
                    break;
                case Key.Down:
                    Pan(0, rotationDelta);
                    break;
                case Key.OemPlus:
                case Key.Add:
                    Zoom(0.9);
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    Zoom(1.1);
                    break;
            }
        }

        /// <inheritdoc />
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            Zoom(e.Delta > 0 ? 0.9 : 1.1);
        }

        private void Zoom(double amount)
        {
            var fov = _camera.FieldOfView * amount;
            _camera.FieldOfView = Math.Min(120, Math.Max(15, fov));
        }

        private void Pan(double x, double y)
        {
            _yaw += x;
            _pitch = Math.Min(89.9, Math.Max(-89.9, _pitch + y));

            var yawRadians = _yaw * Math.PI / 180.0;
            var pitchRadians = _pitch * Math.PI / 180.0;
            var cosPitch = Math.Cos(pitchRadians);

            _camera.LookDirection = new Vector3D(
                cosPitch * Math.Sin(yawRadians),
                Math.Sin(pitchRadians),
                cosPitch * Math.Cos(yawRadians));
            _camera.UpDirection = new Vector3D(0, 1, 0);
        }

        private void InitializeAnglesFromLookDirection()
        {
            var direction = _camera.LookDirection;
            if (direction.LengthSquared <= double.Epsilon)
            {
                _yaw = 0;
                _pitch = 0;
                return;
            }

            direction.Normalize();
            _yaw = Math.Atan2(direction.X, direction.Z) * 180.0 / Math.PI;
            _pitch = Math.Asin(Math.Max(-1.0, Math.Min(1.0, direction.Y))) * 180.0 / Math.PI;
        }

        /// <summary>
        /// Given a screen coordinate, returns the corresponding location on the physical image normalized to 0..1.
        /// </summary>
        /// <param name="point">The screen coordinate.</param>
        /// <returns>The normalized location on the physical image.</returns>
        public Point GetLocation(Point point)
        {
            // Based on screen location, scale and rotation, calculate the corresponding location on physical image normalized to 0..1
            var width = this.ActualWidth;
            var height = this.ActualHeight;
            if (width <= 0 || height <= 0)
            {
                return default;
            }

            var lookDirection = _camera.LookDirection;
            lookDirection.Normalize();

            var upDirection = _camera.UpDirection;
            upDirection.Normalize();

            var right = Vector3D.CrossProduct(lookDirection, upDirection);
            if (right.LengthSquared <= double.Epsilon)
            {
                return default;
            }

            right.Normalize();
            upDirection = Vector3D.CrossProduct(right, lookDirection);
            upDirection.Normalize();

            var aspect = width / height;
            var halfHorizontalFov = (_camera.FieldOfView * Math.PI / 180.0) * 0.5;
            var halfWidthOnNearPlane = Math.Tan(halfHorizontalFov);
            var halfHeightOnNearPlane = halfWidthOnNearPlane / aspect;

            var normalizedX = (point.X / width) * 2.0 - 1.0;
            var normalizedY = 1.0 - (point.Y / height) * 2.0;

            var rayDirection = lookDirection
                + (right * (normalizedX * halfWidthOnNearPlane))
                + (upDirection * (normalizedY * halfHeightOnNearPlane));
            rayDirection.Normalize();

            var phi = Math.Acos(Math.Max(-1.0, Math.Min(1.0, rayDirection.Y)));
            var theta = Math.Atan2(rayDirection.Z, rayDirection.X);
            if (theta < 0)
            {
                theta += Math.PI * 2.0;
            }

            var u = theta / (Math.PI * 2.0);
            var v = phi / Math.PI;

            return new Point(u, v);
        }

    }
}