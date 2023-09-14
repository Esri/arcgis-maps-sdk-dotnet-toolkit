using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.Security;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "PopupViewer", Description = "Use PopupViewer to display detailed feature information")]
    public partial class PopupViewerSample : ContentPage
    {
        Dictionary<string, string> testMaps = new Dictionary<string, string>() {
            {"Old sample", "https://www.arcgis.com/home/item.html?id=d4fe39d300c24672b1821fa8450b6ae2" },
            {"Public sample", "https://www.arcgis.com/home/item.html?id=9f3a674e998f461580006e626611f9ad" },
            { "4 types: text, media image, media chart, fields list", "https://www.arcgis.com/home/item.html?id=67c72e385e6e46bc813e0b378696aba8" },
            { "Display popup with expression elements defining media", "https://www.arcgis.com/home/item.html?id=34752f1d149f4b2db96f7a1637767173" },
            { "Display popup with multiple fields elements", "https://www.arcgis.com/home/item.html?id=8d75d1dbdb5c4ad5849abb26b783987e" },
            { "Recreation Map with Attachments", "https://www.arcgis.com/home/item.html?id=2afef81236db4eabbbae357e4f990039" },
            { "Recreation Map with Attachments - New", "https://www.arcgis.com/home/item.html?id=79c995874bea47d08aab5a2c85120e7f" },
            { "Attachments", "https://www.arcgis.com/home/item.html?id=9e3baeb5dcd4473aa13e0065d7794ca6" },
            { "Relationship1", "https://pulsars.mapsdevext.arcgis.com/home/item.html?id=10d6138cd1a1419d8053ae7d4aa3ffa3"},
            { "Relationship2", "https://testrail.mapsdevext.arcgis.com/home/item.html?id=f81406c259be4a4ba3bbb09948a1f079"},
            { "Text: Road signs", "https://www.arcgis.com/home/webmap/viewer.html?webmap=bfc83d51e66d4acdb6a403aca2539bf5" },
            { "Text: Mountains", "https://www.arcgis.com/home/item.html?id=9f3a674e998f461580006e626611f9ad" },
            { "Text: Wells", "https://www.arcgis.com/apps/mapviewer/index.html?webmap=2d355bc8cea34423b089776cb7518480" },
            { "Text: Commute alternatives", "https://www.arcgis.com/apps/mapviewer/index.html?webmap=cabbd03603ce405bbacac205034b7143" },
            { "Text: Botanical Garden trees", "https://www.arcgis.com/apps/mapviewer/index.html?webmap=ed3bdc9525aa4386813d0e675d7ac8a1" },
            { "Text: Smoky Mountain peaks and trails", "https://www.arcgis.com/apps/mapviewer/index.html?webmap=bcecad5bc4a5456bada1cd5e230bbf7f" },
            { "Text: Quakes", "https://www.arcgis.com/apps/mapviewer/index.html?webmap=430d86ab8cef40c68d353c0aa81dc743" },
            { "Text: Disability", "https://jsapi.maps.arcgis.com/apps/mapviewer/index.html?webmap=5bdf129e171f4684a6c0cbc7897877b4" },
            { "Text: ImageExample", "https://prodtesting.maps.arcgis.com/apps/mapviewer/index.html?webmap=47fbaba77563425ab7d2516d085baad2" },
            { "Text: COVID case trends", "https://www.arcgis.com/apps/mapviewer/index.html?webmap=c6ba9c02006948b9bdc1d66436fa5d29" },
            { "Text: Volcanos", "https://www.arcgis.com/apps/mapviewer/index.html?webmap=b08f94d39cec4fd0948734de8b7ca923" },
            { "Text: Stream Gauges", "https://maps.arcgis.com/apps/mapviewer/index.html?webmap=7b685771fedc490db6905d4ed1cafb0d" },
            { "Text: Wildfires", "https://maps.arcgis.com/apps/mapviewer/index.html?webmap=df8bcc10430f48878b01c96e907a1fc3" },
            {"Text: Current Weather", "https://runtime.maps.arcgis.com/apps/mapviewer/index.html?webmap=b76dec26742c48768ec152dd6db05962" },
            {"Text: Unemployment", "https://runtime.maps.arcgis.com/apps/mapviewer/index.html?webmap=45e6bb0c0960449cb29031f590af9a59" },
            {"Text: Canada Trails", "https://runtime.maps.arcgis.com/apps/mapviewer/index.html?webmap=e46c737f1f1341df9338f1bf17540f80" },
            {"Text: Social Vulnerability Index", "https://runtime.maps.arcgis.com/apps/mapviewer/index.html?webmap=2c8fdc6267e4439e968837020e7618f3" },
            {"Text: Physical Health", "https://runtime.maps.arcgis.com/apps/mapviewer/index.html?webmap=cd7a9db319784bc6afb39cfbf7762fd0" },
            {"Text: France Election Results", "https://runtime.maps.arcgis.com/apps/mapviewer/index.html?webmap=582da975ca9a4c5584646461b923fe8a" },
            {"Text: Energy Labels", "https://runtime.maps.arcgis.com/apps/mapviewer/index.html?webmap=22f951f0951f4922b800021b0ba98539" },
            {"Text: Gezondheidsmonitor", "https://runtime.maps.arcgis.com/apps/mapviewer/index.html?webmap=1865c129def4409c947d8407ec30092d" },
            {"Text: NL Geology", "https://runtime.maps.arcgis.com/apps/mapviewer/index.html?webmap=ee1d6acb82fe4f1d9b2442ff050015a4" },
            {"Text: NL vaccination", "https://runtime.maps.arcgis.com/apps/mapviewer/index.html?webmap=0a082752c7e548db8c71b1d9eafb3ecb" },
            {"NEW: Wind Forecast", "https://mstefarov.maps.arcgis.com/apps/mapviewer/index.html?webmap=75dd0107381b48579281fe4245de361c" },
            {"NEW: AQI (many divs)", "https://mstefarov.maps.arcgis.com/apps/mapviewer/index.html?webmap=818e099f8a25446385fb601dac6f5be2" },
            {"NEW: Wind Speed Forecast (align)", "https://mstefarov.maps.arcgis.com/apps/mapviewer/index.html?webmap=a7b007939f02406ca2b8559a821c08ab" },
            {"NEW: OSM NL", "https://mstefarov.maps.arcgis.com/apps/mapviewer/index.html?webmap=1db8173189e34ad28a0cba0f2e58287e" },
        };

        public PopupViewerSample()
        {
            InitializeComponent();
            mapPicker.ItemsSource = testMaps.Keys.ToList();
            mapPicker.SelectedIndexChanged += MapPicker_SelectedIndexChanged;

            mapView.Map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("https://www.arcgis.com/home/item.html?id=9f3a674e998f461580006e626611f9ad"));
            mapView.GeoViewTapped += mapView_GeoViewTapped;
        }

        private async void MapPicker_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var uri = testMaps[(string)mapPicker.SelectedItem];
            mapView.Map = new Map(new Uri(uri));
        }

        private async void mapView_GeoViewTapped(object? sender, GeoViewInputEventArgs e)
        {
            Exception? error = null;
            try
            {
                var result = await mapView.IdentifyLayersAsync(e.Position, 3, false);

                // Retrieves or builds Popup from IdentifyLayerResult
                var popup = GetPopup(result);

                if (popup != null)
                {
                    popupViewer.Popup = popup;
                    popupPanel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                error = ex;

            }
            if (error != null)
                await DisplayAlert(error.GetType().Name, error.Message, "OK");
        }

        private Popup GetPopup(IdentifyLayerResult result)
        {
            if (result == null)
            {
                return null;
            }

            var popup = result.Popups.FirstOrDefault();
            if (popup != null)
            {
                return popup;
            }

            var geoElement = result.GeoElements.FirstOrDefault();
            if (geoElement != null)
            {
                if (result.LayerContent is IPopupSource)
                {
                    var popupDefinition = ((IPopupSource)result.LayerContent).PopupDefinition;
                    if (popupDefinition != null)
                    {
                        return new Popup(geoElement, popupDefinition);
                    }
                }

                return Popup.FromGeoElement(geoElement);
            }

            return null;
        }

        private Popup GetPopup(IEnumerable<IdentifyLayerResult> results)
        {
            if (results == null)
            {
                return null;
            }
            foreach (var result in results)
            {
                var popup = GetPopup(result);
                if (popup != null)
                {
                    return popup;
                }

                foreach (var subResult in result.SublayerResults)
                {
                    popup = GetPopup(subResult);
                    if (popup != null)
                    {
                        return popup;
                    }
                }
            }

            return null;
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            popupPanel.IsVisible = false;
        }

        private void popupViewer_PopupAttachmentClicked(object sender, Esri.ArcGISRuntime.Toolkit.Maui.PopupAttachmentClickedEventArgs e)
        {
            e.Handled = true; // Prevent default launch action
            // Share file:
            // _ = Share.Default.RequestAsync(new ShareFileRequest(new ReadOnlyFile(e.Attachment.Filename!, e.Attachment.ContentType)));

            // Open default file handler
            _ = Microsoft.Maui.ApplicationModel.Launcher.Default.OpenAsync(
                 new Microsoft.Maui.ApplicationModel.OpenFileRequest(e.Attachment.Name, new ReadOnlyFile(e.Attachment.Filename!, e.Attachment.ContentType)));
        }
    }
}