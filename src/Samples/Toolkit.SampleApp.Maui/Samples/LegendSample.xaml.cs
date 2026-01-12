using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "Legend", Description = "Legend with MapView sample")]
    public partial class LegendSample : ContentPage
    {
        public LegendSample()
        {
            InitializeComponent();
            mapView.Map = CreateMap();
        }

        private Map CreateMap()
        {
            return new Map(new Uri("http://www.arcgis.com/home/webmap/viewer.html?webmap=df8bcc10430f48878b01c96e907a1fc3"));
        }

        private void Grid_SizeChanged(object? sender, EventArgs e)
        {
            // Place legend on left size when view is wide, otherwise below mapview
            if (sender is Grid sg && sg.Width > sg.Height)
            {
                Grid.SetColumnSpan(mapView, 1);
                Grid.SetColumnSpan(legend, 1);
                Grid.SetColumn(mapView, 1);
                Grid.SetRow(legend, 0);
                Grid.SetRowSpan(mapView, 2);
                Grid.SetRowSpan(legend, 2);
                if (((Grid)sender).Width > 600)
                    legend.WidthRequest = 300;
                else
                    legend.ClearValue(WidthRequestProperty);
            }
            else
            {
                legend.ClearValue(WidthRequestProperty);
                Grid.SetColumnSpan(mapView, 2);
                Grid.SetColumnSpan(legend, 2);
                Grid.SetColumn(mapView, 0);
                Grid.SetRow(legend, 1);
                Grid.SetRowSpan(mapView, 1);
                Grid.SetRowSpan(legend, 1);
            }
        }
    }
}