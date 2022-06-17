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

#if !__IOS__ && !__ANDROID__
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ToggleButton = Windows.UI.Xaml.Controls.ToggleSwitch;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Control for selecting a single color from a short list of colors.
    /// </summary>
    internal class ToolkitColorPalette : Control
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolkitColorPalette"/> class.
        /// </summary>
        public ToolkitColorPalette()
        {
            DefaultStyleKey = typeof(ToolkitColorPalette);
            AvailableColors = new ObservableCollection<System.Drawing.Color>(new[]
            {
                System.Drawing.Color.Red,
                System.Drawing.Color.Orange,
                System.Drawing.Color.Yellow,
                System.Drawing.Color.Green,
                System.Drawing.Color.Blue,
                System.Drawing.Color.Indigo,
                System.Drawing.Color.HotPink,
                System.Drawing.Color.Black,
            });
        }

        /// <summary>
        /// Gets or sets the selected color.
        /// </summary>
        public System.Drawing.Color SelectedColor
        {
            get => (System.Drawing.Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(nameof(SelectedColor), typeof(System.Drawing.Color), typeof(ToolkitColorPalette), new PropertyMetadata(System.Drawing.Color.Black, OnSelectionChanged));


        private static void OnSelectionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as ToolkitColorPalette)?.SelectionChanged?.Invoke(sender, EventArgs.Empty);
        }

        /// <summary>
        /// Gets or sets the list of available colors.
        /// </summary>
        public IList<System.Drawing.Color>? AvailableColors
        {
            get => GetValue(AvailableColorsProperty) as IList<System.Drawing.Color>;
            set => SetValue(AvailableColorsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AvailableColors"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AvailableColorsProperty =
            DependencyProperty.Register(nameof(AvailableColors), typeof(IList<System.Drawing.Color>), typeof(ToolkitColorPalette), null);

        public event EventHandler SelectionChanged;
    }
}
#endif