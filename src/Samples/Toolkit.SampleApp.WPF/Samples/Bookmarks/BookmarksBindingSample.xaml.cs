using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Samples.Bookmarks
{
    /// <summary>
    /// Interaction logic for MapBookmarksSample.xaml
    /// </summary>
    [SampleInfoAttribute(Category = "Bookmarks", DisplayName = "Bookmarks - Binding tests", Description = "Tools for testing various binding scenarios.")]
    public partial class BookmarksBindingSample : UserControl
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
