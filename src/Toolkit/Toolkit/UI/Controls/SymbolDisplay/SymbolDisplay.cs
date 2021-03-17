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

using System.Threading.Tasks;
using Esri.ArcGISRuntime.Symbology;
#if NETFX_CORE
using Windows.UI.Xaml.Controls;
#elif __IOS__
using Control = UIKit.UIView;
#elif __ANDROID__
using Control = Android.Widget.ImageView;
#else
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// A control that renders a <see cref="Esri.ArcGISRuntime.Symbology.Symbol"/>.
    /// </summary>
    public partial class SymbolDisplay : Control
    {
        private Internal.WeakEventListener<System.ComponentModel.INotifyPropertyChanged, object, System.ComponentModel.PropertyChangedEventArgs> _inpcListener;
        private Task _currentUpdateTask;
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
        public Symbology.Symbol Symbol
        {
            get => SymbolImpl;
            set => SymbolImpl = value;
        }

        private void OnSymbolChanged(Symbology.Symbol oldValue, Symbology.Symbol newValue)
        {
            if (oldValue != null)
            {
                _inpcListener?.Detach();
                _inpcListener = null;
            }

            if (newValue != null)
            {
                _inpcListener = new Internal.WeakEventListener<System.ComponentModel.INotifyPropertyChanged, object, System.ComponentModel.PropertyChangedEventArgs>(newValue)
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

        private async void Refresh()
        {
            if (_currentUpdateTask != null)
            {
                // Instead of refreshing immediately when a refresh is already in progress, avoid updating too frequently, but just flag it dirty
                // This avoid multiple refreshes where properties change very frequently, but just the latest state gets refreshed.
                _isRefreshRequired = true;
                return;
            }

            _isRefreshRequired = false;
            var task = _currentUpdateTask = UpdateSwatchAsync();
            await task;
            _currentUpdateTask = null;
            if (_isRefreshRequired)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Triggered when the image source has updated
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public event System.EventHandler SourceUpdated;
    }
}