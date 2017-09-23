// /*******************************************************************************
//  * Copyright 2012-2016 Esri
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
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The Legend control is used to display symbology and description for a set of <see cref="Layer"/>s
    /// in a <see cref="Map"/> or <see cref="Scene"/> contained in a <see cref="GeoView"/>.
    /// </summary>
    [TemplatePart(Name = "List", Type = typeof(ItemsControl))]
    public class Legend : LayerList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Legend"/> class.
        /// </summary>
        public Legend()
            : base()
        {
            DefaultStyleKey = typeof(Legend);
        }

        /// <inheritdoc/>
        protected override bool RespectScaleRangeInternal => RespectScaleRange;

        /// <summary>
        /// Gets or sets a value indicating whether the scale of <see cref="GeoView"/> and any scale ranges on the <see cref="Layer"/>s
        /// are used to determine when legend for layer is displayed.
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, legend for layer is displayed only when layer is in visible scale range;
        /// otherwise, <c>false</c>, legend for layer is displayed regardless of its scale range.
        /// </remarks>
        public bool RespectScaleRange
        {
            get { return (bool)GetValue(RespectScaleRangeProperty); }
            set { SetValue(RespectScaleRangeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="RespectScaleRange"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RespectScaleRangeProperty =
            DependencyProperty.Register(nameof(RespectScaleRange), typeof(bool), typeof(Legend), new PropertyMetadata(true, OnRespectScaleRangePropertyChanged));

        private static void OnRespectScaleRangePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Legend)d).RebuildList();
        }

        /// <summary>
        /// Gets or sets the title used by control.
        /// </summary>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(Legend), new PropertyMetadata("Legend"));
    }
}
#endif