/*
(c) Copyright ESRI.
This source is subject to the Microsoft Public License (Ms-PL).
Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
All other rights reserved.
*/

using System;
using System.Timers;


namespace Esri.ArcGISRuntime.Toolkit.Internal
{
	/// <summary>
	/// The throttle timer is useful for limiting the number of requests to a method if
	/// the method is repeatly called many times but you only want the method raised once.
	/// It delays raising the method until a set interval, and any previous calls to the
	/// actions in that interval will be cancelled.
	/// </summary>
	internal class ThrottleTimer
	{
		Timer throttleTimer;

		internal ThrottleTimer(int milliseconds) : this(milliseconds, null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ThrottleTimer"/> class.
		/// </summary>
		/// <param name="milliseconds">Milliseconds to throttle.</param>
		/// <param name="handler">The delegate to invoke.</param>
		internal ThrottleTimer(int milliseconds, Action handler)
		{
			this.Action = handler;
			throttleTimer = new Timer(milliseconds) { AutoReset = true };
			throttleTimer.Elapsed += (s, e) =>
			{
				if (this.Action != null)
					this.Action.Invoke();
                if (!AutoReset)
    				throttleTimer.Stop();
			};
		}

        public bool AutoReset { get; set; }

		/// <summary>
		/// Delegate to Invoke.
		/// </summary>
		/// <value>The action.</value>
		public Action Action { get; set; }

		/// <summary>
		/// Invokes this instance (note that this will happen asynchronously and delayed).
		/// </summary>
		public void Invoke()
		{
			throttleTimer.Stop();
			throttleTimer.Start();
		}

		/// <summary>
		/// Cancels this timer if running.
		/// </summary>
		internal void Cancel()
		{
			throttleTimer.Stop();
		}
	}
}