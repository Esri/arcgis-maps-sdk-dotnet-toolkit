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

using System.Collections.Generic;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Esri.ArcGISRuntime.Toolkit.Xamarin.Forms")]

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Internal interface enables code sharing for <see cref="BasemapGalleryController" /> with Forms- and Windows-specific BasemapGallery implementations.
    /// </summary>
    internal interface IBasemapGallery
    {
        public GeoModel? GeoModel { get; set; }

        public BasemapGalleryItem? SelectedBasemap { get; set; }

        public IList<BasemapGalleryItem>? AvailableBasemaps { get; set; }

        public ArcGISPortal? Portal { get; set; }

        internal void SetListViewSource(IList<BasemapGalleryItem>? newSource);

        internal void SetListViewSelection(BasemapGalleryItem? item);

        internal void NotifyBasemapSelected(BasemapGalleryItem item);

        internal void SetIsLoading(bool isLoading);
    }
}
