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

#if !XAMARIN

using System.Collections.Generic;
using System.Text;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if NETFX_CORE
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class Tickbar : Panel
    {
        private const string _template =
            "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
            "<TextBlock Text=\"|\" VerticalAlignment=\"Center\" HorizontalAlignment=\"Center\" />" +
            "</DataTemplate>";

        private string _originalTickLabelFormat;
        private static DataTemplate _defaultTickmarkTemplate;

        internal static readonly DependencyProperty PositionProperty =
            DependencyProperty.RegisterAttached("Position", typeof(double), typeof(Tickbar), new PropertyMetadata(0.0));

        internal static readonly DependencyProperty IsMajorTickmarkProperty =
            DependencyProperty.RegisterAttached("IsMajorTickmark", typeof(bool), typeof(Tickbar), new PropertyMetadata(false));

        private void Initialize()
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
        protected override Size ArrangeOverride(Size finalSize) => OnArrange(finalSize);

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize) => OnMeasure(availableSize);

        /// <summary>
        /// Gets or sets the tick mark positions.
        /// </summary>
        /// <value>The tick mark positions.</value>
        /// <remarks>The tick mark position values should be between 0 and 1.  They represent proportional positions along the tick bar.</remarks>
        private IEnumerable<double> TickmarkPositionsImpl
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

        private static void OnTickmarkPositionsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((Tickbar)d).OnTickmarkPositionsPropertyChanged(e.NewValue as IEnumerable<double>, e.OldValue as IEnumerable<double>);

        /// <summary>
        /// Gets or sets the data sources for the tick marks.  These can be bound to in the tick bar's tick templates.
        /// </summary>
        /// <value>The data source objects.</value>
        private IEnumerable<object> TickmarkDataSourcesImpl
        {
            get { return (IEnumerable<object>)GetValue(TickmarkDataSourcesProperty); }
            set { SetValue(TickmarkDataSourcesProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TickmarkDataSources"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickmarkDataSourcesProperty =
            DependencyProperty.Register(nameof(TickmarkDataSources), typeof(IEnumerable<object>), typeof(Tickbar),
            new PropertyMetadata(default(IEnumerable<object>), OnTickmarkDataSourcesPropertyChanged));

        private static void OnTickmarkDataSourcesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((Tickbar)d).OnTickmarkDataSourcesPropertyChanged(e.NewValue as IEnumerable<object>);

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
            ContentPresenter c = new ContentPresenter()
            {
                VerticalAlignment = VerticalAlignment.Top,
                Content = dataSource,
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
                Content = dataSource,
            };
            c.SetValue(PositionProperty, position);
            c.SetValue(IsMajorTickmarkProperty, true);
            c.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding()
            {
                Source = this,
                Path = new PropertyPath(nameof(MajorTickmarkTemplate)),
            });

            if (TickLabelFormat != null)
            {
                ApplyTickLabelFormat(c, TickLabelFormat);
            }

            Children.Add(c);
            _majorTickmarks.Add(c);
        }

        private bool GetIsMajorTickmark(DependencyObject view) => (bool)view.GetValue(IsMajorTickmarkProperty);

        private void SetIsMajorTickmark(DependencyObject view, bool isMajorTickmark) =>
            view.SetValue(IsMajorTickmarkProperty, isMajorTickmark);

        private double GetPosition(DependencyObject view) => (double)view.GetValue(PositionProperty);

        private void SetPosition(DependencyObject view, double position) => view.SetValue(PositionProperty, position);

        /// <summary>
        /// Gets or sets the item template for each minor tick mark.
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
        /// Gets or sets the item template for each major tick mark.
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
        /// Gets or sets the fill color for each tick mark.
        /// </summary>
        private Brush TickFillImpl
        {
            get { return (Brush)GetValue(TickFillProperty); }
            set { SetValue(TickFillProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TickFill"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickFillProperty =
            DependencyProperty.Register(nameof(TickFill), typeof(Brush), typeof(Tickbar), null);

        /// <summary>
        /// Gets or sets the fill color for each tick mark.
        /// </summary>
        private Brush TickLabelColorImpl
        {
            get { return (Brush)GetValue(TickLabelColorProperty); }
            set { SetValue(TickLabelColorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TickLabelColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickLabelColorProperty =
            DependencyProperty.Register(nameof(TickLabelColor), typeof(Brush), typeof(Tickbar), null);

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets whether to display labels on the ticks.
        /// </summary>
        /// <value>The item template.</value>
        private bool ShowTickLabelsImpl
        {
            get { return (bool)GetValue(ShowTickLabelsProperty); }
            set { SetValue(ShowTickLabelsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ShowTickLabels"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowTickLabelsProperty =
            DependencyProperty.Register(nameof(ShowTickLabels), typeof(bool), typeof(Tickbar),
            new PropertyMetadata(default(bool), OnShowTickLabelsPropertyChanged));

        private static void OnShowTickLabelsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((Tickbar)d).OnShowTickLabelsPropertyChanged();

        /// <summary>
        /// Gets or sets the string format to use for displaying the tick labels.
        /// </summary>
        private string TickLabelFormatImpl
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

        private static void OnTickLabelFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((Tickbar)d).OnTickLabelFormatPropertyChanged(e.NewValue as string);

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
            else
            {
                // Children are not populated yet.  Wait for tick to load.
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

        private Size GetDesiredSize(UIElement el) => el.DesiredSize;

        private int ChildCount => Children.Count;
    }
}

#endif