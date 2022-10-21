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

using System;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Symbology;
#if __IOS__
using Control = UIKit.UIView;
#elif __ANDROID__
using Control = Android.Widget.ImageView;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// A control that renders a <see cref="Esri.ArcGISRuntime.Symbology.Symbol"/>.
    /// </summary>
    public partial class SymbolDisplay : Control
    {
        private Internal.WeakEventListener<SymbolDisplay, System.ComponentModel.INotifyPropertyChanged, object?, System.ComponentModel.PropertyChangedEventArgs>? _inpcListener;
        private Task? _currentUpdateTask;
        private bool _isRefreshRequired;

#if !__ANDROID__
        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDisplay"/> class.
        /// </summary>
        public SymbolDisplay() => Initialize();
#endif

        /// <summary>
        /// Gets or sets the symbol to render.
        /// </summary>
        public Symbology.Symbol? Symbol
        {
            get => SymbolImpl;
            set => SymbolImpl = value;
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
                _inpcListener = new Internal.WeakEventListener<SymbolDisplay, System.ComponentModel.INotifyPropertyChanged, object?, System.ComponentModel.PropertyChangedEventArgs>(this, newValue)
                {
                    OnEventAction = static (instance, source, eventArgs) =>
                    {
                        instance.Refresh();
                    },
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                newValue.PropertyChanged += _inpcListener.OnEvent;
            }

            Refresh();
        }

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
        // Visual Studio knows that CS0108 doesn't apply because this code isn't includes in .NET standard build,
        // which causes IDE0079
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS0108
        /// <summary>
        /// Triggered when the image source has updated
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#if NETCOREAPP
        public new event System.EventHandler? SourceUpdated;
#else
        public event System.EventHandler? SourceUpdated;
#endif
#pragma warning restore CS0108
#pragma warning restore IDE0079
    }
}