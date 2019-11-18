using Android.App;
using Android.OS;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using System;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [Activity(Label = "Bookmarks", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
    [SampleInfoAttribute(Category = "Bookmarks", Description = "Bookmarks used with a MapView")]
    public class BookmarksSampleActivity : Activity
    {
        private MapView mapView;
        private Bookmarks bookmarksView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.BookmarksSample);
            mapView = FindViewById<MapView>(Resource.Id.mapView);
            mapView.Map = new Map(new Uri("https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee"));
            bookmarksView = FindViewById<Bookmarks>(Resource.Id.bookmarksView);
            bookmarksView.GeoView = mapView;
        }
    }
}