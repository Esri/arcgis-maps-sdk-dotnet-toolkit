// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using System.Resources;

namespace Esri.ArcGISRuntime.Toolkit.Properties
{
    internal static class Resources
    {
#if NETFX_CORE
        private static readonly Windows.ApplicationModel.Resources.ResourceLoader _resource = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse("Esri.ArcGISRuntime.Toolkit/Resources");

        public static string GetString(string name)
        {
            return _resource.GetString(name);
        }
#else
        private static ResourceManager _resourceManager;

        private static ResourceManager ResourceManager
        {
            get
            {
                if (_resourceManager == null)
                {
                    _resourceManager = new ResourceManager("Esri.ArcGISRuntime.Toolkit.LocalizedStrings.Resources", typeof(Resources).Assembly);
                }

                return _resourceManager;
            }
        }

        public static string GetString(string name)
        {
            return ResourceManager.GetString(name);
        }
#endif
    }
}
