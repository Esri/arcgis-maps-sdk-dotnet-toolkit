using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Toolkit.Samples.Forms.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "UtilityNetworkTraceTool", Description = "UtilityNetworkTraceTool")]
    public partial class UtilityNetworkTraceToolSample : ContentPage
    {
        private const string WebmapURL = "https://www.arcgis.com/home/item.html?id=471eb0bf37074b1fbb972b1da70fb310";

        public UtilityNetworkTraceToolSample()
        {
            InitializeComponent();
            Initialize();

        }

        private async void Initialize()
        {
            try
            {
                // Using public credentials from https://developers.arcgis.com/javascript/latest/sample-code/widgets-untrace/
                var portal1Credential = await AuthenticationManager.Current.GenerateCredentialAsync(new Uri("https://sampleserver7.arcgisonline.com/portal/sharing/rest"), "viewer01", "I68VGU^nMurF");
                AuthenticationManager.Current.AddCredential(portal1Credential);

                MyMapView.Map = new Map(new Uri(WebmapURL));
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Initializing sample failed: {ex.Message}", ex.GetType().Name);
            }
        }
    }
}