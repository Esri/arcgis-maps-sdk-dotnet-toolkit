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

#if !XAMARIN
using System;
using System.Globalization;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#else
using System.Windows;
using System.Windows.Data;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// *FOR INTERNAL USE* Returns visibility for the given load status.
    /// </summary>
    internal sealed class LoadStatusToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc />
        object IValueConverter.Convert(object? value, Type targetType, object? parameter,
#if NETFX_CORE
            string language)
#else
            CultureInfo culture)
#endif
        {
            if (DesignTime.IsDesignMode)
            {
                if (parameter is string statusParameter)
                {
                    switch (statusParameter)
                    {
                        case "Loaded":
                            return Visibility.Visible;
                        case "NotLoaded":
                            return Visibility.Collapsed;
                        case "Loading":
                            return Visibility.Collapsed;
                        case "FailedToLoad":
                            return Visibility.Collapsed;
                    }

                    return true;
                }
            }

            if (value is LoadStatus status && parameter is string parameterString)
            {
                if (parameterString == "Loaded" && status == LoadStatus.Loaded)
                {
                    return Visibility.Visible;
                }
                else if (parameterString == "Loading" && status == LoadStatus.Loading)
                {
                    return Visibility.Visible;
                }
                else if (parameterString == "FailedToLoad" && status == LoadStatus.FailedToLoad)
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        /// <inheritdoc />
        object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter,
#if NETFX_CORE
            string language)
#else
            CultureInfo culture)
#endif
        {
            throw new NotSupportedException();
        }
    }
}
#endif
