using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Samples
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.Initialize();
            InitializeComponent();

            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = "";

            if (string.IsNullOrWhiteSpace(Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey))
            {
                MessageBox.Show("Please edit MainWindow.xaml.cs to supply an API key");
                System.Environment.Exit(1);
            }

            // Configure this app to use the Toolkit's Authentication Challenge Handler
            Esri.ArcGISRuntime.Security.AuthenticationManager.Current.ChallengeHandler =
                new Esri.ArcGISRuntime.Toolkit.Preview.Authentication.ChallengeHandler(this.Dispatcher);
            LoadSamples();

        }

        private void LoadSamples()
        {
            var samples = SampleDatasource.Current.Samples;
            Dictionary<string, MenuItem> rootMenus = new Dictionary<string, MenuItem>();
            MenuItem samplesItem = new MenuItem() { Header = "Samples" };
            foreach (var sample in samples.OrderBy(s=>s.Category))
            {
                MenuItem sampleitem = new MenuItem() { Header = sample.Name, Tag = sample, ToolTip = sample.Description };
                sampleitem.Click += (s, e) => { sampleitem_Click(sample, s as MenuItem); };
                MenuItem root = samplesItem;
                if (sample.Category != null)
                {
                    if (!rootMenus.ContainsKey(sample.Category))
                    {
                        rootMenus[sample.Category] = new MenuItem() { Header = sample.Category };
                        menu.Items.Add(rootMenus[sample.Category]);
                    }
                    root = rootMenus[sample.Category];
                }
                root.Items.Add(sampleitem);
            }
            if (samplesItem.Items.Count > 0)
                menu.Items.Insert(0, samplesItem);

        }

        private MenuItem currentSampleMenuItem;
        private void sampleitem_Click(Sample sample, MenuItem menu)
        {
            var c = sample.Page.GetConstructor(new Type[] { });
            var ctrl = c.Invoke(new object[] { }) as UIElement;
            SampleContainer.Child = ctrl;
            if (currentSampleMenuItem != null)
                currentSampleMenuItem.IsChecked = false;
            menu.IsChecked = true;
            currentSampleMenuItem = menu;
        }
    }
}
