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
#if !__IOS__ && !__ANDROID__

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
    public class Tickbar : Panel
    {
        internal static readonly DependencyProperty PositionProperty =
            DependencyProperty.RegisterAttached("Position", typeof(double), typeof(Tickbar), new PropertyMetadata(0.0));
        internal static readonly DependencyProperty IsMajorTickmarkProperty =
            DependencyProperty.RegisterAttached("IsMajorTickmark", typeof(bool), typeof(Tickbar), new PropertyMetadata(false));
        private const string _template =
            "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
            "<TextBlock Text=\"|\" VerticalAlignment=\"Center\" HorizontalAlignment=\"Center\" />" +
            "</DataTemplate>";
        private static DataTemplate _defaultTickmarkTemplate;
        private List<ContentPresenter> _majorTickmarks = new List<ContentPresenter>();
        private List<ContentPresenter> _minorTickmarks = new List<ContentPresenter>();
        private string _originalTickLabelFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tickbar"/> class.
        /// </summary>
        public Tickbar()
        {
            if (_defaultTickmarkTemplate == null)
            {
#if NETFX_CORE
                _defaultTickmarkTemplate = XamlReader.Load(_template) as DataTemplate;
#else
                System.IO.MemoryStream stream = new System.IO.MemoryStream(UTF8Encoding.Default.GetBytes(_template));
                _defaultTickmarkTemplate = XamlReader.Load(stream) as DataTemplate;
#endif
            }
        }
        
        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (TickmarkPositions == null || TickmarkPositions.Count() < 2)
                return finalSize;

            Rect childBounds = new Rect(0, 0, finalSize.Width, finalSize.Height);
            var majorTickmarksBounds = new List<Rect>();
            var minorTickmarksBounds = new List<Rect>();

            // Iterate all child ticks and calculate bounds for each
            foreach (UIElement child in Children)
            {
                FrameworkElement c = (child as FrameworkElement);
                if (c == null)
                    continue;

                double position = (double)c.GetValue(PositionProperty);
                var isMajorTickmark = (bool)c.GetValue(IsMajorTickmarkProperty);

                if (isMajorTickmark && !ShowTickLabels)
                    continue; // Don't worry about calculating bounds for major ticks if labels are hidden

                // Calculate the bounds of the tick mark
                position = finalSize.Width * position;
                childBounds.X = position - c.DesiredSize.Width * .5;
                childBounds.Width = c.DesiredSize.Width;

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

            if (ShowTickLabels) // Calculate positioning of tick labels and major/minor ticks
            {
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
                        continue;

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
            else // !ShowTickLabels
            {
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

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            double width = availableSize.Width == double.PositiveInfinity ? Width : availableSize.Width;
            double height = availableSize.Height == double.PositiveInfinity ? Height : availableSize.Height;

            // Get the set of ticks that we want to measure.  This could either be all ticks or only minor ticks
            var measuredChildren = ShowTickLabels ? Children.Cast<UIElement>() : Children.Cast<UIElement>().Where(el => !(bool)el.GetValue(IsMajorTickmarkProperty));
            foreach (UIElement d in measuredChildren)
            {
                d.Measure(availableSize);
            }

            if (double.IsNaN(height))
            {
                height = 0;
                foreach (UIElement d in measuredChildren)
                {
                    height = Math.Max(d.DesiredSize.Height, height);
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
            get { return (IEnumerable<double>)GetValue(TickmarkPositionsProperty); }
            set { SetValue(TickmarkPositionsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TickmarkPositions"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickmarkPositionsProperty =
            DependencyProperty.Register(nameof(TickmarkPositions), typeof(IEnumerable<double>),
            typeof(Tickbar), new PropertyMetadata(default(IEnumerable<double>), OnTickmarkPositionsPropertyChanged));

        private static void OnTickmarkPositionsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bar = (Tickbar)d;

            if (bar.MinorTickmarkTemplate == null)
                bar.MinorTickmarkTemplate = _defaultTickmarkTemplate;

            var newTickPositions = (IEnumerable<double>)e.NewValue;
            var oldTickPositions = (IEnumerable<double>)e.OldValue;
            var newTickCount = newTickPositions == null ? 0 : newTickPositions.Count();
            var oldTickCount = oldTickPositions == null ? 0 : oldTickPositions.Count();

            if (newTickCount < oldTickCount)
            {
                // Reduce the number of ticks to the number of positions specified
                for (var i = oldTickCount; i > newTickCount; i--)
                {
                    var tickToRemove = bar._majorTickmarks[i - 1];
                    bar.Children.Remove(tickToRemove);
                    bar._majorTickmarks.Remove(tickToRemove);

                    tickToRemove = bar._minorTickmarks[i - 1];
                    bar.Children.Remove(tickToRemove);
                    bar._minorTickmarks.Remove(tickToRemove);
                }

                // Update the positions of the remaining ticks
                for (var i = 0; i < bar.Children.Count; i++)
                {
                    bar._minorTickmarks[i].SetValue(PositionProperty, newTickPositions.ElementAt(i));
                    bar._majorTickmarks[i].SetValue(PositionProperty, newTickPositions.ElementAt(i));
                }
            }
            else if (newTickPositions != null)
            {
                for (var i = 0; i < newTickCount; i++)
                {
                    if (i < oldTickCount)
                    {
                        // Update positions of existing ticks
                        bar._minorTickmarks[i].SetValue(PositionProperty, newTickPositions.ElementAt(i));
                        bar._majorTickmarks[i].SetValue(PositionProperty, newTickPositions.ElementAt(i));
                    }
                    else
                    {
                        // Add new ticks to bring the number of ticks up to the number of positions specified
                        var tickDataSource = bar.TickmarkDataSources == null || i >= bar.TickmarkDataSources.Count() ? null : bar.TickmarkDataSources.ElementAt(i);
                        bar.AddTickmark(newTickPositions.ElementAt(i), tickDataSource);
                    }
                }
            }
            bar.InvalidateMeasureAndArrange();
        }

        /// <summary>
        /// Gets or sets the data sources for the tick marks.  These can be bound to in the tick bar's tick templates.
        /// </summary>
        /// <value>The data source objects</value>
        public IEnumerable<object> TickmarkDataSources
        {
            get { return (IEnumerable<object>)GetValue(TickmarkDataSourcesProperty); }
            set { SetValue(TickmarkDataSourcesProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TickmarkDataSources"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickmarkDataSourcesProperty =
            DependencyProperty.Register(nameof(TickmarkDataSources), typeof(IEnumerable<object>), typeof(Tickbar),
            new PropertyMetadata(default(IEnumerable<object>), OnTickMarkDataSourcesPropertyChanged));

        private static void OnTickMarkDataSourcesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bar = (Tickbar)d;
            var newDataSources = e.NewValue == null ? new List<object>() : (IEnumerable<object>)e.NewValue;

            for (var i = 0; i < bar._majorTickmarks.Count; i++)
            {
                bar._majorTickmarks[i].Content = i < newDataSources.Count() ? newDataSources.ElementAt(i) : null;
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
            ContentPresenter c = new ContentPresenter()
            {
                VerticalAlignment = VerticalAlignment.Top,
                Content = dataSource
            };
            c.SetValue(PositionProperty, position);
            c.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding()
            {
                Source = this,
                Path = new PropertyPath(nameof(MinorTickmarkTemplate)),
            });
            Children.Add(c);
            _minorTickmarks.Add(c);

            // Create a major tickmark
            c = new ContentPresenter()
            {
                VerticalAlignment = VerticalAlignment.Top,
                Content = dataSource
            };
            c.SetValue(PositionProperty, position);
            c.SetValue(IsMajorTickmarkProperty, true);
            c.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding()
            {
                Source = this,
                Path = new PropertyPath(nameof(MajorTickmarkTemplate))
            });

            if (TickLabelFormat != null)
            {
                ApplyTickLabelFormat(c, TickLabelFormat);
            }
            Children.Add(c);
            _majorTickmarks.Add(c);
        }

        /// <summary>
        /// Gets or sets the item template for each minor tick mark
        /// </summary>
        public DataTemplate MinorTickmarkTemplate
        {
            get { return (DataTemplate)GetValue(MinorTickmarkTemplateProperty); }
            set { SetValue(MinorTickmarkTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MinorTickmarkTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinorTickmarkTemplateProperty =
            DependencyProperty.Register(nameof(MinorTickmarkTemplate), typeof(DataTemplate), typeof(Tickbar), null);

        /// <summary>
        /// Gets or sets the item template for each major tick mark
        /// </summary>
        public DataTemplate MajorTickmarkTemplate
        {
            get { return (DataTemplate)GetValue(MajorTickmarkTemplateProperty); }
            set { SetValue(MajorTickmarkTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MajorTickmarkTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MajorTickmarkTemplateProperty =
            DependencyProperty.Register(nameof(MajorTickmarkTemplate), typeof(DataTemplate), typeof(Tickbar), null);

        /// <summary>
        /// Gets or sets the fill color for each tick mark
        /// </summary>
        public Brush TickFill
        {
            get { return (Brush)GetValue(TickFillProperty); }
            set { SetValue(TickFillProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TickFill"/> dependency property
        /// </summary>
        public static readonly DependencyProperty TickFillProperty =
            DependencyProperty.Register(nameof(TickFill), typeof(Brush), typeof(Tickbar), null);

        /// <summary>
        /// Gets or sets the fill color for each tick mark
        /// </summary>
        public Brush TickLabelColor
        {
            get { return (Brush)GetValue(TickLabelColorProperty); }
            set { SetValue(TickLabelColorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TickLabelColor"/> dependency property
        /// </summary>
        public static readonly DependencyProperty TickLabelColorProperty =
            DependencyProperty.Register(nameof(TickLabelColor), typeof(Brush), typeof(Tickbar), null);

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
        /// Identifies the <see cref="ShowTickLabels"/> dependency property
        /// </summary>
        public static readonly DependencyProperty ShowTickLabelsProperty =
            DependencyProperty.Register(nameof(ShowTickLabels), typeof(bool), typeof(Tickbar),
            new PropertyMetadata(default(bool), OnShowTickLabelsPropertyChanged));

        private static void OnShowTickLabelsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Invoke a layout pass to account for tick labels being shown or hidden
            ((Tickbar)d).InvalidateMeasureAndArrange();
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
            DependencyProperty.Register(nameof(TickLabelFormat), typeof(string), typeof(Tickbar),
                new PropertyMetadata(default(string), OnTickLabelFormatPropertyChanged));

        private static void OnTickLabelFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tickbar = (Tickbar)d;

            // Update the label format string for each of the major ticks
            foreach (var majorTick in tickbar._majorTickmarks)
            {
                tickbar.ApplyTickLabelFormat(majorTick, e.NewValue as string);
            }

            // Invoke a layout pass to accommodate new label sizes
            tickbar.InvalidateMeasureAndArrange();
        }

        private void ApplyTickLabelFormat(ContentPresenter tick, string tickLabelFormat)
        {
            // Check whether the tick element has its children populated
            if (VisualTreeHelper.GetChildrenCount(tick) > 0)
            {
                // Find the tick label in the visual tree
                var contentRoot = VisualTreeHelper.GetChild(tick, 0) as FrameworkElement;
                var labelTextBlock = contentRoot.FindName("TickLabel") as TextBlock;
                labelTextBlock?.UpdateStringFormat(
                    targetProperty: TextBlock.TextProperty,
                    stringFormat: tickLabelFormat,
                    fallbackFormat: ref _originalTickLabelFormat);
            }
            else // Children are not populated yet.  Wait for tick to load.
            {
                // Defer the method call until the tick element is loaded
                void tickLoadedHandler(object o, RoutedEventArgs e)
                {
                    ApplyTickLabelFormat(tick, tickLabelFormat);
                    tick.Loaded -= tickLoadedHandler;
                }

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

#endif