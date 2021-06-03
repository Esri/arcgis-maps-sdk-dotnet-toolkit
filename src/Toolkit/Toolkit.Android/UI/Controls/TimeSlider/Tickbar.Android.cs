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
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    [Register("Esri.ArcGISRuntime.Toolkit.Primitives.Tickbar")]
    public partial class Tickbar : FrameLayout
    {
        private int _lastMeasuredWidth = 0;
        private int _lastMeasuredHeight = 0;
        private int _lastLayoutWidth = 0;
        private int _lastLayoutHeight = 0;
        private bool _lastLabelsVisible = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tickbar"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        /// <param name="attr">The attributes of the AXML element declaring the view.</param>
        public Tickbar(Context context, IAttributeSet attr)
            : base(context, attr)
        {
            Initialize();
        }

        private void Initialize()
        {
        }

        private Size _majorTickSize = new Size(1, 10);

        /// <summary>
        /// Gets or sets the size of major tickmarks.
        /// </summary>
        public Size MajorTickSize
        {
            get => _majorTickSize;
            set
            {
                if (value.Width == _majorTickSize.Width && value.Height == _majorTickSize.Height)
                {
                    return;
                }

                _majorTickSize = value;
                foreach (RectangleView tick in _majorTickmarks)
                {
                    tick.Width = _majorTickSize.Width;
                    tick.Height = _majorTickSize.Height;
                }

                InvalidateMeasureAndArrange();
            }
        }

        private Size _minorTickSize = new Size(1, 5);

        /// <summary>
        /// Gets or sets the size of minor tickmarks.
        /// </summary>
        public Size MinorTickSize
        {
            get => _minorTickSize;
            set
            {
                if (value.Width == _minorTickSize.Width && value.Height == _minorTickSize.Height)
                {
                    return;
                }

                _minorTickSize = value;
                foreach (RectangleView tick in _minorTickmarks)
                {
                    tick.Width = _minorTickSize.Width;
                    tick.Height = _minorTickSize.Height;
                }

                InvalidateMeasureAndArrange();
            }
        }

        private double _labelOffset = 4;

        /// <summary>
        /// Gets or sets the spacing between ticks and tick labels.
        /// </summary>
        public double LabelOffset
        {
            get => _labelOffset;
            set
            {
                if (_labelOffset == value)
                {
                    return;
                }

                _labelOffset = value;
                InvalidateMeasureAndArrange();
            }
        }

        private float _tickInset;

        /// <summary>
        /// Gets or sets the amount by which the tick rendering area is offset from the left and right edge of the tickbar.
        /// </summary>
        internal float TickInset
        {
            get => _tickInset;
            set
            {
                _tickInset = value;
                InvalidateMeasureAndArrange();
            }
        }

        /// <summary>
        /// Adds a tick to the bar's visual tree.
        /// </summary>
        /// <param name="position">The position to place the tick at along the tick bar.</param>
        /// <param name="dataSource">The data to pass to the tick's template.</param>
        private void AddTickmark(double position, object dataSource)
        {
            // Create both a minor and major tick mark at the specified position.  Layout logic will determine which
            // one to actually show at the position.

            // Get dimension of ticks in DIPs
            var minorTickWidthDp = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, MinorTickSize.Width, ViewExtensions.GetDisplayMetrics());
            var minorTickHeightDp = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, MinorTickSize.Height, ViewExtensions.GetDisplayMetrics());
            var majorTickWidthDp = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, MajorTickSize.Width, ViewExtensions.GetDisplayMetrics());
            var majorTickHeightDp = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, MajorTickSize.Height, ViewExtensions.GetDisplayMetrics());

            // Create a minor tickmark
            var tick = new View(Context)
            {
                LayoutParameters = new FrameLayout.LayoutParams(minorTickWidthDp, minorTickHeightDp)
                {
                    Gravity = GravityFlags.Bottom,
                },
            };
            tick.SetBackgroundFill(TickFill);
            SetIsMajorTickmark(tick, false);
            SetPosition(tick, position);

            AddView(tick);
            _minorTickmarks.Add(tick);

            // Create a major tickmark
            tick = new View(Context);
            tick.SetBackgroundFill(TickFill);

            if (dataSource is DateTimeOffset dateTime)
            {
                var majorTickContainer = new LinearLayout(Context)
                {
                    Orientation = Orientation.Vertical,
                    LayoutParameters = new FrameLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent)
                    {
                        Gravity = GravityFlags.Bottom,
                    },
                };

                // Create label for major tickmark
                var timeStepIntervalDateFormat = string.IsNullOrEmpty(TickLabelFormat)
                    ? _defaultTickLabelFormat : TickLabelFormat;
                var label = new TextView(Context)
                {
                    Text = dateTime.ToString(timeStepIntervalDateFormat),
                    Tag = dateTime.ToUnixTimeMilliseconds(),
                    LayoutParameters = new LinearLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent),
                };
                label.SetTextColor(TickLabelColor);

                var text = label.Text;
                var color = label.CurrentTextColor;

                tick.LayoutParameters = new LinearLayout.LayoutParams(majorTickWidthDp, majorTickHeightDp)
                {
                    Gravity = GravityFlags.CenterHorizontal,
                };

                majorTickContainer.AddView(label);
                majorTickContainer.AddView(tick);

                // Flag the tick as a major tickmark and set it's proportional position along the tick bar
                SetIsMajorTickmark(majorTickContainer, true);
                SetPosition(majorTickContainer, position);

                AddView(majorTickContainer);
                _majorTickmarks.Add(majorTickContainer);
            }
            else
            {
                tick.LayoutParameters = new FrameLayout.LayoutParams(majorTickWidthDp, majorTickHeightDp)
                {
                    Gravity = GravityFlags.Bottom,
                };
                SetIsMajorTickmark(tick, true);
                SetPosition(tick, position);

                AddView(tick);
                _majorTickmarks.Add(tick);
            }
        }

        private void ApplyTickLabelFormat(View tickContainer, string tickLabelFormat)
        {
            // Retrieve the label from the container holding the major tick rectangle and label
            if (tickContainer is ViewGroup group && group.ChildCount > 1 && group.GetChildAt(0) is TextView label)
            {
                // Apply the specified format to the tick's date and update the label
                var labelFormat = string.IsNullOrEmpty(tickLabelFormat) ? _defaultTickLabelFormat : tickLabelFormat;
                var labelDate = DateTimeOffset.FromUnixTimeMilliseconds((long)label.Tag);
                label.Text = labelDate.ToString(labelFormat);
            }
        }

        private void ApplyTickLabelColor(View tick, Color color)
        {
            if (tick is ViewGroup group && group.ChildCount > 1 && group.GetChildAt(0) is TextView label)
            {
                label.SetTextColor(color);
            }
        }

        private List<View> _children = new List<View>();

        private IEnumerable<View> Children
        {
            get
            {
                _children.Clear();
                for (var i = 0; i < ChildCount; i++)
                {
                    _children.Add(GetChildAt(i));
                }

                return _children;
            }
        }

        private void SetIsMajorTickmark(View view, bool isMajorTickmark)
        {
            UpdatePositionAndIsMajorTickmark(view, GetPosition(view), isMajorTickmark);
        }

        private bool GetIsMajorTickmark(View view)
        {
            return (double)view.Tag % 10 == 0 ? false : true;
        }

        private double GetPosition(View view)
        {
            // Remove last digit as that stores whether the tickmark is major or minor
            var positionDigits = Math.Truncate((double)view.Tag / 10);

            // Convert remaining digits to decimal value between 0 and 1
            return positionDigits / 10000000;
        }

        private void SetPosition(View view, double position)
        {
            UpdatePositionAndIsMajorTickmark(view, position, GetIsMajorTickmark(view));
        }

        private void UpdatePositionAndIsMajorTickmark(View view, double position, bool isMajorTickmark)
        {
            // Use the view's tag property to store both the tick's proportional position along the tick bar and whether
            // or not the tick is a major tickmark.  Use the first 9 digits to store the position and the last digit to
            // store the flag.
            var storedPosition = Math.Truncate(position * 100000000);
            storedPosition -= storedPosition % 10;
            var tickmarkFlagInt = isMajorTickmark ? 1 : 0;
            var positionAndTickmarkFlag = storedPosition + tickmarkFlagInt;
            view.Tag = positionAndTickmarkFlag;
        }

        private Size GetDesiredSize(View view)
        {
            if (view is RectangleView rectangleView)
            {
                return new Size((int)rectangleView.Width, (int)rectangleView.Height);
            }
            else if (view.LayoutParameters != null && view.LayoutParameters.Width > 0 && view.LayoutParameters.Height > 0)
            {
                return new Size(view.LayoutParameters.Width, view.LayoutParameters.Height);
            }
            else if (view is LinearLayout linearLayout && GetIsMajorTickmark(linearLayout) && linearLayout.GetChildAt(0) is TextView label)
            {
                label.Measure((int)MeasureSpecMode.Unspecified, (int)MeasureSpecMode.Unspecified);
                return new Size(label.MeasuredWidth, label.MeasuredHeight);
            }
            else
            {
                view.Measure((int)MeasureSpecMode.Unspecified, (int)MeasureSpecMode.Unspecified);
                return new Size(view.MeasuredWidth, view.MeasuredHeight);
            }
        }

        /// <inheritdoc />
        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            InvalidateMeasureAndArrange();
        }

        private void InvalidateMeasureAndArrange()
        {
            var layoutWidth = Right - Left;
            var layoutHeight = Bottom - Top;

            if (MeasuredWidth == _lastMeasuredWidth
                && MeasuredHeight == _lastMeasuredHeight
                && layoutWidth == _lastLayoutWidth
                && layoutHeight == _lastLayoutHeight
                && ShowTickLabels == _lastLabelsVisible)
            {
                // No change in size or label visibility
                return;
            }

            _lastMeasuredWidth = MeasuredWidth;
            _lastMeasuredHeight = MeasuredHeight;
            _lastLayoutWidth = layoutWidth;
            _lastLayoutHeight = layoutHeight;
            _lastLabelsVisible = ShowTickLabels;

            var availableWidth = MeasuredWidth - (TickInset * 2);
            OnArrange(new SizeF(availableWidth, MeasuredHeight));
        }
    }
}