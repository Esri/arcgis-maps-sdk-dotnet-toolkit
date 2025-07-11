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

#nullable enable
using Esri.ArcGISRuntime.Data;
#if WINUI 
using Point = Windows.Foundation.Point;
#elif WINDOWS_UWP
using Point = Windows.Foundation.Point;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI
#endif
{
    /// <summary>
    /// Helper class for controlling a <see cref="MapView"/> instance in an MVVM pattern, while allowing operations on the view from ViewModel.
    /// </summary>
    public class MapViewController : GeoViewController
    {
        /// <summary>
        /// Gets a reference to the MapView this controller is currently connected to.
        /// </summary>
        public MapView? ConnectedMapView => ConnectedView as MapView;

        #region Identify

        /// <inheritdoc cref="MapView.IdentifyGeometryEditorAsync(Point, double)" />
        public virtual async Task<IdentifyGeometryEditorResult?> IdentifyGeometryEditorAsync(Point screenPoint, double tolerance) =>
            ConnectedMapView is null
                ? null
                : await ConnectedMapView.IdentifyGeometryEditorAsync(screenPoint, tolerance).ConfigureAwait(false);

        #endregion
    }
}
