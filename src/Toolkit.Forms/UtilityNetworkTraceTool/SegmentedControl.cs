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
using System;
using System.Linq;
using Xamarin.Forms;
using Rectangle = Xamarin.Forms.Shapes.Rectangle;
using Rectangle2 = Xamarin.Forms.Rectangle;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Primitives
{
    /// <summary>
    /// Internal control used by the <see cref="UtilityNetworkTraceTool"/>.
    /// </summary>
    public class SegmentedControl : Layout<View>
    {
        private Rectangle _selectionFill;
        private Rectangle _backgroundFill;
        private Label[]? _segmentLabels;

        private double _lastHeight;
        private double _lastWidth;
        private int _lastSelection = -1;
        private TapGestureRecognizer _tapGesture;

        /// <summary>
        /// Initializes a new instance of the <see cref="SegmentedControl"/> class.
        /// </summary>
        public SegmentedControl()
        {
            _selectionFill = new Rectangle
            {
                RadiusX = 6.0,
                RadiusY = 6.0,
            };

            _backgroundFill = new Rectangle
            {
                RadiusX = 6.0,
                RadiusY = 6.0,
            };
            _backgroundFill.SetOnAppTheme<Brush>(Rectangle.FillProperty, new SolidColorBrush(System.Drawing.Color.FromArgb(234, 234, 234)), new SolidColorBrush(System.Drawing.Color.FromArgb(21, 21, 21)));
            _selectionFill.SetOnAppTheme<Brush>(Rectangle.FillProperty, new SolidColorBrush(System.Drawing.Color.FromArgb(0, 122, 194)), new SolidColorBrush(System.Drawing.Color.FromArgb(0, 154, 242)));

            Children.Add(_backgroundFill);
            Children.Add(_selectionFill);

            SelectedSegmentIndex = 0;
            _tapGesture = new TapGestureRecognizer();
            _tapGesture.Tapped += HandleLabelTap;
            Segments = new[] { "Select", "Configure", "Run", "View" };
        }

        /// <summary>
        /// Gets or sets the segment titles.
        /// </summary>
        public string[] ? Segments
        {
            get => GetValue(SegmentsProperty) as string[];
            set => SetValue(SegmentsProperty, value);
        }

        /// <summary>
        /// Gets or sets the index of the currently selected segment.
        /// </summary>
        public int SelectedSegmentIndex
        {
            get => (int)GetValue(SelectedSegmentIndexProperty);
            set => SetValue(SelectedSegmentIndexProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Segments"/> bindable property.
        /// </summary>
        public static readonly BindableProperty SegmentsProperty = BindableProperty.Create(nameof(Segments), typeof(string[]), typeof(SegmentedControl), propertyChanged: OnSegmentsChanged);

        /// <summary>
        /// Identifies the <see cref="SelectedSegmentIndex"/> bindable property.
        /// </summary>
        public static readonly BindableProperty SelectedSegmentIndexProperty = BindableProperty.Create(nameof(SelectedSegmentIndex), typeof(int), typeof(SegmentedControl), defaultValue: 0, propertyChanged: OnSelectionChanged);

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
            (sender as SegmentedControl)?.InvalidateLayout();
        }

        private static void OnSegmentsChanged(BindableObject sender, object oldValue, object newValue)
        {
            if (sender is SegmentedControl sendingControl)
            {
                var oldChildren = sendingControl.Children.OfType<Label>().ToList();
                foreach (var child in oldChildren)
                {
                    child.GestureRecognizers.Clear();
                    sendingControl.Children.Remove(child);
                }

                sendingControl._segmentLabels = sendingControl.Segments.Select(segTitle => new Label() { Text = segTitle, VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.Center }).ToArray();

                foreach (var label in sendingControl._segmentLabels)
                {
                    label.GestureRecognizers.Add(sendingControl._tapGesture);
                    sendingControl.Children.Add(label);
                }

                sendingControl.InvalidateLayout();
            }
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        protected override void LayoutChildren(double x, double y, double width, double height)
        {
            if (height < 0 || width < 0 || _segmentLabels == null)
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
            double widthPerUnselectedSegment;
            double widthForSelectedSegment = widthPerUnselectedSegment = Width / _segmentLabels.Count();

            // Divide segments
            int labelIndex = 0;
            double lastXPosition = 0;
            double animationXStart = 0;

            foreach (var label in _segmentLabels)
            {
                if (labelIndex == SelectedSegmentIndex)
                {
                    label.WidthRequest = widthForSelectedSegment;
                    label.Layout(new Rectangle2(lastXPosition, 0, widthForSelectedSegment, availableHeight));
                    animationXStart = lastXPosition;
                    lastXPosition += widthForSelectedSegment;
                    label.TextColor = Color.White;
                }
                else
                {
                    label.Layout(new Rectangle2(lastXPosition, 0, widthPerUnselectedSegment, availableHeight));
                    lastXPosition += widthPerUnselectedSegment;
                    label.SetAppThemeColor(Label.TextColorProperty, Color.FromRgb(21, 21, 21), Color.White);
                }

                labelIndex++;
            }

            _backgroundFill.Layout(new Rectangle2(x, y, width, height));
            _selectionFill.LayoutTo(new Rectangle2(animationXStart, 0, widthForSelectedSegment, availableHeight), 150, Easing.CubicInOut);
        }
    }
}
