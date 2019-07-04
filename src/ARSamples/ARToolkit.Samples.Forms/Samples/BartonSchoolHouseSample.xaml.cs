using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ARToolkit.SampleApp.Forms.Samples
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleData(ItemId = "b30f53d65c714054b75a0eb16639529a", Path = "BartonSchoolHouse_3d_mesh.slpk")]
    public partial class BartonSchoolHouseSample : ContentPage
	{
		public BartonSchoolHouseSample()
		{
			InitializeComponent ();
		}
        private Scene Scene;

        private async void Init()
        {
            try
            {
                Scene = await ARTestScenes.CreateBartonSchoolHouse(ARView);
                ARView.Scene = Scene;
                ARView.StartTracking();
            }
            catch (System.Exception ex)
            {
                await DisplayAlert("Failed to load scene", ex.Message, "OK");
                await Navigation.PopAsync();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Init();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ARView.StopTracking();
        }
    }
}