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
    [SampleInfoAttribute(Category = "OverviewMap", Description = "OverviewMap with map sample")]
    public partial class OverviewMapSample : ContentPage
	{
		public OverviewMapSample()
		{
			InitializeComponent();
            mapView.Map = new Map(BasemapStyle.ArcGISImagery);
		}

        private void Slider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            mapView.SetViewpointRotationAsync(e.NewValue);
        }
    }
}