using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfo(Category = "UtilityNetworkTraceTool", Description = "Use named trace configurations defined in a web map to perform connected trace operations and compare results.")]
    public partial class UtilityNetworkTraceToolSample : ContentPage
    {
        private const string WebmapURL = "https://www.arcgis.com/home/item.html?id=471eb0bf37074b1fbb972b1da70fb310";

        public UtilityNetworkTraceToolSample()
        {
            InitializeComponent();

            MyTraceTool.UtilityNetworkChanged += MyTraceTool_UtilityNetworkChanged;
            MyTraceTool.UtilityNetworkTraceCompleted += MyTraceTool_UtilityNetworkTraceCompleted;

            Initialize();
        }

        private void MyTraceTool_UtilityNetworkTraceCompleted(object? sender, Esri.ArcGISRuntime.Toolkit.Maui.UtilityNetworkTraceCompletedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Trace completed {e}");
        }

        private void MyTraceTool_UtilityNetworkChanged(object? sender, Esri.ArcGISRuntime.Toolkit.Maui.UtilityNetworkChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Network changed. New selection: {e.UtilityNetwork?.Name}");
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
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}