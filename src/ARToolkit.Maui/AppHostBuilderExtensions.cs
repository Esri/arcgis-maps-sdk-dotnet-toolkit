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

using System;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace Esri.ArcGISRuntime.ARToolkit.Maui
{
    /// <summary>
    /// ArcGIS Runtime AppHost builder methods for registering the runtime with .NET MAUI.
    /// </summary>
    public static class AppHostBuilderExtensions
    {
        /// <summary>
        /// Initializes the ArcGIS Runtime AR Toolkit MAUI UI Controls.
        /// </summary>
        /// <param name="builder">The Maui host builder.</param>
        /// <returns>The host builder</returns>
        public static MauiAppBuilder UseARToolkit(this MauiAppBuilder builder)
        {
            builder.ConfigureMauiHandlers((a) => { a.AddHandler(typeof(ARSceneView), typeof(Handlers.ARSceneViewHandler)); });
            return builder;
        }
    }
}
