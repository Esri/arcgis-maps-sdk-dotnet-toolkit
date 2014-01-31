using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NETFX_CORE
using Windows.UI.Xaml;
#else
using System.Windows;
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
	}
}
