// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
#if NETFX_CORE
using Windows.UI.Core;
#else
using System.Windows;
using System.Windows.Threading;
using System.Runtime.InteropServices;
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

#if NETFX_CORE
		public static float LogicalDpi()
		{
			return Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi;
		}
#else
		[DllImport("gdi32.dll")]
		public static extern int GetDeviceCaps(IntPtr hDc, int nIndex);

		[DllImport("user32.dll")]
		public static extern IntPtr GetDC(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

		public const int LOGPIXELSX = 88;

		public static float LogicalDpi()
		{
			float dpi = 96.0f;
			IntPtr hDc = GetDC(IntPtr.Zero);
			if (hDc != IntPtr.Zero)
			{
				dpi = (float)GetDeviceCaps(hDc, LOGPIXELSX);
				ReleaseDC(IntPtr.Zero, hDc);
			}
			return dpi;
		}
#endif

#if NETFX_CORE
		public static void ExecuteOnUIThread(Action action, CoreDispatcher dispatcher)
		{
			if (dispatcher.HasThreadAccess)
				action();
			else
			{
				var asyncaction = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
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
	}
}
