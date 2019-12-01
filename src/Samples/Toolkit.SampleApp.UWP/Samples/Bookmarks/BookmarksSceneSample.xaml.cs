using Esri.ArcGISRuntime.Mapping;
using System;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Bookmarks
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BookmarksSceneSample : Page
    {
        private const string _sceneUrl = "https://arcgisruntime.maps.arcgis.com/home/item.html?id=6b6588041965408e84ba319e12d9d7ad";
        public BookmarksSceneSample()
        {
            InitializeComponent();
            MySceneView.Scene = new Scene(new Uri(_sceneUrl));
            configureSlides();
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
    }
}
