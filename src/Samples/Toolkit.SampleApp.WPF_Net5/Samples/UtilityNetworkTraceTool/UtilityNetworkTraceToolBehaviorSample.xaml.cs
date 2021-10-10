using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.UtilityNetworkTraceTool
{
    [SampleInfoAttribute(Category = "UtilityNetworkTraceTool", DisplayName = "UtilityNetworkTraceTool - Behavior", Description = "Sample showing behaviors")]
    public partial class UtilityNetworkTraceToolBehaviorSample : UserControl
    {
        private const string Portal1Item1 = "https://rt-server109.esri.com/portal/home/item.html?id=54fa9aadf6c645d39f006cf279147204";
        private readonly Tuple<string, string, string> _portal1Login = new Tuple<string, string, string>("https://rt-server109.esri.com/portal/sharing/rest", "publisher1", "test.publisher01");

        public UtilityNetworkTraceToolBehaviorSample()
        {
            InitializeComponent();
            Initialize();
        }

        private async void Initialize()
        {
            try
            {
                var portal1Credential = await AuthenticationManager.Current.GenerateCredentialAsync(new Uri(_portal1Login.Item1), _portal1Login.Item2, _portal1Login.Item3);
                AuthenticationManager.Current.AddCredential(portal1Credential);

                MyMapView.Map = new Map(new Uri(Portal1Item1));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initializing sample failed: {ex.Message}", ex.GetType().Name);
            }
        }
    }
}
