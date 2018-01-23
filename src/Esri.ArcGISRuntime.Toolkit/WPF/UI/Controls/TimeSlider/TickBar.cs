// Copyright 2017 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

// Implementation ported from https://github.com/Esri/arcgis-toolkit-sl-wpf

using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Windows.Media;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Markup;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// *FOR INTERNAL USE ONLY* TickBar control used for placing a specified amount of tick marks evenly spread out.
    /// </summary>
    /// <exclude/>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class TickBar : Panel
    {
        internal static readonly DependencyProperty PositionProperty = DependencyProperty.RegisterAttached("Position", typeof(double), typeof(TickBar), new PropertyMetadata(0.0));
        internal static readonly DependencyProperty IsMajorTickMarkProperty = DependencyProperty.RegisterAttached("IsMajorTickMark", typeof(bool), typeof(TickBar), new PropertyMetadata(false));
        private const string template = "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><TextBlock Text=\"|\" VerticalAlignment=\"Center\" HorizontalAlignment=\"Center\" /></DataTemplate>";
        private static DataTemplate DefaultTickMarkTemplate;
        private List<ContentPresenter> MajorTickMarks = new List<ContentPresenter>();
        private List<ContentPresenter> MinorTickMarks = new List<ContentPresenter>();
        private string OriginalTickLabelFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="TickBar"/> class.
        /// </summary>
        public TickBar()
        {
            if (DefaultTickMarkTemplate == null)
            {
                System.IO.MemoryStream stream = new System.IO.MemoryStream(
                    System.Text.UTF8Encoding.Default.GetBytes(template));
                DefaultTickMarkTemplate = System.Windows.Markup.XamlReader.Load(stream) as DataTemplate;
            }
        }
        
        /// <summary>
        /// Provides the behavior for the Arrange pass of Silverlight layout.
        /// Classes can override this method to define their own Arrange pass behavior.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this
        /// object should use to arrange itself and its children.</param>
        /// <returns>
        /// The actual size used once the element is arranged.
        /// </returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (TickMarkPositions == null || TickMarkPositions.Length < 2) return finalSize;
            Rect childBounds = new Rect(0, 0, finalSize.Width, finalSize.Height);

            var majorTickMarksBounds = new List<Rect>();
            var minorTickMarksBounds = new List<Rect>();

            foreach (UIElement child in Children)
            {
                FrameworkElement c = (child as FrameworkElement);
                if (c == null) continue;
                double position = (double)c.GetValue(PositionProperty);
                var isMajorTickMark = (bool)c.GetValue(IsMajorTickMarkProperty);

                if (isMajorTickMark && !ShowTickLabels)
                    continue;

                // Calculate the bounds of the tick mark
                if (Orientation == Orientation.Horizontal)
                {
                    position = finalSize.Width * position;
                    childBounds.X = position - c.DesiredSize.Width * .5;
                    childBounds.Width = c.DesiredSize.Width;
                }
                else
                {
                    position = finalSize.Height * position;
                    childBounds.Y = position - c.DesiredSize.Height * .5;
                    childBounds.Height = c.DesiredSize.Height;
                }

                // Store the bounds for application later once tick (i.e. label) collision has been accounted for
                if (isMajorTickMark)
                {
                    majorTickMarksBounds.Add(childBounds);
                }
                else
                {
                    minorTickMarksBounds.Add(childBounds);
                }
            }

            if (ShowTickLabels)
            {
                var minimumLabelSpacing = 6;

                var majorTickInterval = 2;
                var doMajorTicksCollide = false;
                var firstMajorTickIndex = 0;
                var tickCount = MinorTickMarks.Count;

                // Calculate the largest number of ticks to allow between major ticks.  This prevents scenarios where
                // there are two major ticks placed undesirably close to the end of the tick bar.
                var maxMajorTickInterval = Math.Ceiling(tickCount / 2d);

                // Calculate the number of ticks between each major tick and the index of the first major tick
                for (int i = majorTickInterval; i <= maxMajorTickInterval; i++)
                {
                    var prospectiveInterval = i;
                    var allowsEqualNumberOfTicksOnEnds = false;
                    doMajorTicksCollide = false;

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
                        continue;

                    // Calculate the tick index of the first major tick if we were to use the prospective interval.
                    // The index is calculated such that there will be an equal number of minor ticks before and
                    // after the first and last major tick mark.
                    firstMajorTickIndex = (int)Math.Truncate(((tickCount - 1) % prospectiveInterval) / 2d);

                    // With the given positioning of major tick marks, check whether they (i.e. their labels) will overlap
                    for (var j = firstMajorTickIndex; j < tickCount - prospectiveInterval; j += i)
                    {
                        // Get the bounds of the major tick marks at index j and the one subsequent to that
                        var currentBounds = majorTickMarksBounds[j];
                        var nextBounds = majorTickMarksBounds[j + i];

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
                        firstMajorTickIndex = (int)Math.Truncate(tickCount / 2d) - 1;
                    else
                        firstMajorTickIndex = (int)Math.Truncate(tickCount / 2d);
                }

                // Apply the ticks' layouts
                for (int i = 0; i < tickCount; i++)
                {
                    // Check whether the current tick index refers to a major or minor tick
                    var isMajorTickIndex = (i - firstMajorTickIndex) % majorTickInterval == 0;

                    // Arrange either the major or minor tick for the current index
                    if (isMajorTickIndex)
                    {
                        MajorTickMarks[i].Arrange(majorTickMarksBounds[i]);
                        MinorTickMarks[i].Arrange(new Rect(0, 0, 0, 0));
                    }
                    else
                    {
                        MinorTickMarks[i].Arrange(minorTickMarksBounds[i]);
                        MajorTickMarks[i].Arrange(new Rect(0, 0, 0, 0));
                    }
                }
            }
            else // !ShowTickLabels
            {
                for (var i = 0; i < MinorTickMarks.Count; i++)
                {
                    MinorTickMarks[i].Arrange(minorTickMarksBounds[i]);
                }

                foreach (var majorTick in MajorTickMarks)
                {
                    majorTick.Arrange(new Rect(0, 0, 0, 0));
                }
            }

            return finalSize;
        }

        /// <summary>
        /// Provides the behavior for the Measure pass of Silverlight layout. 
        /// Classes can override this method to define their own Measure pass behavior.
        /// </summary>
        /// <param name="availableSize">The available size that this object
        /// can give to child objects. Infinity can be specified as a value
        /// to indicate that the object will size to whatever content is available.</param>
        /// <returns>
        /// The size that this object determines it needs during layout,
        /// based on its calculations of child object allotted sizes.
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            double width = availableSize.Width == double.PositiveInfinity ? this.Width : availableSize.Width;
            double height = availableSize.Height == double.PositiveInfinity ? this.Height : availableSize.Height;
            var measuredChildren = ShowTickLabels ? Children.Cast<UIElement>() : Children.Cast<UIElement>().Where(el => !(bool)el.GetValue(IsMajorTickMarkProperty));
            foreach (UIElement d in measuredChildren)
            {
                d.Measure(availableSize);
            }
            if (double.IsNaN(height))
            {
                height = 0;
                if (Orientation == Orientation.Horizontal)
                {
                    foreach (UIElement d in measuredChildren)
                    {
                        height = Math.Max(d.DesiredSize.Height, height);
                    }
                }
            }
            if (double.IsNaN(width))
            {
                width = 0;
                if (Orientation == Orientation.Vertical)
                {
                    foreach (UIElement d in measuredChildren)
                    {
                        width = Math.Max(d.DesiredSize.Width, width);
                    }
                }
            }
            return new Size(width, height);
        }

        /// <summary>
        /// Gets or sets the tick mark positions.
        /// </summary>
        /// <value>The tick mark positions.</value>
        /// <remarks>The tick mark position values should be between 0 and 1.  They represent proportional positions along the tick bar.</remarks>
        public double[] TickMarkPositions
        {
            get { return (double[])GetValue(TickMarkPositionsProperty); }
            set { SetValue(TickMarkPositionsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TickMarkPositions"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickMarkPositionsProperty =
            DependencyProperty.Register(nameof(TickMarkPositions), typeof(double[]), typeof(TickBar), new PropertyMetadata(OnTickMarkPositionsPropertyChanged));

        private static void OnTickMarkPositionsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bar = (TickBar)d;

            if (bar.MinorTickMarkTemplate == null)
                bar.MinorTickMarkTemplate = DefaultTickMarkTemplate;

            var newTickPositions = (double[])e.NewValue;
            var oldTickPositions = (double[])e.OldValue;
            var newTickCount = newTickPositions == null ? 0 : newTickPositions.Length;
            var oldTickCount = oldTickPositions == null ? 0 : oldTickPositions.Length;

            if (newTickCount < oldTickCount)
            {
                // Reduce the number of ticks to the number of positions specified
                for (var i = oldTickCount; i > newTickCount; i--)
                {
                    var tickToRemove = bar.MajorTickMarks[i - 1];
                    bar.Children.Remove(tickToRemove);
                    bar.MajorTickMarks.Remove(tickToRemove);

                    tickToRemove = bar.MinorTickMarks[i - 1];
                    bar.Children.Remove(tickToRemove);
                    bar.MinorTickMarks.Remove(tickToRemove);
                }

                // Update the positions of the remaining ticks
                for (var i = 0; i < bar.Children.Count; i++)
                {
                    bar.MinorTickMarks[i].SetValue(PositionProperty, newTickPositions[i]);
                    bar.MajorTickMarks[i].SetValue(PositionProperty, newTickPositions[i]);
                }
            }
            else if (newTickPositions != null)
            {
                for (var i = 0; i < newTickCount; i++)
                {
                    if (i < oldTickCount)
                    {
                        // Update positions of existing ticks
                        bar.MinorTickMarks[i].SetValue(PositionProperty, newTickPositions[i]);
                        bar.MajorTickMarks[i].SetValue(PositionProperty, newTickPositions[i]);
                    }
                    else
                    {
                        // Add new ticks to bring the number of ticks up to the number of positions specified
                        var tickDataSource = bar.TickMarkDataSources == null || i >= bar.TickMarkDataSources.Length ? null : bar.TickMarkDataSources[i];
                        bar.AddTickmark(newTickPositions[i], tickDataSource);
                    }
                }
            }
            bar.InvalidateMeasureAndArrange();
        }

        /// <summary>
        /// Gets or sets the tick mark positions.
        /// </summary>
        /// <value>The tick mark positions.</value>
        public object[] TickMarkDataSources
        {
            get { return (object[])GetValue(TickMarkDataSourcesProperty); }
            set { SetValue(TickMarkDataSourcesProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TickMarkDataSources"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickMarkDataSourcesProperty =
            DependencyProperty.Register(nameof(TickMarkDataSources), typeof(object[]), typeof(TickBar), new PropertyMetadata(OnTickMarkDataSourcesPropertyChanged));

        private static void OnTickMarkDataSourcesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bar = (TickBar)d;
            var newDataSources = e.NewValue == null ? new object[0] : (object[])e.NewValue;

            for (var i = 0; i < bar.MajorTickMarks.Count; i++)
            {
                bar.MajorTickMarks[i].Content = i < newDataSources.Length ? newDataSources[i] : null;
            }
        }

        /// <summary>
        /// Adds a tick to the bar's visual tree
        /// </summary>
        /// <param name="position">The position to place the tick at along the tick bar</param>
        /// <param name="dataSource">The data to pass to the tick's template</param>
        private void AddTickmark(double position, object dataSource)
        {
            ContentPresenter c = new ContentPresenter() { VerticalAlignment = VerticalAlignment.Top };
            c.SetValue(PositionProperty, position);
            c.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding()
            {
                Source = this,
                BindsDirectlyToSource = true,
                Path = new PropertyPath(nameof(MinorTickMarkTemplate))
            });
            Children.Add(c);
            MinorTickMarks.Add(c);

            c = new ContentPresenter()
            {
                VerticalAlignment = VerticalAlignment.Top,
                Content = dataSource
            };
            c.SetValue(PositionProperty, position);
            c.SetValue(IsMajorTickMarkProperty, true);
            c.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding()
            {
                Source = this,
                BindsDirectlyToSource = true,
                Path = new PropertyPath(nameof(MajorTickMarkTemplate))
            });

            if (TickLabelFormat != null)
            {
                ApplyTickLabelFormat(c, TickLabelFormat);
            }
            Children.Add(c);
            MajorTickMarks.Add(c);
        }

        /// <summary>
        /// Gets or sets the item template for each tick mark.
        /// </summary>
        /// <value>The item template.</value>
        public DataTemplate MinorTickMarkTemplate
        {
            get { return (DataTemplate)GetValue(MinorTickMarkTemplateProperty); }
            set { SetValue(MinorTickMarkTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MinorTickMarkTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinorTickMarkTemplateProperty =
            DependencyProperty.Register(nameof(MinorTickMarkTemplate), typeof(DataTemplate), typeof(TickBar));

        /// <summary>
        /// Gets or sets the item template for each tick mark.
        /// </summary>
        /// <value>The item template.</value>
        public DataTemplate MajorTickMarkTemplate
        {
            get { return (DataTemplate)GetValue(MajorTickMarkTemplateProperty); }
            set { SetValue(MajorTickMarkTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MajorTickMarkTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MajorTickMarkTemplateProperty =
            DependencyProperty.Register(nameof(MajorTickMarkTemplate), typeof(DataTemplate), typeof(TickBar));

        /// <summary>
        /// Gets or sets the orientation.
        /// </summary>
        /// <value>The orientation.</value>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Orientation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(TickBar), new PropertyMetadata(Orientation.Horizontal, OnOrientationPropertyChanged));

        private static void OnOrientationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Invoke a layout pass to account for the orientation of ticks changing
            ((TickBar)d).InvalidateMeasureAndArrange();
        }

        /// <summary>
        /// Gets or sets the fill color for each tick mark.
        /// </summary>
        /// <value>The item template.</value>
        public Brush TickFill
        {
            get { return (Brush)GetValue(TickFillProperty); }
            set { SetValue(TickFillProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TickFill"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickFillProperty =
            DependencyProperty.Register(nameof(TickFill), typeof(Brush), typeof(TickBar), null);

        /// <summary>
        /// Gets or sets whether to display labels on the ticks
        /// </summary>
        /// <value>The item template.</value>
        public bool ShowTickLabels
        {
            get { return (bool)GetValue(ShowTickLabelsProperty); }
            set { SetValue(ShowTickLabelsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ShowTickLabels"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowTickLabelsProperty =
            DependencyProperty.Register(nameof(ShowTickLabels), typeof(bool), typeof(TickBar), new PropertyMetadata(OnShowTickLabelsPropertyChanged));

        private static void OnShowTickLabelsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Invoke a layout pass to account for tick labels being shown or hidden
            ((TickBar)d).InvalidateMeasureAndArrange();
        }


        /// <summary>
        /// Gets or sets the string format to use for displaying the tick labels
        /// </summary>
        public string TickLabelFormat
        {
            get { return (string)GetValue(TickLabelFormatProperty); }
            set { SetValue(TickLabelFormatProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TickLabelFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickLabelFormatProperty =
            DependencyProperty.Register(nameof(TickLabelFormat), typeof(string), typeof(TickBar),
                new PropertyMetadata(OnTickLabelFormatPropertyChanged));

        private static void OnTickLabelFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tickbar = (TickBar)d;

            // Update the label format string for each of the major ticks
            foreach (var majorTick in tickbar.MajorTickMarks)
            {
                tickbar.ApplyTickLabelFormat(majorTick, e.NewValue as string);
            }

            // Invoke a layout pass to accommodate new label sizes
            tickbar.InvalidateMeasureAndArrange();
        }

        private void ApplyTickLabelFormat(ContentPresenter tick, string tickLabelFormat)
        {
            // The tick element must be loaded to access its visual tree
            if (tick.IsLoaded)
            {
                // Find the tick label in the visual tree
                var tickTemplate = tick.ContentTemplate;
                var labelTextBlock = tickTemplate.FindName("TickLabel", tick) as TextBlock;
                labelTextBlock.UpdateStringFormat(
                    targetProperty: TextBlock.TextProperty,
                    stringFormat: tickLabelFormat,
                    fallbackFormat: ref OriginalTickLabelFormat);
            }
            else // !tick.IsLoaded
            {
                // Defer the method call until the tick element is loaded
                RoutedEventHandler tickLoadedHandler = null;
                tickLoadedHandler = (o, e) =>
                {
                    ApplyTickLabelFormat(tick, tickLabelFormat);
                    tick.Loaded -= tickLoadedHandler;
                };
                tick.Loaded += tickLoadedHandler;
            }
        }


        private void InvalidateMeasureAndArrange()
        {
            InvalidateMeasure();
            InvalidateArrange();
        }
    }
}
