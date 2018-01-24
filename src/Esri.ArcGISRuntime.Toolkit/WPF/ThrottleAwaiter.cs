/*
(c) Copyright ESRI.
This source is subject to the Microsoft Public License (Ms-PL).
Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
All other rights reserved.
*/

using System.Threading.Tasks;
using System.Timers;


namespace Esri.ArcGISRuntime.Toolkit.Internal
{
	/// <summary>
	/// The throttle awaiter is useful for limiting the number of times a block of code executes in cases where that code is repeatly
    /// called many times within a certain interval, but you only want the code executed once.  Awaiting the ThrottleDelay method will
    /// delay code continuation until a set interval has elapsed without any other calls to ThrottleDelay on the same instance.
	/// </summary>
	internal class ThrottleAwaiter
	{
		Timer _throttleTimer;
        TaskCompletionSource<bool> _throttleTcs = new TaskCompletionSource<bool>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ThrottleAwaiter"/> class.
		/// </summary>
		/// <param name="milliseconds">Milliseconds to throttle.</param>
		public ThrottleAwaiter(int milliseconds)
		{
			_throttleTimer = new Timer(milliseconds) { AutoReset = false };
			_throttleTimer.Elapsed += (s, e) =>
			{
                _throttleTcs.TrySetResult(true);
			};
		}

		/// <summary>
		/// Invokes the throttled delay
		/// </summary>
		public Task ThrottleDelay()
		{
            if (_throttleTcs == null || _throttleTcs.Task.IsCompleted || _throttleTcs.Task.IsFaulted)
                _throttleTcs = new TaskCompletionSource<bool>();

            _throttleTimer.Stop();
			_throttleTimer.Start();

            return _throttleTcs.Task;
		}

		/// <summary>
		/// Cancels an in-progress delay interval
		/// </summary>
		internal void Cancel()
		{
			_throttleTimer.Stop();

            _throttleTcs.TrySetCanceled();
		}
	}
}