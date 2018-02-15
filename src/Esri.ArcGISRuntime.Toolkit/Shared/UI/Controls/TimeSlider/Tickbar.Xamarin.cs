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

#if __IOS__ || __ANDROID__

using System.Collections.Generic;
using System.Text;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if __IOS__
using Brush = UIKit.UIColor;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public partial class Tickbar
    {
        IEnumerable<double> _tickmarkPositions;

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
                OnTickmarkPositionsPropertyChanged(oldValue, _tickmarkPositions);
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

        private Brush _tickFill;
        
        /// <summary>
        /// Gets or sets the fill color for each tick mark
        /// </summary>
        private Brush TickFillImpl
        {
            get => _tickFill;
            set
            {
                _tickFill = value;

                // TODO - apply fill to ticks
            }
        }

        private Brush _tickLabelColor;

        /// <summary>
        /// Gets or sets the fill color for each tick mark
        /// </summary>
        private Brush TickLabelColorImpl
        {
            get => _tickLabelColor;
            set
            {
                _tickLabelColor = value;

                // TODO - apply color to tick labels
            }
        }

        private bool _showTicklabels;

        /// <summary>
        /// Gets or sets whether to display labels on the ticks
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
        /// Gets or sets the string format to use for displaying the tick labels
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