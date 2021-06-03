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

#if !XAMARIN

using System;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.UI;
#if NETFX_CORE
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// A control that renders a <see cref="Symbology.Symbol"/>.
    /// </summary>
    public partial class SymbolDisplay
    {
        private void Initialize() => DefaultStyleKey = typeof(SymbolDisplay);

        /// <inheritdoc />
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            Refresh();
        }

        /// <summary>
        /// Gets or sets the symbol to render.
        /// </summary>
        private Symbology.Symbol SymbolImpl
        {
            get { return GetValue(SymbolProperty) as Symbology.Symbol; }
            set { SetValue(SymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Symbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SymbolProperty =
            DependencyProperty.Register(nameof(SymbolDisplay.Symbol), typeof(Symbology.Symbol), typeof(SymbolDisplay), new PropertyMetadata(null, OnSymbolPropertyChanged));

        private static void OnSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sd = (SymbolDisplay)d;
            sd.OnSymbolChanged(e.OldValue as Symbology.Symbol, e.NewValue as Symbology.Symbol);
        }

        private async Task UpdateSwatchAsync()
        {
            var img = GetTemplateChild("image") as Image;
            if (img == null)
            {
                return;
            }

            if (Symbol == null)
            {
                img.Source = null;
                img.MaxWidth = 0;
                img.MaxHeight = 0;
            }
            else
            {
                try
                {
                    var scale = GetScaleFactor();
#pragma warning disable ESRI1800 // Add ConfigureAwait(false) - This is UI Dependent code and must return to UI Thread
                    var imageData = await Symbol.CreateSwatchAsync(scale * 96);
                    img.MaxWidth = imageData.Width / scale;
                    img.MaxHeight = imageData.Height / scale;
                    img.Source = await imageData.ToImageSourceAsync();
                    SourceUpdated?.Invoke(this, EventArgs.Empty);
#pragma warning restore ESRI1800
                }
                catch
                {
                    img.Source = null;
                    img.MaxWidth = 0;
                    img.MaxHeight = 0;
                }
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
                {
                    return visual.CompositionTarget.TransformToDevice.M11;
                }
#endif
            }

            return 1;
        }
    }
}
#endif