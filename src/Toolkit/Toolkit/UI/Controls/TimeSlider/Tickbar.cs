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

// Implementation adapted and enhanced from https://github.com/Esri/arcgis-toolkit-sl-wpf
using System;
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if NETFX_CORE
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
#elif __IOS__
using System.Drawing;
using Brush = UIKit.UIColor;
using ContentPresenter = UIKit.UIView;
using FrameworkElement = UIKit.UIView;
using Rect = CoreGraphics.CGRect;
using Size = CoreGraphics.CGSize;
using UIElement = UIKit.UIView;
#elif __ANDROID__
using System.Drawing;
using Android.Content;
using Brush = Android.Graphics.Color;
using ContentPresenter = Android.Views.View;
using FrameworkElement = Android.Views.View;
using Rect = Android.Graphics.RectF;
using Size = Android.Util.SizeF;
using UIElement = Android.Views.View;
#else
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// *FOR INTERNAL USE ONLY* Tickbar control used for placing a specified amount of tick marks evenly spread out.
    /// </summary>
    /// <exclude/>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class Tickbar
    {
        private List<ContentPresenter> _majorTickmarks = new List<ContentPresenter>();
        private List<ContentPresenter> _minorTickmarks = new List<ContentPresenter>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Tickbar"/> class.
        /// </summary>
#if __ANDROID__
        public Tickbar(Context context)
            : base(context)
#else
        public Tickbar()
            : base()
#endif
        {
            Initialize();
        }

        private Size OnArrange(Size finalSize)
        {
            if (TickmarkPositions == null || TickmarkPositions.Count() < 2)
            {
                return finalSize;
            }

            var majorTickmarksBounds = new List<Rect>();
            var minorTickmarksBounds = new List<Rect>();

            // Iterate all child ticks and calculate bounds for each
            foreach (UIElement child in Children)
            {
                FrameworkElement c = child as FrameworkElement;
                if (c == null)
                {
                    continue;
                }

                double position = GetPosition(c);
                var isMajorTickmark = GetIsMajorTickmark(c);

                if (isMajorTickmark && !ShowTickLabels)
                {
                    continue; // Don't worry about calculating bounds for major ticks if labels are hidden
                }

                // Calculate the bounds of the tick mark
                position = finalSize.Width * position;
                var desiredSize = GetDesiredSize(c);
                var x = position - (desiredSize.Width * .5);
#if __ANDROID__
                // In the implementation of the Android time slider, the tickbar is aligned horizontally with its parent to allow
                // tick labels to use the entire space within the control.  The TickInset property defines how much extra room is
                // available outside the bounds of the Tickbar and needs to be taken into account in the placement of ticks.
                // This inset also needs to be adjusted slightly, as it yields a position that is slightly offset for reasons as
                // yet unknown.
                var pixelsPerDip = Android.Util.TypedValue.ApplyDimension(Android.Util.ComplexUnitType.Dip, 1, ViewExtensions.GetDisplayMetrics());
                x += TickInset - (2 * pixelsPerDip);
#endif
                var childBounds = new Rect(0, 0, desiredSize.Width, finalSize.Height);
                childBounds.SetX(x);

                // Store the bounds for application later once tick (i.e. label) collision has been accounted for
                if (isMajorTickmark)
                {
                    majorTickmarksBounds.Add(childBounds);
                }
                else
                {
                    minorTickmarksBounds.Add(childBounds);
                }
            }

            if (ShowTickLabels)
            {
                // Calculate positioning of tick labels and major/minor ticks
                var minimumLabelSpacing = 6;

                var majorTickInterval = 2;
                var doMajorTicksCollide = false;
                var firstMajorTickIndex = 0;
                var tickCount = _minorTickmarks.Count;

                // Calculate the largest number of ticks to allow between major ticks.  This prevents scenarios where
                // there are two major ticks placed undesirably close to the end of the tick bar.
                var maxMajorTickInterval = Math.Ceiling(tickCount / 2d);

                // Calculate the number of ticks between each major tick and the index of the first major tick
                for (int i = majorTickInterval; i <= maxMajorTickInterval; i++)
                {
                    var prospectiveInterval = i;
                    var allowsEqualNumberOfTicksOnEnds = false;

                    // Check that the prospective interval between major ticks results in an equal number of minor
                    // ticks on both ends of the tick bar
                    for (int m = prospectiveInterval; m < tickCount; m += prospectiveInterval)
                    {
                        var totalNumberOfTicksOnEnds = tickCount - m + 1;

                        // If the total number of minor ticks on both ends of the tick bar (i.e. before and after the
                        // first and last major ticks) is less than the major tick interval being tested, then we've
                        // found the number of minor ticks that would be on the ends if we use this major tick interval.
                        // If that total is divisible by two, then the major tick interval under test allows for an
                        // equal number of minor ticks on the ends.
                        if (totalNumberOfTicksOnEnds / 2 < prospectiveInterval && totalNumberOfTicksOnEnds % 2 == 0)
                        {
                            allowsEqualNumberOfTicksOnEnds = true;
                            break;
                        }
                    }

                    // Only consider intervals that leave an equal number of ticks on the ends
                    if (!allowsEqualNumberOfTicksOnEnds)
                    {
                        continue;
                    }

                    // Calculate the tick index of the first major tick if we were to use the prospective interval.
                    // The index is calculated such that there will be an equal number of minor ticks before and
                    // after the first and last major tick mark.
                    firstMajorTickIndex = (int)Math.Truncate(((tickCount - 1) % prospectiveInterval) / 2d);
                    doMajorTicksCollide = false;

                    // With the given positioning of major tick marks, check whether they (i.e. their labels) will overlap
                    for (var j = firstMajorTickIndex; j < tickCount - prospectiveInterval; j += i)
                    {
                        // Get the bounds of the major tick marks at index j and the one subsequent to that
                        var currentBounds = majorTickmarksBounds[j];
                        var nextBounds = majorTickmarksBounds[j + i];

                        if (currentBounds.Right + minimumLabelSpacing > nextBounds.Left)
                        {
                            doMajorTicksCollide = true;
                            break;
                        }
                    }

                    if (!doMajorTicksCollide)
                    {
                        // The ticks don't at the given interval, so use that
                        majorTickInterval = prospectiveInterval;
                        break;
                    }
                }

                if (doMajorTicksCollide)
                {
                    // Multiple major ticks (and their labels) won't fit without overlapping.  Display one major tick
                    // in the middle instead
                    majorTickInterval = tickCount;

                    // Calculate the index of the middle tick.  Note that, if there are an even number of ticks, there
                    // is not one perfectly centered.  This logic takes the one before the true center of the tick bar.
                    if (tickCount % 2 == 0)
                    {
                        firstMajorTickIndex = (int)Math.Truncate(tickCount / 2d) - 1;
                    }
                    else
                    {
                        firstMajorTickIndex = (int)Math.Truncate(tickCount / 2d);
                    }
                }

                // Apply the ticks' layouts
                for (var i = 0; i < tickCount; i++)
                {
                    // Check whether the current tick index refers to a major or minor tick
                    var isMajorTickIndex = (i - firstMajorTickIndex) % majorTickInterval == 0;

                    // Arrange either the major or minor tick for the current index
                    if (isMajorTickIndex)
                    {
                        _majorTickmarks[i].Arrange(majorTickmarksBounds[i]);
                        _minorTickmarks[i].Arrange(new Rect(0, 0, 0, 0));
                    }
                    else
                    {
                        _minorTickmarks[i].Arrange(minorTickmarksBounds[i]);
                        _majorTickmarks[i].Arrange(new Rect(0, 0, 0, 0));
                    }
                }
            }
            else
            {
                // !ShowTickLabels
                for (var i = 0; i < _minorTickmarks.Count; i++)
                {
                    _minorTickmarks[i].Arrange(minorTickmarksBounds[i]);
                }

                foreach (var majorTick in _majorTickmarks)
                {
                    majorTick.Arrange(new Rect(0, 0, 0, 0));
                }
            }

            return finalSize;
        }

        private Size OnMeasure(Size availableSize)
        {
            var width = availableSize.Width == double.PositiveInfinity || availableSize.IsEmpty() ?
                Width : availableSize.Width;
            var height = availableSize.Height == double.PositiveInfinity || availableSize.IsEmpty() ?
                Height : availableSize.Height;

            // Get the set of ticks that we want to measure.  This could either be all ticks or only minor ticks
            var measuredChildren = ShowTickLabels ? Children.Cast<UIElement>() : Children.Cast<UIElement>().Where(el => !GetIsMajorTickmark(el));
#if !__IOS__
            foreach (UIElement d in measuredChildren)
            {
                d.Measure(availableSize);
            }
#endif

            if (double.IsNaN(height))
            {
                height = 0;
                foreach (UIElement d in measuredChildren)
                {
                    var maxHeight = Math.Max(GetDesiredSize(d).Height, height);
                    height =
#if __IOS__
                        (nfloat)maxHeight;
#else
                        maxHeight;
#endif
                }
            }

            width = double.IsNaN(width) ? 0 : width;

            return new Size(width, height);
        }

        /// <summary>
        /// Gets or sets the tick mark positions.
        /// </summary>
        /// <value>The tick mark positions.</value>
        /// <remarks>The tick mark position values should be between 0 and 1.  They represent proportional positions along the tick bar.</remarks>
        public IEnumerable<double> TickmarkPositions
        {
            get => TickmarkPositionsImpl;
            set => TickmarkPositionsImpl = value;
        }

        private void OnTickmarkPositionsPropertyChanged(IEnumerable<double> newTickPositions, IEnumerable<double> oldTickPositions)
        {
#if !XAMARIN
            if (MinorTickmarkTemplate == null)
            {
                MinorTickmarkTemplate = _defaultTickmarkTemplate;
            }
#endif

            var newTickCount = newTickPositions == null ? 0 : newTickPositions.Count();
            var oldTickCount = oldTickPositions == null ? 0 : oldTickPositions.Count();

            if (newTickCount < oldTickCount)
            {
                // Reduce the number of ticks to the number of positions specified
                for (var i = oldTickCount; i > newTickCount; i--)
                {
                    var tickToRemove = _majorTickmarks[i - 1];
                    this.RemoveChild(tickToRemove);
                    _majorTickmarks.Remove(tickToRemove);

                    tickToRemove = _minorTickmarks[i - 1];
                    this.RemoveChild(tickToRemove);
                    _minorTickmarks.Remove(tickToRemove);
                }

                // Update the positions of the remaining ticks
                for (var i = 0; i < _minorTickmarks.Count; i++)
                {
                    SetPosition(_minorTickmarks[i], newTickPositions.ElementAt(i));
                    SetPosition(_majorTickmarks[i], newTickPositions.ElementAt(i));
                }
            }
            else if (newTickPositions != null)
            {
                for (var i = 0; i < newTickCount; i++)
                {
                    if (i < oldTickCount)
                    {
                        // Update positions of existing ticks
                        SetPosition(_minorTickmarks[i], newTickPositions.ElementAt(i));
                        SetPosition(_majorTickmarks[i], newTickPositions.ElementAt(i));
                    }
                    else
                    {
                        // Add new ticks to bring the number of ticks up to the number of positions specified
                        var tickDataSource = TickmarkDataSources == null || i >= TickmarkDataSources.Count() ? null : TickmarkDataSources.ElementAt(i);
                        AddTickmark(newTickPositions.ElementAt(i), tickDataSource);
                    }
                }
            }

            InvalidateMeasureAndArrange();
        }

        /// <summary>
        /// Gets or sets the data sources for the tick marks.  These can be bound to in the tick bar's tick templates.
        /// </summary>
        /// <value>The data source objects.</value>
        public IEnumerable<object> TickmarkDataSources
        {
            get => TickmarkDataSourcesImpl;
            set => TickmarkDataSourcesImpl = value;
        }

        private void OnTickmarkDataSourcesPropertyChanged(IEnumerable<object> dataSources)
        {
            var newDataSources = dataSources ?? new List<object>();

#if !XAMARIN
            for (var i = 0; i < _majorTickmarks.Count; i++)
            {
                _majorTickmarks[i].Content = i < newDataSources.Count() ? newDataSources.ElementAt(i) : null;
            }
#endif
        }

        /// <summary>
        /// Gets or sets the fill color for each tick mark.
        /// </summary>
        public Brush TickFill
        {
            get => TickFillImpl;
            set => TickFillImpl = value;
        }

        /// <summary>
        /// Gets or sets the fill color for each tick mark.
        /// </summary>
        public Brush TickLabelColor
        {
            get => TickLabelColorImpl;
            set => TickLabelColorImpl = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets whether to display labels on the ticks.
        /// </summary>
        public bool ShowTickLabels
        {
            get => ShowTickLabelsImpl;
            set => ShowTickLabelsImpl = value;
        }

        private void OnShowTickLabelsPropertyChanged() => InvalidateMeasureAndArrange();

        /// <summary>
        /// Gets or sets the string format to use for displaying the tick labels.
        /// </summary>
        public string TickLabelFormat
        {
            get => TickLabelFormatImpl;
            set => TickLabelFormatImpl = value;
        }

        private void OnTickLabelFormatPropertyChanged(string labelFormat)
        {
            if (_majorTickmarks == null)
            {
                return;
            }

            // Update the label format string for each of the major ticks
            foreach (var majorTick in _majorTickmarks)
            {
                ApplyTickLabelFormat(majorTick, labelFormat);
            }

            // Invoke a layout pass to accommodate new label sizes
            InvalidateMeasureAndArrange();
        }
    }
}