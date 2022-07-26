using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Shapes;
using Rectangle = Xamarin.Forms.Shapes.Rectangle;
using Rectangle2 = Xamarin.Forms.Rectangle;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Primitives
{
    public class SegmentedControl : Layout<View>
    {
        private Rectangle _selectionFill;
        private Rectangle _backgroundFill;
        private Label[] _segmentLabels;

        private double _lastHeight;
        private double _lastWidth;
        private int _lastSelection = -1;
        private TapGestureRecognizer _tapGesture;

        public SegmentedControl()
        {
            _selectionFill = new Rectangle
            {
                Fill = new SolidColorBrush(System.Drawing.Color.FromArgb(243, 243, 243)),
                RadiusX = 6.0,
                RadiusY = 6.0
            };

            _backgroundFill = new Rectangle
            {
                Fill = new SolidColorBrush(System.Drawing.Color.FromArgb(168, 168, 168)),
                RadiusX = 6.0,
                RadiusY = 6.0,
            };
            Children.Add(_backgroundFill);
            Children.Add(_selectionFill);

            SelectedSegmentIndex = 0;
            _tapGesture = new TapGestureRecognizer();
            _tapGesture.Tapped += HandleLabelTap;
            Segments = new[] { "Select", "Configure", "Run", "View" };
        }

        public string[]? Segments
        {
            get => GetValue(SegmentsProperty) as string[];
            set => SetValue(SegmentsProperty, value);
        }

        public int SelectedSegmentIndex
        {
            get => (int)GetValue(SelectedSegmentIndexProperty);
            set => SetValue(SelectedSegmentIndexProperty, value);
        }

        public static BindableProperty SegmentsProperty = BindableProperty.Create(nameof(Segments), typeof(string[]), typeof(SegmentedControl), propertyChanged: OnSegmentsChanged);
        public static BindableProperty SelectedSegmentIndexProperty = BindableProperty.Create(nameof(SelectedSegmentIndex), typeof(int), typeof(SegmentedControl), propertyChanged: OnSelectionChanged);

        private void HandleLabelTap(object sender, EventArgs e)
        {
            if (sender is Label labelElement && _segmentLabels.Contains(labelElement))
            {
                int index = Array.IndexOf(_segmentLabels, labelElement);
                SelectedSegmentIndex = index;
            }
        }

        private static void OnSelectionChanged(BindableObject sender, object oldValue, object newValue)
        {
            (sender as SegmentedControl).InvalidateLayout();
        }

        private static void OnSegmentsChanged(BindableObject sender, object oldValue, object newValue)
        {
            if (sender is SegmentedControl sendingControl)
            {
                var oldChildren = sendingControl.Children.OfType<Label>().ToList();
                foreach(var child in oldChildren)
                {
                    child.GestureRecognizers.Clear();
                    sendingControl.Children.Remove(child);
                }
                sendingControl._segmentLabels = sendingControl.Segments.Select(segTitle => new Label() { Text = segTitle, VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.Center }).ToArray();
                foreach(var label in sendingControl._segmentLabels)
                {
                    label.GestureRecognizers.Add(sendingControl._tapGesture);
                    sendingControl.Children.Add(label);
                }
                sendingControl.InvalidateLayout();
            }
        }

        protected override void LayoutChildren(double x, double y, double width, double height)
        {
            if (height< 0 || width < 0)
            {
                return;
            }

            if (SelectedSegmentIndex == _lastSelection && Height == _lastHeight && Width == _lastWidth)
            {
                return;
            }
            else
            {
                _lastSelection = SelectedSegmentIndex;
                _lastHeight = height;
                _lastWidth = width;
            }

            double availableHeight = Height;
            double availableWidth = Width;
            double widthForSelectedSegment = 0.0;
            double widthPerUnselectedSegment = 0.0;

            widthForSelectedSegment = widthPerUnselectedSegment = Width / _segmentLabels.Count();

            // Divide segments
            int _labelIndex = 0;
            double _lastXPosition = 0;
            double animationXStart = 0;

            foreach (var label in _segmentLabels)
            {
                if (_labelIndex == SelectedSegmentIndex)
                {
                    label.WidthRequest = widthForSelectedSegment;
                    label.Layout(new Rectangle2(_lastXPosition, 0, widthForSelectedSegment, availableHeight));
                    animationXStart = _lastXPosition;
                    _lastXPosition += widthForSelectedSegment;
                }
                else
                {
                    label.Layout(new Rectangle2(_lastXPosition, 0, widthPerUnselectedSegment, availableHeight));
                    _lastXPosition += widthPerUnselectedSegment;
                }
                _labelIndex++;
            }
            _backgroundFill.Layout(new Rectangle2(x, y, width, height));
            _selectionFill.LayoutTo(new Rectangle2(animationXStart, 0, widthForSelectedSegment, availableHeight), 150, Easing.CubicInOut);
        }
    }
}

