using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace ArcGISRuntime.Toolkit.Samples.Windows.Attribution
{
	/// <summary>
	/// Demonstrates how to style attribution control.
	/// </summary>
	/// <title>Styling Attribution</title>
	/// <category>Toolkit</category>
	/// <subcategory>Attribution</subcategory>
	/// <usesoffline>false</usesoffline>
	/// <usesonline>true</usesonline>
	public sealed partial class AttributionStylingSample : Page
	{
		public AttributionStylingSample()
		{
			this.InitializeComponent();
		}
	}

	public class StringJoinConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is IEnumerable<string>)
			{
				return string.Join(parameter is string ? (string)parameter : "", (value as IEnumerable<string>).ToArray());
			}
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
