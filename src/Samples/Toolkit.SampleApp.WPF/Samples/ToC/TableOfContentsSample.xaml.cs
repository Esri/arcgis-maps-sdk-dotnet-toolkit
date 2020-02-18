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

        private void toc_LayerContentContextMenuOpening(object sender, Preview.UI.Controls.TableOfContentsContextMenuEventArgs args)
        {
            if (args.TableOfContentItem is Mapping.Basemap)
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
            if (args.TableOfContentItem is Mapping.Layer layer)
            {
                if (layer.LoadStatus == LoadStatus.FailedToLoad)
                {
                    var retry = new MenuItem() { Header = "Retry load" };
                    retry.Click += (s, e) => layer.RetryLoadAsync();
                    args.MenuItems.Add(retry);
                    return;
                }
                if(layer.FullExtent != null)
                {
                    var zoomTo = new MenuItem() { Header = "Zoom To" };
                    zoomTo.Click += (s, e) => mapView.SetViewpointGeometryAsync(layer.FullExtent);
                    args.MenuItems.Add(zoomTo);
                }
                if (args.MenuItems.Count > 0)
                    args.MenuItems.Add(new Separator());
                var remove = new MenuItem() { Header = "Remove" };
                remove.Click += (s, e) =>
                {
                    var result = MessageBox.Show("Remove layer " + layer.Name + " ?", "Confirm", MessageBoxButton.OKCancel);
                    if(result == MessageBoxResult.OK)
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
