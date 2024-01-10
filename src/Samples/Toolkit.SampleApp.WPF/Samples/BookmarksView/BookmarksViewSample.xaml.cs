using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
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

namespace Esri.ArcGISRuntime.Toolkit.Samples.BookmarksView
{
    /// <summary>
    /// Interaction logic for BookmarksViewSample.xaml
    /// </summary>
    [SampleInfo(Category = "BookmarksView", DisplayName = "BookmarksView - Comprehensive", Description = "Full BookmarksView scenario")]
    public partial class BookmarksViewSample : UserControl
    {
        private const string webMapOneUrl = "https://arcgisruntime.maps.arcgis.com/home/item.html?id=e50fafe008ac4ce4ad2236de7fd149c3";
        private const string webMapTwoUrl = "https://arcgisruntime.maps.arcgis.com/home/item.html?id=16f1b8ba37b44dc3884afc8d5f454dd2";
        private const string webSceneOne = "https://arcgisruntime.maps.arcgis.com/home/item.html?id=6b6588041965408e84ba319e12d9d7ad";
        private const string webSceneTwo = "https://arcgisruntime.maps.arcgis.com/home/item.html?id=b3e2230e170d4f91aa3d47f88821743d";

        private Random _randomizer = new Random();

        private ObservableCollection<Bookmark> bookmarksOverride = new ObservableCollection<Bookmark>
        {
            new Bookmark("Override One", new Viewpoint(11,11,1100)),
            new Bookmark("Override Two", new Viewpoint(22,-22.22,1200)),
            new Bookmark("Override Three", new Viewpoint(-33,33,3000)),
            new Bookmark("Override Four", new Viewpoint(44,44,4400)),
            new Bookmark("Override Five", new Viewpoint(-55,-55,5500)),
        };

        public BookmarksViewSample()
        {
            InitializeComponent();

            MyMapView.Map = new Map(new Uri(webMapTwoUrl));
            MySceneView.Scene = new Scene(new Uri(webSceneOne));
        }

        private void SetMapViewBinding_Click(object sender, RoutedEventArgs e)
        {
            MyMapView.Visibility = Visibility.Visible;
            MySceneView.Visibility = Visibility.Collapsed;
            Binding geoviewBinding = new Binding();
            geoviewBinding.Source = MyMapView;
            BookmarksView.SetBinding(UI.Controls.BookmarksView.GeoViewProperty, geoviewBinding);
        }

        private void SetSceneViewBinding_Click(object sender, RoutedEventArgs e)
        {
            MyMapView.Visibility = Visibility.Collapsed;
            MySceneView.Visibility = Visibility.Visible;
            Binding geoviewBinding = new Binding();
            geoviewBinding.Source = MySceneView;
            BookmarksView.SetBinding(UI.Controls.BookmarksView.GeoViewProperty, geoviewBinding);
        }

        private void SetDocumentOne_Click(object sender, RoutedEventArgs e)
        {
            if (BookmarksView.GeoView is MapView mapView)
            {
                mapView.Map = new Map(new Uri(webMapOneUrl));
            }
            else if (BookmarksView.GeoView is SceneView sceneView)
            {
                sceneView.Scene = new Scene(new Uri(webSceneOne));
            }
        }

        private void SetDocumentTwo_Click(object sender, RoutedEventArgs e)
        {
            if (BookmarksView.GeoView is MapView mapView)
            {
                mapView.Map = new Map(new Uri(webMapTwoUrl));
            }
            else if (BookmarksView.GeoView is SceneView sceneView)
            {
                sceneView.Scene = new Scene(new Uri(webSceneTwo));
            }
        }

        private void SetOverrideList_Click(object sender, RoutedEventArgs e)
        {
            BookmarksView.BookmarksOverride = bookmarksOverride;
        }

        private void ClearOverrideList_Click(object sender, RoutedEventArgs e)
        {
            BookmarksView.BookmarksOverride = null;
        }

        private void AddToOverrideList_Click(object sender, RoutedEventArgs e)
        {
            double longitude = (_randomizer.NextDouble() * 360) - 180;
            double latitude = (_randomizer.NextDouble() * 180) - 90;

            bookmarksOverride.Add(new Bookmark($"BM {bookmarksOverride.Count}", new Viewpoint(latitude, longitude, 2375)));
        }

        private void AddToMapScene_Click(object sender, RoutedEventArgs e)
        {
            double longitude = (_randomizer.NextDouble() * 360) - 180;
            double latitude = (_randomizer.NextDouble() * 180) - 90;

            if (BookmarksView.GeoView is MapView mapView)
            {
                mapView.Map.Bookmarks.Add(new Bookmark($"doc {mapView.Map.Bookmarks.Count}", new Viewpoint(latitude, longitude, 2375)));
            }
            else if (BookmarksView.GeoView is SceneView sceneView)
            {
                sceneView.Scene.Bookmarks.Add(new Bookmark($"doc {sceneView.Scene.Bookmarks.Count}", new Viewpoint(latitude, longitude, 2375)));
            }
        }

        private void SetItemTemplateOne_Click(object sender, RoutedEventArgs e)
        {
            DataTemplate template = Resources["ItemTemplateOne"] as DataTemplate;
            BookmarksView.ItemTemplate = template;
        }

        private void SetItemTemplateTwo_Click(object sender, RoutedEventArgs e)
        {
            DataTemplate template = Resources["ItemTemplateTwo"] as DataTemplate;
            BookmarksView.ItemTemplate = template;
        }

        private void AddSelectionListener_Click(object sender, RoutedEventArgs e)
        {
            BookmarksView.BookmarkSelected += BookmarkSelected;
        }

        private void RemoveSelectionListener_Click(object sender, RoutedEventArgs e)
        {
            BookmarksView.BookmarkSelected -= BookmarkSelected;
        }

        private void BookmarkSelected(object sender, Bookmark bookmark)
        {
            MessageBox.Show($"{bookmark.Name} Selected!");
        }

        private void SelectDefaultItemContainer_Click(object sender, RoutedEventArgs e)
        {
            BookmarksView.ItemContainerStyle = null;
        }

        private void SelectCustomItemContainer_Click(object sender, RoutedEventArgs e)
        {
            Style style = (Style)Resources["AlternateItemContainerStyle"];
            BookmarksView.ItemContainerStyle = style;
        }
    }
}
