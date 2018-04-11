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
using System.Linq;
using System.Text;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class Tickbar : ViewGroup
    {
        private void Initialize()
        {
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            throw new NotImplementedException();
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            throw new NotImplementedException();
            //base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
        }

        private Size _majorTickSize = new Size(1, 10);

        /// <summary>
        /// Gets or sets the size of major tickmarks
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
        /// Gets or sets the size of minor tickmarks
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
        /// Gets or sets the spacing between ticks and tick labels
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

        /// <summary>
        /// Adds a tick to the bar's visual tree
        /// </summary>
        /// <param name="position">The position to place the tick at along the tick bar</param>
        /// <param name="dataSource">The data to pass to the tick's template</param>
        private void AddTickmark(double position, object dataSource)
        {
            // Create both a minor and major tick mark at the specified position.  Layout logic will determine which
            // one to actually show at the position.

            // Create a minor tickmark
            var tick = new RectangleView(Context)
            {
                BackgroundColor = TickFill,
                Width = MinorTickSize.Width,
                Height = MinorTickSize.Height,
                //BorderWidth = 0
            };
            SetIsMajorTickmark(tick, false);
            SetPosition(tick, position);

            AddView(tick);
            _minorTickmarks.Add(tick);

            // Create a major tickmark
            tick = new RectangleView(Context)
            {
                BackgroundColor = TickFill,
                Width = MajorTickSize.Width,
                Height = MajorTickSize.Height,
                //BorderWidth = 0
            };

            if (dataSource is DateTimeOffset dateTime)
            {
                var majorTickContainer = new RelativeLayout(Context);

                // Create label for major tickmark
                var timeStepIntervalDateFormat = string.IsNullOrEmpty(TickLabelFormat)
                    ? _defaultTickLabelFormat : TickLabelFormat;
                var label = new TextView(Context)
                {
                    Text = dateTime.ToString(timeStepIntervalDateFormat),
                    //Font = UIFont.SystemFontOfSize(11),
                    //TextColor = TickLabelColor,
                    Tag = dateTime.ToUnixTimeMilliseconds()
                };
                label.SetTextColor(TickLabelColor);

                // Calculate positions of the tickmark and label
                //var labelSize = label.SizeThatFits(Frame.Size);
                //var tickLeft = (labelSize.Width - tick.Width) / 2;
                //tick.Frame = new CGRect(tickLeft, 0, tick.Width, tick.Height);
                //label.Frame = new CGRect(0, tick.Height + LabelOffset, labelSize.Width, labelSize.Height);

                majorTickContainer.AddView(tick);
                majorTickContainer.AddView(label);
                //majorTickContainer.Frame = new CGRect(0, 0, labelSize.Width, label.Frame.Bottom);

                // Flag the tick as a major tickmark and set it's proportional position along the tick bar
                SetIsMajorTickmark(majorTickContainer, true);
                SetPosition(majorTickContainer, position);

                AddView(majorTickContainer);
                _majorTickmarks.Add(majorTickContainer);
            }
            else
            {
                SetIsMajorTickmark(tick, true);
                SetPosition(tick, position);

                AddView(tick);
                _majorTickmarks.Add(tick);
            }
        }

        private void ApplyTickLabelFormat(View tickContainer, string tickLabelFormat)
        {
            // Retrieve the label from the container holding the major tick rectangle and label
            if (tickContainer is ViewGroup group && group.ChildCount > 1 && group.GetChildAt(1) is TextView label)
            {
                // Apply the specified format to the tick's date and update the label
                var labelFormat = string.IsNullOrEmpty(tickLabelFormat) ? _defaultTickLabelFormat : tickLabelFormat;
                var labelDate = DateTimeOffset.FromUnixTimeMilliseconds((long)label.Tag);
                label.Text = labelDate.ToString(labelFormat);
            }
        }

        private void ApplyTickLabelColor(View tick, Color color)
        {
            if (tick is ViewGroup group && group.ChildCount > 1 && group.GetChildAt(1) is TextView label)
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
            throw new NotImplementedException();

            //view.SizeThatFits(Frame.Size);
        }

        private void InvalidateMeasureAndArrange()
        {
            throw new NotImplementedException();

            //// Re-layout major tickmarks and labels to accommodate possible change in label lengths
            //foreach (var tickContainer in _majorTickmarks)
            //{
            //    // Get tick rectangle and label for current tick
            //    var tick = tickContainer.Subviews.OfType<RectangleView>().FirstOrDefault();
            //    var label = tickContainer.Subviews.OfType<UILabel>().FirstOrDefault();
            //    if (tick == null || label == null)
            //    {
            //        continue;
            //    }

            //    // Get size of the tick label and calculate the frame for the tick, label, and container
            //    var labelSize = label.SizeThatFits(Frame.Size);
            //    var tickLeft = (labelSize.Width - tick.Width) / 2;
            //    tick.Frame = new CGRect(tickLeft, 0, tick.Width, tick.Height);
            //    label.Frame = new CGRect(0, tick.Height + LabelOffset, labelSize.Width, labelSize.Height);
            //    tickContainer.Frame = new CGRect(0, 0, labelSize.Width, label.Frame.Bottom);
            //}

            //OnArrange(Frame.Size);
        }
    }
}