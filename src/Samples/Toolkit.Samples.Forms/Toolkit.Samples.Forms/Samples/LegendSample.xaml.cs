using Esri.ArcGISRuntime.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Toolkit.Samples.Forms.Samples
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "Legend", Description = "Render a legend for a map")]
    public partial class LegendSample : ContentPage
	{
		public LegendSample ()
		{
			InitializeComponent ();
            mapView.Map = new Esri.ArcGISRuntime.Mapping.Map(new Uri("http://www.arcgis.com/home/webmap/viewer.html?webmap=f1ed0d220d6447a586203675ed5ac213"));
        }
    }
}