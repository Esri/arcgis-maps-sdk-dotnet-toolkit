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
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Xaml.Controls;
#elif __IOS__
using Control = UIKit.UIView;
#elif __ANDROID__
using Android.App;
using Android.Views;
using Control = Android.Widget.ListView;
#else
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The BookmarksView view presents bookmarks, either from a list defined by <see cref="BookmarksOverride" /> or
    /// the Map or Scene shown in the associated <see cref="GeoView" />.
    /// </summary>
    public partial class BookmarksView : Control
    {
#if !__ANDROID__
        /// <summary>
        /// Initializes a new instance of the <see cref="BookmarksView"/> class.
        /// </summary>
        public BookmarksView()
            : base()
        {
            Initialize();
        }
#endif

        /// <summary>
        /// Gets or sets the list of bookmarks to display.
        /// Otherwise, the bookmarks from the Map or Scene shown in the associated <see cref="GeoView" /> are displayed.
        /// </summary>
        /// <remarks>If set to a <see cref="System.Collections.Specialized.INotifyCollectionChanged" />, the view will be updated with collection changes.</remarks>
        public IEnumerable<Bookmark> BookmarksOverride
        {
            get => BookmarksOverrideImpl;
            set => BookmarksOverrideImpl = value;
        }

        /// <summary>
        /// Gets or sets the MapView or SceneView associated with this view. When a bookmark is selected, the viewpoint of this
        /// geoview will be set to the bookmark's viewpoint. By default, bookmarks from the geoview's Map or Scene
        /// property will be shown. To show a custom bookmark list, set <see cref="BookmarksOverride" />.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        public GeoView GeoView
        {
            get => GeoViewImpl;
            set => GeoViewImpl = value;
        }

        /// <summary>
        /// Configures events and updates display when the <see cref="GeoView" /> property changes.
        /// </summary>
        /// <param name="oldView">The previously set view.</param>
        /// <param name="newView">The new view.</param>
        private void OnViewChanged(GeoView oldView, GeoView newView)
        {
            if (oldView != null)
            {
                (oldView as INotifyPropertyChanged).PropertyChanged -= GeoView_PropertyChanged;
            }

            if (newView != null)
            {
                (newView as INotifyPropertyChanged).PropertyChanged += GeoView_PropertyChanged;
            }

            Refresh();
        }

        /// <summary>
        /// Refreshes the view when the Map or Scene has finished loading.
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Updated load status</param>
#if NETFX_CORE
        private async void Loadable_LoadStatusChanged(object sender, LoadStatusEventArgs e)
#else
        private void Loadable_LoadStatusChanged(object sender, LoadStatusEventArgs e)
#endif
        {
            if (e.Status != LoadStatus.Loaded)
            {
                return;
            }
#if __ANDROID__
            Activity activity = null;
            if (Context is Activity contextActivity)
            {
                activity = contextActivity;
            }
            else if (Context is ContextThemeWrapper wrapper)
            {
                if (wrapper.BaseContext is Activity wrappedActivity)
                {
                    activity = wrappedActivity;
                }
            }
#endif

#if NETFX_CORE
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
#elif __ANDROID__
            activity.RunOnUiThread(() =>
#elif __IOS__
            InvokeOnMainThread(() =>
#else
            Dispatcher.Invoke(() =>
#endif
                {
                    Refresh();
                });
        }

        /// <summary>
        /// Handles <see cref="GeoView" /> property changes, primarily to handle Map and Scene changes.
        /// </summary>
        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is MapView mv)
            {
                // Future - check for map property changes specifically; currently will raise multiple times for map change
                if (mv.Map != null)
                {
                    mv.Map.LoadStatusChanged -= Loadable_LoadStatusChanged;
                    mv.Map.LoadStatusChanged += Loadable_LoadStatusChanged;

                    var incc = mv.Map as INotifyPropertyChanged;
                    var listener = new Internal.WeakEventListener<INotifyPropertyChanged, object, PropertyChangedEventArgs>(incc);
                    listener.OnEventAction = (instance, source, eventArgs) => { Document_PropertyChanged(source, eventArgs); };
                    listener.OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent;
                    incc.PropertyChanged += listener.OnEvent;
                }
            }
            else if (sender is SceneView sv)
            {
                if (sv.Scene != null)
                {
                    sv.Scene.LoadStatusChanged -= Loadable_LoadStatusChanged;
                    sv.Scene.LoadStatusChanged += Loadable_LoadStatusChanged;

                    var incc = sv.Scene as INotifyPropertyChanged;
                    var listener = new Internal.WeakEventListener<INotifyPropertyChanged, object, PropertyChangedEventArgs>(incc);
                    listener.OnEventAction = (instance, source, eventArgs) => { Document_PropertyChanged(source, eventArgs); };
                    listener.OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent;
                    incc.PropertyChanged += listener.OnEvent;
                }
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
        /// Raised whenever a bookmark is selected.
        /// </summary>
        public event EventHandler<BookmarkSelectedEventArgs> BookmarkSelected;
    }
}