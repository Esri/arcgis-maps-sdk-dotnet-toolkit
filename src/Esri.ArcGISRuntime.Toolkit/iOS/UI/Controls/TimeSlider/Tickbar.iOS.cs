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
using System.Text;
using CoreGraphics;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class Tickbar : UIView
    {
        private const string _template =
            "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
            "<TextBlock Text=\"|\" VerticalAlignment=\"Center\" HorizontalAlignment=\"Center\" />" +
            "</DataTemplate>";

        private void Initialize()
        {
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
            var tick = new RectangleView()
            {
                BackgroundColor = TickFill,
                Width = 1,
                Height = 4
            };
            SetIsMajorTickmark(tick, false);
            SetPosition(tick, position);

            AddSubview(tick);
            _minorTickmarks.Add(tick);


            // Create a major tickmark
            tick = new RectangleView()
            {
                BackgroundColor = TickFill,
                Width = 1,
                Height = 7
            };

            var majorTickContainer = new UIView();
            if (dataSource is DateTimeOffset dateTime)
            {
                // Create label for major tickmark
                var timeStepIntervalDateFormat = string.IsNullOrEmpty(TickLabelFormat)
                    ? _defaultTickLabelFormat : TickLabelFormat;
                var label = new UILabel()
                {
                    Text = dateTime.ToString(timeStepIntervalDateFormat),
                    Font = UIFont.SystemFontOfSize(11),
                    TextColor = TickLabelColor,
                    Tag = (nint)dateTime.ToUnixTimeMilliseconds()
                };

                var labelSize = label.SizeThatFits(Frame.Size);
                var tickLeft = (labelSize.Width - tick.Width) / 2;
                tick.Frame = new CGRect(tickLeft, 0, tick.Width, tick.Height);

                label.Frame = new CGRect(0, tick.Height + 1, labelSize.Width, labelSize.Height);

                majorTickContainer.AddSubviews(tick, label);
                majorTickContainer.Frame = new CGRect(0, 0, labelSize.Width, label.Frame.Bottom);

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
            if (tick.Subviews.Length > 1 && tick.Subviews[1] is UILabel label)
            {
                var labelFormat = string.IsNullOrEmpty(tickLabelFormat) ? _defaultTickLabelFormat : tickLabelFormat;
                var labelDate = DateTimeOffset.FromUnixTimeMilliseconds(label.Tag);
                label.Text = labelDate.ToString(labelFormat);
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
            var storedPosition = Math.Truncate(position * 100000000);
            storedPosition -= storedPosition % 10;
            var tickmarkFlagInt = isMajorTickmark ? 1 : 0;
            var positionAndTickmarkFlag = storedPosition + tickmarkFlagInt;
            view.Tag = (nint)positionAndTickmarkFlag;
        }

        private CGSize GetDesiredSize(UIView view) => view.SizeThatFits(Frame.Size);

        private void RemoveChild(UIView parent, UIView child) => child.RemoveFromSuperview();

        private int ChildCount => Subviews.Length;

        private void InvalidateMeasureAndArrange()
        {
            try
            {
                // TODO
                //throw new NotImplementedException("TODO");
                OnArrange(Frame.Size);
            }
            catch (Exception ex)
            {
                var m = ex.Message;
            }
        }
    }
}

#endif