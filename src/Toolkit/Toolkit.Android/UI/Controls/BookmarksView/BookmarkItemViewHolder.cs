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
using Android.Views;
using Android.Widget;
#if NET6_0_OR_GREATER
using AndroidX.RecyclerView.Widget;
#else
using Android.Support.V7.Widget;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Used to enable ViewHolder pattern used by RecyclerView.
    /// </summary>
    internal class BookmarkItemViewHolder : RecyclerView.ViewHolder
    {
        public TextView BookmarkLabel { get; }

        private Action<int> _listener;

        public BookmarkItemViewHolder(BookmarkItemView bmView, Action<int> listener)
            : base(bmView)
        {
            BookmarkLabel = bmView.BookmarkLabel;
            _listener = listener;

            var weakEventHandler = new Internal.WeakEventListener<BookmarkItemViewHolder, object, EventArgs>(this)
            {
                OnEventAction = static (instance, source, eventArgs) =>
                {
                    if (source is BookmarkItemViewHolder bmivh)
                    {
                        bmivh._listener(bmivh.LayoutPosition);
                    }
                },
                OnDetachAction = static (instance, source, weakEventListener) => (source as BookmarkItemView).Click -= weakEventListener.OnEvent,
            };

            bmView.Click += weakEventHandler.OnEvent;
        }
    }
}
