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
using UIKit;
using ContentPresenter = UIKit.UIView;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class Tickbar
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
            //// Create both a minor and major tick mark at the specified position.  Layout logic will determine which
            //// one to actually show at the position.

            //// Create a minor tickmark
            //ContentPresenter c = new ContentPresenter()
            //{
            //    VerticalAlignment = VerticalAlignment.Top,
            //    Content = dataSource
            //};
            //c.SetValue(PositionProperty, position);
            //c.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding()
            //{
            //    Source = this,
            //    Path = new PropertyPath(nameof(MinorTickmarkTemplate)),
            //});
            //Children.Add(c);
            //_minorTickmarks.Add(c);

            //// Create a major tickmark
            //c = new ContentPresenter()
            //{
            //    VerticalAlignment = VerticalAlignment.Top,
            //    Content = dataSource
            //};
            //c.SetValue(PositionProperty, position);
            //c.SetValue(IsMajorTickmarkProperty, true);
            //c.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding()
            //{
            //    Source = this,
            //    Path = new PropertyPath(nameof(MajorTickmarkTemplate))
            //});

            //if (TickLabelFormat != null)
            //{
            //    ApplyTickLabelFormat(c, TickLabelFormat);
            //}
            //Children.Add(c);
            //_majorTickmarks.Add(c);
        }

        private void ApplyTickLabelFormat(ContentPresenter tick, string tickLabelFormat)
        {
            //// Check whether the tick element has its children populated
            //if (VisualTreeHelper.GetChildrenCount(tick) > 0)
            //{
            //    // Find the tick label in the visual tree
            //    var contentRoot = VisualTreeHelper.GetChild(tick, 0) as FrameworkElement;
            //    var labelTextBlock = contentRoot.FindName("TickLabel") as TextBlock;
            //    labelTextBlock?.UpdateStringFormat(
            //        targetProperty: TextBlock.TextProperty,
            //        stringFormat: tickLabelFormat,
            //        fallbackFormat: ref _originalTickLabelFormat);
            //}
            //else // Children are not populated yet.  Wait for tick to load.
            //{
            //    // Defer the method call until the tick element is loaded
            //    void tickLoadedHandler(object o, RoutedEventArgs e)
            //    {
            //        ApplyTickLabelFormat(tick, tickLabelFormat);
            //        tick.Loaded -= tickLoadedHandler;
            //    }

            //    tick.Loaded += tickLoadedHandler;
            //}
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
            return (double)view.Tag / 10000000;
        }

        private void SetPosition(UIView view, double position)
        {
            UpdatePositionAndIsMajorTickmark(view, position, GetIsMajorTickmark(view));            
        }

        private void UpdatePositionAndIsMajorTickmark(UIView view, double position, bool isMajorTickmark)
        {
            var storedPosition = Math.Truncate(position * 100000000);
            var tickmarkFlagInt = isMajorTickmark ? 1 : 0;
            var positionAndTickmarkFlag = storedPosition + tickmarkFlagInt;
            view.Tag = (nint)positionAndTickmarkFlag;
        }

        private CGSize GetDesiredSize(UIView view) => view.SizeThatFits(new CGSize(Frame.Width, Frame.Height));

        private void RemoveChild(UIView parent, UIView child) => child.RemoveFromSuperview();

        private int ChildCount => Subviews.Length;

        private void InvalidateMeasureAndArrange()
        {
            // TODO
            throw new NotImplementedException("TODO");
        }
    }

    internal static class UIViewExtensions
    {
        public static void Arrange(this UIView view, CGRect bounds) => view.Bounds = bounds;
    }
}

#endif