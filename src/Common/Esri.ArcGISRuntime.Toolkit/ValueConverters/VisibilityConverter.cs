using System;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#else
using System.Globalization;
using System.Windows;
using System.Windows.Data;
#endif

namespace Esri.ArcGISRuntime.Toolkit.ValueConverters
{
	/// <summary>
	/// *FOR INTERNAL USE ONLY* Convert a string or a boolean to a Visibility.
	/// </summary>
	/// <exclude/>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public class VisibilityConverter : IValueConverter
	{
		/// <summary>
		/// Modifies the source data before passing it to the target for display in the UI.
		/// </summary>
		/// <param name="value">The source data being passed to the target.</param>
		/// <param name="targetType">The <see cref="T:System.Type"/> of data expected by the target dependency property.</param>
		/// <param name="parameter">An optional parameter to be used in the converter logic.</param>
		/// <param name="culture">The culture of the conversion.</param>
		/// <returns>
		/// The value to be passed to the target dependency property.
		/// </returns>
#if NETFX_CORE
		object IValueConverter.Convert(object value, Type targetType, object parameter, string culture)
#else
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
#endif
		{
			bool vis;
			if (value is string)
				vis = !string.IsNullOrEmpty((string)value);
			else if (value is bool)
				vis = (bool)value;
			else
				vis = false;

			if (parameter is string && ((string)parameter).Equals("reverse", StringComparison.OrdinalIgnoreCase))
				vis = !vis;
			return vis ? Visibility.Visible : Visibility.Collapsed;
		}

		/// <summary>
		/// Modifies the target data before passing it to the source object.  This method is called only in <see cref="F:System.Windows.Data.BindingMode.TwoWay"/> bindings.
		/// </summary>
		/// <param name="value">The target data being passed to the source.</param>
		/// <param name="targetType">The <see cref="T:System.Type"/> of data expected by the source object.</param>
		/// <param name="parameter">An optional parameter to be used in the converter logic.</param>
		/// <param name="culture">The culture of the conversion.</param>
		/// <returns>
		/// The value to be passed to the source object.
		/// </returns>
#if NETFX_CORE
		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string culture)
#else
		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
#endif
		{
			var visibility = (Visibility)value;
			return (visibility == Visibility.Visible);
		}
	}
}
