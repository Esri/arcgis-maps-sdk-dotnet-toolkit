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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Android.Content;
#if NET6_0_OR_GREATER
using AndroidX.RecyclerView.Widget;
#else
using Android.Support.V7.Widget;
#endif
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
        private readonly Context? _context;
        private List<Bookmark> _shadowList = new List<Bookmark>();

        internal BookmarksAdapter(Context? context, BookmarksViewDataSource dataSource)
        {
            _context = context;
            _dataSource = dataSource;
            _shadowList = dataSource.ToList();

            var listener = new Internal.WeakEventListener<BookmarksAdapter, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>(this, dataSource)
            {
                OnEventAction = static (instance, source, eventArgs) =>
                {
                    switch (eventArgs.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            instance._shadowList.InsertRange(eventArgs.NewStartingIndex, eventArgs.NewItems.OfType<Bookmark>());
                            instance.NotifyItemInserted(eventArgs.NewStartingIndex);
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            instance._shadowList.RemoveRange(eventArgs.OldStartingIndex, eventArgs.OldItems.Count);
                            instance.NotifyItemRemoved(eventArgs.OldStartingIndex);
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            instance._shadowList = instance._dataSource.ToList();
                            instance.NotifyDataSetChanged();
                            break;
                        case NotifyCollectionChangedAction.Move:
                            instance._shadowList = instance._dataSource.ToList();
                            instance.NotifyDataSetChanged();
                            break;
                        case NotifyCollectionChangedAction.Replace:
                            instance._shadowList[eventArgs.OldStartingIndex] = (Bookmark)eventArgs.NewItems[0];
                            instance.NotifyItemChanged(eventArgs.OldStartingIndex);
                            break;
                    }
                },
                OnDetachAction = static (instance, source, weakEventListener) => source.CollectionChanged -= weakEventListener.OnEvent,
            };

            dataSource.CollectionChanged += listener.OnEvent;
        }

        public override int ItemCount => _shadowList?.Count() ?? 0;

        /// <inheritdoc />
        public override long GetItemId(int position) => position;

        public event EventHandler<Bookmark>? BookmarkSelected;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            BookmarkItemViewHolder? bookmarkHolder = holder as BookmarkItemViewHolder;
            if (bookmarkHolder?.BookmarkLabel != null && _shadowList != null && _shadowList.Count() > position)
            {
                bookmarkHolder.BookmarkLabel.Text = _shadowList.ElementAt(position).Name;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            BookmarkItemView itemView = new BookmarkItemView(_context);
            return new BookmarkItemViewHolder(itemView, OnBookmarkClicked);
        }

        private void OnBookmarkClicked(int position)
        {
            BookmarkSelected?.Invoke(this, _shadowList.ElementAt(position));
        }
    }
}