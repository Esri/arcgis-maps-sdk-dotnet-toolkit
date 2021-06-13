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
    [SampleInfoAttribute(Category = "OverviewMap", Description = "OverviewMap connected to a scene sample")]
    public partial class OverviewMapWithSceneSample: ContentPage
	{
		public OverviewMapWithSceneSample()
		{
			InitializeComponent();
            sceneView.Scene = new Scene(BasemapStyle.ArcGISImageryStandard);
		}
    }
}