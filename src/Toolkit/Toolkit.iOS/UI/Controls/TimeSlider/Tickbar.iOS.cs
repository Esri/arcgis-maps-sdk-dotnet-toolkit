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

#if __IOS__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreGraphics;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class Tickbar : UIView
    {
        private void Initialize()
        {
        }

        private CGSize _majorTickSize = new CGSize(1, 10);

        /// <summary>
        /// Gets or sets the size of major tickmarks.
        /// </summary>
        public CGSize MajorTickSize
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

        private CGSize _minorTickSize = new CGSize(1, 5);

        /// <summary>
        /// Gets or sets the size of minor tickmarks.
        /// </summary>
        public CGSize MinorTickSize
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

        /// <summary>
        /// Adds a tick to the bar's visual tree.
        /// </summary>
        /// <param name="position">The position to place the tick at along the tick bar.</param>
        /// <param name="dataSource">The data to pass to the tick's template.</param>
        private void AddTickmark(double position, object dataSource)
        {
            // Create both a minor and major tick mark at the specified position.  Layout logic will determine which
            // one to actually show at the position.

            // Create a minor tickmark
            var tick = new RectangleView()
            {
                BackgroundColor = TickFill,
                Width = MinorTickSize.Width,
                Height = MinorTickSize.Height,
                BorderWidth = 0,
            };
            SetIsMajorTickmark(tick, false);
            SetPosition(tick, position);

            AddSubview(tick);
            _minorTickmarks.Add(tick);

            // Create a major tickmark
            tick = new RectangleView()
            {
                BackgroundColor = TickFill,
                Width = MajorTickSize.Width,
                Height = MajorTickSize.Height,
                BorderWidth = 0,
            };

            if (dataSource is DateTimeOffset dateTime)
            {
                var majorTickContainer = new UIView();

                // Create label for major tickmark
                var timeStepIntervalDateFormat = string.IsNullOrEmpty(TickLabelFormat)
                    ? _defaultTickLabelFormat : TickLabelFormat;
                var label = new UILabel()
                {
                    Text = dateTime.ToString(timeStepIntervalDateFormat),
                    Font = UIFont.SystemFontOfSize(11),
                    TextColor = TickLabelColor,
                    Tag = (nint)dateTime.ToUnixTimeMilliseconds(),
                };

                // Calculate positions of the tickmark and label
                var labelSize = label.SizeThatFits(Frame.Size);
                var tickLeft = (labelSize.Width - tick.Width) / 2;
                tick.Frame = new CGRect(tickLeft, 0, tick.Width, tick.Height);
                label.Frame = new CGRect(0, tick.Height + LabelOffset, labelSize.Width, labelSize.Height);

                majorTickContainer.AddSubviews(tick, label);
                majorTickContainer.Frame = new CGRect(0, 0, labelSize.Width, label.Frame.Bottom);

                // Flag the tick as a major tickmark and set it's proportional position along the tick bar
                SetIsMajorTickmark(majorTickContainer, true);
                SetPosition(majorTickContainer, position);

                AddSubview(majorTickContainer);
                _majorTickmarks.Add(majorTickContainer);
            }
            else
            {
                SetIsMajorTickmark(tick, true);
                SetPosition(tick, position);

                AddSubview(tick);
                _majorTickmarks.Add(tick);
            }
        }

        private void ApplyTickLabelFormat(UIView tick, string tickLabelFormat)
        {
            // Retrieve the label from the container holding the major tick rectangle and label
            if (tick.Subviews.Length > 1 && tick.Subviews[1] is UILabel label)
            {
                // Apply the specified format to the tick's date and update the label
                var labelFormat = string.IsNullOrEmpty(tickLabelFormat) ? _defaultTickLabelFormat : tickLabelFormat;
                var labelDate = DateTimeOffset.FromUnixTimeMilliseconds(label.Tag);
                label.Text = labelDate.ToString(labelFormat);
            }
        }

        private void ApplyTickLabelColor(UIView tick, UIColor color)
        {
            if (tick.Subviews.Length > 1 && tick.Subviews[1] is UILabel label)
            {
                label.TextColor = color;
            }
        }

        private UIView[] Children => Subviews;

        private nfloat Width => Frame.Width;

        private nfloat Height => Frame.Height;

        private void SetIsMajorTickmark(UIView view, bool isMajorTickmark)
        {
            UpdatePositionAndIsMajorTickmark(view, GetPosition(view), isMajorTickmark);
        }

        private bool GetIsMajorTickmark(UIView view)
        {
            return view.Tag % 10 == 0 ? false : true;
        }

        private double GetPosition(UIView view)
        {
            // Remove last digit as that stores whether the tickmark is major or minor
            var positionDigits = Math.Truncate((double)view.Tag / 10);

            // Convert remaining digits to decimal value between 0 and 1
            return positionDigits / 10000000;
        }

        private void SetPosition(UIView view, double position)
        {
            UpdatePositionAndIsMajorTickmark(view, position, GetIsMajorTickmark(view));
        }

        private void UpdatePositionAndIsMajorTickmark(UIView view, double position, bool isMajorTickmark)
        {
            // Use the view's tag property to store both the tick's proportional position along the tick bar and whether
            // or not the tick is a major tickmark.  Use the first 9 digits to store the position and the last digit to
            // store the flag.
            var storedPosition = Math.Truncate(position * 100000000);
            storedPosition -= storedPosition % 10;
            var tickmarkFlagInt = isMajorTickmark ? 1 : 0;
            var positionAndTickmarkFlag = storedPosition + tickmarkFlagInt;
            view.Tag = (nint)positionAndTickmarkFlag;
        }

        private CGSize GetDesiredSize(UIView view) => view.SizeThatFits(Frame.Size);

        private int ChildCount => Subviews.Length;

        private void InvalidateMeasureAndArrange()
        {
            // Re-layout major tickmarks and labels to accommodate possible change in label lengths
            foreach (var tickContainer in _majorTickmarks)
            {
                // Get tick rectangle and label for current tick
                var tick = tickContainer.Subviews.OfType<RectangleView>().FirstOrDefault();
                var label = tickContainer.Subviews.OfType<UILabel>().FirstOrDefault();
                if (tick == null || label == null)
                {
                    continue;
                }

                // Get size of the tick label and calculate the frame for the tick, label, and container
                var labelSize = label.SizeThatFits(Frame.Size);
                var tickLeft = (labelSize.Width - tick.Width) / 2;
                tick.Frame = new CGRect(tickLeft, 0, tick.Width, tick.Height);
                label.Frame = new CGRect(0, tick.Height + LabelOffset, labelSize.Width, labelSize.Height);
                tickContainer.Frame = new CGRect(0, 0, labelSize.Width, label.Frame.Bottom);
            }

            OnArrange(Frame.Size);
        }
    }
}

#endif