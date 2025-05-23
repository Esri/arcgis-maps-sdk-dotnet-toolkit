﻿using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Maui;
using System.Collections.ObjectModel;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "BookmarksView", Description = "BookmarksView with MapView sample")]
    public partial class BookmarksViewSample : ContentPage
    {
        private const string webMapOneUrl = "https://arcgisruntime.maps.arcgis.com/home/item.html?id=e50fafe008ac4ce4ad2236de7fd149c3";
        private const string webMapTwoUrl = "https://arcgisruntime.maps.arcgis.com/home/item.html?id=16f1b8ba37b44dc3884afc8d5f454dd2";
        private const string webSceneOne = "https://arcgisruntime.maps.arcgis.com/home/item.html?id=6b6588041965408e84ba319e12d9d7ad";
        private const string webSceneTwo = "https://arcgisruntime.maps.arcgis.com/home/item.html?id=b3e2230e170d4f91aa3d47f88821743d";
        private MapView MyMapView;
        private SceneView MySceneView;
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
            MyMapView = new MapView();
            MySceneView = new SceneView();
            BookmarksView.GeoView = MyMapView;
            _viewContainer.Children.Add(MyMapView);
            MyMapView.Map = new Map(new Uri(webMapTwoUrl));
            MySceneView.Scene = new Scene(new Uri(webSceneOne));
        }

        private void SetMapViewBinding_Click(object? sender, EventArgs e)
        {
            if (_viewContainer.Children.Contains(MyMapView) == false)
                _viewContainer.Children.Add(MyMapView);
            if (_viewContainer.Children.Contains(MySceneView))
                _viewContainer.Children.Remove(MySceneView);
            BookmarksView.SetBinding(Esri.ArcGISRuntime.Toolkit.Maui.BookmarksView.GeoViewProperty, static (MapView mapView) => mapView, source: MyMapView);
        }

        // Note that the Web Scene Specification does not use bookmarks.
        // As such, the BookmarksView will initially be empty, so click the "Set list" button to generate bookmarks.
        private void SetSceneViewBinding_Click(object? sender, EventArgs e)
        {
            if (_viewContainer.Children.Contains(MySceneView) == false)
                _viewContainer.Children.Add(MySceneView);
            if (_viewContainer.Children.Contains(MyMapView))
                _viewContainer.Children.Remove(MyMapView);
            BookmarksView.SetBinding(Esri.ArcGISRuntime.Toolkit.Maui.BookmarksView.GeoViewProperty, static (SceneView sceneView) => sceneView, source: MySceneView);
        }

        private void SetDocumentOne_Click(object? sender, EventArgs e)
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

        private void SetDocumentTwo_Click(object? sender, EventArgs e)
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

        private void SetOverrideList_Click(object? sender, EventArgs e)
        {
            BookmarksView.BookmarksOverride = bookmarksOverride;
        }

        private void ClearOverrideList_Click(object? sender, EventArgs e)
        {
            BookmarksView.BookmarksOverride = null;
        }

        private void AddToOverrideList_Click(object? sender, EventArgs e)
        {
            double longitude = (_randomizer.NextDouble() * 360) - 180;
            double latitude = (_randomizer.NextDouble() * 180) - 90;

            bookmarksOverride.Add(new Bookmark($"BM {bookmarksOverride.Count}", new Viewpoint(latitude, longitude, 2375)));
        }

        private void AddToMapScene_Click(object? sender, EventArgs e)
        {
            double longitude = (_randomizer.NextDouble() * 360) - 180;
            double latitude = (_randomizer.NextDouble() * 180) - 90;

            if (BookmarksView.GeoView is MapView mapView && mapView.Map != null)
            {
                mapView.Map.Bookmarks.Add(new Bookmark($"doc {mapView.Map.Bookmarks.Count}", new Viewpoint(latitude, longitude, 2375)));
            }
            else if (BookmarksView.GeoView is SceneView sceneView && sceneView.Scene != null)
            {
                sceneView.Scene.Bookmarks.Add(new Bookmark($"doc {sceneView.Scene.Bookmarks.Count}", new Viewpoint(latitude, longitude, 2375)));
            }
        }

        private void SetItemTemplateOne_Click(object? sender, EventArgs e)
        {
            var template = Resources["ItemTemplateOne"] as DataTemplate;
            BookmarksView.ItemTemplate = template;
        }

        private void SetItemTemplateTwo_Click(object? sender, EventArgs e)
        {
            var template = Resources["ItemTemplateTwo"] as DataTemplate;
            BookmarksView.ItemTemplate = template;
        }

        private void BookmarkListenerSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (BookmarkListenerSwitch.IsToggled)
            {
                BookmarksView.BookmarkSelected += BookmarkSelected;
            }
            else
            {
                BookmarksView.BookmarkSelected -= BookmarkSelected;
            }
        }

        private void BookmarkSelected(object? sender, Bookmark bookmark)
        {
            DisplayAlert("Bookmark selected", $"Selected {bookmark.Name}", "Ok");
        }
    }
}