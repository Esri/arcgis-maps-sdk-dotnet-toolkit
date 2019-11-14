using System;
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
    public partial class Bookmarks : Control
    {
        private BookmarkItemViewModel ViewModel = new BookmarkItemViewModel();

#if !__ANDROID__
        public Bookmarks()
            : base()
        {
            Initialize();
        }
#endif

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

            UpdateControlFromGeoView(newView);
        }

        private async void Loadable_LoadStatusChanged(object sender, LoadStatusEventArgs e)
        {
#if NETFX_CORE
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
#elif __ANDROID__
            ((Activity)Context).RunOnUiThread(()=> //TODO make this not assume context is activity
#elif __IOS__
            InvokeOnMainThread(()=>
#else
            Dispatcher.Invoke(() =>
#endif
            {
                if (e.Status == LoadStatus.Loaded)
                {
                    UpdateControlFromGeoView(GeoView);
                }
            });
        }

        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is MapView mv)
            {
                if (mv.Map != null)
                {
                    mv.Map.LoadStatusChanged -= Loadable_LoadStatusChanged;
                    UpdateControlFromGeoView(mv);
                    mv.Map.LoadStatusChanged += Loadable_LoadStatusChanged;
                    //
                    var incc = mv.Map as INotifyPropertyChanged;
                    var listener = new Internal.WeakEventListener<INotifyPropertyChanged, object, PropertyChangedEventArgs>(incc);
                    listener.OnEventAction = (instance, source, eventArgs) => { Map_PropertyChanged(source, eventArgs); };
                    listener.OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent;
                    incc.PropertyChanged += listener.OnEvent;
                }

                UpdateControlFromGeoView(GeoView);
            }
            else if (sender is SceneView sv)
            {
                sv.Scene.LoadStatusChanged -= Loadable_LoadStatusChanged;
                UpdateControlFromGeoView(sv);
                sv.Scene.LoadStatusChanged += Loadable_LoadStatusChanged;
            }
        }

        private void Map_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Map.Bookmarks))
            {
                UpdateControlFromGeoView(GeoView);
            }
        }

        private void UpdateControlFromGeoView(GeoView view)
        {
            if (view is MapView mapView && mapView.Map != null)
            {
                ViewModel.Bookmarks = mapView.Map.Bookmarks;
            }
            else if (view is SceneView sceneView && sceneView.Scene != null)
            {
                ViewModel.Bookmarks = sceneView.Scene.Bookmarks;
            }

            Refresh();
        }

        private void NavigateToBookmark(Bookmark bookmark)
        {
            if (bookmark == null || bookmark.Viewpoint == null)
            {
                throw new ArgumentNullException("Bookmark or bookmark viewpoint is null");
            }

            GeoView?.SetViewpoint(bookmark.Viewpoint);
        }
    }
}