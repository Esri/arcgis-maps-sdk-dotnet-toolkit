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

namespace Esri.ArcGISRuntime.Toolkit.TestApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			DataContext = SampleDatasource.Current;
		}


		private void SampleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var pagetype = ((sender as ListBox).SelectedItem as Sample).Page;
			string url = "Samples/" + pagetype.Name + ".xaml";
			MainFrame.Navigate(new Uri(url, UriKind.RelativeOrAbsolute));
			ObjectTracker.GarbageCollect();
		}
	}
}
