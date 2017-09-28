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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Resources;

namespace Esri.ArcGISRuntime.Toolkit.Properties
{
    internal static class Resources
    {
        private static ResourceManager s_resourceManager;

        private static ResourceManager ResourceManager
        {
            get
            {
                if (s_resourceManager == null)
                    s_resourceManager = new ResourceManager("Esri.ArcGISRuntime.Toolkit.LocalizedStrings.Resources", typeof(Resources).Assembly);
                return s_resourceManager;
            }
        }

        public static string GetString(string name)
        {
            return ResourceManager.GetString(name);
        }
    }
}
