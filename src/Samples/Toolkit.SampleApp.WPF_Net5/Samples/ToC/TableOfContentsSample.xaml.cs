using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
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

namespace Esri.ArcGISRuntime.Toolkit.Samples.ToC
{
    public partial class TableOfContentsSample : UserControl
    {
        public TableOfContentsSample()
        {
            InitializeComponent();
            Map map = new Map(Basemap.CreateLightGrayCanvasVector())
            {
                InitialViewpoint = new Viewpoint(new Envelope(-178, 17.8, -65, 71.4, SpatialReference.Create(4269)))
            };

            map.OperationalLayers.Add(new ArcGISMapImageLayer(new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/Census/MapServer")));
            map.OperationalLayers.Add(new FeatureLayer(new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/SF311/FeatureServer/0")));

            mapView.Map = map;
        }

        private void toc_LayerContentContextMenuOpening(object sender, Preview.UI.Controls.TocItemContextMenuEventArgs args)
        {
            var tocItem = args.Item;
            if (tocItem.Content is Mapping.Basemap)
            {
                var item = new MenuItem() { Header = "Imagery" };
                item.Click += (s, e) => mapView.Map.Basemap = Basemap.CreateImagery();
                args.MenuItems.Add(item);

                item = new MenuItem() { Header = "Streets" };
                item.Click += (s, e) => mapView.Map.Basemap = Basemap.CreateStreetsVector();
                args.MenuItems.Add(item);

                item = new MenuItem() { Header = "OpenStreetMap" };
                item.Click += (s, e) => mapView.Map.Basemap = Basemap.CreateOpenStreetMap();
                args.MenuItems.Add(item);

            }
            if (tocItem.Content is ILoadable loadable && loadable.LoadStatus == LoadStatus.FailedToLoad)
            {
                var retry = new MenuItem() { Header = "Retry load", Icon = new TextBlock() { Text = "", FontFamily = new FontFamily("Segoe UI Symbol") } };
                retry.Click += (s, e) => loadable.RetryLoadAsync();
                args.MenuItems.Add(retry);
                return;
            }
            if(tocItem.Content is FeatureLayer fl)
            {
                var resetRenderer = new MenuItem() { Header = "Reset Renderer" };
                resetRenderer.Click += (s, e) => fl.Renderer = new Symbology.SimpleRenderer(new Symbology.SimpleMarkerSymbol() { Color = System.Drawing.Color.Red });
                args.MenuItems.Add(resetRenderer);
            }

            if (!(tocItem.Content is LegendInfo))
            {
                Func<Task<Envelope>> getExtent = null;
                if (tocItem.Layer?.FullExtent != null)
                {
                    getExtent = () => Task.FromResult(tocItem.Layer?.FullExtent);
                }
                if(tocItem.Content is ArcGISSublayer sublayer)
                {
                    if (sublayer.LoadStatus == LoadStatus.NotLoaded || sublayer.LoadStatus == LoadStatus.Loading)
                    {
                        getExtent = async () =>
                        {
                            try
                            {
                                await sublayer.LoadAsync();
                            }
                            catch { return null; }
                            return sublayer.MapServiceSublayerInfo?.Extent;
                        };
                    }
                    else if(sublayer.MapServiceSublayerInfo?.Extent != null)
                    {
                        getExtent = () => Task.FromResult(sublayer.MapServiceSublayerInfo.Extent);
                    }
                }
                if (getExtent != null)
                {
                    var zoomTo = new MenuItem() { Header = "Zoom To", Icon = new TextBlock() { Text = "", FontFamily = new FontFamily("Segoe UI Symbol") } };
                    zoomTo.Click += async (s, e) =>
                    {
                        var extent = await getExtent();
                        if (extent != null)
                            _ = mapView.SetViewpointGeometryAsync(extent);
                    };
                    args.MenuItems.Add(zoomTo);
                }
            }

            if (args.MenuItems.Count > 0)
                args.MenuItems.Add(new Separator());
            if (tocItem.Content is Layer layer)
            {
                var remove = new MenuItem() { Header = "Remove", Icon = new TextBlock() { Text = "", FontFamily = new FontFamily("Segoe UI Symbol") } };
                remove.Click += (s, e) =>
                {
                    var result = MessageBox.Show("Remove layer " + layer.Name + " ?", "Confirm", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        if (mapView.Map.OperationalLayers.Contains(layer))
                            mapView.Map.OperationalLayers.Remove(layer);
                    }
                };
                args.MenuItems.Add(remove);
            }
        }
    }
}
