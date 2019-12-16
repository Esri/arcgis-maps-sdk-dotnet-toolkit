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

using System;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// Event arguments for bookmark selection.
    /// </summary>
    public sealed class BookmarkSelectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the selected bookmark.
        /// </summary>
        public Bookmark Bookmark { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BookmarkSelectedEventArgs"/> class
        /// for the specified bookmark.
        /// </summary>
        /// <param name="bookmark">The selected bookmark</param>
        public BookmarkSelectedEventArgs(Bookmark bookmark)
        {
            Bookmark = bookmark;
        }
    }
}
