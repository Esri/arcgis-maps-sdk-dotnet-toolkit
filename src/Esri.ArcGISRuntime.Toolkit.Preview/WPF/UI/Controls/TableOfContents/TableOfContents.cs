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

using System.Windows;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Preview.UI.Controls
{
    /// <summary>
    /// A control that creates a table of content tree view from a <see cref="GeoView"/>.
    /// </summary>
    [TemplatePart(Name = "List", Type = typeof(ItemsControl))]
    public class TableOfContents : LayerList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableOfContents"/> class.
        /// </summary>
        public TableOfContents()
            : base()
        {
            DefaultStyleKey = typeof(TableOfContents);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show a legend for the layers in the tree view
        /// </summary>
        public bool ShowLegend
        {
            get { return (bool)GetValue(ShowLegendProperty); }
            set { SetValue(ShowLegendProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ShowLegend"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowLegendProperty =
            DependencyProperty.Register(nameof(ShowLegend), typeof(bool), typeof(TableOfContents), new PropertyMetadata(true, OnShowLegendPropertyChanged));

        private static void OnShowLegendPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Use reflection until this moves into the main package
            var prop = typeof(LayerList).GetProperty("ShowLegendInternal", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            prop.SetValue(d, e.NewValue);

            // ((TableOfContents)d).ShowLegendInternal = (bool)e.NewValue;
        }
    }
}