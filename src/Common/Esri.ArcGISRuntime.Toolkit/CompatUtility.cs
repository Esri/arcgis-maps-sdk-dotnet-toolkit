// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
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
	internal class CompatUtility
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
#elif WINDOWS_PHONE
					_isInDesignMode = System.ComponentModel.DesignerProperties.IsInDesignTool;
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
				return Windows.Graphics.Display.DisplayProperties.LogicalDpi;
#elif WINDOWS_PHONE
				return 96f * Application.Current.Host.Content.ScaleFactor / 100 * 2;
#else
			if (dp == null)
				return 96f;
			else
			{
				System.Windows.Media.Matrix m =
					PresentationSource.FromDependencyObject(dp).CompositionTarget.TransformToDevice;
				return (float)m.M11;
			}
#endif
		}


#if NETFX_CORE
		private static CoreDispatcher _dispatcher; // store instance to avoid exception when leaving app
		private static CoreDispatcher GetDispatcher()
		{
			if (_dispatcher == null && CoreApplication.MainView != null && CoreApplication.MainView.CoreWindow != null)
				_dispatcher = Application.Current == null ? null : CoreApplication.MainView.CoreWindow.Dispatcher;
			return _dispatcher != null && !_dispatcher.HasThreadAccess ? _dispatcher : null;
		}

		public static void ExecuteOnUIThread(DispatchedHandler dispatchedHandler)
		{
			var dispatcher = GetDispatcher();
			if (dispatcher != null)
			{
				var asyncaction = dispatcher.RunAsync(CoreDispatcherPriority.Normal, dispatchedHandler);
			}
			else
				dispatchedHandler();
		}
#else
		private static Dispatcher _dispatcher; // store instance to avoid exception when leaving app
		private static Dispatcher GetDispatcher()
		{
			if (_dispatcher == null)
#if WINDOWS_PHONE
				_dispatcher = Application.Current == null ? null : Deployment.Current.Dispatcher;
#else
				_dispatcher = Application.Current == null ? null : Application.Current.Dispatcher;
#endif
			return _dispatcher != null && !_dispatcher.CheckAccess() ? _dispatcher : null;
		}

		public static void ExecuteOnUIThread(Action action)
		{
			var dispatcher = GetDispatcher();
			if (dispatcher != null)
				dispatcher.BeginInvoke(action);
			else
				action();
		}
#endif
	}
}
