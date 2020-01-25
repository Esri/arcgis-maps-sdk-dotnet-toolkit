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
using System.Collections.Specialized;
using System.Linq;
using Android.Content;
using Android.Support.V7.Widget;
using Android.Views;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Creates the UI for the list items in the associated list of bookmarks.
    /// </summary>
    internal class BookmarksAdapter : RecyclerView.Adapter
    {
        private BookmarksViewDataSource _dataSource;
        private readonly Context _context;

        internal BookmarksAdapter(Context context, BookmarksViewDataSource dataSource)
        {
            _context = context;
            _dataSource = dataSource;

            var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(dataSource)
            {
                OnEventAction = (instance, source, eventArgs) =>
                {
                    switch (eventArgs.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            NotifyItemInserted(eventArgs.NewStartingIndex);
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            NotifyItemRemoved(eventArgs.OldStartingIndex);
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            NotifyDataSetChanged();
                            break;
                        case NotifyCollectionChangedAction.Move:
                            NotifyItemMoved(eventArgs.OldStartingIndex, eventArgs.NewStartingIndex);
                            break;
                        case NotifyCollectionChangedAction.Replace:
                            NotifyItemChanged(eventArgs.OldStartingIndex);
                            break;
                    }
                },
                OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent
            };

            dataSource.CollectionChanged += listener.OnEvent;
        }

        public override int ItemCount => _dataSource?.Count() ?? 0;

        /// <inheritdoc />
        public override long GetItemId(int position) => position;

        public event EventHandler<Bookmark> BookmarkSelected;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            BookmarkItemViewHolder bookmarkHolder = holder as BookmarkItemViewHolder;
            if (_dataSource != null && _dataSource.Count() > position)
            {
                bookmarkHolder.BookmarkLabel.Text = _dataSource.ElementAt(position).Name;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = new BookmarkItemView(_context);

            BookmarkItemViewHolder holder = new BookmarkItemViewHolder(itemView, OnBookmarkClicked);
            return holder;
        }

        private void OnBookmarkClicked(int position)
        {
            BookmarkSelected?.Invoke(this, _dataSource.ElementAt(position));
        }
    }
}