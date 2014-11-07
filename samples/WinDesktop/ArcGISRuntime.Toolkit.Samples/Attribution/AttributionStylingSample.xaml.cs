using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace ArcGISRuntime.Toolkit.Samples.Desktop.Attribution
{
	/// <summary>
	/// Demonstrates how to style attribution control.
	/// </summary>
	/// <title>Styling Attribution</title>
	/// <category>Toolkit</category>
	/// <subcategory>Attribution</subcategory>
	/// <usesoffline>false</usesoffline>
	/// <usesonline>true</usesonline>
	public partial class AttributionStylingSample : UserControl
	{
		public AttributionStylingSample()
		{
			InitializeComponent();
		}
	}

	public class StringJoinConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is IEnumerable<string>)
			{
				return string.Join(parameter is string ? (string)parameter : "", (value as IEnumerable<string>).ToArray());
			}
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
