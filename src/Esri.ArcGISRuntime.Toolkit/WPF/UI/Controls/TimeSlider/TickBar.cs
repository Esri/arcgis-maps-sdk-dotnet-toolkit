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
using System.Windows;
using System.Windows.Controls;
using System;
using System.Windows.Media;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
	/// <summary>
	/// *FOR INTERNAL USE ONLY* TickBar control used for placing a specified amount of tick marks evenly spread out.
	/// </summary>
	/// <exclude/>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public class TickBar : Panel
	{
		internal static readonly DependencyProperty PositionProperty = DependencyProperty.RegisterAttached("Position", typeof(double), typeof(GeoView), new PropertyMetadata(0.0));
		private const string template = "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><TextBlock Text=\"|\" VerticalAlignment=\"Center\" HorizontalAlignment=\"Center\" /></DataTemplate>";
		private static DataTemplate DefaultTickMarkTemplate;
		
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
			foreach (UIElement child in Children)
			{
				FrameworkElement c = (child as FrameworkElement);
				if (c == null) continue;
				double position = (double)c.GetValue(PositionProperty);
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
				child.Arrange(childBounds);
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
			foreach (UIElement d in this.Children)
			{
				d.Measure(availableSize);
			}
			if (double.IsNaN(height))
			{
				height = 0;
				if (this.Orientation == System.Windows.Controls.Orientation.Horizontal)
				{
					foreach (UIElement d in this.Children)
					{
						height = System.Math.Max(d.DesiredSize.Height, height);
					}
				}
			}
			if (double.IsNaN(width))
			{
				width = 0;
				if(this.Orientation == System.Windows.Controls.Orientation.Vertical)
				{
					foreach (UIElement d in this.Children)
					{
						width = System.Math.Max(d.DesiredSize.Width, width);
					}
				}
			}
			return new Size(width, height);
		}

		/// <summary>
		/// Gets or sets the tick mark positions.
		/// </summary>
		/// <value>The tick mark positions.</value>
		public double[] TickMarkPositions
		{
			get { return (double[])GetValue(TickMarkPositionsProperty); }
			set { SetValue(TickMarkPositionsProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="TickMarkPositions"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TickMarkPositionsProperty =
			DependencyProperty.Register("TickMarkPositions", typeof(double[]), typeof(TickBar), new PropertyMetadata( OnTickMarkPositionsPropertyChanged));

		private static void OnTickMarkPositionsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TickBar bar = (TickBar)d;

            if (bar.TickMarkTemplate == null)
                bar.TickMarkTemplate = DefaultTickMarkTemplate;

            double[] newValue = (double[])e.NewValue;
			double[] oldValue = (double[])e.OldValue;
			int now = newValue == null ? 0 : newValue.Length;
			int before = oldValue == null ? 0 : oldValue.Length;

			if (now < before)
			{
				while (bar.Children.Count > now)
					bar.Children.RemoveAt(bar.Children.Count - 1);
			}
			else if(newValue != null)
			{
				for (int i = before; i < now; i++)
				{
					bar.AddTickmark(newValue[i]);
				}
			}
			bar.InvalidateMeasure();
			bar.InvalidateArrange();
		}

		private void AddTickmark(double position)
		{
			ContentPresenter c = new ContentPresenter();
			c.SetValue(PositionProperty, position);
			c.SetBinding(ContentPresenter.ContentTemplateProperty, new System.Windows.Data.Binding()
			{
				Source = this,
				BindsDirectlyToSource = true,
				Path = new PropertyPath(nameof(TickMarkTemplate))
			});
			Children.Add(c);
		}

		/// <summary>
		/// Gets or sets the item template for each tick mark.
		/// </summary>
		/// <value>The item template.</value>
		public DataTemplate TickMarkTemplate
		{
			get { return (DataTemplate)GetValue(TickMarkTemplateProperty); }
			set { SetValue(TickMarkTemplateProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="TickMarkTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TickMarkTemplateProperty =
			DependencyProperty.Register(nameof(TickMarkTemplate), typeof(DataTemplate), typeof(TickBar), new PropertyMetadata(OnTickMarkTemplatePropertyChanged));

        private static void OnTickMarkTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var nw = e.NewValue;
            var old = e.OldValue;
        }

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
			TickBar bar = (TickBar)d;
			bar.InvalidateMeasure();
			bar.InvalidateArrange();
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

    }
}
