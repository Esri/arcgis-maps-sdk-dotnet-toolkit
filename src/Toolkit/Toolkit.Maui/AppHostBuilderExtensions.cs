using Esri.ArcGISRuntime.Maui.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    public static class AppHostBuilderExtensions
    {
        /// <summary>
        ///  Initializes the ArcGIS Runtime Toolkit Controls.
        /// </summary>
        /// <param name="builder">The Maui host builder.</param>
        /// <returns>The host builder</returns>
        public static MauiAppBuilder UseArcGISToolkit(this MauiAppBuilder builder)
        {
            //builder.ConfigureMauiHandlers(delegate (IMauiHandlersCollection a)
            //{
            //    a.AddHandler(typeof(Compass), typeof(CompassHandler));
            //});

            return builder; 
        }
    }
}
