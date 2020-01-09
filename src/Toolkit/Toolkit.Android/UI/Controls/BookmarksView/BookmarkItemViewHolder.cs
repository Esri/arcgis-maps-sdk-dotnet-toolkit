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

using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class BookmarkItemViewHolder : RecyclerView.ViewHolder
    {
        public TextView BookmarkLabel { get; private set; }

        public BookmarkItemViewHolder(View itemView)
            : base(itemView)
        {
            // Locate and cache view references:
            if (itemView is BookmarkItemView bmView)
            {
                BookmarkLabel = bmView.BookmarkLabel;
            }
        }
    }
}
