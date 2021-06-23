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

#if XAMARIN

using System;
using System.Timers;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Timer for Xamarin use that mimics Windows DispatcherTimer in Android / iOS.
    /// </summary>
    /// <remarks>
    /// - Raises Tick events on the UI thread.
    /// </remarks>
    internal class DispatcherTimer : IDisposable
    {
        private readonly Timer _timer;
        private bool _isInitialized;
        private bool _isDisposed;

        /// <summary>
        /// Gets or sets time interval of Tick event callbacks.
        /// </summary>
        public TimeSpan Interval
        {
            get => TimeSpan.FromMilliseconds(_timer.Interval);
            set => _timer.Interval = value.TotalMilliseconds;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the timer is started or stopped.
        /// </summary>
        public bool IsEnabled
        {
            get => !_isDisposed && _timer.Enabled;
            set => _timer.Enabled = value;
        }

        /// <summary>
        /// Event fired at each Interval
        /// </summary>
        public event EventHandler? Tick;

        public DispatcherTimer()
        {
            _timer = new Timer();
            _timer.AutoReset = true;
            _isInitialized = false;
            _isDisposed = false;
        }

        public void Start()
        {
            // check for a disposed timer
            if (_isDisposed)
            {
                return;
            }

            if (!_isInitialized)
            {
                _timer.Interval = Interval.TotalMilliseconds;
                _timer.Elapsed += Timer_Elapsed;
                _isInitialized = true;
            }

            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var tick = Tick;
            if (tick != null)
            {
                Action action = () =>
                {
                    if (tick != null && _timer.Enabled)
                    {
                        tick(sender, e);
                    }
                };
                Dispatcher.RunAsyncAction(action);
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            if (_timer.Enabled)
            {
                _timer.Stop();
            }

            if (_isInitialized)
            {
                _timer.Elapsed -= Timer_Elapsed;
            }

            _timer.Dispose();
        }
    }
}

#endif