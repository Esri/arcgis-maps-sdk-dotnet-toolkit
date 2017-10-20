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

using System.Threading.Tasks;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
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
    /// A control that renders a <see cref="Esri.ArcGISRuntime.Symbology.Symbol"/>.
    /// </summary>
    public class SymbolDisplay : Control
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDisplay"/> class.
        /// </summary>
        public SymbolDisplay()
        {
            DefaultStyleKey = typeof(SymbolDisplay);
        }

        /// <inheritdoc />
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            RefreshSymbol();
        }

        /// <summary>
        /// Gets or sets the symbol to render
        /// </summary>
        public Esri.ArcGISRuntime.Symbology.Symbol Symbol
        {
            get { return (Esri.ArcGISRuntime.Symbology.Symbol)GetValue(SymbolProperty); }
            set { SetValue(SymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Symbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SymbolProperty =
            DependencyProperty.Register(nameof(Symbol), typeof(Symbology.Symbol), typeof(SymbolDisplay), new PropertyMetadata(null, OnSymbolPropertyChanged));

        private static void OnSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as SymbolDisplay)?.RefreshSymbol();
        }

        private async void RefreshSymbol()
        {
            var img = GetTemplateChild("image") as Image;
            if (img == null)
            {
                return;
            }

            if (Symbol == null)
            {
                img.Source = null;
                img.Width = 0;
                img.Height = 0;
            }
            else
            {
#pragma warning disable ESRI1800 // Add ConfigureAwait(false) - This is UI Dependent code and must return to UI Thread
                try
                {
                    var scale = GetScaleFactor();
                    var imageData = await Symbol.CreateSwatchAsync(scale * 96);
                    img.Width = imageData.Width / scale;
                    img.Height = imageData.Height / scale;
                    img.Source = await imageData.ToImageSourceAsync();
                }
                catch
                {
                    img.Source = null;
                }
#pragma warning restore ESRI1800
            }
        }

        private double GetScaleFactor()
        {
            if (!DesignTime.IsDesignMode)
            {
#if NETFX_CORE
                return Windows.Graphics.Display.DisplayInformation.GetForCurrentView()?.RawPixelsPerViewPixel ?? 1;
#else
                var visual = PresentationSource.FromVisual(this);
                if (visual != null)
                    return visual.CompositionTarget.TransformToDevice.M11;
#endif
            }

            return 1;
        }
    }
}
#endif