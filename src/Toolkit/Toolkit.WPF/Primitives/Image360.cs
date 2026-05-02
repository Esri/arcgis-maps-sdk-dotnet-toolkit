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
        private readonly AxisAngleRotation3D _horizontalRot = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
        private readonly AxisAngleRotation3D _verticalRot = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0);
        private readonly ScaleTransform3D _scaleTransform = new ScaleTransform3D(1, 1, 1);
        private readonly GeometryModel3D _model;
        private Point _lastMousePosition;

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
            var transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(new RotateTransform3D(_horizontalRot));
            transformGroup.Children.Add(new RotateTransform3D(_verticalRot));
            transformGroup.Children.Add(_scaleTransform);
            _model.Transform = transformGroup;            
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
            viewport.Camera = new PerspectiveCamera
            {
                Position = new Point3D(0, 0, 0),
                LookDirection = new Vector3D(0, 0, 1),
                UpDirection = new Vector3D(0, 1, 0),
                FieldOfView = 90
            };
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
                _horizontalRot.Angle -= deltaX * 0.25;
                double newVerticalAngle = _verticalRot.Angle - deltaY * 0.25;
                if (newVerticalAngle >= -90 && newVerticalAngle <= 45)
                {
                    _verticalRot.Angle = newVerticalAngle;
                }
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
                    _horizontalRot.Angle -= rotationDelta;
                    break;
                case Key.Right:
                    _horizontalRot.Angle += rotationDelta;
                    break;
                case Key.Up:
                    if (_verticalRot.Angle + rotationDelta <= 45)
                        _verticalRot.Angle += rotationDelta;
                    break;
                case Key.Down:
                    if (_verticalRot.Angle - rotationDelta >= -90)
                        _verticalRot.Angle -= rotationDelta;
                    break;
                case Key.OemPlus:
                case Key.Add:
                    var scale = Math.Min(5, Math.Max(1, _scaleTransform.ScaleX * 1.5));
                    _scaleTransform.ScaleX = _scaleTransform.ScaleY = scale;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    scale = Math.Min(5, Math.Max(1, _scaleTransform.ScaleX * .667));
                    _scaleTransform.ScaleX = _scaleTransform.ScaleY = scale;
                    break;
            }
        }

        /// <inheritdoc />
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            var scale = Math.Min(5, Math.Max(1, _scaleTransform.ScaleX * (e.Delta > 0 ? 1.1 : 0.9)));
            _scaleTransform.ScaleX = _scaleTransform.ScaleY = scale;
        }
    }
}