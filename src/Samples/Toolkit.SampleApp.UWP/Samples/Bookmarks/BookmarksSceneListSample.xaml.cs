using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System.Collections.ObjectModel;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Bookmarks
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BookmarksSceneListSample : Page
    {
        private const string _sceneUrl = "https://arcgisruntime.maps.arcgis.com/home/item.html?id=6b6588041965408e84ba319e12d9d7ad";

        private ObservableCollection<Bookmark> _bookmarks = new ObservableCollection<Bookmark>();

        public BookmarksSceneListSample()
        {
            InitializeComponent();
            MySceneView.Scene = new Scene(new Uri(_sceneUrl));
            configureSlides();
            MyBookmarks.PrefersBookmarksList = true;
            MyBookmarks.BookmarkList = _bookmarks;
            configureManualList();
        }

        private async void configureSlides()
        {
            // Note: adding bookmarks manually because RT doesn't support reading bookmarks from web scenes as of 100.7
            Scene scene = MySceneView.Scene;
            await scene.LoadAsync();

            Viewpoint vp = new Viewpoint(0, 0, 1500, new Camera(0, 0, 150000, 60, 35, 20));
            scene.Bookmarks.Add(new Bookmark("0,0", vp));

            Viewpoint vp2 = new Viewpoint(47.876126, -121.779435, 1000, new Camera(47.876126, -121.779435, 1500, 100, 35, 0));
            scene.Bookmarks.Add(new Bookmark("🍎 Apples", vp2));
        }

        private void configureManualList()
        {
            Viewpoint vp = new Viewpoint(0, 0, 1500, new Camera(0, 0, 150000, 60, 35, 20));
            _bookmarks.Add(new Bookmark("0,0", vp));

            Viewpoint vp2 = new Viewpoint(48.850684, 2.347735, 1000, new Camera(48.850684, 2.347735, 1500, 100, 35, 0));
            _bookmarks.Add(new Bookmark("Paris", vp2));

            Viewpoint vp3 = new Viewpoint(48.034682, 13.710577, 1300, new Camera(48.034682, 13.710577, 2000, 100, 35, 0));
            _bookmarks.Add(new Bookmark("Pühret, Austria", vp3));

            Viewpoint vp4 = new Viewpoint(47.787947, 16.755135, 1300, new Camera(47.787947, 16.755135, 3000, 100, 35, 0));
            _bookmarks.Add(new Bookmark("Nationalpark Neusiedler See - Seewinkel Informationszentrum", vp4));
        }
    }
}
