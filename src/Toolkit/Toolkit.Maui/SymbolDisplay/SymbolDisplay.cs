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

using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    /// <summary>
    /// A control that renders a <see cref="Esri.ArcGISRuntime.Symbology.Symbol"/>.
    /// </summary>
    public partial class SymbolDisplay : TemplatedView
    {
        private WeakEventListener<System.ComponentModel.INotifyPropertyChanged, object?, System.ComponentModel.PropertyChangedEventArgs>? _inpcListener;
        private Task? _currentUpdateTask;
        private bool _isRefreshRequired;
        private static readonly ControlTemplate DefaultControlTemplate;
        private Image? image;

        static SymbolDisplay()
        {
            string template = @"<Image xmlns=""http://schemas.microsoft.com/dotnet/2021/maui"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" x:Name=""image"" Aspect=""AspectFit"" Margin=""{TemplateBinding Padding}""/>";
            DefaultControlTemplate = new ControlTemplate()
            {
                LoadTemplate = () =>
                {
                    return Microsoft.Maui.Controls.Xaml.Extensions.LoadFromXaml(new Image(), template);
                }
            };
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDisplay"/> class.
        /// </summary>
        public SymbolDisplay()
        {
            ControlTemplate = DefaultControlTemplate;
        }

        /// <summary>
        /// Gets or sets the symbol to render.
        /// </summary>
        public Symbology.Symbol? Symbol
        {
            get => (Symbol?)GetValue(SymbolProperty);
            set => SetValue(SymbolProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Symbol"/> bindable property.
        /// </summary>
        public static readonly BindableProperty SymbolProperty =
            BindableProperty.Create(nameof(Symbol), typeof(Symbol), typeof(SymbolDisplay), null, propertyChanged: OnSymbolPropertyChanged);

        private static void OnSymbolPropertyChanged(BindableObject sender, object? oldValue, object? newValue)
        {
            ((SymbolDisplay)sender).OnSymbolChanged(oldValue as Symbol, newValue as Symbol);
        }

        private void OnSymbolChanged(Symbology.Symbol? oldValue, Symbology.Symbol? newValue)
        {
            if (oldValue != null)
            {
                _inpcListener?.Detach();
                _inpcListener = null;
            }

            if (newValue != null)
            {
                _inpcListener = new WeakEventListener<System.ComponentModel.INotifyPropertyChanged, object?, System.ComponentModel.PropertyChangedEventArgs>(newValue)
                {
                    OnEventAction = (instance, source, eventArgs) =>
                    {
                        Refresh();
                    },
                    OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent,
                };
                newValue.PropertyChanged += _inpcListener.OnEvent;
            }

            Refresh();
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            image = GetTemplateChild("image") as Image;
            Refresh();
        }

        private async Task UpdateSwatchAsync()
        {

            if (image is null)
            {
                return;
            }

            if (Symbol == null)
            {
                image.Source = null;
                image.MaximumWidthRequest = 0;
                image.MaximumHeightRequest = 0;
            }
            else
            {
                try
                {
                    var scale = GetScaleFactor();
#pragma warning disable ESRI1800 // Add ConfigureAwait(false) - This is UI Dependent code and must return to UI Thread
                    var imageData = await Symbol.CreateSwatchAsync(scale * 96);
                    image.MaximumWidthRequest = imageData.Width / scale;
                    image.MaximumHeightRequest = imageData.Height / scale;
                    image.Source = await imageData.ToImageSourceAsync();
                    SourceUpdated?.Invoke(this, EventArgs.Empty);
#pragma warning restore ESRI1800
                }
                catch
                {
                    image.Source = null;
                    image.MaximumWidthRequest = 0;
                    image.MaximumHeightRequest = 0;
                }
            }
        }

        private double GetScaleFactor() => Window?.DisplayDensity ?? DeviceDisplay.Current.MainDisplayInfo.Density;

        private async void Refresh()
        {
            try
            {
                if (_currentUpdateTask != null)
                {
                    // Instead of refreshing immediately when a refresh is already in progress, avoid updating too frequently, but just flag it dirty
                    // This avoid multiple refreshes where properties change very frequently, but just the latest state gets refreshed.
                    _isRefreshRequired = true;
                    return;
                }

#pragma warning disable SA1500 // Braces for multi-line statements should not share line

                do
                {
                    _isRefreshRequired = false;
                    var task = _currentUpdateTask = UpdateSwatchAsync();
                    await task;
                } while (_isRefreshRequired);

#pragma warning restore SA1500 // Braces for multi-line statements should not share line

                _currentUpdateTask = null;
            }
            catch (Exception)
            {
                // Ignore
            }
        }
        // Even though this code doesn't apply to .NET standard build, Visual Studio sill warns about it.
        /// <summary>
        /// Triggered when the image source has updated
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public event System.EventHandler? SourceUpdated;
    }
}