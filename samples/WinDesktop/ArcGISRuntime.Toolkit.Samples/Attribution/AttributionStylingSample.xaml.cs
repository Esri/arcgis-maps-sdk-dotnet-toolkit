using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
