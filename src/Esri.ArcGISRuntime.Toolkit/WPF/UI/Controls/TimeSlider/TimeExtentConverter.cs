using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.Toolkit
{
    /// <summary>
    ///  *FOR INTERNAL USE ONLY* Provides a way of converting <see cref="TimeExtent"/> to and from a string representation.
    /// </summary>
    /// <exclude/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TimeExtentConverter : TypeConverter
    {
        /// <summary>
        ///     Returns whether this converter can convert an object of the given type to
        /// the type of this converter, using the specified context.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="sourceType">A System.Type that represents the type you want to convert from.</param>
        /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || sourceType == typeof(TimeExtent);
        }

        /// <summary>
        /// Returns whether this converter can convert the object to the specified type,
        /// using the specified context.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="destinationType">A System.Type that represents the type you want to convert to.</param>
        /// <returns>Returns whether this converter can convert the object to the specified type.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || destinationType == typeof(TimeExtent) || base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the given object to the type of this converter, using the specified
        /// context and culture information.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">The System.Globalization.CultureInfo to use as the current culture.</param>
        /// <param name="value">The System.Object to convert.</param>
        /// <returns>An System.Object that represents the converted value.</returns>
        /// <exception cref="System.NotSupportedException">The conversion cannot be performed.</exception>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return ConvertFromString(value as string);
        }

        /// <summary>
        /// Converts the given value object to the specified type, using the specified
        /// context and culture information.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">A System.Globalization.CultureInfo. If null is passed, the current culture is assumed.</param>
        /// <param name="value">The System.Object to convert.</param>
        /// <param name="destinationType">The System.Type to convert the value parameter to.</param>
        /// <returns>An System.Object that represents the converted value.</returns>
        /// <exception cref="System.NotSupportedException">The conversion cannot be performed.</exception>
        /// <exception cref="System.ArgumentNullException">The destinationType parameter is null.</exception>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }
            if (destinationType == typeof(string) && value is TimeExtent)
            {
                return ConvertToString((TimeExtent)value);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }


        private TimeExtent ConvertFromString(string timeExtentString)
        {
            if (string.IsNullOrEmpty(timeExtentString))
                return null;

            TimeExtent timeExtent = null;
            var timeExtentParts = timeExtentString.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            if (timeExtentParts.Length == 0 || timeExtentParts.Length > 2)
                return null;

            DateTimeOffset startTime;
            if (!DateTimeOffset.TryParse(timeExtentParts[0], out startTime))
                return null;

            DateTimeOffset endTime;
            if (timeExtentParts.Length == 2 && DateTimeOffset.TryParse(timeExtentParts[1], out endTime))
                timeExtent = new TimeExtent(startTime, endTime);
            else
                timeExtent = new TimeExtent(startTime);

            return timeExtent;
        }

        private string ConvertToString(TimeExtent timeExtent)
        {
            if (timeExtent == null)
                return null;

            if (timeExtent.StartTime == timeExtent.EndTime)
                return timeExtent.StartTime.ToString();
            else
                return $"{timeExtent.StartTime.ToString()} - {timeExtent.EndTime.ToString()}";
        }
    }
}
