using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.BookmarksView
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BookmarksViewSample : Page
    {
        private readonly string[] _mapUrl = new[]
        {
            "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee",
            "https://runtime.maps.arcgis.com/home/item.html?id=16f1b8ba37b44dc3884afc8d5f454dd2",
            "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=e50fafe008ac4ce4ad2236de7fd149c3"
        };
        private int _mapIndex = 0;

        private ObservableCollection<Bookmark> _bookmarksObservable = new ObservableCollection<Bookmark>();

        public BookmarksViewSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(new Uri(_mapUrl[0]));
            InitializeLists();
        }

        private void InitializeLists()
        {
            Viewpoint vp = new Viewpoint(0, 0, 1500, new Camera(0, 0, 150000, 60, 35, 20));
            _bookmarksObservable.Add(new Bookmark("O: 0,0", vp));

            Viewpoint vp2 = new Viewpoint(48.850684, 2.347735, 1000, new Camera(48.850684, 2.347735, 1500, 100, 35, 0));
            _bookmarksObservable.Add(new Bookmark("O: Paris", vp2));

            Viewpoint vp3 = new Viewpoint(48.034682, 13.710577, 1300, new Camera(48.034682, 13.710577, 2000, 100, 35, 0));
            _bookmarksObservable.Add(new Bookmark("O: Pühret, Austria", vp3));


            Viewpoint vp4 = new Viewpoint(47.787947, 16.755135, 1300, new Camera(47.787947, 16.755135, 3000, 100, 35, 0));
            _bookmarksObservable.Add(new Bookmark("O: Nationalpark Neusiedler See - Seewinkel Informationszentrum", vp4));
        }

        private void ShowObservableList_Click(object sender, RoutedEventArgs e) => MyBookmarks.BookmarksOverride = _bookmarksObservable;

        private void RemoveFromObservableButton_Click(object sender, RoutedEventArgs e) => _bookmarksObservable.RemoveAt(0);

        private void AddToObservableButton_Click(object sender, RoutedEventArgs e)
        {
            Viewpoint vp5 = new Viewpoint(47.787947, 16.755135, 1300, new Camera(47.787947, 16.755135, 3000, 100, 35, 0));

            _bookmarksObservable.Add(new Bookmark($"O: {_bookmarksObservable.Count}", vp5));
        }

        private void SwitchMapButton_Click(object sender, RoutedEventArgs e) => MyMapView.Map = new Map(new Uri(_mapUrl[++_mapIndex % _mapUrl.Length]));

        private void RemoveListOverrideButton_Click(object sender, RoutedEventArgs e) => MyBookmarks.BookmarksOverride = null;
    }
}
