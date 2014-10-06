using Esri.ArcGISRuntime.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace ArcGISRuntime.Toolkit.Samples.Windows.ScaleLine
{
	/// <summary>
	/// Demonstrates how to style ScaleLine control.
	/// </summary>
	/// <title>Styling ScaleLine</title>
	/// <category>Toolkit</category>
	/// <subcategory>ScaleLine</subcategory>
	/// <usesoffline>false</usesoffline>
	/// <usesonline>true</usesonline>
	public sealed partial class ScaleLineStylingSample : Page
	{
		public ScaleLineStylingSample()
		{
			this.InitializeComponent();
			
			object stylesDictionary = null;
	        Resources.TryGetValue("Styles", out stylesDictionary);

			var styles = new List<Tuple<string, Style>>();
			foreach (var item in stylesDictionary as ResourceDictionary)
			{
				styles.Add(Tuple.Create(item.Key.ToString(), item.Value as Style));
			}

			styles.Sort();

			StyleComboBox.ItemsSource = styles;
		}

		private void MyMapView_LayerLoaded(object sender, LayerLoadedEventArgs e)
		{
			if (e.LoadError == null)
				return;

			Debug.WriteLine(string.Format("Error while loading layer : {0} - {1}", e.Layer.ID, e.LoadError.Message));
		}
	}

	public class MultiplicationConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
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

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
