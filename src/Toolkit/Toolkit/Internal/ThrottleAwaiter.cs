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

using System.Threading;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// The throttle awaiter is useful for limiting the number of times a block of code executes in cases where that code is repeatly
    /// called many times within a certain interval, but you only want the code executed once.  Awaiting the ThrottleDelay method will
    /// delay code continuation until a set interval has elapsed without any other calls to ThrottleDelay on the same instance.
    /// </summary>
    internal class ThrottleAwaiter
    {
        private Timer _throttleTimer;
        private int _interval;
        private TaskCompletionSource<bool> _throttleTcs = new TaskCompletionSource<bool>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottleAwaiter"/> class.
        /// </summary>
        /// <param name="milliseconds">Milliseconds to throttle.</param>
        public ThrottleAwaiter(int milliseconds)
        {
            _throttleTimer = new Timer((o) => _throttleTcs.TrySetResult(true), null, Timeout.Infinite, Timeout.Infinite);
            _interval = milliseconds;
        }

        /// <summary>
        /// Invokes the throttled delay.
        /// </summary>
        /// <returns>Task.</returns>
        public Task ThrottleDelay()
        {
            if (_throttleTcs == null || _throttleTcs.Task.IsCompleted || _throttleTcs.Task.IsFaulted)
            {
                _throttleTcs = new TaskCompletionSource<bool>();
            }

            _throttleTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _throttleTimer.Change(_interval, Timeout.Infinite);

            return _throttleTcs.Task;
        }

        /// <summary>
        /// Cancels an in-progress delay interval.
        /// </summary>
        internal void Cancel()
        {
            _throttleTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _throttleTcs.TrySetCanceled();
        }
    }
}