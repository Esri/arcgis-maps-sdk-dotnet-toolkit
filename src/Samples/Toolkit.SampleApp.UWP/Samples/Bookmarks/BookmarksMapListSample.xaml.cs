using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Bookmarks
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BookmarksMapListSample : Page
    {
        private const string _mapUrl = "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee";
        public BookmarksMapListSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(new Uri(_mapUrl));
            MyBookmarks.PrefersBookmarksList = true;
            configureManualList();
        }
        private void configureManualList()
        {
            List<Bookmark> bookmarks = new List<Bookmark>();

            Viewpoint vp = new Viewpoint(0, 0, 1500, new Camera(0, 0, 150000, 60, 35, 20));
            bookmarks.Add(new Bookmark("0,0", vp));

            Viewpoint vp2 = new Viewpoint(48.850684, 2.347735, 1000, new Camera(48.850684, 2.347735, 1500, 100, 35, 0));
            bookmarks.Add(new Bookmark("Paris", vp2));

            Viewpoint vp3 = new Viewpoint(48.034682, 13.710577, 1300, new Camera(48.034682, 13.710577, 2000, 100, 35, 0));
            bookmarks.Add(new Bookmark("Pühret, Austria", vp3));

            Viewpoint vp4 = new Viewpoint(47.787947, 16.755135, 1300, new Camera(47.787947, 16.755135, 3000, 100, 35, 0));
            bookmarks.Add(new Bookmark("Nationalpark Neusiedler See - Seewinkel Informationszentrum", vp4));

            MyBookmarks.BookmarkList = bookmarks;
        }
    }
}
