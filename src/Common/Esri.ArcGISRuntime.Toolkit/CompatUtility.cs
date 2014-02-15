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
