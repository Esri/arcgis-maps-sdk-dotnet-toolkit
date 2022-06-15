using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UtilityNetworks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.UtilityNetworkTraceTool
{
    public partial class UNTraceSimple : UserControl
    {
        private const string WebmapURL = "https://www.arcgis.com/home/item.html?id=471eb0bf37074b1fbb972b1da70fb310";
        private readonly Tuple<string, string, string> _portal1Login = new Tuple<string, string, string>("https://sampleserver7.arcgisonline.com/portal/sharing/rest", "username", "password");

        public UNTraceSimple()
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

                MyMapView.Map = new Map(new Uri(WebmapURL));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initializing sample failed: {ex.Message}", ex.GetType().Name);
            }
        }
    }
}