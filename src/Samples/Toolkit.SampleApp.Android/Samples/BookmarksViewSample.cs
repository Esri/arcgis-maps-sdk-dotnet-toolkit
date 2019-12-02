using Android.App;
using Android.OS;
using Android.Widget;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [Activity(Label = "BookmarksView", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
    [SampleInfoAttribute(Category = "BookmarksView", Description = "BookmarksView used with a MapView")]
    public class BookmarksViewSampleActivity : Activity
    {
        private MapView mapView;
        private BookmarksView bookmarksView;
        private Button _SetObservableButton;
        private Button _removeListButton;
        private Button _swapMapButton;
        private Button _addToObservableButton;
        private Button _removeFromObservableButton;

        private readonly string[] _mapUrl = new[]
        {
            "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee",
            "https://runtime.maps.arcgis.com/home/item.html?id=16f1b8ba37b44dc3884afc8d5f454dd2",
            "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=e50fafe008ac4ce4ad2236de7fd149c3"
        };
        private int _mapIndex = 0;

        private ObservableCollection<Bookmark> _bookmarksObservable = new ObservableCollection<Bookmark>();
        private List<Bookmark> _bookmarksStatic = new List<Bookmark>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.BookmarksViewSample);
            mapView = FindViewById<MapView>(Resource.Id.mapView);
            mapView.Map = new Map(new Uri(_mapUrl[_mapIndex]));
            bookmarksView = FindViewById<BookmarksView>(Resource.Id.bookmarksView);
            bookmarksView.GeoView = mapView;

            _addToObservableButton = FindViewById<Button>(Resource.Id.AddToObservableButton);
            _removeFromObservableButton = FindViewById<Button>(Resource.Id.RemoveFromObservableButton);
            _swapMapButton = FindViewById<Button>(Resource.Id.swapMapButton);
            _SetObservableButton = FindViewById<Button>(Resource.Id.setObservableListButton);
            _removeListButton = FindViewById<Button>(Resource.Id.removeListButton);

            _addToObservableButton.Click += AddToObservableButton_Click;
            _removeFromObservableButton.Click += RemoveFromObservableButton_Click;
            _swapMapButton.Click += SwitchMapButton_Click;
            _SetObservableButton.Click += ShowObservableList_Click;
            _removeListButton.Click += RemoveListButton_Click;

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

        private void ShowObservableList_Click(object sender, EventArgs e)
        {
            bookmarksView.BookmarksOverride = _bookmarksObservable;
        }

        private void RemoveFromObservableButton_Click(object sender, EventArgs e)
        {
            _bookmarksObservable.RemoveAt(0);
        }
        private void RemoveListButton_Click(object sender, EventArgs e)
        {
            bookmarksView.BookmarksOverride = null;
        }

        private void AddToObservableButton_Click(object sender, EventArgs e)
        {
            Viewpoint vp5 = new Viewpoint(47.787947, 16.755135, 1300, new Camera(47.787947, 16.755135, 3000, 100, 35, 0));

            _bookmarksObservable.Add(new Bookmark($"O: {_bookmarksObservable.Count}", vp5));
        }

        private void SwitchMapButton_Click(object sender, EventArgs e)
        {
            _mapIndex++;
            mapView.Map = new Map(new Uri(_mapUrl[_mapIndex % _mapUrl.Length]));
        }
    }
}