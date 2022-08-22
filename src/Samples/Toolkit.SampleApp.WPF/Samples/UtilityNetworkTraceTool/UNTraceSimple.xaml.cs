﻿using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.UtilityNetworkTraceTool
{
    public partial class UNTraceSimple : UserControl
    {
        private const string WebmapURL = "https://www.arcgis.com/home/item.html?id=471eb0bf37074b1fbb972b1da70fb310";

        public UNTraceSimple()
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
                MessageBox.Show($"Initializing sample failed: {ex.Message}", ex.GetType().Name);
            }
        }
    }
}