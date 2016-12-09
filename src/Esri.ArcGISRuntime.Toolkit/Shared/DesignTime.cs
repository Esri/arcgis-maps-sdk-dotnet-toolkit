// COPYRIGHT © 2016 ESRI
//
// TRADE SECRETS: ESRI PROPRIETARY AND CONFIDENTIAL
// Unpublished material - all rights reserved under the
// Copyright Laws of the United States and applicable international
// laws, treaties, and conventions.
//
// For additional information, contact:
// Environmental Systems Research Institute, Inc.
// Attn: Contracts and Legal Services Department
// 380 New York Street
// Redlands, California, 92373
// USA
//
// email: contracts@esri.com

using System.ComponentModel;
using System.Windows;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal static partial class DesignTime
    {
        private static bool? s_isInDesignMode;

        /// <summary>
        /// Gets a value indicating if the process is in design mode (running in Blend
        /// or Visual Studio).
        /// </summary>
        public static bool IsDesignMode
        {
            get
            {
                if (!s_isInDesignMode.HasValue)
                {
#if NETFX_CORE
                    s_isInDesignMode = Windows.ApplicationModel.DesignMode.DesignModeEnabled;
#elif !XAMARIN      // Design-time detection is TODO
                    // http://geekswithblogs.net/lbugnion/archive/2009/09/05/detecting-design-time-mode-in-wpf-and-silverlight.aspx
                    var prop = DesignerProperties.IsInDesignModeProperty;
                    s_isInDesignMode
                        = (bool)DependencyPropertyDescriptor
                        .FromProperty(prop, typeof(FrameworkElement))
                        .Metadata.DefaultValue;
#elif XAMARIN
                    s_isInDesignMode = false;
#endif
                }

                return s_isInDesignMode.Value;
            }
#if __IOS__
            set
            {
                s_isInDesignMode = value;
            }
#endif
        }
    }
}
