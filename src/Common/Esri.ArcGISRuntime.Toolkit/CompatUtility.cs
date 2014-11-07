// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
#else
using System.Windows;
using System.Windows.Threading;
#endif

namespace Esri.ArcGISRuntime.Toolkit
{
	internal static class CompatUtility
	{
		private static bool? _isInDesignMode;
		public static bool IsDesignMode
		{
			get
			{
				if (!_isInDesignMode.HasValue)
				{
#if NETFX_CORE
					_isInDesignMode = Windows.ApplicationModel.DesignMode.DesignModeEnabled;
#else
					var prop = System.ComponentModel.DesignerProperties.IsInDesignModeProperty;
					_isInDesignMode
						= (bool)System.ComponentModel.DependencyPropertyDescriptor
									 .FromProperty(prop, typeof(FrameworkElement))
									 .Metadata.DefaultValue;
#endif
				}
				return _isInDesignMode.Value;
			}
		}

		public static float LogicalDpi(DependencyObject dp = null)
		{
#if NETFX_CORE
			return Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi;
#else
			if (dp == null)
				return 96f;
			else
			{
				System.Windows.Media.Matrix m =
					PresentationSource.FromDependencyObject(dp).CompositionTarget.TransformToDevice;
				return (float)m.M11 * 96.0f;
			}
#endif
		}

#if NETFX_CORE
		public static void ExecuteOnUIThread(Action action, CoreDispatcher dispatcher)
		{
			if (dispatcher.HasThreadAccess)
				action();
			else
			{
				var asyncAction = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
			}
		}
#else
		public static void ExecuteOnUIThread(Action action, Dispatcher dispatcher)
		{
			if (dispatcher.CheckAccess())
				action();
			else
				dispatcher.BeginInvoke(action);
		}
#endif

		// Execute a task in the UI thread
		internal static Task<T> ExecuteOnUIThread<T>(Func<Task<T>> f)
		{
			var dispatcher = GetDispatcher();
			return dispatcher == null ? f() : ExecuteOnUIThread(f, dispatcher);
		}

#if NETFX_CORE
		private static CoreDispatcher GetDispatcher()
		{
			return Application.Current != null && CoreApplication.MainView != null && CoreApplication.MainView.CoreWindow != null && !CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess
				? CoreApplication.MainView.CoreWindow.Dispatcher
				: null;
		}

		private static async Task<T> ExecuteOnUIThread<T>(Func<Task<T>> f, CoreDispatcher dispatcher)
		{
			Debug.Assert(dispatcher != null);
			Task<T> task = null;
			await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { task = f(); });
			return await task;
		}
#else
		private static Dispatcher GetDispatcher()
		{
			return Application.Current != null && Application.Current.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess()
				? Application.Current.Dispatcher
				: null;
		}

		private async static Task<T> ExecuteOnUIThread<T>(Func<Task<T>> f, Dispatcher dispatcher)
		{
			Debug.Assert(dispatcher != null);
			Task<T> task = null;
			await dispatcher.BeginInvoke(new Action(() => { task = f(); }));
			return await task;
		}
#endif
	}
}
