﻿// /*******************************************************************************
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
using System.Globalization;
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Internal
{
    /// <summary>
    /// *FOR INTERNAL USE* Returns a boolean representing visibility for a given load status.
    /// </summary>
    internal class LoadStatusToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (DesignMode.IsDesignModeEnabled)
            {
                if (parameter is string statusParameter)
                {
                    switch (statusParameter)
                    {
                        case "Loaded":
                            return true;
                        case "NotLoaded":
                            return false;
                        case "Loading":
                            return false;
                        case "FailedToLoad":
                            return false;
                        default:
                            return true;
                    }
                }
            }

            if (value is LoadStatus status && parameter is string parameterString)
            {
                if (parameterString == "Loaded" && status == LoadStatus.Loaded)
                {
                    return true;
                }
                else if (parameterString == "Loading" && status == LoadStatus.Loading)
                {
                    return true;
                }
                else if (parameterString == "FailedToLoad" && status == LoadStatus.FailedToLoad)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
