using System;
using System.Collections.Generic;
using System.Globalization;
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
