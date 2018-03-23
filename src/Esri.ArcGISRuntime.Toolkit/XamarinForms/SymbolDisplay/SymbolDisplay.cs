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

using Esri.ArcGISRuntime.Symbology;
using Xamarin.Forms;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// A control that renders a <see cref="Symbology.Symbol"/>.
    /// </summary>
    public class SymbolDisplay : View
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDisplay"/> class
        /// </summary>
        public SymbolDisplay()
            : this(new UI.Controls.SymbolDisplay())
        {
        }

        internal SymbolDisplay(UI.Controls.SymbolDisplay nativeSymbolDisplay)
        {
            NativeSymbolDisplay = nativeSymbolDisplay;

#if NETFX_CORE
            nativeSymbolDisplay.SizeChanged += (o, e) => InvalidateMeasure();
#endif
        }

        internal UI.Controls.SymbolDisplay NativeSymbolDisplay { get; }

        /// <summary>
        /// Identifies the <see cref="Symbol"/> bindable property.
        /// </summary>
        public static readonly BindableProperty SymbolProperty =
            BindableProperty.Create(nameof(Symbol), typeof(Symbol), typeof(SymbolDisplay), null, BindingMode.OneWay, null, OnSymbolPropertyChanged);

        /// <summary>
        /// Gets or sets the symbol to render.
        /// </summary>
        /// <seealso cref="SymbolProperty"/>
        public Symbol Symbol
        {
            get { return (Symbol)GetValue(SymbolProperty); }
            set { SetValue(SymbolProperty, value); }
        }

        private static void OnSymbolPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var symbolDisplay = bindable as SymbolDisplay;
            if (symbolDisplay?.NativeSymbolDisplay != null)
            {
                symbolDisplay.NativeSymbolDisplay.Symbol = newValue as Symbol;
                symbolDisplay.InvalidateMeasure();
            }
        }
    }
}
