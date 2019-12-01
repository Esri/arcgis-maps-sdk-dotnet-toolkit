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
using System.Collections.Specialized;
using Android.Content;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Creates the UI for the list items in the associated list of <see cref="Bookmark" />.
    /// </summary>
    internal class BookmarksAdapter : BaseAdapter<Bookmark>
    {
        private IList<Bookmark> _bookmarks;
        private readonly Context _context;

        internal BookmarksAdapter(Context context, IList<Bookmark> bookmarks)
        {
            _context = context;
            SetList(bookmarks);
        }

        /// <summary>
        /// Sets the list used by the adapter. Avoids re-drawing if the <paramref name="bookmarks"/> list
        /// is the same as what has already been shown.
        /// </summary>
        /// <param name="bookmarks">List of bookmarks to display.</param>
        public void SetList(IList<Bookmark> bookmarks)
        {
            if (bookmarks == _bookmarks)
            {
                return;
            }

            _bookmarks = bookmarks;
            if (_bookmarks is INotifyCollectionChanged)
            {
                var incc = _bookmarks as INotifyCollectionChanged;
                var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                {
                    OnEventAction = (instance, source, eventArgs) =>
                    {
                        NotifyDataSetChanged();
                    },
                    OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent
                };

                incc.CollectionChanged += listener.OnEvent;
            }

            NotifyDataSetChanged();
        }

        /// <inheritdoc />
        public override Bookmark this[int position] => _bookmarks[position];

        /// <inheritdoc />
        public override int Count => _bookmarks.Count;

        /// <inheritdoc />
        public override long GetItemId(int position)
        {
            return position;
        }

        /// <inheritdoc />
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var bookmark = _bookmarks[position];
            if (convertView == null)
            {
                convertView = new BookmarkItemView(_context);
            }

            ((BookmarkItemView)convertView).Update(bookmark);
            return convertView;
        }
    }
}