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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// *FOR INTERNAL USE* Allows converting an object to a formatted string representation
    /// </summary>
    public class StringFormatConverter : IValueConverter
    {
        /// <summary>
        /// Converts the bound object into its string representation and formats it according to the format
        /// string specified by the converter parameter
        /// </summary>
        /// <param name="value">The object to convert</param>
        /// <param name="targetType">The type of the target property</param>
        /// <param name="parameter">The format string to use when formatting the bound object</param>
        /// <param name="language">The language of the conversion</param>
        /// <returns>The formatted string if the conversion is successful, otherwise, the bound object</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && parameter is string formatString)
            {
                return string.Format(formatString, value);
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Not supported.  Will throw an exception if called.
        /// </summary>
        /// <exception cref="NotSupportedException" />
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
