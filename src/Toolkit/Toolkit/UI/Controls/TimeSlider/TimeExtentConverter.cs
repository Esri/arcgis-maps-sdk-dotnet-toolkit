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

#if !NETFX_CORE && !__IOS__ && !__ANDROID__

using System;
using System.ComponentModel;
using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit
{
    /// <summary>
    /// *FOR INTERNAL USE ONLY* Provides a way of converting <see cref="TimeExtent"/> to and from a string representation.
    /// </summary>
    /// <exclude/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TimeExtentConverter : TypeConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeExtentConverter"/> class.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TimeExtentConverter()
        {
        }

        /// <inheritdoc />
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || sourceType == typeof(TimeExtent);
        }

        /// <inheritdoc />
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || destinationType == typeof(TimeExtent) || base.CanConvertTo(context, destinationType);
        }

        /// <inheritdoc />
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return Convert(value as string, culture);
        }

        /// <inheritdoc />
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == typeof(string) && value is TimeExtent)
            {
                return Convert((TimeExtent)value, culture);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        private TimeExtent Convert(string timeExtentString, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(timeExtentString))
            {
                return null;
            }

            var timeExtentParts = timeExtentString.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            if (timeExtentParts.Length == 0)
            {
                return null;
            }

            if (timeExtentParts.Length > 2)
            {
                throw new FormatException("Multiple date splitters found in TimeExtent string");
            }

            DateTimeOffset startTime = DateTimeOffset.Parse(timeExtentParts[0], culture, DateTimeStyles.AssumeUniversal);

            if (timeExtentParts.Length == 2)
            {
                DateTimeOffset endTime = DateTimeOffset.Parse(timeExtentParts[1], culture, DateTimeStyles.AssumeUniversal);
                return new TimeExtent(startTime, endTime);
            }
            else
            {
                return new TimeExtent(startTime);
            }
        }

        private string Convert(TimeExtent timeExtent, CultureInfo culture)
        {
            if (timeExtent == null)
            {
                return null;
            }

            if (timeExtent.StartTime == timeExtent.EndTime)
            {
                return timeExtent.StartTime.ToString(culture);
            }
            else
            {
                return $"{timeExtent.StartTime.ToString(culture)} - {timeExtent.EndTime.ToString(culture)}";
            }
        }
    }
}

#endif