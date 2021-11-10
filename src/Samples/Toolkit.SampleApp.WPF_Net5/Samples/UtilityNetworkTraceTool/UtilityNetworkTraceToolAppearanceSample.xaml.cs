using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.UtilityNetworks;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.UtilityNetworkTraceTool
{
    [SampleInfoAttribute(Category = "UtilityNetworkTraceTool", DisplayName = "UtilityNetworkTraceTool - Appearance", Description = "Sample showing customization options related to appearance")]
    public partial class UtilityNetworkTraceToolAppearanceSample : UserControl
    {
        private const string WebmapURL = "https://rt-server109.esri.com/portal/home/item.html?id=54fa9aadf6c645d39f006cf279147204";
        private readonly Tuple<string, string, string> _portal1Login = new Tuple<string, string, string>("https://rt-server109.esri.com/portal/sharing/rest", "publisher1", "test.publisher01");
        private const string FeatureServerURL = "https://sampleserver7.arcgisonline.com/server/rest/services/UtilityNetwork/NapervilleElectric/FeatureServer";
        private readonly Tuple<string, string, string> _portal3Login = new Tuple<string, string, string>("https://sampleserver7.arcgisonline.com/portal/sharing/rest", "viewer01", "I68VGU^nMurF");

        public UtilityNetworkTraceToolAppearanceSample()
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

                var portal3Credential = await AuthenticationManager.Current.GenerateCredentialAsync(new Uri(_portal3Login.Item1), _portal3Login.Item2, _portal3Login.Item3);
                AuthenticationManager.Current.AddCredential(portal3Credential);

                var map = new Map(new Uri(WebmapURL));
                await map.LoadAsync();
                map.UtilityNetworks.Add(new UtilityNetwork(new Uri(FeatureServerURL)));
                foreach (var un in map.UtilityNetworks)
                    await un.LoadAsync();

                MyMapView.Map = map;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initializing sample failed: {ex.Message}", ex.GetType().Name);
            }
        }


        private void OnUtilityNetworkItemTemplateChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.Resources["UN_ItemTemplate"] is DataTemplate dataTemplate)
            {
                if (UtilityNetworkTraceTool.UtilityNetworkItemTemplate != dataTemplate)
                {
                    UtilityNetworkTraceTool.UtilityNetworkItemTemplate = dataTemplate;
                }
                else
                {
                    UtilityNetworkTraceTool.UtilityNetworkItemTemplate = null;
                }
            }
        }

        private void OnUtilityNetworkItemContainerStyleChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.Resources["ComboBoxItemStyle1"] is Style comboBoxItemStyle)
            {
                if (UtilityNetworkTraceTool.UtilityNetworkItemContainerStyle != comboBoxItemStyle)
                {
                    UtilityNetworkTraceTool.UtilityNetworkItemContainerStyle = comboBoxItemStyle;
                }
                else
                {
                    UtilityNetworkTraceTool.UtilityNetworkItemContainerStyle = null;
                }
            }
        }

        private void OnTraceTypeItemTemplateChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.Resources["TT_ItemTemplate"] is DataTemplate dataTemplate)
            {
                if (UtilityNetworkTraceTool.TraceTypeItemTemplate != dataTemplate)
                {
                    UtilityNetworkTraceTool.TraceTypeItemTemplate = dataTemplate;
                }
                else
                {
                    UtilityNetworkTraceTool.TraceTypeItemTemplate = null;
                }
            }
        }

        private void OnTraceTypeItemContainerStyleChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.Resources["ComboBoxItemStyle1"] is Style comboBoxItemStyle)
            {
                if (UtilityNetworkTraceTool.TraceTypeItemContainerStyle != comboBoxItemStyle)
                {
                    UtilityNetworkTraceTool.TraceTypeItemContainerStyle = comboBoxItemStyle;
                }
                else
                {
                    UtilityNetworkTraceTool.TraceTypeItemContainerStyle = null;
                }
            }
        }

        private void OnStartingPointItemTemplateChanged(object sender, System.Windows.RoutedEventArgs e)
        {

            if (this.Resources["SP_ItemTemplate"] is DataTemplate dataTemplate)
            {
                if (UtilityNetworkTraceTool.StartingPointItemTemplate != dataTemplate)
                {
                    UtilityNetworkTraceTool.StartingPointItemTemplate = dataTemplate;
                }
                else
                {
                    UtilityNetworkTraceTool.StartingPointItemTemplate = null;
                }
            }
        }

        private void OnStartingPointItemContainerStyleChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.Resources["ListViewItemStyle1"] is Style listViewItemStyle)
            {
                if (UtilityNetworkTraceTool.StartingPointItemContainerStyle != listViewItemStyle)
                {
                    UtilityNetworkTraceTool.StartingPointItemContainerStyle = listViewItemStyle;
                }
                else
                {
                    UtilityNetworkTraceTool.StartingPointItemContainerStyle = null;
                }
            }
        }

        private void OnResultItemTemplateChanged(object sender, System.Windows.RoutedEventArgs e)
        {

            if (this.Resources["R_ItemTemplate"] is DataTemplate dataTemplate)
            {
                if (UtilityNetworkTraceTool.ResultItemTemplate != dataTemplate)
                {
                    UtilityNetworkTraceTool.ResultItemTemplate = dataTemplate;
                }
                else
                {
                    UtilityNetworkTraceTool.ResultItemTemplate = null;
                }
            }
        }

        private void OnResultItemContainerStyleChanged(object sender, System.Windows.RoutedEventArgs e)
        { 
            if (this.Resources["ListViewItemStyle1"] is Style listViewItemStyle)
            {
                if (UtilityNetworkTraceTool.ResultItemContainerStyle != listViewItemStyle)
                {
                    UtilityNetworkTraceTool.ResultItemContainerStyle = listViewItemStyle;
                }
                else
                {
                    UtilityNetworkTraceTool.ResultItemContainerStyle = null;
                }
            }
        }

        private void OnAddStartingPointToggleButtonStyleChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.Resources["ToggleButtonStyle1"] is Style toggleButtonStyle)
            {
                if (UtilityNetworkTraceTool.AddStartingPointToggleButtonStyle != toggleButtonStyle)
                {
                    UtilityNetworkTraceTool.AddStartingPointToggleButtonStyle = toggleButtonStyle;
                }
                else
                {
                    UtilityNetworkTraceTool.AddStartingPointToggleButtonStyle = null;
                }
            }
        }

        private void OnTraceButtonStyleChanged(object sender, System.Windows.RoutedEventArgs e)
        {
                        if (this.Resources["ButtonStyle1"] is Style buttonStyle)
            {
                if (UtilityNetworkTraceTool.TraceButtonStyle != buttonStyle)
                {
                    UtilityNetworkTraceTool.TraceButtonStyle = buttonStyle;
                }
                else
                {
                    UtilityNetworkTraceTool.TraceButtonStyle = null;
                }
            }
        }

        private void OnResetButtonStyleChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.Resources["ButtonStyle1"] is Style buttonStyle)
            {
                if (UtilityNetworkTraceTool.ResetButtonStyle != buttonStyle)
                {
                    UtilityNetworkTraceTool.ResetButtonStyle = buttonStyle;
                }
                else
                {
                    UtilityNetworkTraceTool.ResetButtonStyle = null;
                }
            }

        }

        private void OnBusyProgressBarStyleChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.Resources["ProgressBarStyle1"] is Style progressBarStyle)
            {
                if (UtilityNetworkTraceTool.BusyProgressBarStyle != progressBarStyle)
                {
                    UtilityNetworkTraceTool.BusyProgressBarStyle = progressBarStyle;
                }
                else
                {
                    UtilityNetworkTraceTool.BusyProgressBarStyle = null;
                }
            }

        }

        private void OnStatusTextBlockStyleChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.Resources["TextBlockStyle1"] is Style textBlockStyle)
            {
                if (UtilityNetworkTraceTool.StatusTextBlockStyle != textBlockStyle)
                {
                    UtilityNetworkTraceTool.StatusTextBlockStyle = textBlockStyle;
                }
                else
                {
                    UtilityNetworkTraceTool.StatusTextBlockStyle = null;
                }
            }
        }
    }
}
