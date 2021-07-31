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

#if XAMARIN_FORMS
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    /// <summary>
    /// The view style of a basemap gallery.
    /// </summary>
    public enum BasemapGalleryViewStyle
    {
        /// <summary>
        /// Display a grid when there is enough width to support that, otherwise display a list.
        /// </summary>
        Automatic = 0,

        /// <summary>
        /// Always display a grid.
        /// </summary>
        Grid = 1,

        /// <summary>
        /// Always display a list.
        /// </summary>
        List = 2,
    }
}
