using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Linq;
using System.Collections.Generic;

namespace ArcGISRuntime.Toolkit.Samples.Desktop.ScaleLine
{
	/// <summary>
	/// Demonstrates how to style ScaleLine control.
	/// </summary>
	/// <title>Styling ScaleLine</title>
	/// <category>Toolkit</category>
	/// <subcategory>ScaleLine</subcategory>
	/// <usesoffline>false</usesoffline>
	/// <usesonline>true</usesonline>
	public partial class ScaleLineStylingSample : UserControl
	{
		public ScaleLineStylingSample()
		{
			InitializeComponent();
			var stylesDictionary = Resources["Styles"] as ResourceDictionary;

			var styles = new List<Tuple<string, Style>>();
			foreach (var key in stylesDictionary.Keys)
			{
				var value = stylesDictionary[key] as Style;
				styles.Add(Tuple.Create(key.ToString(), value));
			}
			
			styles.Sort();
			StyleComboBox.ItemsSource = styles;
		}
	}

	public class MultiplicationConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null || (value is string && string.IsNullOrEmpty(value as string)))
				return value;
			double d = System.Convert.ToDouble(value);
			double frac = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
			double ret = d * frac;
			if (targetType == typeof(double))
			{
				return ret;
			}
			if (targetType == typeof(float))
			{
				return (float)ret;
			}
			if (targetType == typeof(int))
			{
				return (int)Math.Round(ret);
			}
			if (targetType == typeof(string))
			{
				return ret.ToString(System.Globalization.CultureInfo.InvariantCulture);
			}
			throw new NotSupportedException(string.Format("Conversion to {0} not supported", targetType));
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
