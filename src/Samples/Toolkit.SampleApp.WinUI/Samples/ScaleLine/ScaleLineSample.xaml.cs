using Esri.ArcGISRuntime.Mapping;
using System;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.ScaleLine
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ScaleLineSample : Page
    {
        public ScaleLineSample()
        {
            this.InitializeComponent();
        }

        public Map Map { get; } = new Map(new Uri("http://www.arcgis.com/home/webmap/viewer.html?webmap=c50de463235e4161b206d000587af18b"));
    }
}
