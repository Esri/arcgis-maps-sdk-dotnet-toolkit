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

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    /// <summary>
    /// Defines how many results should be returned by a search.
    /// </summary>
    public enum SearchResultMode
    {
        /// <summary>
        /// Always try to return a single result.
        /// </summary>
        Single,

        /// <summary>
        /// Always try to return multiple results.
        /// </summary>
        Multiple,

        /// <summary>
        /// Try to make the right choice of single or multiple results based on context.
        /// </summary>
        Automatic,
    }
}
