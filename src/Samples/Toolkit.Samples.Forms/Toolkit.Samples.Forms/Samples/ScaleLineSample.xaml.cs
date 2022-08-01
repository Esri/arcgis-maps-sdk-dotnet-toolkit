using Esri.ArcGISRuntime.Mapping;
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
    [SampleInfoAttribute(Category = "ScaleLine", Description = "Demonstrates ScaleLine.")]
    public partial class ScaleLineSample : ContentPage
	{
		public ScaleLineSample ()
		{
			InitializeComponent ();
            mapView.Map = new Map(BasemapStyle.ArcGISLightGray);
        }
	}
}