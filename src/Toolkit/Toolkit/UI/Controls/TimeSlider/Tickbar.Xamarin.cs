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

#if XAMARIN

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI;
#if __IOS__
using Color = UIKit.UIColor;
#elif __ANDROID__
using Android.Views;
using Color = Android.Graphics.Color;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class Tickbar
    {
        private IEnumerable<double> _tickmarkPositions;
        private string _defaultTickLabelFormat = "M/d/yyyy";

        /// <summary>
        /// Gets or sets the tick mark positions.
        /// </summary>
        /// <value>The tick mark positions.</value>
        /// <remarks>The tick mark position values should be between 0 and 1.  They represent proportional positions along the tick bar.</remarks>
        private IEnumerable<double> TickmarkPositionsImpl
        {
            get => _tickmarkPositions;
            set
            {
                var oldValue = _tickmarkPositions;
                _tickmarkPositions = value;
                OnTickmarkPositionsPropertyChanged(_tickmarkPositions, oldValue);
            }
        }

        private IEnumerable<object> _tickmarkDataSources;

        private IEnumerable<object> TickmarkDataSourcesImpl
        {
            get => _tickmarkDataSources;
            set
            {
                _tickmarkDataSources = value;
                OnTickmarkDataSourcesPropertyChanged(_tickmarkDataSources);
            }
        }

        private Color _tickFill =
#if __IOS__
            Color.Black;
#elif __ANDROID__
            Color.Rgb(188, 188, 188);
#endif

        /// <summary>
        /// Gets or sets the fill color for each tick mark.
        /// </summary>
        private Color TickFillImpl
        {
            get => _tickFill;
            set
            {
                _tickFill = value;

                if (_majorTickmarks != null)
                {
                    foreach (var tickContainer in _majorTickmarks)
                    {
#if __IOS__
                        var tick = tickContainer.Subviews.OfType<RectangleView>().FirstOrDefault();
#elif __ANDROID__
                        var tick = tickContainer is ViewGroup group ? group.GetChildren().OfType<View>().LastOrDefault() : tickContainer;
#endif
                        tick?.SetBackgroundFill(value);
                    }
                }

                if (_minorTickmarks != null)
                {
                    foreach (var tick in _minorTickmarks)
                    {
                        tick.SetBackgroundFill(value);
                    }
                }
            }
        }

        private Color _tickLabelColor =
#if __IOS__
            Color.Black;
#elif __ANDROID__
            Color.Rgb(188, 188, 188);
#endif

        /// <summary>
        /// Gets or sets the fill color for each tick mark.
        /// </summary>
        private Color TickLabelColorImpl
        {
            get => _tickLabelColor;
            set
            {
                _tickLabelColor = value;

                if (_majorTickmarks == null)
                {
                    return;
                }

                foreach (var tick in _majorTickmarks)
                {
                    ApplyTickLabelColor(tick, value);
                }
            }
        }

        private bool _showTicklabels;

        /// <summary>
        /// Gets or sets a value indicating whether to display labels on the ticks.
        /// </summary>
        /// <value>The item template.</value>
        private bool ShowTickLabelsImpl
        {
            get => _showTicklabels;
            set
            {
                _showTicklabels = value;
                OnShowTickLabelsPropertyChanged();
            }
        }

        private string _tickLabelFormat;

        /// <summary>
        /// Gets or sets the string format to use for displaying the tick labels.
        /// </summary>
        private string TickLabelFormatImpl
        {
            get => _tickLabelFormat;
            set
            {
                _tickLabelFormat = value;
                OnTickLabelFormatPropertyChanged(_tickLabelFormat);
            }
        }
    }
}

#endif