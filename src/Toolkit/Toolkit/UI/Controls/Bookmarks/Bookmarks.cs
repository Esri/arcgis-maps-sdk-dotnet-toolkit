using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

#if NETFX_CORE
using Windows.UI.Xaml.Controls;
#elif __IOS__
using Control = UIKit.UIView;
#elif __ANDROID__
using Control = Android.Views.ViewGroup;
using Android.App;
#else
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The base class for <see cref="Legend"/>
    /// and TableOfContents control is used to display symbology and description for a set of <see cref="Layer"/>s
    /// in a <see cref="Map"/> or <see cref="Scene"/> contained in a <see cref="Esri.ArcGISRuntime.UI.Controls.GeoView"/>.
    /// </summary>
    public partial class Bookmarks : Control, INotifyPropertyChanged
    {
#if !__ANDROID__
        public Bookmarks()
            : base()
        {
            Initialize();
        }
#endif

        public IList<Bookmark> BookmarkList
        {
            get => BookmarkListImpl;
            set
            {
                BookmarkListImpl = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BookmarkList)));
                Refresh();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether bookmarks should be shown from the map/scene or the explicitly set bookmark list.
        /// When true, the control only shows the bookmarks explicitly set through the <see cref="BookmarksList" /> property.
        /// Bookmarks from the Map or Scene are ignored, even if the map or scene is changed in the associated MapView/SceneView
        /// or the Map or Scene load status changes.
        /// </summary>
        public bool PrefersBookmarksList
        {
            get => PrefersBookmarkListImpl;
            set
            {
                PrefersBookmarkListImpl = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PrefersBookmarksList)));
                Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the geoview that contain the layers whose symbology and description will be displayed.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        public GeoView GeoView
        {
            get => GeoViewImpl;
            set => GeoViewImpl = value;
        }

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

        private async void Loadable_LoadStatusChanged(object sender, LoadStatusEventArgs e)
        {
#if NETFX_CORE
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
#elif __ANDROID__
            ((Activity)Context).RunOnUiThread(()=> //TODO make this not assume context is activity
#elif __IOS__
                InvokeOnMainThread(() =>
#else
            Dispatcher.Invoke(() =>
#endif
                {
                    if (e.Status == LoadStatus.Loaded)
                    {
                        Refresh();
                    }
                });
        }

        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is MapView mv) //TODO - make specific to the map property
            {
                if (mv.Map != null)
                {
                    mv.Map.LoadStatusChanged -= Loadable_LoadStatusChanged;
                    mv.Map.LoadStatusChanged += Loadable_LoadStatusChanged;
                    //
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

        private void Document_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Map.Bookmarks) || e.PropertyName == nameof(Scene.Bookmarks))
            {
                Refresh();
            }
        }

        private IList<Bookmark> GetCurrentBookmarkList()
        {
            if (PrefersBookmarksList)
            {
                if (BookmarkList != null)
                {
                    return BookmarkList;
                }
            }

            if (GeoView is MapView mv && mv.Map is Map m)
            {
                return m.Bookmarks;
            }
            else if (GeoView is SceneView sv && sv.Scene is Scene s)
            {
                return s.Bookmarks;
            }

            return new List<Bookmark>();
        }

        private void NavigateToBookmark(Bookmark bookmark)
        {
            if (bookmark == null || bookmark.Viewpoint == null)
            {
                throw new ArgumentNullException("Bookmark or bookmark viewpoint is null");
            }

            GeoView?.SetViewpoint(bookmark.Viewpoint);

            BookmarkSelected?.Invoke(this, new BookmarkSelectedEventArgs(bookmark));
        }

        public event EventHandler<BookmarkSelectedEventArgs> BookmarkSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        public class BookmarkSelectedEventArgs
        {
            public Bookmark Bookmark { get; set; }

            public BookmarkSelectedEventArgs(Bookmark bookmark)
            {
                Bookmark = bookmark;
            }
        }
    }
}