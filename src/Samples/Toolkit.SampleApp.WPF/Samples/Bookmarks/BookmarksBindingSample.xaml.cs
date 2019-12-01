using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.Bookmarks
{
    /// <summary>
    /// Interaction logic for MapBookmarksSample.xaml
    /// </summary>
    [SampleInfoAttribute(Category = "Bookmarks", DisplayName = "Bookmarks - Binding tests", Description = "Tools for testing various binding scenarios.")]
    public partial class BookmarksBindingSample : UserControl
    {
        private readonly string[] _mapUrl = new[] 
        { 
            "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee",
            "https://runtime.maps.arcgis.com/home/item.html?id=16f1b8ba37b44dc3884afc8d5f454dd2",
            "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=e50fafe008ac4ce4ad2236de7fd149c3"
        };
        private int _mapIndex = 0;

        private ObservableCollection<Bookmark> _bookmarksObservable = new ObservableCollection<Bookmark>();
        private List<Bookmark> _bookmarksStatic = new List<Bookmark>();

        public BookmarksBindingSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(new Uri(_mapUrl[0]));
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

        private void ToggleListCheckbox_Click(object sender, RoutedEventArgs e) => MyBookmarks.PrefersBookmarksList = !MyBookmarks.PrefersBookmarksList;

        private void ShowStaticList_Click(object sender, RoutedEventArgs e) => MyBookmarks.BookmarkList = _bookmarksStatic;

        private void ShowObservableList_Click(object sender, RoutedEventArgs e) => MyBookmarks.BookmarkList = _bookmarksObservable;

        private void RemoveFromObservableButton_Click(object sender, RoutedEventArgs e) => _bookmarksObservable.RemoveAt(0);

        private void AddToObservableButton_Click(object sender, RoutedEventArgs e)
        {
            Viewpoint vp5 = new Viewpoint(47.787947, 16.755135, 1300, new Camera(47.787947, 16.755135, 3000, 100, 35, 0));

            _bookmarksObservable.Add(new Bookmark($"O: {_bookmarksObservable.Count}", vp5));
        }

        private void SwitchMapButton_Click(object sender, RoutedEventArgs e)
        {
            _mapIndex++;
            MyMapView.Map = new Map(new Uri(_mapUrl[_mapIndex % _mapUrl.Length]));
        }
    }
}
