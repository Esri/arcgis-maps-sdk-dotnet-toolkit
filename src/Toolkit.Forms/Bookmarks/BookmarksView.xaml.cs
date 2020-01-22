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
using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// The BookmarksView view presents bookmarks, either from a list defined by <see cref="BookmarksOverride" /> or
    /// the Map or Scene shown in the associated <see cref="GeoView" />.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BookmarksView : TemplatedView
    {
        private ListView _presentingView;

        private static readonly DataTemplate DefaultDataTemplate;

        static BookmarksView()
        {
            DefaultDataTemplate = new DataTemplate(() =>
            {
                var defaultCell = new TextCell();
                defaultCell.SetBinding(TextCell.TextProperty, nameof(Bookmark.Name));
                return defaultCell;
            });
        }

        public BookmarksView()
        {
            ItemTemplate = DefaultDataTemplate;

            InitializeComponent();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_presentingView != null)
            {
                _presentingView.ItemSelected -= Internal_bookmarkSelected;
            }

            _presentingView = GetTemplateChild("PresentingView") as ListView;

            if (_presentingView != null)
            {
                _presentingView.ItemSelected += Internal_bookmarkSelected;
            }
        }

        /// <summary>
        /// Gets or sets the data template that renders bookmark entries in the list.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the list of bookmarks to display.
        /// Otherwise, the bookmarks from the Map or Scene shown in the associated <see cref="GeoView" /> are displayed.
        /// </summary>
        /// <remarks>If set to a <see cref="System.Collections.Specialized.INotifyCollectionChanged" />, the view will be updated with collection changes.</remarks>
        /// <seealso cref="BookmarksOverrideProperty" />
        public IEnumerable<Bookmark> BookmarksOverride
        {
            get { return (IEnumerable<Bookmark>)GetValue(BookmarksOverrideProperty); }
            set { SetValue(BookmarksOverrideProperty, value); }
        }

        /// <summary>
        /// Gets or sets the geoview that contain the layers whose symbology and description will be displayed.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        /// <seealso cref="GeoViewProperty"/>
        public GeoView GeoView
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> bindable property.
        /// </summary>
        public static readonly BindableProperty GeoViewProperty =
            BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(BookmarksView), null, BindingMode.OneWay, null, propertyChanged: GeoViewChanged);

        /// <summary>
        /// Identifies the <see cref="BookmarksOverride"/> bindable property.
        /// </summary>
        public static readonly BindableProperty BookmarksOverrideProperty =
            BindableProperty.Create(nameof(BookmarksOverride), typeof(IEnumerable<Bookmark>), typeof(BookmarksView), null, BindingMode.OneWay, null, propertyChanged: BookmarksOverrideChanged);

        /// <summary>
        /// Identifies the <see cref="ItemTemplate" /> bindable property.
        /// </summary>
        public static readonly BindableProperty ItemTemplateProperty =
            BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(BookmarksView), DefaultDataTemplate, BindingMode.OneWay, null, propertyChanged: ItemTemplateChanged);

        /// <summary>
        /// Handles property changes for the <see cref="BookmarksOverride" /> bindable property.
        /// </summary>
        private static void BookmarksOverrideChanged(BindableObject sender, object oldValue, object newValue)
        {
            ((BookmarksView)sender).Refresh();
        }

        /// <summary>
        /// Handles property changes for the <see cref="GeoView" /> bindable property.
        /// </summary>
        private static void GeoViewChanged(BindableObject sender, object oldValue, object newValue)
        {
            BookmarksView bookmarkView = (BookmarksView)sender;

            if (oldValue is INotifyPropertyChanged oldInpc)
            {
                oldInpc.PropertyChanged -= bookmarkView.GeoViewPropertyChanged;
            }

            if (newValue is INotifyPropertyChanged newInpc)
            {
                newInpc.PropertyChanged += bookmarkView.GeoViewPropertyChanged;
            }

            bookmarkView.Refresh();
        }

        /// <summary>
        /// Manages event subscription and unsubcription for the GeoView.
        /// </summary>
        private void GeoViewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is MapView mv && e.PropertyName == nameof(mv.Map))
            {
                mv.Map.PropertyChanged -= Document_PropertyChanged;
                mv.Map.LoadStatusChanged -= Document_LoadStatusChanged;

                mv.Map.PropertyChanged += Document_PropertyChanged;
                mv.Map.LoadStatusChanged += Document_LoadStatusChanged;
            }
            else if (sender is SceneView sv && e.PropertyName == nameof(sv.Scene))
            {
                sv.Scene.PropertyChanged -= Document_PropertyChanged;
                sv.Scene.LoadStatusChanged -= Document_LoadStatusChanged;

                sv.Scene.PropertyChanged += Document_PropertyChanged;
                sv.Scene.LoadStatusChanged += Document_LoadStatusChanged;
            }
            else
            {
                return;
            }

            Refresh();
        }

        /// <summary>
        /// Handles property changes to the Map or Scene associated with the <see cref="GeoView" />.
        /// </summary>
        private void Document_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Map.Bookmarks) || e.PropertyName == nameof(Scene.Bookmarks))
            {
                Refresh();
            }
        }

        /// <summary>
        /// Handles load status changes on the Map or Scene associated with the <see cref="GeoView" />.
        /// </summary>
        private void Document_LoadStatusChanged(object sender, LoadStatusEventArgs e)
        {
            if (e.Status == LoadStatus.Loaded)
            {
                Device.BeginInvokeOnMainThread(Refresh);
            }
        }

        /// <summary>
        /// Handles property changes for the <see cref="ItemTemplate" /> bindable property.
        /// </summary>
        private static void ItemTemplateChanged(BindableObject sender, object oldValue, object newValue)
        {
            BookmarksView bookmarkView = (BookmarksView)sender;

            if (bookmarkView?._presentingView != null)
            {
                bookmarkView._presentingView.ItemTemplate = (DataTemplate)newValue;
            }

        }

        /// <summary>
        /// Gets the list of bookmarks as it should be shown in the view.
        /// </summary>
        private IEnumerable<Bookmark> CurrentBookmarkList
        {
            get
            {
                if (BookmarksOverride != null)
                {
                    return BookmarksOverride;
                }

                if (GeoView is MapView mv && mv.Map is Map m)
                {
                    return m.Bookmarks;
                }
                else if (GeoView is SceneView sv && sv.Scene is Scene s)
                {
                    return s.Bookmarks;
                }

                return null;
            }
        }

        /// <summary>
        /// Updates the list view with the latest bookmark list.
        /// </summary>
        private void Refresh()
        {
            if (_presentingView != null)
            {
                _presentingView.ItemsSource = CurrentBookmarkList;
            }
        }

        /// <summary>
        /// Selects the bookmark and navigates to it in the associated <see cref="GeoView" />.
        /// </summary>
        /// <param name="bookmark">Bookmark to navigate to. Must be non-null with a valid viewpoint.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="bookmark"/> is <code>null</code>.</exception>
        private void SelectAndNavigateToBookmark(Bookmark bookmark)
        {
            if (bookmark?.Viewpoint == null)
            {
                throw new ArgumentNullException("Bookmark or bookmark viewpoint is null");
            }

            GeoView?.SetViewpointAsync(bookmark.Viewpoint);

            BookmarkSelected?.Invoke(this, new BookmarkSelectedEventArgs(bookmark));
        }

        /// <summary>
        /// Handles selection on the underlying list view.
        /// </summary>
        private void Internal_bookmarkSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is Bookmark bm)
            {
                SelectAndNavigateToBookmark(bm);
            }

            if (e.SelectedItem != null)
            {
                ((ListView)sender).SelectedItem = null;
            }
        }

        /// <summary>
        /// Raised whenever a bookmark is selected.
        /// </summary>
        public event EventHandler<BookmarkSelectedEventArgs> BookmarkSelected;
    }
}