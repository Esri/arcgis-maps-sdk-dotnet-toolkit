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
    public sealed partial class BookmarksBindingSample : Page
    {
        private const string _mapUrl = "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee";

        private ObservableCollection<Bookmark> _bookmarksObservable = new ObservableCollection<Bookmark>();
        private List<Bookmark> _bookmarksStatic = new List<Bookmark>();

        public BookmarksBindingSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(new Uri(_mapUrl));
            InitializeLists();
        }

        private void InitializeLists()
        {
            Viewpoint vp = new Viewpoint(0, 0, 1500, new Camera(0, 0, 150000, 60, 35, 20));
            _bookmarksStatic.Add(new Bookmark("S: 0,0", vp));
            _bookmarksObservable.Add(new Bookmark("O: 0,0", vp));

            Viewpoint vp2 = new Viewpoint(48.850684, 2.347735, 1000, new Camera(48.850684, 2.347735, 1500, 100, 35, 0));
            _bookmarksStatic.Add(new Bookmark("S: Paris", vp2));
            _bookmarksObservable.Add(new Bookmark("O: Paris", vp2));

            Viewpoint vp3 = new Viewpoint(48.034682, 13.710577, 1300, new Camera(48.034682, 13.710577, 2000, 100, 35, 0));
            _bookmarksStatic.Add(new Bookmark("S: Pühret, Austria", vp3));
            _bookmarksObservable.Add(new Bookmark("O: Pühret, Austria", vp3));


            Viewpoint vp4 = new Viewpoint(47.787947, 16.755135, 1300, new Camera(47.787947, 16.755135, 3000, 100, 35, 0));
            _bookmarksStatic.Add(new Bookmark("S: Nationalpark Neusiedler See - Seewinkel Informationszentrum", vp4));
            _bookmarksObservable.Add(new Bookmark("O: Nationalpark Neusiedler See - Seewinkel Informationszentrum", vp4));
        }

        private void ToggleListCheckbox_Click(object sender, RoutedEventArgs e)
        {
            MyBookmarks.PrefersBookmarksList = !MyBookmarks.PrefersBookmarksList;
        }

        private void ShowStaticList_Click(object sender, RoutedEventArgs e)
        {
            MyBookmarks.BookmarkList = _bookmarksStatic;
        }

        private void ShowObservableList_Click(object sender, RoutedEventArgs e)
        {
            MyBookmarks.BookmarkList = _bookmarksObservable;
        }

        private void RemoveFromObservableButton_Click(object sender, RoutedEventArgs e)
        {
            _bookmarksObservable.RemoveAt(0);
        }

        private void AddToObservableButton_Click(object sender, RoutedEventArgs e)
        {
            Viewpoint vp5 = new Viewpoint(47.787947, 16.755135, 1300, new Camera(47.787947, 16.755135, 3000, 100, 35, 0));

            _bookmarksObservable.Add(new Bookmark($"O: {_bookmarksObservable.Count}", vp5));
        }
    }
}
