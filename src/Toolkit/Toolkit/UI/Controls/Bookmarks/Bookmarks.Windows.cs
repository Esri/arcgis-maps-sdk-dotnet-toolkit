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

#if !XAMARIN
using System.Collections.Generic;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
#if !__NETFX_CORE__
    [TemplatePart(Name = "List", Type = typeof(ListView))]
#endif
    public partial class Bookmarks
    {
        private void Initialize() => DefaultStyleKey = typeof(Bookmarks);

#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            Refresh();
        }

        internal void Refresh()
        {
            ListView = GetTemplateChild("List") as ListView;
            if (ListView == null)
            {
                return;
            }

            ListView.ItemsSource = GetCurrentBookmarkList();
        }

        private ListView _listView;

        private ListView ListView
        {
            get => _listView;
            set
            {
                if (value != _listView)
                {
                    if (_listView != null)
                    {
                        _listView.SelectionChanged -= ListSelectionChanged;
                        _listView = value;
                    }

                    _listView = value;

                    if (_listView != null)
                    {
                        _listView.SelectionChanged += ListSelectionChanged;
                    }
                }
            }
        }

        private void ListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is Bookmark bm)
                {
                    SelectAndNavigateToBookmark(bm);
                }
            }

            ((ListView)sender).SelectedItem = null;
        }

        private GeoView GeoViewImpl
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        private IList<Bookmark> BookmarkListImpl
        {
            get { return (IList<Bookmark>)GetValue(BookmarkListProperty); }
            set { SetValue(BookmarkListProperty, value); }
        }

        private bool PrefersBookmarkListImpl
        {
            get => (bool)GetValue(PrefersBookmarksListProperty);
            set { SetValue(PrefersBookmarksListProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(Bookmarks), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="PrefersBookmarksList" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty PrefersBookmarksListProperty =
            DependencyProperty.Register(nameof(PrefersBookmarksList), typeof(bool), typeof(Bookmarks), new PropertyMetadata(false, OnPrefersBookmarksListPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="BookmarkList" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty BookmarkListProperty =
            DependencyProperty.Register(nameof(BookmarkList), typeof(IList<Bookmark>), typeof(Bookmarks), new PropertyMetadata(null, OnBookmarkListPropertyChanged));

        /// <summary>
        /// Sets <see cref="GeoView" /> when the <see cref="GeoViewProperty" /> changes.
        /// </summary>
        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contents = (Bookmarks)d;
            contents.OnViewChanged(e.OldValue as GeoView, e.NewValue as GeoView);
        }

        /// <summary>
        /// Sets <see cref="PrefersBookmarksList" /> when the <see cref="PrefersBookmarksListProperty" /> changes.
        /// </summary>
        private static void OnPrefersBookmarksListPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bm = (Bookmarks)d;
            bm.PrefersBookmarksList = (bool)e.NewValue;
        }

        /// <summary>
        /// Sets <see cref="BookmarkList" /> when the <see cref="BookmarkListProperty" /> changes.
        /// </summary>
        private static void OnBookmarkListPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bm = (Bookmarks)d;
            bm.BookmarkList = (IList<Bookmark>)e.NewValue;
        }

        /// <summary>
        /// Gets or sets the item template used to render bookmark entries in the list.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(Bookmarks), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the Items Panel Template
        /// </summary>
        public ItemsPanelTemplate ItemsPanel
        {
            get { return (ItemsPanelTemplate)GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemsPanel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsPanelProperty =
            DependencyProperty.Register(nameof(ItemsPanel), typeof(ItemsPanelTemplate), typeof(Bookmarks), new PropertyMetadata(null));
    }
}
#endif